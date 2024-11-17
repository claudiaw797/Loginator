// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Loginator.Application.Common;
using Loginator.Application.Option;
using Loginator.Application.Service;
using Loginator.Domain.Model;
using Loginator.Domain.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static Loginator.Domain.Common.Constants;
using LogLevel = Loginator.Domain.Model.LogLevel;

namespace Loginator.Application.ViewModel {

    public sealed partial class LoginatorViewModel : ObservableObject, IDisposable {

        private static readonly TimeSpan BATCH_TIME_INTERVAL = TimeSpan.FromMilliseconds(300);

        private readonly IOptionsMonitor<ApplicationOptions> optionsMonitor;
        private readonly IDisposable? optionsChangeListener;
        private readonly TimeProvider timeProvider;
        private readonly IDispatcher dispatcher;
        private readonly ILogger<LoginatorViewModel> logger;
        private readonly OrderedObservableCollection orderedLogs = [];
        private readonly CancellationTokenSource cancellationTokenSource = new();

        private IStopwatch stopwatch;
        private ILogRepository? logRepository;
        private ApplicationOptions currentOptions;

        public LoginatorViewModel(
            IOptionsMonitor<ApplicationOptions> optionsMonitor,
            TimeProvider timeProvider,
            IStopwatch stopwatch,
            IDispatcher dispatcher,
            ILogger<LoginatorViewModel> logger) {
            this.optionsMonitor = optionsMonitor;
            this.timeProvider = timeProvider;
            this.stopwatch = stopwatch;
            this.dispatcher = dispatcher;
            this.logger = logger;

            optionsChangeListener = optionsMonitor.OnChange(OnChange_Options);
            currentOptions = optionsMonitor.CurrentValue;
            isActive = true;
            selectedInitialLogLevel = LogLevel.TRACE;
            numberOfLogsPerLevel = Constants.DefaultMaxNumberOfLogsPerLevel;
            cancellationTokenSource.Token.ThrowIfCancellationRequested();
            Search = new SearchViewModel();
            Search.UpdateSearch += OnUpdate_Search;
        }

