// Copyright (C) 2024 Claudia Wagner

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Loginator.Domain.Option;
using System;
using static Loginator.Application.Common.Constants;

namespace Loginator.Application.ViewModel {

    public partial class ConnectionAddViewModel(ConnectionsViewModel connectionsViewModel) : ObservableObject {

        private readonly ConnectionsViewModel connectionsViewModel = connectionsViewModel;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private ConnectionType connectionType;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private LogType logType;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private int port = 0;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private string? ipAddress;

        public bool StartImmediately { get; set; } = true;

        public OnCloseHandler? OnClose { get; set; }

        public OnErrorHandler? OnError { get; set; }

        internal Connection ToConnection() =>
            new() {
                ConnectionType = this.ConnectionType,
                LogType = this.LogType,
                Port = this.Port,
                IpAddress = this.IpAddress,
                State = StartImmediately ? ConnectionState.Running : ConnectionState.Stopped,
            };

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
                var connection = ToConnection();
                connectionsViewModel.AddConnection(connection);

                this.OnClose?.Invoke();
            }
            catch (Exception ex) {
                this.OnError?.Invoke("Saving", ex);
            }
        }

        private bool CanAcceptChanges() {
            var result = ConnectionType != ConnectionType.None &&
                LogType != LogType.None &&
                Port > 0 && connectionsViewModel.IsAvailablePort(Port) &&
                (IpAddress is null || IpAddressRegex().IsMatch(IpAddress));
            return result;
        }
    }
}
