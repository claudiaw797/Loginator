// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Loginator.Application.Option;
using Loginator.Domain.Option;
using Loginator.Infrastructure.Option;
using System;

namespace Loginator.Application.ViewModel {

    public partial class ConfigurationViewModel : ObservableObject {

        private readonly IOptionsRepository<ApplicationOptions> optionsRepository;

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

        public Action? OnClose { get; set; }

        public OnErrorHandler? OnError { get; set; }

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
                this.TraceMessages != options.LogProcessing.TraceMessages;
            return result;
        }

        public delegate void OnErrorHandler(string actionKey, Exception exception);
    }
}
