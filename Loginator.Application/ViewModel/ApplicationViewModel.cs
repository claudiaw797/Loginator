// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using CommunityToolkit.Mvvm.ComponentModel;
using Loginator.Application.Common;
using Loginator.Application.Model;
using Loginator.Domain.Model;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using static Loginator.Domain.Common.Constants;
using LogLevel = Loginator.Domain.Model.LogLevel;

namespace Loginator.Application.ViewModel {

    /// <summary>
    /// If you add a new function / filter assure the following
    /// * Check that application is active: IsActive
    /// * Check that you got logs from selected loglevel: GetLogsFromLevel()
    /// * Check if the namespace is active: IsNamespaceActive()
    /// * Check if the search criteria match: IsSearchCriteriaMatch()
    /// </summary>
    public partial class ApplicationViewModel : ObservableObject {

        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly List<Log> logsTrace = [];
        private readonly List<Log> logsDebug = [];
        private readonly List<Log> logsInfo = [];
        private readonly List<Log> logsWarn = [];
        private readonly List<Log> logsError = [];
        private readonly List<Log> logsFatal = [];
        private readonly OrderedObservableCollection logs;
        private readonly ObservableCollection<NamespaceViewModel> namespaces;

        internal ApplicationViewModel(
            string name,
            OrderedObservableCollection logs,
            ObservableCollection<NamespaceViewModel> namespaces,
            LogLevel initialLogLevel) {
            this.Name = name;
            this.logs = logs;
            this.namespaces = namespaces;

            selectedMinLogLevel = initialLogLevel;
            isActive = true;
            maxNumberOfLogsPerLevel = Constants.DefaultMaxNumberOfLogsPerLevel;
            searchOptions = new();
        }

        [ObservableProperty]
        private LogLevel selectedMinLogLevel;
        partial void OnSelectedMinLogLevelChanged(LogLevel? oldValue, LogLevel newValue) {
            lock (Constants.SyncObject) UpdateSelectedMinLogLevel(oldValue, newValue);
        }

        [ObservableProperty]
        private bool isActive;
        partial void OnIsActiveChanged(bool oldValue, bool newValue) {
            lock (Constants.SyncObject) UpdateIsActive(oldValue, newValue);
        }

        [ObservableProperty]
        private int maxNumberOfLogsPerLevel;
        partial void OnMaxNumberOfLogsPerLevelChanged(int oldValue, int newValue) {
            lock (Constants.SyncObject) UpdateMaxNumberOfLogs(oldValue, newValue);
        }

        [ObservableProperty]
        private SearchOptions searchOptions;
        partial void OnSearchOptionsChanged(SearchOptions? oldValue, SearchOptions newValue) {
            lock (Constants.SyncObject) UpdateSearchCriteria(oldValue, newValue);
        }

        public string Name { get; init; }

        public IReadOnlyList<LogLevel> LogLevels { get; } = [.. LogLevel.AllLogLevels.Order()];

        internal bool HasLogs => this.GetLogsFromLevel(LogLevel.TRACE).Any();

        internal void AddLog(Log log) {
            var logToRemove = this.AddByLevelPossiblyRemovingFirst(log);

            if (this.IsActive &&
                this.SelectedMinLogLevel != LogLevel.NOT_SET &&
                log.Level >= this.SelectedMinLogLevel &&
                this.IsNamespaceActive(log) &&
                this.IsSearchCriteriaMatch(log)) {

                logs.AddLeading(log);

                if (logToRemove is not null) {
                    logs.Remove(logToRemove);
                }
            }
        }

        internal void ClearLogs() {
            logsTrace.Clear();
            logsDebug.Clear();
            logsInfo.Clear();
            logsWarn.Clear();
            logsError.Clear();
            logsFatal.Clear();
        }

