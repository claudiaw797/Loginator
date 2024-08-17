using AutoMapper;
using Backend;
using Backend.Dao;
using Backend.Events;
using Backend.Model;
using Common;
using Common.Configuration;
using CommunityToolkit.Mvvm.Input;
using Loginator.Collections;
using Loginator.Controls;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace Loginator.ViewModels {

    public sealed class LoginatorViewModel : INotifyPropertyChanged, IDisposable {

        private const int TIME_INTERVAL_IN_MILLISECONDS = 1000;

        private IConfigurationDao ConfigurationDao { get; set; }
        private IApplicationConfiguration ApplicationConfiguration { get; set; }
        private ILogger Logger { get; set; }
        private IMapper Mapper { get; set; }
        private IReceiver? Receiver { get; set; }
        private Timer? Timer { get; set; }

        private LogTimeFormat LogTimeFormat { get; set; }

        private bool isActive;
        public bool IsActive {
            get { return isActive; }
            set {
                isActive = value;
                OnPropertyChanged(nameof(IsActive));
            }
        }
        
        private int numberOfLogsPerApplicationAndLevelInternal;
        private int numberOfLogsPerLevel;
        public int NumberOfLogsPerLevel {
            get { return numberOfLogsPerLevel; }
            set {
                numberOfLogsPerLevel = value;
                OnPropertyChanged(nameof(NumberOfLogsPerLevel));
            }
        }

        public IReadOnlyList<LoggingLevel> LogLevels { get; } = [.. LoggingLevel.GetAllLogLevels().Order()];

        private LoggingLevel selectedInitialLogLevel;
        public LoggingLevel SelectedInitialLogLevel {
            get { return selectedInitialLogLevel; }
            set {
                selectedInitialLogLevel = value;
                OnPropertyChanged(nameof(SelectedInitialLogLevel));
            }
        }

        private LogViewModel? selectedLog;
        public LogViewModel? SelectedLog {
            get { return selectedLog; }
            set {
                selectedLog = value;
                SetNamespaceHighlight(selectedLog);
                OnPropertyChanged(nameof(SelectedLog));
                copySelectedLogExceptionCommand?.NotifyCanExecuteChanged();
            }
        }

        private List<LogViewModel> LogsToInsert { get; set; }

        public OrderedObservableCollection Logs { get; private set; }
        public ObservableCollection<NamespaceViewModel> Namespaces { get; private set; }
        public ObservableCollection<ApplicationViewModel> Applications { get; private set; }

        private NamespaceViewModel? _selectedNamespaceViewModel;
        public NamespaceViewModel? SelectedNamespaceViewModel {
            get { return _selectedNamespaceViewModel; }
            set {
                if (_selectedNamespaceViewModel != null) {
                    _selectedNamespaceViewModel.IsHighlighted = false;
                }
                _selectedNamespaceViewModel = value;
                if (_selectedNamespaceViewModel != null) {
                    _selectedNamespaceViewModel.IsHighlighted = true;
                }
                OnPropertyChanged(nameof(SelectedNamespaceViewModel));
            }
        }

        public SearchViewModel Search { get; private set; }

        public LoginatorViewModel(IApplicationConfiguration applicationConfiguration, IConfigurationDao configurationDao, IMapper mapper) {
            ApplicationConfiguration = applicationConfiguration;
            ConfigurationDao = configurationDao;
            ConfigurationDao.OnConfigurationChanged += ConfigurationDao_OnConfigurationChanged;
            isActive = true;
            selectedInitialLogLevel = LoggingLevel.TRACE;
            numberOfLogsPerLevel = Constants.DEFAULT_MAX_NUMBER_OF_LOGS_PER_LEVEL;
            Logger = LogManager.GetCurrentClassLogger();
            Logs = [];
            LogsToInsert = [];
            Namespaces = [];
            Applications = [];
            Mapper = mapper;
            Search = new SearchViewModel();
            Search.UpdateSearch += OnUpdateSearch;
        }

        public void Dispose() {
            ConfigurationDao.OnConfigurationChanged -= ConfigurationDao_OnConfigurationChanged;
            Search.UpdateSearch -= OnUpdateSearch;
            if (Receiver is not null) Receiver.LogReceived -= Receiver_LogReceived;
            Timer?.Dispose();
        }

        private void OnUpdateSearch(object? sender, EventArgs e) {
            var searchOptions = Search.ToOptions();
            foreach (var application in Applications) {
                application.SearchOptions = searchOptions;
            }
        }

        private void ConfigurationDao_OnConfigurationChanged(object? sender, EventArgs e) {
            Logger.Info("[ConfigurationDao_OnConfigurationChanged] Configuration changed.");
            LogTimeFormat = ConfigurationDao.Read().LogTimeFormat;
        }

        public void StartListener() {
            if (Receiver is not null) return;

            Receiver = IoC.Get<IReceiver>();
            Receiver.LogReceived += Receiver_LogReceived;
            Timer = new Timer(Callback, null, TIME_INTERVAL_IN_MILLISECONDS, Timeout.Infinite);
            Receiver.Initialize(ConfigurationDao.Read());
        }

        private void Callback(object? state) {
            DispatcherHelper.CheckBeginInvokeOnUI(() => {
                // TODO: Refactor this so we can use using(...)
                Stopwatch sw = new Stopwatch();
                lock (ViewModelConstants.SYNC_OBJECT) {
                    var logsToInsert = LogsToInsert.OrderBy(m => m.Timestamp);

                    // 1. Add missing applications using incoming logs
                    sw.Start();
                    UpdateApplications(logsToInsert);
                    sw.Stop();
                    if (ApplicationConfiguration.IsTimingTraceEnabled) {
                        Logger.Trace("[UpdateApplications] " + sw.ElapsedMilliseconds + "ms");
                    }
                    // 2. Add missing namespaces using incoming logs
                    sw.Restart();
                    UpdateNamespaces(logsToInsert);
                    sw.Stop();
                    if (ApplicationConfiguration.IsTimingTraceEnabled) {
                        Logger.Trace("[UpdateNamespaces] " + sw.ElapsedMilliseconds + "ms");
                    }

                    sw.Restart();
                    AddLogs(logsToInsert);
                    sw.Stop();
                    if (ApplicationConfiguration.IsTimingTraceEnabled) {
                        Logger.Trace("[UpdateLogs] " + sw.ElapsedMilliseconds + "ms");
                    }

                    LogsToInsert.Clear();

                    Timer?.Change(TIME_INTERVAL_IN_MILLISECONDS, Timeout.Infinite);
                }
            });
        }

        private LogViewModel ToLogViewModel(Log log) {
            var logViewModel = Mapper.Map<Log, LogViewModel>(log);
            if (LogTimeFormat == LogTimeFormat.CONVERT_TO_LOCAL_TIME) {
                logViewModel.Timestamp = logViewModel.Timestamp.ToLocalTime();
            }
            return logViewModel;
        }

        private void Receiver_LogReceived(object? sender, LogReceivedEventArgs e) {
            DispatcherHelper.CheckBeginInvokeOnUI(() => {
                lock (ViewModelConstants.SYNC_OBJECT) {
                    // Add a log entry only to the list if global logging is active (checkbox)
                    if (!IsActive) {
                        return;
                    }
                    LogViewModel log = ToLogViewModel(e.Log);
                    LogsToInsert.Add(log);
                }
            });
        }

        private void AddLogs(IEnumerable<LogViewModel> logsToInsert) {
            try {
                foreach (var logToInsert in logsToInsert) {
                    var application = Applications.FirstOrDefault(m => m.Name == logToInsert.Application);
                    if (application == null) {
                        Logger.Error("[AddLogs] The application has to be set at this point.");
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
                        Logger.Error("[UpdateNamespaces] The application has to be set at this point.");
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
                        application = new ApplicationViewModel(log.Application, Logs, Namespaces, SelectedInitialLogLevel);
                        Applications.Add(application);
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine("Could not update applications: " + e);
            }
        }

        private static void ResetAllCount(NamespaceViewModel ns) {
            ns.Count = 0;
            ns.CountTrace = 0;
            ns.CountDebug = 0;
            ns.CountInfo = 0;
            ns.CountWarn = 0;
            ns.CountError = 0;
            ns.CountFatal = 0;
            foreach (var child in ns.Children) {
                ResetAllCount(child);
            }
        }

        private void SetNamespaceHighlight(LogViewModel? log) {
            if (log != null) {
                SelectedNamespaceViewModel = Namespaces.Flatten(x => x.Children).FirstOrDefault(model => model.Fullname.Equals($"{log.Application}{Constants.NAMESPACE_SPLITTER}{log.Namespace}"));
            }
        }

        private void ClearNamespaceHighlight() {
            Namespaces.Flatten(x => x.Children).ToList().ForEach(m => m.IsHighlighted = false);
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

        private ICommand? clearLogsCommand;
        public ICommand ClearLogsCommand {
            get {
                clearLogsCommand ??= new RelayCommand(ClearLogs, CanClearLogs);
                return clearLogsCommand;
            }
        }
        private bool CanClearLogs() {
            return true;
        }
        public void ClearLogs() {
            DispatcherHelper.CheckBeginInvokeOnUI(() => {
                lock (ViewModelConstants.SYNC_OBJECT) {
                    Logs.Clear();
                    foreach (var application in Applications) {
                        application.ClearLogs();
                    }
                    foreach (var ns in Namespaces) {
                        ResetAllCount(ns);
                    }
                    ClearNamespaceHighlight();
                }
            });
        }

        private ICommand? clearAllCommand;
        public ICommand ClearAllCommand {
            get {
                clearAllCommand ??= new RelayCommand(ClearAll, CanClearAll);
                return clearAllCommand;
            }
        }

        private ICommand? unselectAllCommand;
        public ICommand UnselectAllCommand {
            get {
                unselectAllCommand ??= new RelayCommand(UnselectAll, CanUnselectAll);
                return unselectAllCommand;
            }
        }

        private bool CanClearAll() {
            return true;
        }
        public void ClearAll() {
            DispatcherHelper.CheckBeginInvokeOnUI(() => {
                lock (ViewModelConstants.SYNC_OBJECT) {
                    Logs.Clear();
                    Namespaces.Clear();
                    Applications.Clear();
                }
            });
        }

        private bool CanUnselectAll() {
            return true;
        }
        public void UnselectAll() {
            DispatcherHelper.CheckBeginInvokeOnUI(() => {
                lock (ViewModelConstants.SYNC_OBJECT) {
                    foreach (var application in this.Applications) {
                        application.IsActive = false;
                    }
                }
            });
        }

        private ICommand? updateNumberOfLogsPerLevelCommand;
        public ICommand UpdateNumberOfLogsPerLevelCommand {
            get {
                updateNumberOfLogsPerLevelCommand ??= new RelayCommand(UpdateNumberOfLogsPerLevel, CanUpdateNumberOfLogsPerLevel);
                return updateNumberOfLogsPerLevelCommand;
            }
        }
        private bool CanUpdateNumberOfLogsPerLevel() {
            return true;
        }
        public void UpdateNumberOfLogsPerLevel() {
            DispatcherHelper.CheckBeginInvokeOnUI(() => {
                lock (ViewModelConstants.SYNC_OBJECT) {
                    numberOfLogsPerApplicationAndLevelInternal = NumberOfLogsPerLevel;
                    foreach (var application in Applications) {
                        application.MaxNumberOfLogsPerLevel = numberOfLogsPerApplicationAndLevelInternal;
                    }
                }
            });
        }

        private ICommand? openConfigurationCommand;
        public ICommand OpenConfigurationCommand {
            get {
                openConfigurationCommand ??= new RelayCommand(OpenConfiguration, CanOpenConfiguration);
                return openConfigurationCommand;
            }
        }
        private bool CanOpenConfiguration() {
            return true;
        }
        public static void OpenConfiguration() {
            new ConfigurationWindow().Show();
        }

        private RelayCommand? copySelectedLogExceptionCommand;
        public ICommand CopySelectedLogExceptionCommand {
            get {
                copySelectedLogExceptionCommand ??= new RelayCommand(CopySelectedLogException, CanCopySelectedLogException);
                return copySelectedLogExceptionCommand;
            }
        }
        private bool CanCopySelectedLogException() {
            return !string.IsNullOrEmpty(SelectedLog?.Exception);
        }
        private void CopySelectedLogException() {
            if (SelectedLog is not null)
                Clipboard.SetText(SelectedLog.Exception);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string property) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
