using AutoMapper;
using Backend;
using Backend.Events;
using Backend.Model;
using Common;
using Common.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Loginator.Collections;
using Loginator.Controls;
using Loginator.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;

namespace Loginator.ViewModels {

    public sealed partial class LoginatorViewModel : ObservableObject, IDisposable {

        private static readonly TimeSpan TIME_INTERVAL_IN_MILLISECONDS = TimeSpan.FromMilliseconds(1000);

        private IOptionsMonitor<Configuration> ConfigurationDao { get; set; }
        private IDisposable? ConfigurationChangeListener { get; set; }
        private IMapper Mapper { get; set; }
        private IReceiver? Receiver { get; set; }
        private ITimer Timer { get; set; }
        private IStopwatch Stopwatch { get; set; }
        private ILogger<LoginatorViewModel> Logger { get; set; }

        private LogTimeFormat LogTimeFormat { get; set; }
        private List<LogViewModel> LogsToInsert { get; set; }

        public LoginatorViewModel(
            IOptionsMonitor<Configuration> configurationDao,
            IMapper mapper,
            IStopwatch stopwatch,
            TimeProvider timeProvider,
            ILogger<LoginatorViewModel> logger) {
            ConfigurationDao = configurationDao;
            ConfigurationChangeListener = configurationDao.OnChange(ConfigurationDao_OnConfigurationChanged);

            isActive = true;
            selectedInitialLogLevel = LoggingLevel.TRACE;
            numberOfLogsPerLevel = Constants.DEFAULT_MAX_NUMBER_OF_LOGS_PER_LEVEL;
            Logger = logger;
            Logs = [];
            LogsToInsert = [];
            Namespaces = [];
            Applications = [];
            Mapper = mapper;
            Stopwatch = stopwatch;
            Search = new SearchViewModel();
            Search.UpdateSearch += Search_OnUpdateSearch;
            Timer = timeProvider.CreateTimer(Callback, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        [ObservableProperty]
        private bool isActive;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(UpdateNumberOfLogsPerLevelCommand))]
        private int numberOfLogsPerLevel;

