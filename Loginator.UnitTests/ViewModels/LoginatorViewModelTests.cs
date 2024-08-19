using AutoMapper;
using Backend;
using Backend.Dao;
using Backend.Events;
using Backend.Model;
using Common;
using Common.Configuration;
using FakeItEasy;
using FluentAssertions;
using Loginator.Controls;
using Loginator.ViewModels;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;

namespace Loginator.UnitTests.ViewModels {

    /// <summary>
    /// Represents unit tests for <see cref="LoginatorViewModel"/>.
    /// </summary>
    [FixtureLifeCycle(LifeCycle.SingleInstance)]
    public class LoginatorViewModelTests {

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

        private static readonly TimeSpan TIME_INTERVAL_IN_MILLISECONDS = TimeSpan.FromSeconds(1.5);

        private readonly LoginatorViewModel sut;
        private readonly IReceiver receiver = A.Fake<IReceiver>();
        private EventHandler<LogReceivedEventArgs>? logReceivedEventHandler;
        private readonly IMapper mapper;
        private readonly FakeTimeProvider timeProvider;

        private readonly IEnumerable<Log> testItems;

        public LoginatorViewModelTests() {
            mapper = new MapperConfiguration(cfg => cfg.CreateMap<Log, LogViewModel>()).CreateMapper();
            timeProvider = new FakeTimeProvider();

            sut = Sut();

            DateTime ts = DateTime.Now;
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
            sut.Dispose();
        }

        [TearDown]
        public void TearDown() {
            sut.ClearAll();
            sut.UpdateNumberOfLogsPerLevelCommand.Execute(Constants.DEFAULT_MAX_NUMBER_OF_LOGS_PER_LEVEL);
            sut.IsActive = true;
            sut.SelectedInitialLogLevel = LoggingLevel.NOT_SET;
            sut.SelectedLog = null;
            SetSearch();
        }

