// Copyright (C) 2024 Claudia Wagner

using FluentAssertions;
using Loginator.Application.Common;
using Loginator.Application.Model;
using Loginator.Application.ViewModel;
using Loginator.Domain.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Loginator.Application.UnitTests.ViewModel {

    /// <summary>
    /// Represents unit tests for <see cref="ApplicationViewModel"/>.
    /// </summary>
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public class ApplicationViewModelTests {

        private const string APP_NAME = "TestApp";
        private const string NAMESPACE_NAME = "TestNs";

        private readonly ApplicationViewModel sut;
        private readonly NamespaceViewModel namespaceApp;
        private readonly ObservableCollection<NamespaceViewModel> namespaces = [];
        private readonly OrderedObservableCollection logs = [];

        private readonly IEnumerable<LogViewModel> testItems;
        private readonly LogViewModel[] testItemsWithParent = new LogViewModel[6];

        public ApplicationViewModelTests() {
            sut = new ApplicationViewModel(APP_NAME, logs, namespaces, LogLevel.NOT_SET);
            namespaceApp = new NamespaceViewModel(APP_NAME, sut);

            var ts = DateTimeOffset.Now;
            testItems = [
                LogVM(LogLevel.FATAL, ts.AddMinutes(6)),
                LogVM(LogLevel.ERROR, ts.AddMinutes(5)),
                LogVM(LogLevel.WARN, ts.AddMinutes(4)),
                LogVM(LogLevel.INFO, ts.AddMinutes(3)),
                LogVM(LogLevel.DEBUG, ts.AddMinutes(2)),
                LogVM(LogLevel.TRACE, ts.AddMinutes(1))
            ];
        }

        [SetUp]
        public void Setup() {
            var ns = new NamespaceViewModel(NAMESPACE_NAME, sut) { Parent = namespaceApp };
            namespaceApp.Children.Add(ns);

            var ts = DateTimeOffset.Now;
            testItemsWithParent[0] = LogVM(ns, LogLevel.FATAL, ts.AddMinutes(6));
            testItemsWithParent[1] = LogVM(ns, LogLevel.ERROR, ts.AddMinutes(5));
            testItemsWithParent[2] = LogVM(ns, LogLevel.WARN, ts.AddMinutes(4));
            testItemsWithParent[3] = LogVM(ns, LogLevel.INFO, ts.AddMinutes(3));
            testItemsWithParent[4] = LogVM(ns, LogLevel.DEBUG, ts.AddMinutes(2));
            testItemsWithParent[5] = LogVM(ns, LogLevel.TRACE, ts.AddMinutes(1));
        }

        [Test]
        public void Can_create_sut() {
            var expectedAppName = "TestApp(101)";
            var expectedLogLevels = LogLevel.AllLogLevels.Order();
            var expectedLogLevel = LogLevel.INFO;
            var sut = new ApplicationViewModel(expectedAppName, logs, namespaces, expectedLogLevel);

            sut.Name.Should().Be(expectedAppName);
            sut.LogLevels.Should().BeEquivalentTo(expectedLogLevels);
            sut.SelectedMinLogLevel.Should().Be(expectedLogLevel);
            logs.Should().BeEmpty();
            namespaces.Should().BeEmpty();
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.AllLogLevels))]
        public void Cannot_show_logs_without_namespace(LogLevel level) {
            sut.SelectedMinLogLevel = level;

            AddItemsToSut();

            logs.Should().BeEmpty();
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public void Can_show_logs_for_present_namespace_if_not_set_as_parent(LogLevel level) {
            TestOrderNamespaceLevelItems(level);
        }

        [TestCase]
        public void Cannot_show_logs_for_present_namespace_if_level_is_invalid() {
            TestOrderNamespaceLevelItems(LogLevel.NOT_SET);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public void Can_show_logs_for_present_namespace_if_set_as_parent(LogLevel level) {
            TestOrderNamespaceLevelItems(level, testItemsWithParent);

            namespaceApp.IsActive = false;
            logs.Should().BeEmpty();
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public void Can_show_logs_for_namespace_change(LogLevel level) {
            TestOrderLevelItemsNamespace(level);
        }

        [TestCase]
        public void Cannot_show_logs_for_namespace_change_if_level_is_invalid() {
            TestOrderLevelItemsNamespace(LogLevel.NOT_SET);
        }

        [TestCase]
        public void Can_show_logs_for_level_change() {
            AddItemsToSut(setNamespaceFirst: true);

            TestOrderNamespaceItemsLevel(LogLevel.NOT_SET);
            TestOrderNamespaceItemsLevel(LogLevel.TRACE);
            TestOrderNamespaceItemsLevel(LogLevel.DEBUG);
            TestOrderNamespaceItemsLevel(LogLevel.INFO);
            TestOrderNamespaceItemsLevel(LogLevel.WARN);
            TestOrderNamespaceItemsLevel(LogLevel.ERROR);
            TestOrderNamespaceItemsLevel(LogLevel.FATAL);
            TestOrderNamespaceItemsLevel(LogLevel.NOT_SET);
            TestOrderNamespaceItemsLevel(LogLevel.ERROR);
            TestOrderNamespaceItemsLevel(LogLevel.WARN);
            TestOrderNamespaceItemsLevel(LogLevel.INFO);
            TestOrderNamespaceItemsLevel(LogLevel.DEBUG);
            TestOrderNamespaceItemsLevel(LogLevel.TRACE);
            TestOrderNamespaceItemsLevel(LogLevel.NOT_SET);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.DifferentLogLevels), new object[] { true })]
        [TestCaseSource(typeof(TestData), nameof(TestData.DifferentLogLevels), new object[] { false })]
        public void Cannot_show_logs_for_level_change_if_application_is_inactive(LogLevel levelFrom, LogLevel levelTo, bool isParentSet) {
            TestOrderNamespaceLevelItems(levelFrom, items: isParentSet ? testItemsWithParent : testItems);

            sut.IsActive = false;
            logs.Should().BeEmpty();

            sut.SelectedMinLogLevel = levelTo;
            logs.Should().BeEmpty();
        }

        [TestCase]
        public void Can_clear_logs() {
            sut.HasLogs.Should().BeFalse();

            AddItemsToSut(setNamespaceFirst: true);
            sut.HasLogs.Should().BeTrue();

            sut.ClearLogs();
            sut.HasLogs.Should().BeFalse();
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.AllLogLevels))]
        public void Can_hide_logs_for_inactive_application(LogLevel level) {
            AddItemsToSut(setNamespaceFirst: true);

            var expectedItems = GetExpectedItemsFromLevel(level);
            sut.SelectedMinLogLevel = level;

            sut.IsActive = false;
            logs.Should().BeEmpty();

            sut.IsActive = true;
            AssertLogs(expectedItems);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.AllLogLevels))]
        public void Can_hide_logs_for_inactive_namespace(LogLevel level) {
            AddItemsToSut(setNamespaceFirst: true);

            var expectedItems = GetExpectedItemsFromLevel(level);
            sut.SelectedMinLogLevel = level;

            namespaceApp.IsActive = false;
            logs.Should().BeEmpty();

            namespaceApp.IsActive = true;
            AssertLogs(expectedItems);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevelsAndBool), new object[] { true })]
        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevelsAndBool), new object[] { false })]
        public void Cannot_remove_surplus_logs_if_new_number_is_higher_than_previous_number(LogLevel level, bool isParentSet) {
            var items = isParentSet ? testItemsWithParent : testItems;
            var expectedItems = TestOrderNamespaceLevelItems(level, items: items);

            sut.MaxNumberOfLogsPerLevel += 1;
            AssertLogs(expectedItems);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.AllLogLevels))]
        public void Can_remove_surplus_logs(LogLevel level) {
            var allItems1 = AddItemsToSut(setNamespaceFirst: true);
            var expectedItems1 = GetExpectedItemsFromLevel(level, allItems1);

            var allItems2 = AddItemsToSut(11);
            var expectedItems2 = GetExpectedItemsFromLevel(level, allItems2);
            sut.SelectedMinLogLevel = level;

            sut.MaxNumberOfLogsPerLevel = 2;
            AssertLogs(expectedItems2, expectedItems1);

            sut.MaxNumberOfLogsPerLevel = 1;
            AssertLogs(expectedItems2);

            var allItems3 = AddItemsToSut(21, "Three");
            var expectedItems3 = GetExpectedItemsFromLevel(level, allItems3);
            AssertLogs(expectedItems3);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public void Can_show_logs_for_present_search_options(LogLevel level) {
            TestOrderLevelSearchNamespaceItems(level);
        }

        [TestCase]
        public void Cannot_show_logs_for_present_search_options_if_level_is_invalid() {
            TestOrderLevelSearchNamespaceItems(LogLevel.NOT_SET);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public void Can_show_logs_for_present_search_options_with_inversion(LogLevel level) {
            TestOrderLevelSearchInvertedNamespaceItems(level);
        }

        [TestCase]
        public void Cannot_show_logs_for_present_search_options_with_inversion_if_level_is_invalid() {
            TestOrderLevelSearchInvertedNamespaceItems(LogLevel.NOT_SET);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.AllLogLevels))]
        public void Can_show_logs_for_search_option_change(LogLevel level) {
            sut.SelectedMinLogLevel = level;

            (var expectedItems1, var expectedItems2, var expectedItems3) = AddItemsOneTwoThreeToSut(level);

            AssertLogs(expectedItems3, expectedItems2, expectedItems1);

            sut.SearchOptions = Search("One", false);
            AssertLogs(expectedItems1);

            sut.SearchOptions = Search("One", true);
            AssertLogs(expectedItems3, expectedItems2);

            sut.SearchOptions = Search("Two", false);
            AssertLogs(expectedItems2);

            sut.SearchOptions = Search("Two", true);
            AssertLogs(expectedItems3, expectedItems1);

            sut.SearchOptions = Search("Three", false);
            AssertLogs(expectedItems3);

            sut.SearchOptions = Search("Three", true);
            AssertLogs(expectedItems2, expectedItems1);

            sut.SearchOptions = Search();
            AssertLogs(expectedItems3, expectedItems2, expectedItems1);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevelsAndBool), new object[] { true })]
        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevelsAndBool), new object[] { false })]
        public void Can_show_logs_for_search_in_application(LogLevel level, bool isParentSet) {
            var search = AddAndTestItemsAndNamespacesForSearch(level, isParentSet ? testItemsWithParent : testItems);

            sut.SearchOptions = Search(search.Application.Text);
            AssertLogs(search.Application.Expected);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevelsAndBool), new object[] { true })]
        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevelsAndBool), new object[] { false })]
        public void Can_show_logs_for_search_in_namespace(LogLevel level, bool isParentSet) {
            var search = AddAndTestItemsAndNamespacesForSearch(level, isParentSet ? testItemsWithParent : testItems);

            sut.SearchOptions = Search(search.NamespaceFirst.Text);
            AssertLogs(search.NamespaceFirst.Expected);

            sut.SearchOptions = Search(search.NamespaceSecond.Text);
            AssertLogs(search.NamespaceSecond.Expected);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevelsAndBool), new object[] { true })]
        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevelsAndBool), new object[] { false })]
        public void Can_show_logs_for_search_in_message(LogLevel level, bool isParentSet) {
            var search = AddAndTestItemsAndNamespacesForSearch(level, isParentSet ? testItemsWithParent : testItems);

            sut.SearchOptions = Search(search.Message.Text);
            AssertLogs(search.Message.Expected);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevelsAndBool), new object[] { true })]
        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevelsAndBool), new object[] { false })]
        public void Can_show_logs_for_search_in_exception(LogLevel level, bool isParentSet) {
            var search = AddAndTestItemsAndNamespacesForSearch(level, isParentSet ? testItemsWithParent : testItems);

            sut.SearchOptions = Search(search.Exception.Text);
            AssertLogs(search.Exception.Expected);
        }

        [TestCase]
        public void Cannot_show_logs_for_search_option_change_if_level_is_invalid() {
            sut.SelectedMinLogLevel = LogLevel.NOT_SET;

            TestSearchOptions(LogLevel.NOT_SET);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public void Cannot_show_logs_for_search_option_change_if_application_is_inactive(LogLevel level) {
            sut.SelectedMinLogLevel = level;
            sut.IsActive = false;

            TestSearchOptions(level);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.AllLogLevels))]
        public void Can_highlight_logs_for_namespace_if_set_as_parent(LogLevel level) {
            AddItemsToSut(setNamespaceFirst: true, items: testItemsWithParent);
            sut.SelectedMinLogLevel = level;

            var ns = namespaceApp.Children.First();
            sut.UpdateIsHighlighted(ns);
            AssertLogsHighlighted(true);

            sut.UpdateIsHighlighted(null);
            AssertLogsHighlighted(false);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.AllLogLevels))]
        public void Cannot_highlight_logs_for_namespace_if_not_set_as_parent(LogLevel level) {
            AddItemsToSut(setNamespaceFirst: true);
            sut.SelectedMinLogLevel = level;

            var ns = namespaceApp.Children.First();
            sut.UpdateIsHighlighted(ns);
            AssertLogsHighlighted(false);
        }

        private void AssertLogs(params IEnumerable<LogViewModel>[] expected) =>
            logs.Should().BeEquivalentTo(expected.SelectMany(e => e), c => c.WithStrictOrdering());

        private void AssertLogsHighlighted(bool state) =>
            logs.Select(vm => vm.IsHighlighted).Should().AllBeEquivalentTo(state);

        private void TestOrderNamespaceItemsLevel(LogLevel level) {
            var expectedItems = GetExpectedItemsFromLevel(level);

            sut.SelectedMinLogLevel = level;
            AssertLogs(expectedItems);
        }

        private IEnumerable<LogViewModel> TestOrderNamespaceLevelItems(LogLevel level, IEnumerable<LogViewModel>? items = null) {
            var expectedItems = GetExpectedItemsFromLevel(level, items);
            namespaces.Add(namespaceApp);
            sut.SelectedMinLogLevel = level;

            AddItemsToSut(items: items);

            AssertLogs(expectedItems);
            return expectedItems;
        }

        private void TestOrderLevelItemsNamespace(LogLevel level) {
            var expectedItems = GetExpectedItemsFromLevel(level);
            sut.SelectedMinLogLevel = level;

            AddItemsToSut();
            logs.Should().BeEmpty();

            namespaces.Add(namespaceApp);
            logs.Should().BeEmpty();

            sut.IsActive = false;
            sut.UpdateIsActive(namespaceApp.Children.First());
            logs.Should().BeEmpty();

            sut.IsActive = true;
            sut.UpdateIsActive(namespaceApp.Children.First());
            AssertLogs(expectedItems);
        }

        private void TestOrderLevelSearchNamespaceItems(LogLevel level) {
            sut.SelectedMinLogLevel = level;
            sut.SearchOptions = Search("Two", false);

            (_, var expectedItems2, _) = AddItemsOneTwoThreeToSut(level);

            AssertLogs(expectedItems2);
        }

        private void TestOrderLevelSearchInvertedNamespaceItems(LogLevel level) {
            sut.SelectedMinLogLevel = level;
            sut.SearchOptions = Search("Two", true);

            (var expectedItems1, _, var expectedItems3) = AddItemsOneTwoThreeToSut(level);

            AssertLogs(expectedItems3, expectedItems1);
        }

        private void TestSearchOptions(LogLevel level) {
            _ = AddItemsOneTwoThreeToSut(level);

            logs.Should().BeEmpty();

            sut.SearchOptions = Search("Two", false);
            logs.Should().BeEmpty();

            sut.SearchOptions = Search("Two", true);
            logs.Should().BeEmpty();

            sut.SearchOptions = Search();
            logs.Should().BeEmpty();
        }

        private SearchAssertions AddAndTestItemsAndNamespacesForSearch(LogLevel level, IEnumerable<LogViewModel>? items = null) {
            var expectedItems1 = TestOrderNamespaceLevelItems(level, items: items);

            var nsFirst = "FirstLayerNs";
            var nsSecond = "SecondLayerNs";
            var ns1 = new NamespaceViewModel(nsFirst, sut) { Parent = namespaceApp };
            var ns2 = new NamespaceViewModel(nsSecond, sut) { Parent = ns1 };
            var ns3 = new NamespaceViewModel(string.Empty, sut) { Parent = namespaceApp };
            var ns4 = new NamespaceViewModel(null!, sut) { Parent = namespaceApp };
            namespaceApp.Children.AddRange(ns1, ns3, ns4);
            ns1.Children.Add(ns2);

            var ts = DateTimeOffset.Now.AddMinutes(20);
            var itNo1 = LogVM(level, ts.AddMinutes(1), "message1", string.Empty, string.Empty);
            var itOk1 = LogVM(level, ts.AddMinutes(2), "message2", APP_NAME, string.Empty);
            var itNo2 = LogVM(level, ts.AddMinutes(3), "message3", string.Empty, NAMESPACE_NAME);
            var itOk2 = LogVM(level, ts.AddMinutes(4), "message4", APP_NAME, nsFirst);
            var itNo3 = LogVM(level, ts.AddMinutes(5), "message5", APP_NAME, nsSecond);
            var itOk3 = LogVM(level, ts.AddMinutes(6), "message6", APP_NAME, $"{nsFirst}.{nsSecond}");
            var itNo4 = LogVM(level, ts.AddMinutes(7), "message7", APP_NAME, $"{nsFirst}.{nsSecond}.ThirdLayerNs");
            var itOk4 = LogVM(level, ts.AddMinutes(8), "message8", APP_NAME, null!, "this is error nr. 1");
            var itNo5 = LogVM(level, ts.AddMinutes(9), "message9", APP_NAME, "OtherNs");

            AddItemsToSut(items: [itNo5, itOk4, itNo4, itOk3, itNo3, itOk2, itNo2, itOk1, itNo1]);

            var expectedItems2 = new[] { itOk4, itOk3, itOk2, itOk1 };
            AssertLogs(expectedItems2, expectedItems1);

            return new SearchAssertions(
                new(APP_NAME, expectedItems2.Concat(expectedItems1)),
                new(nsFirst, [itOk3, itOk2]),
                new(nsSecond, [itOk3]),
                new("message", [itOk4, itOk3, itOk2, itOk1]),
                new("error", [itOk4])
            );
        }

        private IEnumerable<LogViewModel> AddItemsToSut(bool setNamespaceFirst = false, IEnumerable<LogViewModel>? items = null) {
            if (setNamespaceFirst) {
                namespaces.Add(namespaceApp);
            }

            items ??= testItems;
            AddItemsReversed(items);

            return items;
        }

        private IEnumerable<LogViewModel> AddItemsToSut(int tsOffset, string message = "Two") {
            var ts = DateTimeOffset.Now;
            var itemV2 = LogVM(LogLevel.TRACE, ts.AddMinutes(tsOffset++), message);
            var itemD2 = LogVM(LogLevel.DEBUG, ts.AddMinutes(tsOffset++), message);
            var itemI2 = LogVM(LogLevel.INFO, ts.AddMinutes(tsOffset++), message);
            var itemW2 = LogVM(LogLevel.WARN, ts.AddMinutes(tsOffset++), message);
            var itemE2 = LogVM(LogLevel.ERROR, ts.AddMinutes(tsOffset++), message);
            var itemF2 = LogVM(LogLevel.FATAL, ts.AddMinutes(tsOffset++), message);
            var itemN2 = LogVM(LogLevel.NOT_SET, ts.AddMinutes(tsOffset++), message);

            IEnumerable<LogViewModel> items = [itemF2, itemE2, itemW2, itemI2, itemD2, itemV2, itemN2];
            AddItemsReversed(items);

            return items;
        }

        private (IEnumerable<LogViewModel>, IEnumerable<LogViewModel>, IEnumerable<LogViewModel>) AddItemsOneTwoThreeToSut(LogLevel level) {
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

        private IEnumerable<LogViewModel> GetExpectedItemsFromLevel(LogLevel level, IEnumerable<LogViewModel>? items = null) =>
            level == LogLevel.NOT_SET
            ? []
            : (items ?? testItems).TakeWhile(item => item.Level >= level);

        private static LogViewModel LogVM(LogLevel level, DateTimeOffset ts, string message, string application, string nspace, string? exception = null) =>
            new(new() {
                Application = application,
                Namespace = nspace,
                Level = level,
                Timestamp = ts,
                Message = message,
                Exception = exception,
            });

        private static LogViewModel LogVM(LogLevel level, DateTimeOffset ts, string message = "One") =>
            LogVM(level, ts, $"start {message} end", APP_NAME, NAMESPACE_NAME);

        private static LogViewModel LogVM(NamespaceViewModel parent, LogLevel level, DateTimeOffset ts, string message = "One") {
            var vm = LogVM(level, ts, message);
            vm.Parent = parent;
            return vm;
        }

        private static SearchOptions Search(string? criteria = null, bool isInverted = false) =>
            new() {
                Criteria = criteria,
                IsInverted = isInverted
            };

        private record SearchAssertions(
            SearchAssertion Application,
            SearchAssertion NamespaceFirst,
            SearchAssertion NamespaceSecond,
            SearchAssertion Message,
            SearchAssertion Exception) { }

        private record SearchAssertion(string Text, IEnumerable<LogViewModel> Expected) { }
    }
}