        [ObservableProperty]
        private LoggingLevel selectedInitialLogLevel;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(CopySelectedLogExceptionCommand))]
        private LogViewModel? selectedLog;
        partial void OnSelectedLogChanged(LogViewModel? value) {
            SetSelectedNamespaceFromLog(value);
        }

        [ObservableProperty]
        private NamespaceViewModel? selectedNamespace;
        partial void OnSelectedNamespaceChanged(NamespaceViewModel? oldValue, NamespaceViewModel? newValue) {
            if (oldValue is not null) {
                oldValue.IsHighlighted = false;
            }
            if (newValue is not null) {
                newValue.IsHighlighted = true;
            }
        }

        public IReadOnlyList<LoggingLevel> LogLevels { get; } = [.. LoggingLevel.GetAllLogLevels().Order()];

        public OrderedObservableCollection Logs { get; private set; }
        public ObservableCollection<NamespaceViewModel> Namespaces { get; private set; }
        public ObservableCollection<ApplicationViewModel> Applications { get; private set; }

        public SearchViewModel Search { get; private set; }

        [RelayCommand(CanExecute = nameof(CanClearAnything))]
        private void ClearLogs() {
            lock (ViewModelConstants.SYNC_OBJECT) {
                Logs.Clear();
                foreach (var application in Applications) {
                    application.ClearLogs();
                }
                foreach (var ns in AllNamespaces()) {
                    ClearNamespaceData(ns);
                }

                NotifyApplicationDependentCommands();
            }
        }

        [RelayCommand(CanExecute = nameof(CanClearAnything))]
        private void ClearAll() {
            lock (ViewModelConstants.SYNC_OBJECT) {
                Logs.Clear();
                Namespaces.Clear();
                Applications.Clear();

                NotifyApplicationDependentCommands();
            }
        }

        [RelayCommand(CanExecute = nameof(CanDeactivateAllApplications))]
        private void DeactivateAllApplications() {
            lock (ViewModelConstants.SYNC_OBJECT) {
                foreach (var application in this.Applications) {
                    application.IsActive = false;
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanUpdateNumberOfLogsPerLevel))]
        private void UpdateNumberOfLogsPerLevel(int value) {
            lock (ViewModelConstants.SYNC_OBJECT) {
                NumberOfLogsPerLevel = value;
                foreach (var application in Applications) {
                    application.MaxNumberOfLogsPerLevel = value;
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanCopySelectedLog))]
        private void CopySelectedLog() {
            if (SelectedLog is not null) {
                Clipboard.SetText(SelectedLog.ToString());
            }
        }

        [RelayCommand(CanExecute = nameof(CanCopySelectedLog))]
        private void CopySelectedLogMessage() {
            if (SelectedLog is not null) {
                Clipboard.SetText(SelectedLog.Message);
            }
        }

        [RelayCommand(CanExecute = nameof(CanCopySelectedLogException))]
        private void CopySelectedLogException() {
            if (SelectedLog is not null) {
                Clipboard.SetText(SelectedLog.Exception);
            }
        }

        [RelayCommand]
        private void OpenConfiguration() {
            new ConfigurationWindow().Show();
        }

        public void Dispose() {
            ConfigurationChangeListener?.Dispose();
            Search.UpdateSearch -= Search_OnUpdateSearch;
            if (Receiver is not null) Receiver.LogReceived -= Receiver_OnLogReceived;
            Timer.Dispose();
            ClearAllCommand.Execute(null);
        }

        public void StartListener() {
            if (Receiver is not null) return;

            Receiver = IoC.Get<IReceiver>();
            Receiver.LogReceived += Receiver_OnLogReceived;
            ScheduleNextCallback();
            Receiver.Initialize(ConfigurationDao.CurrentValue);
        }

        internal IEnumerable<NamespaceViewModel> AllNamespaces() =>
            Namespaces.Flatten(x => x.Children);

        private void ConfigurationDao_OnConfigurationChanged(Configuration logConfig, string? name = null) {
            Logger.LogInformation("[ConfigurationDao_OnConfigurationChanged] Configuration changed.");
            LogTimeFormat = logConfig.LogTimeFormat;
        }

        private void Receiver_OnLogReceived(object? sender, LogReceivedEventArgs e) {
            // unnecessary to invoke this on the UI thread, because it does not set any databound fields
            lock (ViewModelConstants.SYNC_OBJECT) {
                // Add a log entry only to the list if global logging is active (checkbox)
                if (!IsActive) return;

                LogViewModel log = ToLogViewModel(e.Log);
                LogsToInsert.Add(log);
            }
        }

        private void Search_OnUpdateSearch(object? sender, EventArgs e) {
            var searchOptions = Search.ToOptions();
            foreach (var application in Applications) {
                application.SearchOptions = searchOptions;
            }
        }

        private void Application_OnPropertyChanged(object? sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(ApplicationViewModel.IsActive)) {
                NotifyApplicationDependentCommands();
            }
        }

        private void ScheduleNextCallback() =>
            Timer.Change(TIME_INTERVAL_IN_MILLISECONDS, Timeout.InfiniteTimeSpan);

        private void Callback(object? state) {
            if (LogsToInsert.Count > 0) {
                DispatcherHelper.CheckBeginInvokeOnUI(ProcessLogsToInsert);
            }
            else {
                ScheduleNextCallback();
            }
        }

        private void ProcessLogsToInsert() {
            lock (ViewModelConstants.SYNC_OBJECT) {
                try {
                    Logger.LogInformation("Processing {0} new log items", LogsToInsert.Count);

                    var logsToInsert = LogsToInsert.OrderBy(m => m.Timestamp);

                    // 1. Add missing applications using incoming logs
                    Stopwatch.Start();
                    UpdateApplications(logsToInsert);
                    Stopwatch.TraceElapsedTime("[UpdateApplications]");

                    // 2. Add missing namespaces using incoming logs
                    Stopwatch.Start();
                    UpdateNamespaces(logsToInsert);
                    Stopwatch.TraceElapsedTime("[UpdateNamespaces]");

                    Stopwatch.Start();
                    AddLogs(logsToInsert);
                    Stopwatch.TraceElapsedTime("[UpdateLogs]");

                    LogsToInsert.Clear();
                }
                catch (Exception ex) {
                    Logger.LogError(ex, "Error processing {0} new log items", LogsToInsert.Count);
                }
                finally {
                    ScheduleNextCallback();
                    NotifyApplicationDependentCommands();
                }
            }
        }

        private LogViewModel ToLogViewModel(Log log) {
            var logViewModel = Mapper.Map<Log, LogViewModel>(log);
            if (ConfigurationDao.CurrentValue.LogTimeFormat == LogTimeFormat.ConvertToLocalTime) {
                logViewModel.Timestamp = logViewModel.Timestamp.ToLocalTime();
            }
            return logViewModel;
        }

        private void AddLogs(IEnumerable<LogViewModel> logsToInsert) {
            try {
                foreach (var logToInsert in logsToInsert) {
                    var application = Applications.FirstOrDefault(m => m.Name == logToInsert.Application);
                    if (application == null) {
                        Logger.LogError("[AddLogs] The application has to be set at this point.");
                        return;
                    }
                    application.AddLog(logToInsert);
                }
            }
            catch (Exception e) {
                Console.WriteLine("Could not update logs: " + e);
            }
        }

        private void UpdateNamespaces(IEnumerable<LogViewModel> logsToInsert) {
            try {
                foreach (var log in logsToInsert) {
                    var application = Applications.FirstOrDefault(m => m.Name == log.Application);
                    if (application == null) {
                        Logger.LogError("[UpdateNamespaces] The application has to be set at this point.");
                        return;
                    }
                    // Try to get existing root namespace with name of application
                    var nsApplication = Namespaces.FirstOrDefault(m => m.Name == log.Application);
                    if (nsApplication == null) {
                        nsApplication = new NamespaceViewModel(log.Application, application);
                        Namespaces.Add(nsApplication);
                    }

                    // Example: Verbosus.VerbTeX.View
                    var nsLogFull = log.Namespace;
                    // Example: Verbosus
                    var nsLogPart = nsLogFull?.Split([Constants.NAMESPACE_SPLITTER], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    // Try to get existing namespace with name Verbosus
                    var nsChild = nsApplication.Children.FirstOrDefault(m => m.Name == nsLogPart);
                    if (nsChild == null) {
                        nsChild = new NamespaceViewModel(nsLogPart, application) {
                            IsChecked = nsApplication.IsChecked
                        };
                        nsApplication.Children.Add(nsChild);
                        nsChild.Parent = nsApplication;
                    }

                    var index = nsLogFull is null ? -1 : nsLogFull.IndexOf(Constants.NAMESPACE_SPLITTER);
                    if (index >= 0) {
                        HandleNamespace(nsChild, nsLogFull![(index + 1)..], application, log);
                    }
                    else {
                        SetLogCountByLevel(log, nsChild);
                    }
                }

            }
            catch (Exception e) {
                Console.WriteLine("Could not update namespaces: " + e);
            }
        }

        private static void HandleNamespace(NamespaceViewModel parent, string suffix, ApplicationViewModel application, LogViewModel log) {
            // Example: VerbTeX.View (Verbosus was processed before)
            var nsLogFull = suffix;
            // Example: VerbTeX
            var nsLogPart = nsLogFull?.Split([Constants.NAMESPACE_SPLITTER], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            // Try to get existing namespace with name VerbTeX
            var nsChild = parent.Children.FirstOrDefault(m => m.Name == nsLogPart);
            if (nsChild == null) {
                nsChild = new NamespaceViewModel(nsLogPart, application) {
                    IsChecked = parent.IsChecked
                };
                parent.Children.Add(nsChild);
                nsChild.Parent = parent;
            }

            var index = suffix is null ? -1 : suffix.IndexOf(Constants.NAMESPACE_SPLITTER);
            if (index >= 0) {
                HandleNamespace(nsChild, suffix![(index + 1)..], application, log);
            }
            else {
                SetLogCountByLevel(log, nsChild);
            }
        }

        private void UpdateApplications(IEnumerable<LogViewModel> logsToInsert) {
            try {
                foreach (var log in logsToInsert) {
                    var application = Applications.FirstOrDefault(m => m.Name == log.Application);
                    if (application == null) {
                        application = new ApplicationViewModel(log.Application, Logs, Namespaces, SelectedInitialLogLevel) {
                            MaxNumberOfLogsPerLevel = NumberOfLogsPerLevel,
                            SearchOptions = Search.ToOptions(),
                        };
                        application.PropertyChanged += Application_OnPropertyChanged;
                        Applications.Add(application);
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine("Could not update applications: " + e);
            }
        }

        private void SetSelectedNamespaceFromLog(LogViewModel? log) {
            SelectedNamespace = log is null
                ? null
                : AllNamespaces().FirstOrDefault(nsp => nsp.Fullname.Equals($"{log.Application}{Constants.NAMESPACE_SPLITTER}{log.Namespace}"));
        }

        private static void ClearNamespaceData(NamespaceViewModel ns) {
            ns.Count = 0;
            ns.CountTrace = 0;
            ns.CountDebug = 0;
            ns.CountInfo = 0;
            ns.CountWarn = 0;
            ns.CountError = 0;
            ns.CountFatal = 0;
            ns.IsHighlighted = false;
        }

        private static void SetLogCountByLevel(LogViewModel log, NamespaceViewModel ns) {
            ns.Count++;
            if (log.Level == LoggingLevel.TRACE) {
                ns.CountTrace++;
            }
            else if (log.Level == LoggingLevel.DEBUG) {
                ns.CountDebug++;
            }
            else if (log.Level == LoggingLevel.INFO) {
                ns.CountInfo++;
            }
            else if (log.Level == LoggingLevel.WARN) {
                ns.CountWarn++;
            }
            else if (log.Level == LoggingLevel.ERROR) {
                ns.CountError++;
            }
            else if (log.Level == LoggingLevel.FATAL) {
                ns.CountFatal++;
            }
        }

        private bool CanClearAnything() {
            return Applications.Any(app => app.HasLogs);
        }

        private bool CanDeactivateAllApplications() {
            return Applications.Any(app => app.IsActive);
        }

        private bool CanUpdateNumberOfLogsPerLevel(int value) {
            return value > 0 && value != NumberOfLogsPerLevel;
        }
        
        private bool CanCopySelectedLog() {
            return SelectedLog is not null;
        }

        private bool CanCopySelectedLogException() {
            return !string.IsNullOrEmpty(SelectedLog?.Exception);
        }

        private void NotifyApplicationDependentCommands() {
            deactivateAllApplicationsCommand?.NotifyCanExecuteChanged();
            clearLogsCommand?.NotifyCanExecuteChanged();
            clearAllCommand?.NotifyCanExecuteChanged();
        }
    }
}
