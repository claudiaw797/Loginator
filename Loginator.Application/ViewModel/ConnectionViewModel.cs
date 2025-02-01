// Copyright (C) 2024 Claudia Wagner

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Loginator.Domain.Channel;
using Loginator.Domain.Option;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

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

            PauseAsyncCommand = new AsyncRelayCommand(
                () => SetStateAsync(ConnectionState.Paused), CanPause, AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler);
            ResumeAsyncCommand = new AsyncRelayCommand(
                () => SetStateAsync(ConnectionState.Running), CanResume, AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler);
            StartAsyncCommand = new AsyncRelayCommand(
                () => SetStateAsync(ConnectionState.Running), CanStart, AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler);
            StopAsyncCommand = new AsyncRelayCommand(
                () => SetStateAsync(ConnectionState.Stopped), CanStop, AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler);
            RemoveAsyncCommand = new AsyncRelayCommand(
                RemoveWriterAsync, AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler);

            var desiredState = logWriter.Connection.State;
            SetStateAsync(desiredState == ConnectionState.Paused ? ConnectionState.Stopped : desiredState).GetAwaiter().GetResult();
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

        public IAsyncRelayCommand PauseAsyncCommand { get; }

        public IAsyncRelayCommand ResumeAsyncCommand { get; }

        public IAsyncRelayCommand StartAsyncCommand { get; }

        public IAsyncRelayCommand StopAsyncCommand { get; }

        public IAsyncRelayCommand RemoveAsyncCommand { get; }

        internal Connection Connection =>
            new() {
                ConnectionType = ConnectionType,
                LogType = LogType,
                Port = Port,
                IpAddress = IpAddress,
                State = this.State
            };

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

        private Task RemoveWriterAsync() =>
            connectionsViewModel.RemoveAsync(this, logWriter);

        private async Task<ConnectionState?> SetStateAsync(ConnectionState value) {
            if (value != this.State) {
                await ChangeStateAsync(value).ConfigureAwait(false);

                if (value == this.State) {
                    OnPropertyChanged(nameof(State));
                    NotifyStateDependentCommands();
                }
                else {
                    logger.LogWarning("State changed: actual={state}, desired={value}", State, value);
                    return null;
                }
            }
            return value;
        }

        private Task ChangeStateAsync(ConnectionState value) {
            switch (value) {
                case ConnectionState.Stopped:
                    if (CanStop()) {
                        return connectionsViewModel.StopAsync(logWriter);
                    }
                    break;

                case ConnectionState.Paused:
                    logWriter.IsActive = false;
                    break;

                case ConnectionState.Running:
                    if (CanStart()) {
                        connectionsViewModel.StartAsync(logWriter);
                    }
                    logWriter.IsActive = true;
                    break;
            }
            return Task.CompletedTask;
        }

        private void NotifyStateDependentCommands() {
            PauseAsyncCommand.NotifyCanExecuteChanged();
            ResumeAsyncCommand.NotifyCanExecuteChanged();
            StartAsyncCommand.NotifyCanExecuteChanged();
            StopAsyncCommand.NotifyCanExecuteChanged();
        }
    }
}