        [ObservableProperty]
        private bool isActive;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(UpdateNumberOfLogsPerLevelCommand))]
        private int numberOfLogsPerLevel;

        [ObservableProperty]
        private LogLevel selectedInitialLogLevel;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(CopySelectedLogCommand), nameof(CopySelectedLogExceptionCommand), nameof(CopySelectedLogMessageCommand), nameof(UnselectLogCommand))]
        private LogViewModel? selectedLog;
        partial void OnSelectedLogChanged(LogViewModel? value) {
            HighlightedNamespace = value is null
                ? null
                : value.Parent ?? GetNamespaceFromLog(value);
        }

        [ObservableProperty]
        private NamespaceViewModel? selectedNamespace;
        partial void OnSelectedNamespaceChanged(NamespaceViewModel? value) {
            var isNotNull = value is not null;
            foreach (var log in this.Logs) {
                log.IsHighlighted = isNotNull && value == log.Parent;
            }
            Task.Run(() => {
                foreach (var application in Applications) {
                    application.UpdateIsHighlighted(value);
                }
            });
        }

        [ObservableProperty]
        private NamespaceViewModel? highlightedNamespace;
        partial void OnHighlightedNamespaceChanged(NamespaceViewModel? oldValue, NamespaceViewModel? newValue) {
            if (oldValue is not null) {
                oldValue.IsHighlighted = false;
            }
            if (newValue is not null) {
                newValue.IsHighlighted = true;
            }
        }

        public IReadOnlyList<LogLevel> LogLevels { get; } = [.. LogLevel.AllLogLevels.Order()];

        public ObservableCollection<LogViewModel> Logs => orderedLogs;
        public ObservableCollection<NamespaceViewModel> Namespaces { get; init; } = [];
        public ObservableCollection<ApplicationViewModel> Applications { get; init; } = [];

        public SearchViewModel Search { get; private set; }

        public Action<string?>? OnCopyToClipboard { get; set; }

        [RelayCommand(CanExecute = nameof(CanClearAnything))]
        private void ClearLogs() {
            lock (Constants.SyncObject) {
                orderedLogs.Clear();
                foreach (var application in Applications) {
                    application.ClearLogs();
                }
                foreach (var ns in AllNamespaces()) {
                    ns.ClearLogData();
                }

                NotifyApplicationDependentCommands();
            }
        }

        [RelayCommand(CanExecute = nameof(CanClearAnything))]
        private void ClearAll() {
            lock (Constants.SyncObject) {
                orderedLogs.Clear();
                Namespaces.Clear();
                Applications.Clear();

                NotifyApplicationDependentCommands();
            }
        }

        [RelayCommand(CanExecute = nameof(CanActivateAnyApplication))]
        private void ActivateAllApplications(bool active) {
            lock (Constants.SyncObject) {
                foreach (var application in this.Applications) {
                    application.IsActive = active;
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanUpdateNumberOfLogsPerLevel))]
        private void UpdateNumberOfLogsPerLevel(int value) {
            lock (Constants.SyncObject) {
                NumberOfLogsPerLevel = value;
                foreach (var application in Applications) {
                    application.MaxNumberOfLogsPerLevel = value;
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanCopySelectedLog))]
        private void CopySelectedLog() {
            if (SelectedLog is not null) {
                OnCopyToClipboard?.Invoke(SelectedLog.ToString());
            }
        }

        [RelayCommand(CanExecute = nameof(CanCopySelectedLog))]
        private void CopySelectedLogMessage() {
            if (SelectedLog is not null) {
                OnCopyToClipboard?.Invoke(SelectedLog.Message);
            }
        }

        [RelayCommand(CanExecute = nameof(CanCopySelectedLogException))]
        private void CopySelectedLogException() {
            if (SelectedLog is not null) {
                OnCopyToClipboard?.Invoke(SelectedLog.Exception);
            }
        }

        [RelayCommand(CanExecute = nameof(CanCopySelectedLog))]
        private void UnselectLog() {
            SelectedLog = null;
        }

        public void Dispose() {
            cancellationTokenSource.Cancel();
            optionsChangeListener?.Dispose();
            Search.UpdateSearch -= OnUpdate_Search;
            ClearAllCommand.Execute(null);
        }

        internal IEnumerable<NamespaceViewModel> AllNamespaces() =>
            Namespaces.Flatten(x => x.Children);

        internal void StartMessageProcessing() {
            if (logRepository is not null) return;

            Task.Run(async () => {
                while (!cancellationTokenSource.IsCancellationRequested) {
                    try {
                        await ReadFromLogRepository();
                    }
                    catch (ObjectDisposedException ex) {
                        logger.LogError(ex, "Receiver listening on port {port} closed unexpectedly, restarting receiver.", optionsMonitor.CurrentValue.Port);
                    }
                    catch (OperationCanceledException ex) {
                        logger.LogInformation("Receiver listening on port {port} closed: {message}", optionsMonitor.CurrentValue.Port, ex.Message);
                        break;
                    }
                    catch (SocketException ex) {
                        if (cancellationTokenSource.IsCancellationRequested) {
                            logger.LogInformation("Receiver listening on port {port} closed: {message}", optionsMonitor.CurrentValue.Port, ex.Message);
                            break;
                        }
                        else {
                            logger.LogError(ex, "Receiver listening on port {port} closed unexpectedly, restarting receiver.", optionsMonitor.CurrentValue.Port);
                        }
                    }
                    catch (Exception ex) {
                        logger.LogError(ex, "Receiver listening on port {port} closed unexpectedly.", optionsMonitor.CurrentValue.Port);
                        break;
                    }
                }
            });
        }

        private void OnChange_Options(ApplicationOptions options, string? name = null) {
            lock (this) {
                if (currentOptions.LogTimeFormat != options.LogTimeFormat) {
                    dispatcher.BeginInvokeOnUIThread(orderedLogs.RaiseReset);
                    logger.LogInformation("Log time format configuration changed from {LogTimeFormat} to {logConfig.LogTimeFormat}.", currentOptions.LogTimeFormat, options.LogTimeFormat);
                }
                if (currentOptions.TracePerformance != options.TracePerformance) {
                    Interlocked.Exchange(ref stopwatch, IoC.Get<IStopwatch>(options.TracePerformance));
                }
                currentOptions = options;

                // refresh language dependent bindings
                this.OnPropertyChanged(nameof(SelectedInitialLogLevel));
            }
        }

        private void OnUpdate_Search(object? sender, EventArgs e) {
            var searchOptions = Search.ToOptions();
            foreach (var application in Applications) {
                application.SearchOptions = searchOptions;
            }
        }

        private void OnPropertyChanged_Application(object? sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(ApplicationViewModel.IsActive)) {
                NotifyApplicationDependentCommands();
            }
        }

        private async Task ReadFromLogRepository() {
            var config = optionsMonitor.CurrentValue;
            logRepository = IoC.Get<ILogRepository>((config.ConnectionType, config.LogType));

            var logQuery = logRepository
                .GetEnumerableAsync(config.Port, cancellationTokenSource.Token)
                .Batch(BATCH_TIME_INTERVAL, timeProvider, cancellationTokenSource.Token);
            await foreach (var logs in logQuery) {
                if (!IsActive) {
                    logger.LogInformation("Discarded {count} log items", logs.Count);
                    continue;
                }

                dispatcher.BeginInvokeOnUIThread(() => ProcessLogs(logs));
            }
        }

        private void ProcessLogs(IEnumerable<Log> logs) {
            lock (Constants.SyncObject) {
                try {
                    var logsToInsert = logs
                        .OrderBy(m => m.Timestamp)
                        .Select(m => new LogViewModel(m))
                        .ToArray();

                    // 1. Add missing applications using incoming logs
                    stopwatch.Start();
                    UpdateApplications(logsToInsert);
                    stopwatch.TraceElapsedTime("[UpdateApplications]");

                    // 2. Add missing namespaces using incoming logs
                    stopwatch.Start();
                    UpdateNamespaces(logsToInsert);
                    stopwatch.TraceElapsedTime("[UpdateNamespaces]");

                    stopwatch.Start();
                    AddLogs(logsToInsert);
                    stopwatch.TraceElapsedTime("[UpdateLogs]");

                    logger.LogInformation("Processed {count} log items", logs.Count());
                }
                catch (Exception ex) {
                    logger.LogError(ex, "Error processing {count} new log items", logs.Count());
                }
                finally {
                    NotifyApplicationDependentCommands();
                }
            }
        }

        private void AddLogs(IEnumerable<LogViewModel> logsToInsert) {
            try {
                foreach (var logToInsert in logsToInsert) {
                    var application = Applications.FirstOrDefault(m => m.Name == logToInsert.Application);
                    if (application is null) {
                        logger.LogError("[AddLogs] The application has to be set at this point.");
                        return;
                    }
                    application.AddLog(logToInsert);
                }
            }
            catch (Exception e) {
                logger.LogError(e, "Could not update logs");
            }
        }

        private void UpdateNamespaces(IEnumerable<LogViewModel> logsToInsert) {
            try {
                foreach (var log in logsToInsert) {
                    var application = Applications.FirstOrDefault(m => m.Name == log.Application);
                    if (application is null) {
                        logger.LogError("[UpdateNamespaces] The application has to be set at this point.");
                        return;
                    }
                    // Try to get existing root namespace with name of application
                    var nsApplication = Namespaces.FirstOrDefault(m => m.Name == log.Application);
                    if (nsApplication is null) {
                        nsApplication = new NamespaceViewModel(log.Application, application);
                        Namespaces.Add(nsApplication);
                    }

                    HandleNamespace(nsApplication, log.Namespace, application, log);
                }
            }
            catch (Exception e) {
                logger.LogError(e, "Could not update namespaces");
            }
        }

        private static void HandleNamespace(NamespaceViewModel parent, string suffix, ApplicationViewModel application, LogViewModel log) {
            // Example: Verbosus.VerbTeX.View
            var nsLogFull = suffix;
            // Example: 1st Verbosus, 2nd VerbTeX, 3rd View
            var nsLogPart = nsLogFull?.Split([NamespaceSplitter], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            // Try to get existing namespace with name 1st Verbosus, 2nd VerbTeX, 3rd View
            var nsChild = parent.Children.FirstOrDefault(m => m.Name == nsLogPart);
            if (nsChild is null) {
                nsChild = new NamespaceViewModel(nsLogPart!, application) {
                    IsActive = parent.IsActive,
                    Parent = parent
                };
                parent.Children.Add(nsChild);
            }

            var index = nsLogFull is null ? -1 : nsLogFull.IndexOf(NamespaceSplitter);
            if (index >= 0) {
                HandleNamespace(nsChild, nsLogFull![(index + 1)..], application, log);
            }
            else {
                nsChild.UpdateLogCounts(log);
                log.Parent = nsChild;
            }
        }

        private void UpdateApplications(IEnumerable<LogViewModel> logsToInsert) {
            try {
                foreach (var log in logsToInsert) {
                    var application = Applications.FirstOrDefault(m => m.Name == log.Application);
                    if (application is null) {
                        application = new ApplicationViewModel(log.Application, orderedLogs, Namespaces, SelectedInitialLogLevel) {
                            MaxNumberOfLogsPerLevel = NumberOfLogsPerLevel,
                            SearchOptions = Search.ToOptions(),
                        };
                        application.PropertyChanged += OnPropertyChanged_Application;
                        Applications.Add(application);
                    }
                }
            }
            catch (Exception e) {
                logger.LogError(e, "Could not update applications");
            }
        }

        private NamespaceViewModel? GetNamespaceFromLog(LogViewModel log) {
            var fullname = $"{log.Application}{NamespaceSplitter}{log.Namespace}";
            return AllNamespaces().FirstOrDefault(nsp => nsp.Fullname.Equals(fullname));
        }

        private bool CanClearAnything() {
            return Applications.Any(app => app.HasLogs);
        }

        private bool CanActivateAnyApplication(bool active) {
            return Applications.Any(app => app.IsActive != active);
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
            activateAllApplicationsCommand?.NotifyCanExecuteChanged();
            clearLogsCommand?.NotifyCanExecuteChanged();
            clearAllCommand?.NotifyCanExecuteChanged();
        }
    }
}