        [Test]
        public void Can_create_sut() {
            var appConfig = A.Fake<IApplicationConfiguration>();
            var configDao = A.Fake<IConfigurationDao>();
            var mapper = A.Fake<IMapper>();
            var stopwatch = A.Fake<IStopwatch>();

            var sut = new LoginatorViewModel(appConfig, configDao, mapper, stopwatch, timeProvider);

            sut.IsActive.Should().BeTrue();
            sut.NumberOfLogsPerLevel.Should().BeGreaterThan(100);
            sut.Search.Should().NotBeNull();
            sut.SelectedInitialLogLevel.Should().NotBe(LoggingLevel.NOT_SET);
            sut.SelectedLog.Should().BeNull();
            sut.SelectedNamespaceViewModel.Should().BeNull();
            sut.Logs.Should().BeEmpty();
            sut.Namespaces.Should().BeEmpty();
            sut.Applications.Should().BeEmpty();
            sut.Dispose();
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public void Can_show_logs_for_selected_level(LoggingLevel level) {
            AssertOrderLevelItems(level);
        }

        [Test]
        public void Cannot_show_logs_if_selected_level_is_invalid() {
            AssertOrderLevelItems(LoggingLevel.NOT_SET);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.AllLogLevels))]
        public void Can_show_logs_for_selected_level_change(LoggingLevel level) {
            var newLevel = level == LoggingLevel.FATAL
                ? LoggingLevel.NOT_SET
                : LoggingLevel.FromId(level.Id + 1)!;
            newLevel.Should().NotBeNull();
            sut.SelectedInitialLogLevel = level;

            (var expectedItems1, var expectedItems2) = AddItemsOneTwoDifferentAppsToSut(level, () => newLevel);

            AssertLogs(expectedItems2, expectedItems1);
            AssertApplicationAndNamespaces(level, newLevel);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public void Can_stop_and_restart_adding_logs_by_changing_active(LoggingLevel level) {
            sut.SelectedInitialLogLevel = level;

            sut.IsActive = false;
            AddItemsToSut();
            AssertLogs();

            sut.IsActive = true;
            var allItems1 = AddItemsToSut();
            var expectedItems1 = GetExpectedItemsFromLevel(level, allItems1);

            sut.IsActive = false;
            AddItemsToSut(11);

            sut.IsActive = true;
            var allItems3 = AddItemsToSut(21, "Three");
            var expectedItems3 = GetExpectedItemsFromLevel(level, allItems3);

            AssertLogs(expectedItems3, expectedItems1);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public void Can_remove_surplus_logs(LoggingLevel level) {
            sut.SelectedInitialLogLevel = level;
            sut.UpdateNumberOfLogsPerLevelCommand.Execute(2);

            var allItems1 = AddItemsToSut();
            var expectedItems1 = GetExpectedItemsFromLevel(level, allItems1);

            var allItems2 = AddItemsToSut(11);
            var expectedItems2 = GetExpectedItemsFromLevel(level, allItems2);
            AssertLogs(expectedItems2, expectedItems1);

            var allItems3 = AddItemsToSut(21, "Three");
            var expectedItems3 = GetExpectedItemsFromLevel(level, allItems3);
            AssertLogs(expectedItems3, expectedItems2);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public void Can_show_logs_for_present_search_options(LoggingLevel level) {
            AssertOrderLevelSearchItems(level);
        }

        [TestCase]
        public void Cannot_show_logs_for_present_search_options_if_level_is_invalid() {
            AssertOrderLevelSearchItems(LoggingLevel.NOT_SET);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public void Can_show_logs_for_present_search_options_with_inversion(LoggingLevel level) {
            AssertOrderLevelSearchInvertedItems(level);
        }

        [TestCase]
        public void Cannot_show_logs_for_present_search_options_with_inversion_if_level_is_invalid() {
            AssertOrderLevelSearchInvertedItems(LoggingLevel.NOT_SET);
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.AllLogLevels))]
        public void Can_show_logs_for_updated_search_options(LoggingLevel level) {
            sut.SelectedInitialLogLevel = level;

            (var expectedItems1, var expectedItems2, var expectedItems3) = AddItemsOneTwoThreeToSut(level);

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
        public void Cannot_show_logs_for_updated_search_options_if_level_is_invalid() {
            sut.SelectedInitialLogLevel = LoggingLevel.NOT_SET;

            _ = AddItemsOneTwoThreeToSut(LoggingLevel.NOT_SET);

            sut.Logs.Should().BeEmpty();

            SetSearch("Two", false);
            sut.Logs.Should().BeEmpty();

            SetSearch("Two", true);
            sut.Logs.Should().BeEmpty();

            SetSearch();
            sut.Logs.Should().BeEmpty();
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.ValidLogLevels))]
        public void Can_select_and_highlight_namespace_by_selecting_log(LoggingLevel level) {
            sut.SelectedInitialLogLevel = level;

            (var expectedItems1, var expectedItems2) = AddItemsOneTwoDifferentAppsToSut(level);

            var zippedItems = expectedItems1
                .Zip(expectedItems2, (f, s) => new Log[] { f, s })
                .SelectMany(f => f);
            foreach (var item in zippedItems) {
                AssertSelectedNamespaceFromSelectedLog(item);
            }
        }

        [Test]
        public void Can_unselect_and_unhighlight_last_selected_namespace_by_unselecting_log() {
            var level = LoggingLevel.TRACE;
            sut.SelectedInitialLogLevel = level;

            AddItemsToSut();
            var expectedItems = GetExpectedItemsFromLevel(level);

            foreach (var item in expectedItems) {
                var last = AssertSelectedNamespaceFromSelectedLog(item);

                sut.SelectedLog = null;

                sut.SelectedNamespaceViewModel.Should().BeNull();
                last.IsHighlighted.Should().BeFalse();
            }
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.AllLogLevels))]
        public void Can_deactivate_all_applications_at_once(LoggingLevel level) {
            sut.SelectedInitialLogLevel = level;

            (var expectedItems1, var expectedItems2) = AddItemsOneTwoDifferentAppsToSut(level);

            var actual = sut.Applications.All(app => app.IsActive);
            actual.Should().BeTrue();
            AssertLogs(expectedItems2, expectedItems1);

            sut.UnselectAllCommand.Execute(null);

            actual = sut.Applications.All(app => !app.IsActive);
            actual.Should().BeTrue();
            sut.Logs.Should().BeEmpty();
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.AllLogLevels))]
        public void Can_clear_all_application_logs_and_namespace_data_at_once(LoggingLevel level) {
            sut.SelectedInitialLogLevel = level;

            (var expectedItems1, var expectedItems2) = AddItemsOneTwoDifferentAppsToSut(level);

            var actual = sut.Applications.All(app => app.IsActive);
            actual.Should().BeTrue();
            AssertLogs(expectedItems2, expectedItems1);

            sut.ClearLogsCommand.Execute(null);

            actual = sut.Applications.All(app => app.IsActive);
            actual.Should().BeTrue();
            actual = sut.Namespaces.Flatten(ns => ns.Children).All(IsNamespaceDataEmpty);
            actual.Should().BeTrue();
            sut.Logs.Should().BeEmpty();
        }

        [TestCaseSource(typeof(TestData), nameof(TestData.AllLogLevels))]
        public void Can_clear_everything(LoggingLevel level) {
            sut.SelectedInitialLogLevel = level;

            (var expectedItems1, var expectedItems2) = AddItemsOneTwoDifferentAppsToSut(level);

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

        private void AssertOrderLevelItems(LoggingLevel level) {
            var expectedItems = GetExpectedItemsFromLevel(level);
            sut.SelectedInitialLogLevel = level;

            AddItemsToSut();

            AssertLogs(expectedItems);
            AssertApplicationAndNamespaces(level);
        }

        private void AssertOrderLevelSearchItems(LoggingLevel level) {
            sut.SelectedInitialLogLevel = level;
            SetSearch("Two", false);

            (_, var expectedItems2, _) = AddItemsOneTwoThreeToSut(level);

            AssertLogs(expectedItems2);
        }

        private void AssertOrderLevelSearchInvertedItems(LoggingLevel level) {
            sut.SelectedInitialLogLevel = level;
            SetSearch("Two", true);

            (var expectedItems1, _, var expectedItems3) = AddItemsOneTwoThreeToSut(level);

            AssertLogs(expectedItems3, expectedItems1);
        }

        private void AssertLogs(params IEnumerable<Log>[] expected) =>
            sut.Logs.Should().BeEquivalentTo(expected.SelectMany(GetViewModels),
                c => c.WithStrictOrdering());

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
            var last = sut.SelectedNamespaceViewModel;

            sut.SelectedLog = current;
            var actual = sut.SelectedNamespaceViewModel!;

            if (last is not null && last != actual) {
                last.IsHighlighted.Should().BeFalse();
            }
            actual.Should().NotBeNull();
            actual.IsHighlighted.Should().BeTrue();
            actual.Name.Should().Be(current.Namespace);
            actual.Fullname.Should().Be(expected);

            return actual;
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

        private LogViewModel GetViewModel(Log log) =>
             mapper.Map<Log, LogViewModel>(log);

        private IEnumerable<LogViewModel> GetViewModels(IEnumerable<Log> logs) =>
            logs.Select(GetViewModel);

        private IEnumerable<Log> AddItemsToSut() {
            AddItemsReversed(testItems);
            timeProvider.Advance(TIME_INTERVAL_IN_MILLISECONDS);

            return testItems;
        }

        private IEnumerable<Log> AddItemsToSut(int tsOffset, string message = "Two", int appId = 1) {
            DateTime ts = DateTime.Now;
            var itemV2 = Log(LoggingLevel.TRACE, ts.AddMinutes(tsOffset++), message, appId);
            var itemD2 = Log(LoggingLevel.DEBUG, ts.AddMinutes(tsOffset++), message, appId);
            var itemI2 = Log(LoggingLevel.INFO, ts.AddMinutes(tsOffset++), message, appId);
            var itemW2 = Log(LoggingLevel.WARN, ts.AddMinutes(tsOffset++), message, appId);
            var itemE2 = Log(LoggingLevel.ERROR, ts.AddMinutes(tsOffset++), message, appId);
            var itemF2 = Log(LoggingLevel.FATAL, ts.AddMinutes(tsOffset++), message, appId);

            IEnumerable<Log> items = [itemF2, itemE2, itemW2, itemI2, itemD2, itemV2];
            AddItemsReversed(items);
            timeProvider.Advance(TIME_INTERVAL_IN_MILLISECONDS);

            return items;
        }

        private (IEnumerable<Log>, IEnumerable<Log>, IEnumerable<Log>) AddItemsOneTwoThreeToSut(LoggingLevel level) {
            // message contains "One"
            var allItems1 = AddItemsToSut();
            var expectedItems1 = GetExpectedItemsFromLevel(level, allItems1);

            // message contains "Two"
            var allItems2 = AddItemsToSut(11);
            var expectedItems2 = GetExpectedItemsFromLevel(level, allItems2);

            // message contains "Three"
            var allItems3 = AddItemsToSut(21, "Three");
            var expectedItems3 = GetExpectedItemsFromLevel(level, allItems3);

            return (expectedItems1, expectedItems2, expectedItems3);
        }

        private (IEnumerable<Log>, IEnumerable<Log>) AddItemsOneTwoDifferentAppsToSut(LoggingLevel level, Func<LoggingLevel>? changeLevel = null) {
            // app 1
            var allItems1 = AddItemsToSut();
            var expectedItems1 = GetExpectedItemsFromLevel(level, allItems1);

            if (changeLevel is not null) {
                level = changeLevel();
                sut.SelectedInitialLogLevel = level;
            }

            // app 2
            var allItems2 = AddItemsToSut(11, appId: 2);
            var expectedItems2 = GetExpectedItemsFromLevel(level, allItems2);

            return (expectedItems1, expectedItems2);
        }

        private void AddItemsReversed(IEnumerable<Log> items) {
            foreach (var item in items.Reverse()) {
                // items are added in front without timestamp ordering
                RaiseLogReceived(item);
            }
        }

        private LoginatorViewModel Sut() {
            var appConfig = A.Fake<IApplicationConfiguration>();
            A.CallTo(() => appConfig.IsMessageTraceEnabled).Returns(false);
            A.CallTo(() => appConfig.IsTimingTraceEnabled).Returns(false);

            var config = new Configuration {
                LogType = Common.LogType.CHAINSAW,
                PortChainsaw = 7071,
                PortLogcat = 7081,
                LogTimeFormat = LogTimeFormat.DO_NOT_CHANGE,
            };
            var configDao = A.Fake<IConfigurationDao>();
            A.CallTo(() => configDao.Read()).Returns(config);

            IoC.Container.Configure(m => m.For<IReceiver>().Singleton().Use(c => receiver));
            A.CallTo(receiver, EventAction.Add("LogReceived")).Invokes((EventHandler<LogReceivedEventArgs> h) => logReceivedEventHandler += h);
            A.CallTo(receiver, EventAction.Remove("LogReceived")).Invokes((EventHandler<LogReceivedEventArgs> h) => logReceivedEventHandler -= h);

            DispatcherHelper.Initialize();

            var stopwatch = A.Fake<IStopwatch>();
            var sut = new LoginatorViewModel(appConfig, configDao, mapper, stopwatch, timeProvider);
            sut.StartListener();

            return sut;
        }

        private void RaiseLogReceived(Log log) =>
            logReceivedEventHandler?.Invoke(receiver, new LogReceivedEventArgs(log));

        private void SetSearch(string? criteria = null, bool isInverted = false) {
            sut.Search.Criteria = criteria;
            sut.Search.IsInverted = isInverted;
            sut.Search.UpdateCommand.Execute(null);
        }

        private static Log Log(LoggingLevel level, DateTime ts, string message = "One", int appId = 1) =>
            new() {
                Application = APP_NAMES[appId],
                Namespace = NAMESPACE_NAME,
                Level = level,
                Timestamp = ts,
                Message = $"start {message} end",
                Exception = EXCEPTION_MESSAGES[level],
            };
    }
}