        internal void OnNamespaceIsActiveChanged(NamespaceViewModel ns) {
            if (!this.IsActive) {
                return;
            }

            var nsName = ns.Fullname;
            var prefix = $"{this.Name}{NamespaceSplitter}";
            var currentLogs = this.GetLogsFromLevel(this.SelectedMinLogLevel)
                .Where(log => $"{prefix}{log.Namespace}" == nsName);

            if (ns.IsActive)
                this.logs.Add(currentLogs, this.IsSearchCriteriaMatch);
            else
                this.logs.Remove(currentLogs);
        }

        private void UpdateIsActive(bool oldIsActive, bool newIsActive) {
            if (oldIsActive == newIsActive) {
                return;
            }

            if (newIsActive)
                logs.Add(this.GetLogsFromLevel(this.SelectedMinLogLevel), this.IsNamespaceActiveSearchCriteriaMatch);
            else
                logs.Remove(this.GetLogsFromLevel(LogLevel.TRACE));
        }

        private void UpdateSelectedMinLogLevel(LogLevel? oldLogLevel, LogLevel? newLogLevel) {
            if (!this.IsActive) {
                return;
            }

            var logs = LogLevel.GetLogLevelsBetween(ref oldLogLevel, ref newLogLevel)
                .SelectMany(l => this.GetLogsByLevel(l) ?? []);

            if (oldLogLevel > newLogLevel)
                this.logs.Add(logs, this.IsNamespaceActiveSearchCriteriaMatch);
            else
                this.logs.Remove(logs, this.IsNamespaceActiveSearchCriteriaMatch);
        }

        private void UpdateMaxNumberOfLogs(int oldMaxNumberOfLogs, int newMaxNumberOfLogs) {
            if (oldMaxNumberOfLogs <= newMaxNumberOfLogs) {
                return;
            }

            var logsToRemoveTrace = this.RemoveSurplus(logsTrace);
            var logsToRemoveDebug = this.RemoveSurplus(logsDebug);
            var logsToRemoveInfo = this.RemoveSurplus(logsInfo);
            var logsToRemoveWarn = this.RemoveSurplus(logsWarn);
            var logsToRemoveError = this.RemoveSurplus(logsError);
            var logsToRemoveFatal = this.RemoveSurplus(logsFatal);

            if (this.IsActive) {
                var logsToRemove = logsToRemoveTrace
                    .Concat(logsToRemoveDebug)
                    .Concat(logsToRemoveInfo)
                    .Concat(logsToRemoveWarn)
                    .Concat(logsToRemoveError)
                    .Concat(logsToRemoveFatal);

                logs.Remove(logsToRemove, m =>
                    m.Level >= this.SelectedMinLogLevel &&
                    this.IsNamespaceActive(m) &&
                    this.IsSearchCriteriaMatch(m));
            }
        }

        private void UpdateSearchCriteria(SearchOptions? oldOptions, SearchOptions newOptions) {
            if (!this.IsActive || oldOptions == newOptions) {
                return;
            }

            var logs = this.GetLogsFromLevel(this.SelectedMinLogLevel);
            if (string.IsNullOrEmpty(newOptions.Criteria))
                this.logs.Add(logs, this.IsNamespaceActive);
            else {
                var logsMatching = logs
                    .GroupBy(m => this.IsSearchCriteriaMatch(m))
                    .ToDictionary(g => g.Key);

                if (logsMatching.TryGetValue(true, out var logsToAdd))
                    this.logs.Add(logsToAdd, this.IsNamespaceActive);
                if (logsMatching.TryGetValue(false, out var logsToRemove))
                    this.logs.Remove(logsToRemove);
            }
        }

