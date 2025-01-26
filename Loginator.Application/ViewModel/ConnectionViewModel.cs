// Copyright (C) 2024 Claudia Wagner

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Loginator.Domain.Channel;
using Loginator.Domain.Option;
using Microsoft.Extensions.Logging;

namespace Loginator.Application.ViewModel {

    public partial class ConnectionViewModel : ObservableObject {

        private readonly ConnectionsViewModel connectionsViewModel;
        private readonly ILogWriter logWriter;
        private readonly ILogger<ConnectionsViewModel> logger;

        internal ConnectionViewModel(
            ConnectionsViewModel connectionsViewModel,
            ILogWriter logWriter,
            ILogger<ConnectionsViewModel> logger) {
            this.connectionsViewModel = connectionsViewModel;
            this.logWriter = logWriter;
            this.logger = logger;
            DesiredState = logWriter.Connection.State;
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(PauseCommand), nameof(ResumeCommand), nameof(StartCommand), nameof(StopCommand))]
        private ConnectionState? desiredState;
        partial void OnDesiredStateChanging(ConnectionState? value) {
            switch (value) {
                case ConnectionState.Stopped:
                    if (CanStop()) {
                        connectionsViewModel.Stop(logWriter);
                    }
                    break;
                case ConnectionState.Paused:
                    logWriter.IsActive = false;
                    break;
                case ConnectionState.Running:
                    if (CanStart()) {
                        connectionsViewModel.Start(logWriter);
                    }
                    logWriter.IsActive = true;
                    break;
            }
        }

        partial void OnDesiredStateChanged(ConnectionState? value) {
            logger.LogTrace("State changed: actual={state}, desired={value}", State, value);

            OnPropertyChanged(nameof(State));
        }

        public ConnectionState State =>
            logWriter.Task.IsCompleted
            ? ConnectionState.Stopped : logWriter.IsActive
            ? ConnectionState.Running : ConnectionState.Paused;

        public ConnectionType ConnectionType =>
            logWriter.Connection.ConnectionType;

        public LogType LogType =>
            logWriter.Connection.LogType;

        public string? IpAddress =>
            logWriter.Connection.IpAddress;

        public int Port =>
            logWriter.Connection.Port;

        internal Connection Connection =>
            new() {
                ConnectionType = ConnectionType,
                LogType = LogType,
                Port = Port,
                IpAddress = IpAddress,
                State = DesiredState ?? ConnectionState.Stopped
            };

        [RelayCommand(CanExecute = nameof(CanPause))]
        private void Pause() =>
            SetDesiredState(ConnectionState.Paused);

        [RelayCommand(CanExecute = nameof(CanResume))]
        private void Resume() =>
            SetDesiredState(ConnectionState.Running);

        [RelayCommand(CanExecute = nameof(CanStart))]
        private void Start() =>
            SetDesiredState(ConnectionState.Running);

        [RelayCommand(CanExecute = nameof(CanStop))]
        private void Stop() =>
            SetDesiredState(ConnectionState.Stopped);

        [RelayCommand]
        private void Remove() =>
            connectionsViewModel.Remove(this, logWriter);

        public override string ToString() =>
            $"{ConnectionType}:{Port}";

        private bool CanResume() =>
            CanStop() && !logWriter.IsActive;

        private bool CanPause() =>
            CanStop() && logWriter.IsActive;

        private bool CanStart() =>
            logWriter.Task.IsCompleted;

        private bool CanStop() =>
            !logWriter.Task.IsCompleted;

        private void SetDesiredState(ConnectionState state) {
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
            desiredState = null;
#pragma warning restore MVVMTK0034
            DesiredState = state;
        }
    }
}
