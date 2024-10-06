// Copyright (C) 2024 Claudia Wagner

using Backend.Model;
using Backend.Server;
using Common;
using Common.Configuration;
using FakeItEasy;
using FluentAssertions;
using Loginator.Controls;
using Loginator.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Loginator.UnitTests.ViewModels {

    /// <summary>
    /// Represents unit tests for <see cref="LoginatorViewModel"/>.
    /// </summary>
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public partial class LoginatorViewModelTests {

        private static readonly Dictionary<int, string> APP_NAMES = new() {
            { 1, "TestApp" },
            { 2, "TästApp" }
        };

        private const string NAMESPACE_NAME = "TestNs";

        private static readonly Dictionary<LoggingLevel, string?> EXCEPTION_MESSAGES = new() {
            { LoggingLevel.NOT_SET, null },
            { LoggingLevel.TRACE, null },
            { LoggingLevel.DEBUG, null },
            { LoggingLevel.INFO, null },
            { LoggingLevel.WARN, "Test warning issued" },
            { LoggingLevel.ERROR, "Test exception happened" },
            { LoggingLevel.FATAL, "Test fatality happened" }
        };

        private readonly LoginatorViewModel sut;
        private readonly IReceiver receiver = A.Fake<IReceiver>();
        private readonly FakeTimeProvider timeProvider;
        private readonly LogListener logListener = new();
        private readonly AsyncEnumerableQueue<Log> receivedLogs;

        private readonly IEnumerable<Log> testItems;

        public LoginatorViewModelTests() {
            timeProvider = new FakeTimeProvider();
            receivedLogs = new();

            sut = Sut();

            var ts = DateTimeOffset.Now;
            var itemV = Log(LoggingLevel.TRACE, ts.AddMinutes(1));
            var itemD = Log(LoggingLevel.DEBUG, ts.AddMinutes(2));
            var itemI = Log(LoggingLevel.INFO, ts.AddMinutes(3));
            var itemW = Log(LoggingLevel.WARN, ts.AddMinutes(4));
            var itemE = Log(LoggingLevel.ERROR, ts.AddMinutes(5));
            var itemF = Log(LoggingLevel.FATAL, ts.AddMinutes(6));

            testItems = [itemF, itemE, itemW, itemI, itemD, itemV];
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() {
            receivedLogs.IsCompleted = true;
            sut.Dispose();
        }

        [Test]
        public void Can_create_sut() {
            var configDao = A.Fake<IOptionsMonitor<Configuration>>();
            var stopwatch = A.Fake<IStopwatch>();
            var logger = A.Fake<ILogger<LoginatorViewModel>>();

            var sut = new LoginatorViewModel(configDao, stopwatch, timeProvider, new DispatcherMock(), logger);

            sut.IsActive.Should().BeTrue();
            sut.NumberOfLogsPerLevel.Should().BeGreaterThan(100);
            sut.Search.Should().NotBeNull();
            sut.SelectedInitialLogLevel.Should().NotBe(LoggingLevel.NOT_SET);
            sut.SelectedLog.Should().BeNull();
            sut.SelectedNamespace.Should().BeNull();
            sut.Logs.Should().BeEmpty();
            sut.Namespaces.Should().BeEmpty();
            sut.Applications.Should().BeEmpty();
            sut.Dispose();
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public async Task Can_show_logs_for_selected_level(LoggingLevel level) {
            await AssertOrderLevelItemsAsync(level);
        }

        [Test]
        public async Task Cannot_show_logs_if_selected_level_is_invalid() {
            await AssertOrderLevelItemsAsync(LoggingLevel.NOT_SET);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.AllLogLevels))]
        public async Task Can_show_logs_for_selected_level_change(LoggingLevel level) {
            var newLevel = level == LoggingLevel.FATAL
                ? LoggingLevel.NOT_SET
                : LoggingLevel.FromId(level.Id + 1)!;
            newLevel.Should().NotBeNull();
            sut.SelectedInitialLogLevel = level;

            (var expectedItems1, var expectedItems2) = await AddItemsOneTwoDifferentAppsToSutAsync(level, () => newLevel);

            AssertLogs(expectedItems2, expectedItems1);
            AssertApplicationAndNamespaces(level, newLevel);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public async Task Can_stop_and_restart_adding_logs_by_changing_active(LoggingLevel level) {
            sut.SelectedInitialLogLevel = level;

            sut.IsActive = false;
            await AddItemsToSutAsync();
            AssertLogs();

            sut.IsActive = true;
            var allItems1 = await AddItemsToSutAsync();
            var expectedItems1 = GetExpectedItemsFromLevel(level, allItems1);

            sut.IsActive = false;
            await AddItemsToSutAsync(11);

            sut.IsActive = true;
            var allItems3 = await AddItemsToSutAsync(21, "Three");
            var expectedItems3 = GetExpectedItemsFromLevel(level, allItems3);

            AssertLogs(expectedItems3, expectedItems1);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public async Task Can_remove_surplus_logs(LoggingLevel level) {
            sut.SelectedInitialLogLevel = level;
            sut.UpdateNumberOfLogsPerLevelCommand.Execute(2);

            var allItems1 = await AddItemsToSutAsync();
            var expectedItems1 = GetExpectedItemsFromLevel(level, allItems1);

            var allItems2 = await AddItemsToSutAsync(11);
            var expectedItems2 = GetExpectedItemsFromLevel(level, allItems2);
            AssertLogs(expectedItems2, expectedItems1);

            var allItems3 = await AddItemsToSutAsync(21, "Three");
            var expectedItems3 = GetExpectedItemsFromLevel(level, allItems3);
            AssertLogs(expectedItems3, expectedItems2);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public async Task Can_show_logs_for_present_search_options(LoggingLevel level) {
            await AssertOrderLevelSearchItemsAsync(level);
        }

        [TestCase]
        public async Task Cannot_show_logs_for_present_search_options_if_level_is_invalid() {
            await AssertOrderLevelSearchItemsAsync(LoggingLevel.NOT_SET);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public async Task Can_show_logs_for_present_search_options_with_inversion(LoggingLevel level) {
            await AssertOrderLevelSearchInvertedItemsAsync(level);
        }

        [TestCase]
        public async Task Cannot_show_logs_for_present_search_options_with_inversion_if_level_is_invalid() {
            await AssertOrderLevelSearchInvertedItemsAsync(LoggingLevel.NOT_SET);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.AllLogLevels))]
        public async Task Can_show_logs_for_updated_search_options(LoggingLevel level) {
            sut.SelectedInitialLogLevel = level;

            (var expectedItems1, var expectedItems2, var expectedItems3) = await AddItemsOneTwoThreeToSutAsync(level);

            AssertLogs(expectedItems3, expectedItems2, expectedItems1);

            SetSearch("One", false);
            AssertLogs(expectedItems1);

            SetSearch("One", true);
            AssertLogs(expectedItems3, expectedItems2);

            SetSearch("Two", false);
            AssertLogs(expectedItems2);

            SetSearch("Two", true);
            AssertLogs(expectedItems3, expectedItems1);

            SetSearch("Three", false);
            AssertLogs(expectedItems3);

            SetSearch("Three", true);
            AssertLogs(expectedItems2, expectedItems1);

            SetSearch();
            AssertLogs(expectedItems3, expectedItems2, expectedItems1);
        }

        [TestCase]
        public async Task Cannot_show_logs_for_updated_search_options_if_level_is_invalid() {
            sut.SelectedInitialLogLevel = LoggingLevel.NOT_SET;

            _ = await AddItemsOneTwoThreeToSutAsync(LoggingLevel.NOT_SET);

            sut.Logs.Should().BeEmpty();

            SetSearch("Two", false);
            sut.Logs.Should().BeEmpty();

            SetSearch("Two", true);
            sut.Logs.Should().BeEmpty();

            SetSearch();
            sut.Logs.Should().BeEmpty();
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public async Task Can_select_and_highlight_namespace_by_selecting_log(LoggingLevel level) {
            sut.SelectedInitialLogLevel = level;

            (var expectedItems1, var expectedItems2) = await AddItemsOneTwoDifferentAppsToSutAsync(level);

            var zippedItems = expectedItems1
                .Zip(expectedItems2, (f, s) => new Log[] { f, s })
                .SelectMany(f => f);
            foreach (var item in zippedItems) {
                AssertSelectedNamespaceFromSelectedLog(item);
            }
        }

        [Test]
        public async Task Can_unselect_and_unhighlight_last_selected_namespace_by_unselecting_log() {
            var level = LoggingLevel.TRACE;
            sut.SelectedInitialLogLevel = level;

            await AddItemsToSutAsync();
            var expectedItems = GetExpectedItemsFromLevel(level);

            foreach (var item in expectedItems) {
                var last = AssertSelectedNamespaceFromSelectedLog(item);

                sut.SelectedLog = null;

                sut.SelectedNamespace.Should().BeNull();
                last.IsHighlighted.Should().BeFalse();
            }
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.AllLogLevels))]
        public async Task Can_deactivate_all_applications_at_once(LoggingLevel level) {
            sut.SelectedInitialLogLevel = level;

            (var expectedItems1, var expectedItems2) = await AddItemsOneTwoDifferentAppsToSutAsync(level);

            var actual = sut.Applications.All(app => app.IsActive);
            actual.Should().BeTrue();
            AssertLogs(expectedItems2, expectedItems1);

            sut.DeactivateAllApplicationsCommand.Execute(null);

            actual = sut.Applications.All(app => !app.IsActive);
            actual.Should().BeTrue();
            sut.Logs.Should().BeEmpty();
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.AllLogLevels))]
        public async Task Can_clear_all_application_logs_and_namespace_data_at_once(LoggingLevel level) {
            sut.SelectedInitialLogLevel = level;

            (var expectedItems1, var expectedItems2) = await AddItemsOneTwoDifferentAppsToSutAsync(level);

            sut.Applications.Should().NotBeEmpty();
            sut.Applications.All(app => app.IsActive).Should().BeTrue();
            sut.Namespaces.Should().NotBeEmpty();
            AssertLogs(expectedItems2, expectedItems1);

            sut.ClearLogsCommand.Execute(null);

            sut.Applications.Should().NotBeEmpty();
            sut.Applications.All(app => app.IsActive && !app.HasLogs).Should().BeTrue();
            sut.Namespaces.Should().NotBeEmpty();
            sut.AllNamespaces().All(IsNamespaceDataEmpty).Should().BeTrue();
            sut.Logs.Should().BeEmpty();
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.AllLogLevels))]
        public async Task Can_clear_everything(LoggingLevel level) {
            sut.SelectedInitialLogLevel = level;

            (var expectedItems1, var expectedItems2) = await AddItemsOneTwoDifferentAppsToSutAsync(level);

            sut.Applications.Should().HaveCount(2);
            sut.Namespaces.Should().HaveCount(2);
            AssertLogs(expectedItems2, expectedItems1);

            sut.ClearAllCommand.Execute(null);

            sut.Applications.Should().BeEmpty();
            sut.Namespaces.Should().BeEmpty();
            sut.Logs.Should().BeEmpty();
        }

        [Test, Apartment(ApartmentState.STA)]
        public void Can_copy_present_exception_from_selected_log() {
            var expectedItems = GetExpectedItemsFromLevel(LoggingLevel.TRACE).ToArray();
            expectedItems.Should().HaveCount(6);

            for (int i = 0; i < expectedItems.Length; i++) {
                var current = GetViewModel(expectedItems[i]);
                var expected = EXCEPTION_MESSAGES[current.Level];
                current.Exception.Should().Be(expected);

                var canCopy = !string.IsNullOrEmpty(expected);
                sut.SelectedLog = current;
                sut.CopySelectedLogExceptionCommand.CanExecute(null).Should().Be(canCopy);

                if (canCopy) {
                    sut.CopySelectedLogExceptionCommand.Execute(null);

                    Clipboard.GetText().Should().Be(expected);
                }
            }
        }

        [Test, Apartment(ApartmentState.STA)]
        public void Can_copy_message_from_selected_log() {
            AssertCanCopySelectedLog(checkMessageOnly: true);
        }

        [Test, Apartment(ApartmentState.STA)]
        public void Can_copy_selected_log() {
            AssertCanCopySelectedLog(checkMessageOnly: false);
        }

        private async Task AssertOrderLevelItemsAsync(LoggingLevel level) {
            var expectedItems = GetExpectedItemsFromLevel(level);
            sut.SelectedInitialLogLevel = level;

            await AddItemsToSutAsync();

            AssertLogs(expectedItems);
            AssertApplicationAndNamespaces(level);
        }

        private async Task AssertOrderLevelSearchItemsAsync(LoggingLevel level) {
            sut.SelectedInitialLogLevel = level;
            SetSearch("Two", false);

            (_, var expectedItems2, _) = await AddItemsOneTwoThreeToSutAsync(level);

            AssertLogs(expectedItems2);
        }

        private async Task AssertOrderLevelSearchInvertedItemsAsync(LoggingLevel level) {
            sut.SelectedInitialLogLevel = level;
            SetSearch("Two", true);

            (var expectedItems1, _, var expectedItems3) = await AddItemsOneTwoThreeToSutAsync(level);

            AssertLogs(expectedItems3, expectedItems1);
        }

        private void AssertLogs(params IEnumerable<Log>[] expected) =>
            sut.Logs.Should().BeEquivalentTo(expected.SelectMany(l => l), c => c.WithStrictOrdering());

        private void AssertApplicationAndNamespaces(params LoggingLevel[] levels) {
            var expectedCount = levels.Length;
            sut.Applications.Should().HaveCount(expectedCount);
            sut.Namespaces.Should().HaveCount(expectedCount);

            for (int i = 0; i < expectedCount; i++) {
                var appName = APP_NAMES[i + 1];
                var app = sut.Applications[i];
                app.Name.Should().Be(appName);
                app.SelectedMinLogLevel.Should().Be(levels[i]);

                var nsApp = sut.Namespaces[i];
                nsApp.Name.Should().Be(appName);
                nsApp.Fullname.Should().Be(appName);
                nsApp.Count.Should().Be(0);
                nsApp.Parent.Should().BeNull();
                nsApp.Children.Should().HaveCount(1);
                AssertNamespaceLevelCounts(nsApp, expectZero: true);

                var ns = nsApp.Children[0];
                ns.Name.Should().Be(NAMESPACE_NAME);
                ns.Fullname.Should().StartWith(appName).And.EndWith(NAMESPACE_NAME);
                ns.Count.Should().Be(6);
                ns.Parent.Should().Be(nsApp);
                ns.Children.Should().BeEmpty();
                AssertNamespaceLevelCounts(ns, expectZero: false);
            }
        }

        private NamespaceViewModel AssertSelectedNamespaceFromSelectedLog(Log item) {
            var current = GetViewModel(item);
            var expected = $"{current.Application}{Constants.NAMESPACE_SPLITTER}{current.Namespace}";
            var last = sut.SelectedNamespace;

            sut.SelectedLog = current;
            var actual = sut.SelectedNamespace!;

            if (last is not null && last != actual) {
                last.IsHighlighted.Should().BeFalse();
            }
            actual.Should().NotBeNull();
            actual.IsHighlighted.Should().BeTrue();
            actual.Name.Should().Be(current.Namespace);
            actual.Fullname.Should().Be(expected);

            return actual;
        }

        private void AssertCanCopySelectedLog(bool checkMessageOnly = false) {
            var command = checkMessageOnly ? sut.CopySelectedLogMessageCommand : sut.CopySelectedLogCommand;
            var expectedItems = GetExpectedItemsFromLevel(LoggingLevel.TRACE).ToArray();
            expectedItems.Should().HaveCount(6);

            for (int i = 0; i < expectedItems.Length; i++) {
                var current = GetViewModel(expectedItems[i]);
                var expected = checkMessageOnly ? current.Message : current.ToString();

                sut.SelectedLog = current;
                command.CanExecute(null).Should().BeTrue();

                command.Execute(null);

                Clipboard.GetText().Should().Be(expected);
            }
        }

        private static void AssertNamespaceLevelCounts(NamespaceViewModel ns, bool expectZero = false) {
            foreach (var l in LoggingLevel.GetAllLogLevels()) {
                var actual = GetLevelCount(l, ns);
                var expected = expectZero ? 0 : 1;
                actual.Should().Be(expected);
            }
        }

        private static bool IsNamespaceDataEmpty(NamespaceViewModel ns) =>
            ns.Count == 0 &&
            ns.CountTrace == 0 &&
            ns.CountDebug == 0 &&
            ns.CountInfo == 0 &&
            ns.CountWarn == 0 &&
            ns.CountError == 0 &&
            ns.CountFatal == 0 &&
            !ns.IsHighlighted;

        private static int GetLevelCount(LoggingLevel level, NamespaceViewModel ns) =>
            level switch {
                var l when l == LoggingLevel.TRACE => ns.CountTrace,
                var l when l == LoggingLevel.DEBUG => ns.CountDebug,
                var l when l == LoggingLevel.INFO => ns.CountInfo,
                var l when l == LoggingLevel.WARN => ns.CountWarn,
                var l when l == LoggingLevel.ERROR => ns.CountError,
                var l when l == LoggingLevel.FATAL => ns.CountFatal,
                _ => -1,
            };

        private IEnumerable<Log> GetExpectedItemsFromLevel(LoggingLevel level, IEnumerable<Log>? items = null) =>
            level == LoggingLevel.NOT_SET
            ? []
            : (items ?? testItems).TakeWhile(item => item.Level >= level);

        private async Task<IEnumerable<Log>> AddItemsToSutAsync() {
            await AddItemsReversedAsync(testItems);
            return testItems;
        }

        private async Task<IEnumerable<Log>> AddItemsToSutAsync(int tsOffset, string message = "Two", int appId = 1) {
            var ts = DateTimeOffset.Now;
            var itemV2 = Log(LoggingLevel.TRACE, ts.AddMinutes(tsOffset++), message, appId);
            var itemD2 = Log(LoggingLevel.DEBUG, ts.AddMinutes(tsOffset++), message, appId);
            var itemI2 = Log(LoggingLevel.INFO, ts.AddMinutes(tsOffset++), message, appId);
            var itemW2 = Log(LoggingLevel.WARN, ts.AddMinutes(tsOffset++), message, appId);
            var itemE2 = Log(LoggingLevel.ERROR, ts.AddMinutes(tsOffset++), message, appId);
            var itemF2 = Log(LoggingLevel.FATAL, ts.AddMinutes(tsOffset++), message, appId);

            IEnumerable<Log> items = [itemF2, itemE2, itemW2, itemI2, itemD2, itemV2];
            await AddItemsReversedAsync(items);
            return items;
        }

        private async Task<(IEnumerable<Log>, IEnumerable<Log>, IEnumerable<Log>)> AddItemsOneTwoThreeToSutAsync(LoggingLevel level) {
            // message contains "One"
            var allItems1 = await AddItemsToSutAsync();
            var expectedItems1 = GetExpectedItemsFromLevel(level, allItems1);

            // message contains "Two"
            var allItems2 = await AddItemsToSutAsync(11);
            var expectedItems2 = GetExpectedItemsFromLevel(level, allItems2);

            // message contains "Three"
            var allItems3 = await AddItemsToSutAsync(21, "Three");
            var expectedItems3 = GetExpectedItemsFromLevel(level, allItems3);

            return (expectedItems1, expectedItems2, expectedItems3);
        }

        private async Task<(IEnumerable<Log>, IEnumerable<Log>)> AddItemsOneTwoDifferentAppsToSutAsync(LoggingLevel level, Func<LoggingLevel>? changeLevel = null) {
            // app 1
            var allItems1 = await AddItemsToSutAsync();
            var expectedItems1 = GetExpectedItemsFromLevel(level, allItems1);

            if (changeLevel is not null) {
                level = changeLevel();
                sut.SelectedInitialLogLevel = level;
            }

            // app 2
            var allItems2 = await AddItemsToSutAsync(11, appId: 2);
            var expectedItems2 = GetExpectedItemsFromLevel(level, allItems2);

            return (expectedItems1, expectedItems2);
        }

        private async Task AddItemsReversedAsync(IEnumerable<Log> items) {
            var itemCount = items.Count();
            foreach (var item in items.Reverse()) {
                // items are added in front without timestamp ordering
                receivedLogs.Enqueue(item);
            }

            while (true) {
                timeProvider.Advance(TimeSpan.FromSeconds(1));
                await Task.Yield();

                var processedItemCount = logListener.SumFromMessage(LogLevel.Information, RegexReceivedItems(), "count");
                if (processedItemCount == itemCount) {
                    logListener.Reset();
                    break;
                }
            }
        }

        private LoginatorViewModel Sut() {
            var config = new Configuration {
                LogType = LogType.Chainsaw,
                PortLogcat = 7081,
                LogTimeFormat = LogTimeFormat.DoNotChange,
            };
            var configDao = A.Fake<IOptionsMonitor<Configuration>>();
            A.CallTo(() => configDao.CurrentValue).Returns(config);

            var serviceProvider = A.Fake<IKeyedServiceProvider>();
            IoC.ServiceProvider = serviceProvider;
            A.CallTo(() => serviceProvider.GetRequiredKeyedService(typeof(IReceiver), config.LogType)).Returns(receiver);
            A.CallTo(() => receiver.ReadAsync(A<int>._, A<CancellationToken>._)).Returns(receivedLogs);

            var stopwatch = A.Fake<IStopwatch>();
            var logger = A.Fake<ILogger<LoginatorViewModel>>();
            A.CallTo(() => logger.IsEnabled(A<LogLevel>._)).Returns(true);
            Fake.GetFakeManager(logger).AddInterceptionListener(logListener);

            var sut = new LoginatorViewModel(configDao, stopwatch, timeProvider, new DispatcherMock(), logger);
            sut.StartListener();

            return sut;
        }

        private void SetSearch(string? criteria = null, bool isInverted = false) {
            sut.Search.Criteria = criteria;
            sut.Search.IsInverted = isInverted;
            sut.Search.UpdateCommand.Execute(null);
        }

        private static Log Log(LoggingLevel level, DateTimeOffset ts, string message = "One", int appId = 1) =>
            new() {
                Application = APP_NAMES[appId],
                Namespace = NAMESPACE_NAME,
                Level = level,
                Timestamp = ts,
                Message = $"start {message} end",
                Exception = EXCEPTION_MESSAGES[level],
            };

        private static LogViewModel GetViewModel(Log log) => new(log);

        [GeneratedRegex(@"((process)|(discard)).*\s+(?<count>\d+)\s+.*items", RegexOptions.IgnoreCase, "de-AT")]
        private static partial Regex RegexReceivedItems();
    }
}