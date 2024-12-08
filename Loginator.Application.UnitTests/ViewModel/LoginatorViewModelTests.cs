// Copyright (C) 2024 Claudia Wagner

using FakeItEasy;
using FluentAssertions;
using Loginator.Application.Option;
using Loginator.Application.Service;
using Loginator.Application.ViewModel;
using Loginator.Domain.Model;
using Loginator.Domain.Server;
using Loginator.Infrastructure.Option;
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
using static Loginator.Domain.Common.Constants;
using LogLevel = Loginator.Domain.Model.LogLevel;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Loginator.Application.UnitTests.ViewModel {

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

        private static readonly Dictionary<LogLevel, string?> EXCEPTION_MESSAGES = new() {
            { LogLevel.NOT_SET, null },
            { LogLevel.TRACE, null },
            { LogLevel.DEBUG, null },
            { LogLevel.INFO, null },
            { LogLevel.WARN, "Test warning issued" },
            { LogLevel.ERROR, "Test exception happened" },
            { LogLevel.FATAL, "Test fatality happened" }
        };

        private readonly LoginatorViewModel sut;
        private readonly FakeTimeProvider timeProvider;
        private readonly Action<string?> clipboardMock;
        private readonly LogListener logListener = new();
        private readonly AsyncEnumerableQueue<Log> receivedLogs;

        private readonly IEnumerable<Log> testItems;

        public LoginatorViewModelTests() {
            timeProvider = new FakeTimeProvider();
            clipboardMock = A.Fake<Action<string?>>();
            receivedLogs = new();

            sut = Sut();

            var ts = DateTimeOffset.Now;
            var itemV = Log(LogLevel.TRACE, ts.AddMinutes(1));
            var itemD = Log(LogLevel.DEBUG, ts.AddMinutes(2));
            var itemI = Log(LogLevel.INFO, ts.AddMinutes(3));
            var itemW = Log(LogLevel.WARN, ts.AddMinutes(4));
            var itemE = Log(LogLevel.ERROR, ts.AddMinutes(5));
            var itemF = Log(LogLevel.FATAL, ts.AddMinutes(6));

            testItems = [itemF, itemE, itemW, itemI, itemD, itemV];
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() {
            receivedLogs.IsCompleted = true;
            sut.Dispose();
        }

        [Test]
        public void Can_create_sut() {
            var configDao = A.Fake<IOptionsMonitor<ApplicationOptions>>();
            var stopwatch = A.Fake<IStopwatch>();
            var logger = A.Fake<ILogger<LoginatorViewModel>>();

            var sut = new LoginatorViewModel(configDao, timeProvider, stopwatch, new DispatcherMock(), logger);

            sut.IsActive.Should().BeTrue();
            sut.NumberOfLogsPerLevel.Should().BeGreaterThan(100);
            sut.Search.Should().NotBeNull();
            sut.SelectedInitialLogLevel.Should().NotBe(LogLevel.NOT_SET);
            sut.SelectedLog.Should().BeNull();
            sut.SelectedNamespace.Should().BeNull();
            sut.HighlightedNamespace.Should().BeNull();
            sut.Logs.Should().BeEmpty();
            sut.Namespaces.Should().BeEmpty();
            sut.Applications.Should().BeEmpty();
            sut.Dispose();
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public async Task Can_show_logs_for_selected_level(LogLevel level) {
            await AssertOrderLevelItemsAsync(level);
        }

        [Test]
        public async Task Cannot_show_logs_if_selected_level_is_invalid() {
            await AssertOrderLevelItemsAsync(LogLevel.NOT_SET);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.AllLogLevels))]
        public async Task Can_show_logs_for_selected_level_change(LogLevel level) {
            var newLevel = level == LogLevel.FATAL
                ? LogLevel.NOT_SET
                : LogLevel.FromId(level.Id + 1)!;
            newLevel.Should().NotBeNull();
            sut.SelectedInitialLogLevel = level;

            (var expectedItems1, var expectedItems2) = await AddItemsOneTwoDifferentAppsToSutAsync(level, () => newLevel);

            AssertLogs(expectedItems2, expectedItems1);
            AssertApplicationAndNamespaces(level, newLevel);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public async Task Can_stop_and_restart_adding_logs_by_changing_active(LogLevel level) {
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
        public async Task Can_remove_surplus_logs(LogLevel level) {
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
        public async Task Can_show_logs_for_present_search_options(LogLevel level) {
            await AssertOrderLevelSearchItemsAsync(level);
        }

        [TestCase]
        public async Task Cannot_show_logs_for_present_search_options_if_level_is_invalid() {
            await AssertOrderLevelSearchItemsAsync(LogLevel.NOT_SET);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public async Task Can_show_logs_for_present_search_options_with_inversion(LogLevel level) {
            await AssertOrderLevelSearchInvertedItemsAsync(level);
        }

        [TestCase]
        public async Task Cannot_show_logs_for_present_search_options_with_inversion_if_level_is_invalid() {
            await AssertOrderLevelSearchInvertedItemsAsync(LogLevel.NOT_SET);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.AllLogLevels))]
        public async Task Can_show_logs_for_updated_search_options(LogLevel level) {
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
            sut.SelectedInitialLogLevel = LogLevel.NOT_SET;

            _ = await AddItemsOneTwoThreeToSutAsync(LogLevel.NOT_SET);

            sut.Logs.Should().BeEmpty();

            SetSearch("Two", false);
            sut.Logs.Should().BeEmpty();

            SetSearch("Two", true);
            sut.Logs.Should().BeEmpty();

            SetSearch();
            sut.Logs.Should().BeEmpty();
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public async Task Can_highlight_namespace_by_selecting_log(LogLevel level) {
            sut.SelectedInitialLogLevel = level;

            (var expectedItems1, var expectedItems2) = await AddItemsOneTwoDifferentAppsToSutAsync(level);

            var zippedItems = expectedItems1
                .Zip(expectedItems2, (f, s) => new Log[] { f, s })
                .SelectMany(f => f);
            foreach (var item in zippedItems) {
                AssertHighlightedNamespaceFromSelectedLog(item);
            }
        }

        [Test]
        public async Task Can_unhighlight_last_selected_namespace_by_unselecting_log() {
            var level = LogLevel.TRACE;
            sut.SelectedInitialLogLevel = level;

            await AddItemsToSutAsync();
            var expectedItems = GetExpectedItemsFromLevel(level);

            foreach (var item in expectedItems) {
                var last = AssertHighlightedNamespaceFromSelectedLog(item);

                sut.SelectedLog = null;

                sut.HighlightedNamespace.Should().BeNull();
                last.IsHighlighted.Should().BeFalse();
            }
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.AllLogLevels))]
        public async Task Can_deactivate_all_applications_at_once(LogLevel level) {
            sut.SelectedInitialLogLevel = level;

            (var expectedItems1, var expectedItems2) = await AddItemsOneTwoDifferentAppsToSutAsync(level);

            var actual = sut.Applications.All(app => app.IsActive);
            actual.Should().BeTrue();
            AssertLogs(expectedItems2, expectedItems1);

            sut.ActivateAllApplicationsCommand.Execute(false);

            actual = sut.Applications.All(app => !app.IsActive);
            actual.Should().BeTrue();
            sut.Logs.Should().BeEmpty();
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.AllLogLevels))]
        public async Task Can_clear_all_application_logs_and_namespace_data_at_once(LogLevel level) {
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
        public async Task Can_clear_everything(LogLevel level) {
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
            var expectedItems = GetExpectedItemsFromLevel(LogLevel.TRACE).ToArray();
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
                    AssertCopiedToClipboard(expected);
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

        private async Task AssertOrderLevelItemsAsync(LogLevel level) {
            var expectedItems = GetExpectedItemsFromLevel(level);
            sut.SelectedInitialLogLevel = level;

            await AddItemsToSutAsync();

            AssertLogs(expectedItems);
            AssertApplicationAndNamespaces(level);
        }

        private async Task AssertOrderLevelSearchItemsAsync(LogLevel level) {
            sut.SelectedInitialLogLevel = level;
            SetSearch("Two", false);

            (_, var expectedItems2, _) = await AddItemsOneTwoThreeToSutAsync(level);

            AssertLogs(expectedItems2);
        }

        private async Task AssertOrderLevelSearchInvertedItemsAsync(LogLevel level) {
            sut.SelectedInitialLogLevel = level;
            SetSearch("Two", true);

            (var expectedItems1, _, var expectedItems3) = await AddItemsOneTwoThreeToSutAsync(level);

            AssertLogs(expectedItems3, expectedItems1);
        }

        private void AssertLogs(params IEnumerable<Log>[] expected) =>
            sut.Logs.Should().BeEquivalentTo(expected.SelectMany(GetViewModels), c => c.WithStrictOrdering());

        private void AssertApplicationAndNamespaces(params LogLevel[] levels) {
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

        private NamespaceViewModel AssertHighlightedNamespaceFromSelectedLog(Log item) {
            var current = GetViewModel(item);
            var expected = $"{current.Application}{NamespaceSplitter}{current.Namespace}";
            var last = sut.HighlightedNamespace;

            sut.SelectedLog = current;
            var actual = sut.HighlightedNamespace!;

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
            var expectedItems = GetExpectedItemsFromLevel(LogLevel.TRACE).ToArray();
            expectedItems.Should().HaveCount(6);

            for (int i = 0; i < expectedItems.Length; i++) {
                var current = GetViewModel(expectedItems[i]);
                var expected = checkMessageOnly ? current.Message : current.ToString();

                sut.SelectedLog = current;
                command.CanExecute(null).Should().BeTrue();

                command.Execute(null);
                AssertCopiedToClipboard(expected);
            }
        }

        private void AssertCopiedToClipboard(string? expected) {
            A.CallTo(() => clipboardMock(A<string>.That.Matches(s => s == expected))).MustHaveHappened();
            Fake.ClearRecordedCalls(clipboardMock);
        }

        private static void AssertNamespaceLevelCounts(NamespaceViewModel ns, bool expectZero = false) {
            foreach (var l in LogLevel.AllLogLevels) {
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

        private static int GetLevelCount(LogLevel level, NamespaceViewModel ns) =>
            level switch {
                var l when l == LogLevel.TRACE => ns.CountTrace,
                var l when l == LogLevel.DEBUG => ns.CountDebug,
                var l when l == LogLevel.INFO => ns.CountInfo,
                var l when l == LogLevel.WARN => ns.CountWarn,
                var l when l == LogLevel.ERROR => ns.CountError,
                var l when l == LogLevel.FATAL => ns.CountFatal,
                _ => -1,
            };

        private IEnumerable<Log> GetExpectedItemsFromLevel(LogLevel level, IEnumerable<Log>? items = null) =>
            level == LogLevel.NOT_SET
            ? []
            : (items ?? testItems).TakeWhile(item => item.Level >= level);

        private async Task<IEnumerable<Log>> AddItemsToSutAsync() {
            await AddItemsReversedAsync(testItems);
            return testItems;
        }

        private async Task<IEnumerable<Log>> AddItemsToSutAsync(int tsOffset, string message = "Two", int appId = 1) {
            var ts = DateTimeOffset.Now;
            var itemV2 = Log(LogLevel.TRACE, ts.AddMinutes(tsOffset++), message, appId);
            var itemD2 = Log(LogLevel.DEBUG, ts.AddMinutes(tsOffset++), message, appId);
            var itemI2 = Log(LogLevel.INFO, ts.AddMinutes(tsOffset++), message, appId);
            var itemW2 = Log(LogLevel.WARN, ts.AddMinutes(tsOffset++), message, appId);
            var itemE2 = Log(LogLevel.ERROR, ts.AddMinutes(tsOffset++), message, appId);
            var itemF2 = Log(LogLevel.FATAL, ts.AddMinutes(tsOffset++), message, appId);

            IEnumerable<Log> items = [itemF2, itemE2, itemW2, itemI2, itemD2, itemV2];
            await AddItemsReversedAsync(items);
            return items;
        }

        private async Task<(IEnumerable<Log>, IEnumerable<Log>, IEnumerable<Log>)> AddItemsOneTwoThreeToSutAsync(LogLevel level) {
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

        private async Task<(IEnumerable<Log>, IEnumerable<Log>)> AddItemsOneTwoDifferentAppsToSutAsync(LogLevel level, Func<LogLevel>? changeLevel = null) {
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

                var processedItemCount = logListener.SumFromMessage(MsLogLevel.Information, ReceivedItemsRegex(), "count");
                if (processedItemCount == itemCount) {
                    logListener.Reset();
                    break;
                }
            }
        }

        private LoginatorViewModel Sut() {
            var config = new ApplicationOptions {
                ConnectionType = ConnectionType.Udp,
                LogType = LogType.Log4j,
                Port = 7081,
                LogTimeFormat = LogTimeFormat.DoNotChange,
            };
            var configDao = A.Fake<IOptionsMonitor<ApplicationOptions>>();
            A.CallTo(() => configDao.CurrentValue).Returns(config);

            var receiver = A.Fake<ILogRepository>();
            var serviceProvider = A.Fake<IKeyedServiceProvider>();
            IoC.ServiceProvider = serviceProvider;
            A.CallTo(() => serviceProvider.GetRequiredKeyedService(typeof(ILogRepository), A<object?>._)).Returns(receiver);
            A.CallTo(() => receiver.GetEnumerableAsync(A<int>._, A<CancellationToken>._)).Returns(receivedLogs);

            var stopwatch = A.Fake<IStopwatch>();
            var logger = A.Fake<ILogger<LoginatorViewModel>>();
            A.CallTo(() => logger.IsEnabled(A<MsLogLevel>._)).Returns(true);
            Fake.GetFakeManager(logger).AddInterceptionListener(logListener);

            var sut = new LoginatorViewModel(configDao, timeProvider, stopwatch, new DispatcherMock(), logger) {
                OnCopyToClipboard = clipboardMock
            };
            sut.StartMessageProcessing();

            return sut;
        }

        private void SetSearch(string? criteria = null, bool isInverted = false) {
            sut.Search.Criteria = criteria;
            sut.Search.IsInverted = isInverted;
            sut.Search.UpdateCommand.Execute(null);
        }

        private static Log Log(LogLevel level, DateTimeOffset ts, string message = "One", int appId = 1) =>
            new() {
                Application = APP_NAMES[appId],
                Namespace = NAMESPACE_NAME,
                Level = level,
                Timestamp = ts,
                Message = $"start {message} end",
                Exception = EXCEPTION_MESSAGES[level],
            };

        private static LogViewModel GetViewModel(Log log) =>
            new(log);

        private IEnumerable<LogViewModel> GetViewModels(IEnumerable<Log> logs) =>
            logs.Select(GetViewModel);

        [GeneratedRegex(@"((process)|(discard)).*\s+(?<count>\d+)\s+.*items", RegexOptions.IgnoreCase, "de-AT")]
        private static partial Regex ReceivedItemsRegex();
    }
}