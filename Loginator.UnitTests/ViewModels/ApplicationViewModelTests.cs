using Backend.Model;
using FluentAssertions;
using Loginator.Collections;
using Loginator.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Loginator.UnitTests.ViewModels {

    /// <summary>
    /// Represents unit tests for <see cref="ApplicationViewModel"/>.
    /// </summary>
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public class ApplicationViewModelTests {

        private const string APP_NAME = "TestApp";
        private const string NAMESPACE_NAME = "TestNs";

        private ApplicationViewModel sut;
        private NamespaceViewModel namespaceApp;
        private readonly ObservableCollection<NamespaceViewModel> namespaces = [];
        private readonly OrderedObservableCollection logs = [];

        private readonly IEnumerable<LogViewModel> testItems;

        public ApplicationViewModelTests() {
            DateTime ts = DateTime.Now;
            var itemV = LogVM(LoggingLevel.TRACE, ts.AddMinutes(1));
            var itemD = LogVM(LoggingLevel.DEBUG, ts.AddMinutes(2));
            var itemI = LogVM(LoggingLevel.INFO, ts.AddMinutes(3));
            var itemW = LogVM(LoggingLevel.WARN, ts.AddMinutes(4));
            var itemE = LogVM(LoggingLevel.ERROR, ts.AddMinutes(5));
            var itemF = LogVM(LoggingLevel.FATAL, ts.AddMinutes(6));

            testItems = [itemF, itemE, itemW, itemI, itemD, itemV];
        }

        [SetUp]
        public void Setup() {
            sut = new ApplicationViewModel(APP_NAME, logs, namespaces, LoggingLevel.NOT_SET);
            namespaceApp = new NamespaceViewModel(APP_NAME, sut);
            var ns = new NamespaceViewModel(NAMESPACE_NAME, sut) { Parent = namespaceApp };
            namespaceApp.Children.Add(ns);
        }

        [Test]
        public void Can_create_sut() {
            var expectedAppName = "TestApp(101)";
            var expectedLogLevels = LoggingLevel.GetAllLogLevels().Order();
            var expectedLogLevel = LoggingLevel.INFO;
            var sut = new ApplicationViewModel(expectedAppName, logs, namespaces, expectedLogLevel);

            sut.Name.Should().Be(expectedAppName);
            sut.LogLevels.Should().BeEquivalentTo(expectedLogLevels);
            sut.SelectedMinLogLevel.Should().Be(expectedLogLevel);
            logs.Should().BeEmpty();
            namespaces.Should().BeEmpty();
        }

        [TestCaseSource(typeof(ApplicationViewModelTestData), nameof(ApplicationViewModelTestData.AllLogLevels))]
        public void Cannot_show_logs_without_namespace(LoggingLevel level) {
            sut.SelectedMinLogLevel = level;

            AddItemsToSut();

            logs.Should().BeEmpty();
        }

        [TestCaseSource(typeof(ApplicationViewModelTestData), nameof(ApplicationViewModelTestData.ValidLogLevels))]
        public void Can_show_logs_for_present_namespace(LoggingLevel level) {
            AssertOrderNamespaceLevelItems(level);
        }

        [TestCase]
        public void Cannot_show_logs_for_present_namespace_if_level_is_invalid() {
            AssertOrderNamespaceLevelItems(LoggingLevel.NOT_SET);
        }

        [TestCaseSource(typeof(ApplicationViewModelTestData), nameof(ApplicationViewModelTestData.ValidLogLevels))]
        public void Can_show_logs_for_updated_namespace(LoggingLevel level) {
            AssertOrderLevelItemsNamespace(level);
        }

        [TestCase]
        public void Cannot_show_logs_for_updated_namespace_if_level_is_invalid() {
            AssertOrderLevelItemsNamespace(LoggingLevel.NOT_SET);
        }

        [TestCase]
        public void Can_show_logs_from_updated_level() {
            AddItemsToSut(setNamespaceFirst: true);

            AssertOrderNamespaceItemsLevel(LoggingLevel.NOT_SET);
            AssertOrderNamespaceItemsLevel(LoggingLevel.TRACE);
            AssertOrderNamespaceItemsLevel(LoggingLevel.DEBUG);
            AssertOrderNamespaceItemsLevel(LoggingLevel.INFO);
            AssertOrderNamespaceItemsLevel(LoggingLevel.WARN);
            AssertOrderNamespaceItemsLevel(LoggingLevel.ERROR);
            AssertOrderNamespaceItemsLevel(LoggingLevel.FATAL);
            AssertOrderNamespaceItemsLevel(LoggingLevel.NOT_SET);
            AssertOrderNamespaceItemsLevel(LoggingLevel.ERROR);
            AssertOrderNamespaceItemsLevel(LoggingLevel.WARN);
            AssertOrderNamespaceItemsLevel(LoggingLevel.INFO);
            AssertOrderNamespaceItemsLevel(LoggingLevel.DEBUG);
            AssertOrderNamespaceItemsLevel(LoggingLevel.TRACE);
            AssertOrderNamespaceItemsLevel(LoggingLevel.NOT_SET);
        }

        [TestCaseSource(typeof(ApplicationViewModelTestData), nameof(ApplicationViewModelTestData.AllLogLevels))]
        public void Can_hide_logs_for_inactive_application(LoggingLevel level) {
            AddItemsToSut(setNamespaceFirst: true);

            var expectedItems = GetExpectedItemsFromLevel(level);
            sut.SelectedMinLogLevel = level;

            sut.IsActive = false;
            logs.Should().BeEmpty();

            sut.IsActive = true;
            AssertLogs(expectedItems);
        }

        [TestCaseSource(typeof(ApplicationViewModelTestData), nameof(ApplicationViewModelTestData.AllLogLevels))]
        public void Can_hide_logs_for_inactive_namespace(LoggingLevel level) {
            AddItemsToSut(setNamespaceFirst: true);

            var expectedItems = GetExpectedItemsFromLevel(level);
            sut.SelectedMinLogLevel = level;

            namespaceApp.IsChecked = false;
            logs.Should().BeEmpty();

            namespaceApp.IsChecked = true;
            AssertLogs(expectedItems);
        }

        [TestCaseSource(typeof(ApplicationViewModelTestData), nameof(ApplicationViewModelTestData.AllLogLevels))]
        public void Can_remove_surplus_logs(LoggingLevel level) {
            var allItems1 = AddItemsToSut(setNamespaceFirst: true);
            var expectedItems1 = GetExpectedItemsFromLevel(level, allItems1);

            var allItems2 = AddItemsToSut(11);
            var expectedItems2 = GetExpectedItemsFromLevel(level, allItems2);
            sut.SelectedMinLogLevel = level;

            sut.UpdateMaxNumberOfLogs(2);
            AssertLogs(expectedItems2, expectedItems1);

            sut.UpdateMaxNumberOfLogs(1);
            AssertLogs(expectedItems2);

            var allItems3 = AddItemsToSut(21, "Three");
            var expectedItems3 = GetExpectedItemsFromLevel(level, allItems3);
            AssertLogs(expectedItems3);
        }

        [TestCaseSource(typeof(ApplicationViewModelTestData), nameof(ApplicationViewModelTestData.ValidLogLevels))]
        public void Can_show_logs_for_present_search_options(LoggingLevel level) {
            AssertOrderLevelSearchNamespaceItems(level);
        }

        [TestCase]
        public void Cannot_show_logs_for_present_search_options_if_level_is_invalid() {
            AssertOrderLevelSearchNamespaceItems(LoggingLevel.NOT_SET);
        }

        [TestCaseSource(typeof(ApplicationViewModelTestData), nameof(ApplicationViewModelTestData.ValidLogLevels))]
        public void Can_show_logs_for_present_search_options_with_inversion(LoggingLevel level) {
            AssertOrderLevelSearchInvertedNamespaceItems(level);
        }

        [TestCase]
        public void Cannot_show_logs_for_present_search_options_with_inversion_if_level_is_invalid() {
            AssertOrderLevelSearchInvertedNamespaceItems(LoggingLevel.NOT_SET);
        }

        [TestCaseSource(typeof(ApplicationViewModelTestData), nameof(ApplicationViewModelTestData.AllLogLevels))]
        public void Can_show_logs_for_updated_search_options(LoggingLevel level) {
            sut.SelectedMinLogLevel = level;

            (var expectedItems1, var expectedItems2, var expectedItems3) = AddItemsOneTwoThreeToSut(level);

            AssertLogs(expectedItems3, expectedItems2, expectedItems1);

            sut.UpdateSearchCriteria(Search("One", false));
            AssertLogs(expectedItems1);

            sut.UpdateSearchCriteria(Search("One", true));
            AssertLogs(expectedItems3, expectedItems2);

            sut.UpdateSearchCriteria(Search("Two", false));
            AssertLogs(expectedItems2);

            sut.UpdateSearchCriteria(Search("Two", true));
            AssertLogs(expectedItems3, expectedItems1);

            sut.UpdateSearchCriteria(Search("Three", false));
            AssertLogs(expectedItems3);

            sut.UpdateSearchCriteria(Search("Three", true));
            AssertLogs(expectedItems2, expectedItems1);

            sut.UpdateSearchCriteria(Search());
            AssertLogs(expectedItems3, expectedItems2, expectedItems1);
        }

        [TestCase]
        public void Cannot_show_logs_for_updated_search_options_if_level_is_invalid() {
            sut.SelectedMinLogLevel = LoggingLevel.NOT_SET;

            _ = AddItemsOneTwoThreeToSut(LoggingLevel.NOT_SET);

            logs.Should().BeEmpty();

            sut.UpdateSearchCriteria(Search("Two", false));
            logs.Should().BeEmpty();

            sut.UpdateSearchCriteria(Search("Two", true));
            logs.Should().BeEmpty();

            sut.UpdateSearchCriteria(Search());
            logs.Should().BeEmpty();
        }

        private void AssertLogs(params IEnumerable<LogViewModel>[] expected) =>
            logs.Should().BeEquivalentTo(expected.SelectMany(e => e), c => c.WithStrictOrdering());

        private void AssertOrderNamespaceItemsLevel(LoggingLevel level) {
            var expectedItems = GetExpectedItemsFromLevel(level);

            sut.SelectedMinLogLevel = level;
            AssertLogs(expectedItems);
        }

        private void AssertOrderNamespaceLevelItems(LoggingLevel level) {
            var expectedItems = GetExpectedItemsFromLevel(level);
            namespaces.Add(namespaceApp);
            sut.SelectedMinLogLevel = level;

            AddItemsToSut();

            AssertLogs(expectedItems);
        }

        private void AssertOrderLevelItemsNamespace(LoggingLevel level) {
            var expectedItems = GetExpectedItemsFromLevel(level);
            sut.SelectedMinLogLevel = level;

            AddItemsToSut();
            logs.Should().BeEmpty();

            namespaces.Add(namespaceApp);
            logs.Should().BeEmpty();

            sut.UpdateByNamespaceChange(namespaceApp.Children.First());
            AssertLogs(expectedItems);
        }

        private void AssertOrderLevelSearchNamespaceItems(LoggingLevel level) {
            sut.SelectedMinLogLevel = level;
            sut.UpdateSearchCriteria(Search("Two", false));

            (_, var expectedItems2, _) = AddItemsOneTwoThreeToSut(level);

            AssertLogs(expectedItems2);
        }

        private void AssertOrderLevelSearchInvertedNamespaceItems(LoggingLevel level) {
            sut.SelectedMinLogLevel = level;
            sut.UpdateSearchCriteria(Search("Two", true));

            (var expectedItems1, _, var expectedItems3) = AddItemsOneTwoThreeToSut(level);

            AssertLogs(expectedItems3, expectedItems1);
        }

        private IEnumerable<LogViewModel> AddItemsToSut(bool setNamespaceFirst = false) {
            if (setNamespaceFirst)
                namespaces.Add(namespaceApp);

            AddItemsReversed(testItems);

            return testItems;
        }

        private IEnumerable<LogViewModel> AddItemsToSut(int tsOffset, string message = "Two") {
            DateTime ts = DateTime.Now;
            var itemV2 = LogVM(LoggingLevel.TRACE, ts.AddMinutes(tsOffset++), message);
            var itemD2 = LogVM(LoggingLevel.DEBUG, ts.AddMinutes(tsOffset++), message);
            var itemI2 = LogVM(LoggingLevel.INFO, ts.AddMinutes(tsOffset++), message);
            var itemW2 = LogVM(LoggingLevel.WARN, ts.AddMinutes(tsOffset++), message);
            var itemE2 = LogVM(LoggingLevel.ERROR, ts.AddMinutes(tsOffset++), message);
            var itemF2 = LogVM(LoggingLevel.FATAL, ts.AddMinutes(tsOffset++), message);

            IEnumerable<LogViewModel> items = [itemF2, itemE2, itemW2, itemI2, itemD2, itemV2];
            AddItemsReversed(items);

            return items;
        }

        private (IEnumerable<LogViewModel>, IEnumerable<LogViewModel>, IEnumerable<LogViewModel>) AddItemsOneTwoThreeToSut(LoggingLevel level) {
            // message contains "One"
            var allItems1 = AddItemsToSut(setNamespaceFirst: true);
            var expectedItems1 = GetExpectedItemsFromLevel(level, allItems1);

            // message contains "Two"
            var allItems2 = AddItemsToSut(11);
            var expectedItems2 = GetExpectedItemsFromLevel(level, allItems2);

            // message contains "Three"
            var allItems3 = AddItemsToSut(21, "Three");
            var expectedItems3 = GetExpectedItemsFromLevel(level, allItems3);

            return (expectedItems1, expectedItems2, expectedItems3);
        }

        private void AddItemsReversed(IEnumerable<LogViewModel> items) {
            foreach (var item in items.Reverse()) {
                // items are added in front without timestamp ordering
                sut.AddLog(item);
            }
        }

        private IEnumerable<LogViewModel> GetExpectedItemsFromLevel(LoggingLevel level, IEnumerable<LogViewModel>? items = null) =>
            level == LoggingLevel.NOT_SET
            ? []
            : (items ?? testItems).TakeWhile(item => item.Level >= level);

        private static LogViewModel LogVM(LoggingLevel level, DateTime ts, string message = "One") =>
            new() { Application = APP_NAME, Namespace = NAMESPACE_NAME, Level = level, Timestamp = ts, Message = $"start {message} end" };

        private static SearchOptions Search(string? criteria = null, bool isInverted = false) =>
            new() { Criteria = criteria, IsInverted = isInverted };
    }
}