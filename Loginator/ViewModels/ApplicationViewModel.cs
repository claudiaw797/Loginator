﻿using Backend.Model;
using Common;
using CommunityToolkit.Mvvm.ComponentModel;
using Loginator.Collections;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Loginator.ViewModels {

    /// <summary>
    /// If you add a new function / filter assure the following
    /// * Check that application is active: IsActive
    /// * Check that you got logs from selected loglevel: GetLogsFromLevel()
    /// * Check if the namespace is active: IsNamespaceActive()
    /// * Check if the search criteria match: IsSearchCriteriaMatch()
    /// </summary>
    public partial class ApplicationViewModel : ObservableObject {

        public string Name { get; private set; }
        public IReadOnlyList<LoggingLevel> LogLevels { get; } = [.. LoggingLevel.GetAllLogLevels().Order()];

        private OrderedObservableCollection Logs { get; set; }
        private List<LogViewModel> LogsTrace { get; set; }
        private List<LogViewModel> LogsDebug { get; set; }
        private List<LogViewModel> LogsInfo { get; set; }
        private List<LogViewModel> LogsWarn { get; set; }
        private List<LogViewModel> LogsError { get; set; }
        private List<LogViewModel> LogsFatal { get; set; }
        private ObservableCollection<NamespaceViewModel> Namespaces { get; set; }
        private ILogger Logger { get; set; }

        [ObservableProperty]
        private LoggingLevel selectedMinLogLevel;

        [ObservableProperty]
        private bool isActive;

        [ObservableProperty]
        private int maxNumberOfLogsPerLevel;

        [ObservableProperty]
        private SearchOptions searchOptions;

        partial void OnSelectedMinLogLevelChanged(LoggingLevel? oldValue, LoggingLevel newValue) =>
            ExecuteLocked(() => UpdateByLogLevelChange(oldValue, newValue));

        partial void OnIsActiveChanged(bool oldValue, bool newValue) =>
            ExecuteLocked(() => UpdateByActiveChange(oldValue, newValue));

        partial void OnMaxNumberOfLogsPerLevelChanged(int oldValue, int newValue) =>
            ExecuteLocked(() => UpdateMaxNumberOfLogs(oldValue, newValue));

        partial void OnSearchOptionsChanged(SearchOptions? oldValue, SearchOptions newValue) =>
            ExecuteLocked(() => UpdateSearchCriteria(oldValue, newValue));

        public ApplicationViewModel(
            string name,
            OrderedObservableCollection logs,
            ObservableCollection<NamespaceViewModel> namespaces,
            LoggingLevel initialLogLevel) {
            Logger = LogManager.GetCurrentClassLogger();
            Name = name;
            Namespaces = namespaces;
            Logs = logs;

            selectedMinLogLevel = initialLogLevel;
            isActive = true;
            maxNumberOfLogsPerLevel = Constants.DEFAULT_MAX_NUMBER_OF_LOGS_PER_LEVEL;
            searchOptions = new();

            LogsTrace = [];
            LogsDebug = [];
            LogsInfo = [];
            LogsWarn = [];
            LogsError = [];
            LogsFatal = [];
        }

        public void ClearLogs() {
            LogsTrace = [];
            LogsDebug = [];
            LogsInfo = [];
            LogsWarn = [];
            LogsError = [];
            LogsFatal = [];
        }

        public void UpdateByNamespaceChange(NamespaceViewModel ns) {
            if (!IsActive) {
                return;
            }

            var nsName = ns.Fullname;
            var logs = GetLogsFromLevel(SelectedMinLogLevel)
                .Where(m => $"{Name}{Constants.NAMESPACE_SPLITTER}{m.Namespace}" == nsName);

            if (ns.IsChecked)
                Logs.Add(logs, IsSearchCriteriaMatch);
            else
                Logs.Remove(logs);
        }

        public void AddLog(LogViewModel log) {
            var logToRemove = AddByLevelPossiblyRemovingFirst(log);

            if (IsActive &&
                SelectedMinLogLevel != LoggingLevel.NOT_SET &&
                log.Level >= SelectedMinLogLevel &&
                IsNamespaceActive(log) &&
                IsSearchCriteriaMatch(log)) {

                Logs.AddLeading(log);

                if (logToRemove is not null) {
                    Logs.Remove(logToRemove);
                }
            }
        }

        private void UpdateByLogLevelChange(LoggingLevel? oldLogLevel, LoggingLevel? newLogLevel) {
            if (!IsActive) {
                return;
            }

            var logs = LoggingLevel.GetLogLevelsBetween(ref oldLogLevel, ref newLogLevel)
                .SelectMany(l => GetLogsByLevel(l) ?? []);

            if (oldLogLevel > newLogLevel)
                Logs.Add(logs, IsNamespaceActiveSearchCriteriaMatch);
            else
                Logs.Remove(logs, IsNamespaceActiveSearchCriteriaMatch);
        }

        private void UpdateByActiveChange(bool oldIsActive, bool newIsActive) {
            if (oldIsActive == newIsActive) {
                return;
            }

            if (newIsActive)
                Logs.Add(GetLogsFromLevel(SelectedMinLogLevel), IsNamespaceActiveSearchCriteriaMatch);
            else
                Logs.Remove(GetLogsFromLevel(LoggingLevel.TRACE));
        }

        private void UpdateMaxNumberOfLogs(int oldMaxNumberOfLogs, int newMaxNumberOfLogs) {
            if (oldMaxNumberOfLogs <= newMaxNumberOfLogs) {
                return;
            }

            var logsToRemoveTrace = RemoveSurplus(LogsTrace);
            var logsToRemoveDebug = RemoveSurplus(LogsDebug);
            var logsToRemoveInfo = RemoveSurplus(LogsInfo);
            var logsToRemoveWarn = RemoveSurplus(LogsWarn);
            var logsToRemoveError = RemoveSurplus(LogsError);
            var logsToRemoveFatal = RemoveSurplus(LogsFatal);

            if (IsActive) {
                var logsToRemove = logsToRemoveTrace
                    .Concat(logsToRemoveDebug)
                    .Concat(logsToRemoveInfo)
                    .Concat(logsToRemoveWarn)
                    .Concat(logsToRemoveError)
                    .Concat(logsToRemoveFatal);

                Logs.Remove(logsToRemove, m =>
                    m.Level >= SelectedMinLogLevel &&
                    IsNamespaceActive(m) &&
                    IsSearchCriteriaMatch(m));
            }
        }

        private void UpdateSearchCriteria(SearchOptions? oldOptions, SearchOptions newOptions) {
            if (!IsActive || oldOptions == newOptions) {
                return;
            }

            var logs = GetLogsFromLevel(SelectedMinLogLevel);
            if (string.IsNullOrEmpty(newOptions.Criteria))
                Logs.Add(logs, IsNamespaceActive);
            else {
                var logsMatching = logs
                    .GroupBy(m => IsSearchCriteriaMatch(m))
                    .ToDictionary(g => g.Key);

                if (logsMatching.TryGetValue(true, out var logsToAdd))
                    Logs.Add(logsToAdd, IsNamespaceActive);
                if (logsMatching.TryGetValue(false, out var logsToRemove))
                    Logs.Remove(logsToRemove);
            }
        }

        private bool IsSearchCriteriaMatch(LogViewModel log) {
            try {
                var criteria = SearchOptions.Criteria;

                // Default
                if (string.IsNullOrEmpty(criteria)) {
                    return true;
                }

                // Search
                if ((!string.IsNullOrEmpty(log.Application) && log.Application.Contains(criteria, StringComparison.CurrentCultureIgnoreCase)) ||
                    (!string.IsNullOrEmpty(log.Namespace) && log.Namespace.Contains(criteria, StringComparison.CurrentCultureIgnoreCase)) ||
                    (!string.IsNullOrEmpty(log.Message) && log.Message.Contains(criteria, StringComparison.CurrentCultureIgnoreCase)) ||
                    (!string.IsNullOrEmpty(log.Exception) && log.Exception.Contains(criteria, StringComparison.CurrentCultureIgnoreCase))) {
                    return !SearchOptions.IsInverted;
                }
                return SearchOptions.IsInverted;
            }
            catch (Exception e) {
                Logger.Error(e, "Invalid search criteria");
                return false;
            }
        }

        private bool IsNamespaceActive(LogViewModel log) {
            // Try to get existing root namespace with name of application
            var nsApplication = Namespaces.FirstOrDefault(m => m.Name == log.Application);
            return nsApplication is not null && IsNamespaceActive(nsApplication, log.Namespace);
        }

        private static bool IsNamespaceActive(NamespaceViewModel parent, string suffix) {
            // Example: VerbTeX.View (Verbosus was processed before)
            var nsLogFull = suffix;
            // Example: VerbTeX
            var nsLogPart = nsLogFull?.Split([Constants.NAMESPACE_SPLITTER], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            // Try to get existing namespace with name VerbTeX
            var nsChild = parent.Children.FirstOrDefault(m => m.Name == nsLogPart);
            if (nsChild == null) {
                return false;
            }

            var index = nsLogFull is null ? -1 : nsLogFull.IndexOf(Constants.NAMESPACE_SPLITTER);
            return index >= 0
                ? IsNamespaceActive(nsChild, nsLogFull![(index + 1)..])
                : nsChild.IsChecked;
        }

        private bool IsNamespaceActiveSearchCriteriaMatch(LogViewModel log) =>
            IsNamespaceActive(log) && IsSearchCriteriaMatch(log);

        private List<LogViewModel>? GetLogsByLevel(LoggingLevel level) =>
            level switch {
                var l when l == LoggingLevel.TRACE => LogsTrace,
                var l when l == LoggingLevel.DEBUG => LogsDebug,
                var l when l == LoggingLevel.INFO => LogsInfo,
                var l when l == LoggingLevel.WARN => LogsWarn,
                var l when l == LoggingLevel.ERROR => LogsError,
                var l when l == LoggingLevel.FATAL => LogsFatal,
                _ => null,
            };

        private IEnumerable<LogViewModel> GetLogsFromLevel(LoggingLevel level) =>
            level switch {
                var l when l == LoggingLevel.TRACE => LogsTrace.Concat(LogsDebug).Concat(LogsInfo).Concat(LogsWarn).Concat(LogsError).Concat(LogsFatal),
                var l when l == LoggingLevel.DEBUG => LogsDebug.Concat(LogsInfo).Concat(LogsWarn).Concat(LogsError).Concat(LogsFatal),
                var l when l == LoggingLevel.INFO => LogsInfo.Concat(LogsWarn).Concat(LogsError).Concat(LogsFatal),
                var l when l == LoggingLevel.WARN => LogsWarn.Concat(LogsError).Concat(LogsFatal),
                var l when l == LoggingLevel.ERROR => LogsError.Concat(LogsFatal),
                var l when l == LoggingLevel.FATAL => LogsFatal,
                _ => [],
            };

        private LogViewModel? AddByLevelPossiblyRemovingFirst(LogViewModel log) {
            LogViewModel? logToRemove = null;

            var currentLevelLogs = GetLogsByLevel(log.Level);
            if (currentLevelLogs is not null) {
                currentLevelLogs.Add(log);

                if (currentLevelLogs.Count > MaxNumberOfLogsPerLevel) {
                    var last = currentLevelLogs.First();
                    currentLevelLogs.Remove(last);
                    logToRemove = last;
                }
            }

            return logToRemove;
        }

        private List<LogViewModel> RemoveSurplus(List<LogViewModel> levelLogs) {
            var count = levelLogs.Count - MaxNumberOfLogsPerLevel;
            if (count <= 0) {
                return [];
            }

            var logsToRemove = levelLogs.Take(count).ToList();
            levelLogs.RemoveRange(0, count);
            return logsToRemove;
        }

        private static void ExecuteLocked(Action action) {
            lock (ViewModelConstants.SYNC_OBJECT) {
                action();
            }
        }
    }
}
