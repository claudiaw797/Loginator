// Copyright (C) 2024 Claudia Wagner

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Loginator.Application.Model;
using Loginator.Infrastructure.Option;
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
        private string port = "7071";

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private string? ipAddress;

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
                var connection = new Connection {
                    ConnectionType = this.ConnectionType,
                    LogType = this.LogType,
                    Port = Convert.ToInt32(this.Port),
                    IpAddress = this.IpAddress
                };
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
                (Port is not null && int.TryParse(Port, out var p) && connectionsViewModel.IsAvailablePort(p)) &&
                (IpAddress is null || IpAddressRegex().IsMatch(IpAddress));
            return result;
        }
    }
}
