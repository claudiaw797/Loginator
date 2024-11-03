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

        private readonly IEnumerable<Log> testItems;

        public ApplicationViewModelTests() {
            sut = new ApplicationViewModel(APP_NAME, logs, namespaces, LogLevel.NOT_SET);
            namespaceApp = new NamespaceViewModel(APP_NAME, sut);

            var ts = DateTimeOffset.Now;
            var itemV = Log(LogLevel.TRACE, ts.AddMinutes(1));
            var itemD = Log(LogLevel.DEBUG, ts.AddMinutes(2));
            var itemI = Log(LogLevel.INFO, ts.AddMinutes(3));
            var itemW = Log(LogLevel.WARN, ts.AddMinutes(4));
            var itemE = Log(LogLevel.ERROR, ts.AddMinutes(5));
            var itemF = Log(LogLevel.FATAL, ts.AddMinutes(6));

            testItems = [itemF, itemE, itemW, itemI, itemD, itemV];
        }

        [SetUp]
        public void Setup() {
            var ns = new NamespaceViewModel(NAMESPACE_NAME, sut) { Parent = namespaceApp };
            namespaceApp.Children.Add(ns);
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
        public void Can_show_logs_for_present_namespace(LogLevel level) {
            AssertOrderNamespaceLevelItems(level);
        }

        [TestCase]
        public void Cannot_show_logs_for_present_namespace_if_level_is_invalid() {
            AssertOrderNamespaceLevelItems(LogLevel.NOT_SET);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public void Can_show_logs_for_updated_namespace(LogLevel level) {
            AssertOrderLevelItemsNamespace(level);
        }

        [TestCase]
        public void Cannot_show_logs_for_updated_namespace_if_level_is_invalid() {
            AssertOrderLevelItemsNamespace(LogLevel.NOT_SET);
        }

        [TestCase]
        public void Can_show_logs_from_updated_level() {
            AddItemsToSut(setNamespaceFirst: true);

            AssertOrderNamespaceItemsLevel(LogLevel.NOT_SET);
            AssertOrderNamespaceItemsLevel(LogLevel.TRACE);
            AssertOrderNamespaceItemsLevel(LogLevel.DEBUG);
            AssertOrderNamespaceItemsLevel(LogLevel.INFO);
            AssertOrderNamespaceItemsLevel(LogLevel.WARN);
            AssertOrderNamespaceItemsLevel(LogLevel.ERROR);
            AssertOrderNamespaceItemsLevel(LogLevel.FATAL);
            AssertOrderNamespaceItemsLevel(LogLevel.NOT_SET);
            AssertOrderNamespaceItemsLevel(LogLevel.ERROR);
            AssertOrderNamespaceItemsLevel(LogLevel.WARN);
            AssertOrderNamespaceItemsLevel(LogLevel.INFO);
            AssertOrderNamespaceItemsLevel(LogLevel.DEBUG);
            AssertOrderNamespaceItemsLevel(LogLevel.TRACE);
            AssertOrderNamespaceItemsLevel(LogLevel.NOT_SET);
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
            AssertOrderLevelSearchNamespaceItems(level);
        }

        [TestCase]
        public void Cannot_show_logs_for_present_search_options_if_level_is_invalid() {
            AssertOrderLevelSearchNamespaceItems(LogLevel.NOT_SET);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public void Can_show_logs_for_present_search_options_with_inversion(LogLevel level) {
            AssertOrderLevelSearchInvertedNamespaceItems(level);
        }

        [TestCase]
        public void Cannot_show_logs_for_present_search_options_with_inversion_if_level_is_invalid() {
            AssertOrderLevelSearchInvertedNamespaceItems(LogLevel.NOT_SET);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.AllLogLevels))]
        public void Can_show_logs_for_updated_search_options(LogLevel level) {
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

        [TestCase]
        public void Cannot_show_logs_for_updated_search_options_if_level_is_invalid() {
            sut.SelectedMinLogLevel = LogLevel.NOT_SET;

            _ = AddItemsOneTwoThreeToSut(LogLevel.NOT_SET);

            logs.Should().BeEmpty();

            sut.SearchOptions = Search("Two", false);
            logs.Should().BeEmpty();

            sut.SearchOptions = Search("Two", true);
            logs.Should().BeEmpty();

            sut.SearchOptions = Search();
            logs.Should().BeEmpty();
        }

        private void AssertOrderNamespaceItemsLevel(LogLevel level) {
            var expectedItems = GetExpectedItemsFromLevel(level);

            sut.SelectedMinLogLevel = level;
            AssertLogs(expectedItems);
        }

        private void AssertOrderNamespaceLevelItems(LogLevel level) {
            var expectedItems = GetExpectedItemsFromLevel(level);
            namespaces.Add(namespaceApp);
            sut.SelectedMinLogLevel = level;

            AddItemsToSut();

            AssertLogs(expectedItems);
        }

        private void AssertOrderLevelItemsNamespace(LogLevel level) {
            var expectedItems = GetExpectedItemsFromLevel(level);
            sut.SelectedMinLogLevel = level;

            AddItemsToSut();
            logs.Should().BeEmpty();

            namespaces.Add(namespaceApp);
            logs.Should().BeEmpty();

            sut.OnNamespaceIsActiveChanged(namespaceApp.Children.First());
            AssertLogs(expectedItems);
        }

        private void AssertOrderLevelSearchNamespaceItems(LogLevel level) {
            sut.SelectedMinLogLevel = level;
            sut.SearchOptions = Search("Two", false);

            (_, var expectedItems2, _) = AddItemsOneTwoThreeToSut(level);

            AssertLogs(expectedItems2);
        }

        private void AssertOrderLevelSearchInvertedNamespaceItems(LogLevel level) {
            sut.SelectedMinLogLevel = level;
            sut.SearchOptions = Search("Two", true);

            (var expectedItems1, _, var expectedItems3) = AddItemsOneTwoThreeToSut(level);

            AssertLogs(expectedItems3, expectedItems1);
        }

        private void AssertLogs(params IEnumerable<Log>[] expected) =>
            logs.Should().BeEquivalentTo(expected.SelectMany(e => e), c => c.WithStrictOrdering());

        private IEnumerable<Log> AddItemsToSut(bool setNamespaceFirst = false) {
            if (setNamespaceFirst)
                namespaces.Add(namespaceApp);

            AddItemsReversed(testItems);

            return testItems;
        }

        private IEnumerable<Log> AddItemsToSut(int tsOffset, string message = "Two") {
            var ts = DateTimeOffset.Now;
            var itemV2 = Log(LogLevel.TRACE, ts.AddMinutes(tsOffset++), message);
            var itemD2 = Log(LogLevel.DEBUG, ts.AddMinutes(tsOffset++), message);
            var itemI2 = Log(LogLevel.INFO, ts.AddMinutes(tsOffset++), message);
            var itemW2 = Log(LogLevel.WARN, ts.AddMinutes(tsOffset++), message);
            var itemE2 = Log(LogLevel.ERROR, ts.AddMinutes(tsOffset++), message);
            var itemF2 = Log(LogLevel.FATAL, ts.AddMinutes(tsOffset++), message);

            IEnumerable<Log> items = [itemF2, itemE2, itemW2, itemI2, itemD2, itemV2];
            AddItemsReversed(items);

            return items;
        }

        private (IEnumerable<Log>, IEnumerable<Log>, IEnumerable<Log>) AddItemsOneTwoThreeToSut(LogLevel level) {
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

        private void AddItemsReversed(IEnumerable<Log> items) {
            foreach (var item in items.Reverse()) {
                // items are added in front without timestamp ordering
                sut.AddLog(item);
            }
        }

        private IEnumerable<Log> GetExpectedItemsFromLevel(LogLevel level, IEnumerable<Log>? items = null) =>
            level == LogLevel.NOT_SET
            ? []
            : (items ?? testItems).TakeWhile(item => item.Level >= level);

        private static Log Log(LogLevel level, DateTimeOffset ts, string message = "One") =>
            new() {
                Application = APP_NAME,
                Namespace = NAMESPACE_NAME,
                Level = level,
                Timestamp = ts,
                Message = $"start {message} end"
            };

        private static SearchOptions Search(string? criteria = null, bool isInverted = false) =>
            new() {
                Criteria = criteria,
                IsInverted = isInverted
            };
    }
}