        private bool IsSearchCriteriaMatch(Log log) {
            try {
                var criteria = this.SearchOptions.Criteria;

                // Default
                if (string.IsNullOrEmpty(criteria)) {
                    return true;
                }

                // Search
                if ((!string.IsNullOrEmpty(log.Application) && log.Application.Contains(criteria, StringComparison.CurrentCultureIgnoreCase)) ||
                    (!string.IsNullOrEmpty(log.Namespace) && log.Namespace.Contains(criteria, StringComparison.CurrentCultureIgnoreCase)) ||
                    (!string.IsNullOrEmpty(log.Message) && log.Message.Contains(criteria, StringComparison.CurrentCultureIgnoreCase)) ||
                    (!string.IsNullOrEmpty(log.Exception) && log.Exception.Contains(criteria, StringComparison.CurrentCultureIgnoreCase))) {
                    return !this.SearchOptions.IsInverted;
                }
                return this.SearchOptions.IsInverted;
            }
            catch (Exception e) {
                logger.Error(e, "Invalid search criteria");
                return false;
            }
        }

        private bool IsNamespaceActive(Log log) {
            // Try to get existing root namespace with name of application
            var nsApplication = namespaces.FirstOrDefault(m => m.Name == log.Application);
            return nsApplication is not null && IsNamespaceActive(nsApplication, log.Namespace);
        }

        private static bool IsNamespaceActive(NamespaceViewModel parent, string suffix) {
            // Example: VerbTeX.View (Verbosus was processed before)
            var nsLogFull = suffix;
            // Example: VerbTeX
            var nsLogPart = nsLogFull?.Split([NamespaceSplitter], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            // Try to get existing namespace with name VerbTeX
            var nsChild = parent.Children.FirstOrDefault(m => m.Name == nsLogPart);
            if (nsChild == null) {
                return false;
            }

            var index = nsLogFull is null ? -1 : nsLogFull.IndexOf(NamespaceSplitter);
            return index >= 0
                ? IsNamespaceActive(nsChild, nsLogFull![(index + 1)..])
                : nsChild.IsActive;
        }

        private bool IsNamespaceActiveSearchCriteriaMatch(Log log) =>
            this.IsNamespaceActive(log) && this.IsSearchCriteriaMatch(log);

        private List<Log>? GetLogsByLevel(LogLevel level) =>
            level switch {
                var l when l == LogLevel.TRACE => logsTrace,
                var l when l == LogLevel.DEBUG => logsDebug,
                var l when l == LogLevel.INFO => logsInfo,
                var l when l == LogLevel.WARN => logsWarn,
                var l when l == LogLevel.ERROR => logsError,
                var l when l == LogLevel.FATAL => logsFatal,
                _ => null,
            };

        private IEnumerable<Log> GetLogsFromLevel(LogLevel level) =>
            level switch {
                var l when l == LogLevel.TRACE => logsTrace.Concat(logsDebug).Concat(logsInfo).Concat(logsWarn).Concat(logsError).Concat(logsFatal),
                var l when l == LogLevel.DEBUG => logsDebug.Concat(logsInfo).Concat(logsWarn).Concat(logsError).Concat(logsFatal),
                var l when l == LogLevel.INFO => logsInfo.Concat(logsWarn).Concat(logsError).Concat(logsFatal),
                var l when l == LogLevel.WARN => logsWarn.Concat(logsError).Concat(logsFatal),
                var l when l == LogLevel.ERROR => logsError.Concat(logsFatal),
                var l when l == LogLevel.FATAL => logsFatal,
                _ => [],
            };

        private Log? AddByLevelPossiblyRemovingFirst(Log log) {
            Log? logToRemove = null;

            var currentLevelLogs = this.GetLogsByLevel(log.Level);
            if (currentLevelLogs is not null) {
                currentLevelLogs.Add(log);

                if (currentLevelLogs.Count > this.MaxNumberOfLogsPerLevel) {
                    var last = currentLevelLogs.First();
                    currentLevelLogs.Remove(last);
                    logToRemove = last;
                }
            }

            return logToRemove;
        }

        private List<Log> RemoveSurplus(List<Log> levelLogs) {
            var count = levelLogs.Count - this.MaxNumberOfLogsPerLevel;
            if (count <= 0) {
                return [];
            }

            var logsToRemove = levelLogs.Take(count).ToList();
            levelLogs.RemoveRange(0, count);
            return logsToRemove;
        }
    }
}
