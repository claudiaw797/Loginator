// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Loginator.Application.Option;
using Loginator.Domain.Option;
using Loginator.Infrastructure.Option;
using System;
using System.Drawing;
using static Loginator.Application.Common.Constants;

namespace Loginator.Application.ViewModel {

    public sealed partial class ConfigurationViewModel : ObservableObject, IDisposable {

        private readonly IOptionsRepository<ApplicationOptions> optionsRepository;
        private readonly IDisposable? optionsChangeListener;

        public ConfigurationViewModel(IOptionsRepository<ApplicationOptions> optionsRepository) {
            this.optionsRepository = optionsRepository;

            var options = optionsRepository.Get();
            connectionType = options.ConnectionType;
            logType = options.LogType;
            port = options.Port.ToString();
            language = options.Language;
            checkForUpdateOnStartup = options.CheckForUpdateOnStartup;
            tracePerformance = options.TracePerformance;
            logTimeFormat = options.LogTimeFormat;
            applicationFormat = options.LogProcessing.ApplicationFormat;
            allowAnonymousMessages = options.LogProcessing.AllowAnonymousMessages;
            traceMessages = options.LogProcessing.TraceMessages;
            colorLevelTrace = options.Colors.LevelTrace;
            colorLevelDebug = options.Colors.LevelDebug;
            colorLevelInfo = options.Colors.LevelInfo;
            colorLevelWarn = options.Colors.LevelWarn;
            colorLevelError = options.Colors.LevelError;
            colorLevelFatal = options.Colors.LevelFatal;
            colorLogHighlight = options.Colors.LogHighlight;

            optionsChangeListener = optionsRepository.OnChanged(OnChange_Options);
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private ConnectionType connectionType;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private LogType logType;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private string port;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private KnownCulture language;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private bool checkForUpdateOnStartup;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private bool tracePerformance;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private LogTimeFormat logTimeFormat;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private ApplicationFormat applicationFormat;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private bool allowAnonymousMessages;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private bool traceMessages;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private Color colorLevelTrace;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private Color colorLevelDebug;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private Color colorLevelInfo;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private Color colorLevelWarn;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private Color colorLevelError;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private Color colorLevelFatal;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private Color colorLogHighlight;

        public Action? OnClose { get; set; }

        public OnErrorHandler? OnError { get; set; }

        public void Dispose() {
            optionsChangeListener?.Dispose();
        }

        [RelayCommand]
        private void CancelChanges() {
            try {
                this.OnClose?.Invoke();
            }
            catch (Exception ex) {
                this.OnError?.Invoke("Canceling", ex);
            }
        }

        [RelayCommand(CanExecute = nameof(CanAcceptChanges))]
        private void AcceptChanges() {
            try {
                optionsRepository.Save(options => {
                    options.ConnectionType = this.ConnectionType;
                    options.LogType = this.LogType;
                    options.Port = Convert.ToInt32(this.Port);
                    options.Language = this.Language;
                    options.CheckForUpdateOnStartup = this.CheckForUpdateOnStartup;
                    options.TracePerformance = this.TracePerformance;
                    options.LogTimeFormat = this.LogTimeFormat;
                    options.LogProcessing.ApplicationFormat = this.ApplicationFormat;
                    options.LogProcessing.AllowAnonymousMessages = this.AllowAnonymousMessages;
                    options.LogProcessing.TraceMessages = this.TraceMessages;
                    options.Colors.LevelTrace = this.ColorLevelTrace;
                    options.Colors.LevelDebug = this.ColorLevelDebug;
                    options.Colors.LevelInfo = this.ColorLevelInfo;
                    options.Colors.LevelWarn = this.ColorLevelWarn;
                    options.Colors.LevelError = this.ColorLevelError;
                    options.Colors.LevelFatal = this.ColorLevelFatal;
                    options.Colors.LogHighlight = this.ColorLogHighlight;
                });

                this.OnClose?.Invoke();
            }
            catch (Exception ex) {
                this.OnError?.Invoke("Saving", ex);
            }
        }

        private bool CanAcceptChanges() {
            var options = optionsRepository.Get();
            var result =
                this.ConnectionType != options.ConnectionType ||
                this.LogType != options.LogType ||
                this.Port != options.Port.ToString() ||
                this.Language != options.Language ||
                this.CheckForUpdateOnStartup != options.CheckForUpdateOnStartup ||
                this.TracePerformance != options.TracePerformance ||
                this.LogTimeFormat != options.LogTimeFormat ||
                this.ApplicationFormat != options.LogProcessing.ApplicationFormat ||
                this.AllowAnonymousMessages != options.LogProcessing.AllowAnonymousMessages ||
                this.TraceMessages != options.LogProcessing.TraceMessages ||
                this.ColorLevelTrace != options.Colors.LevelTrace ||
                this.ColorLevelDebug != options.Colors.LevelDebug ||
                this.ColorLevelInfo != options.Colors.LevelInfo ||
                this.ColorLevelWarn != options.Colors.LevelWarn ||
                this.ColorLevelError != options.Colors.LevelError ||
                this.ColorLevelFatal != options.Colors.LevelFatal ||
                this.ColorLogHighlight != options.Colors.LogHighlight;
            return result;
        }

        private void OnChange_Options(ApplicationOptions options, string? name = null) {
            lock (this) {
                this.ConnectionType = options.ConnectionType;
                this.LogType = options.LogType;
                this.Port = options.Port.ToString();
                this.Language = options.Language;
                this.CheckForUpdateOnStartup = options.CheckForUpdateOnStartup;
                this.TracePerformance = options.TracePerformance;
                this.LogTimeFormat = options.LogTimeFormat;
                this.ApplicationFormat = options.LogProcessing.ApplicationFormat;
                this.AllowAnonymousMessages = options.LogProcessing.AllowAnonymousMessages;
                this.TraceMessages = options.LogProcessing.TraceMessages;
                this.ColorLevelTrace = options.Colors.LevelTrace;
                this.ColorLevelDebug = options.Colors.LevelDebug;
                this.ColorLevelInfo = options.Colors.LevelInfo;
                this.ColorLevelWarn = options.Colors.LevelWarn;
                this.ColorLevelError = options.Colors.LevelError;
                this.ColorLevelFatal = options.Colors.LevelFatal;
                this.ColorLogHighlight = options.Colors.LogHighlight;
            }
        }
    }
}
