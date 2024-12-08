// Copyright (C) 2024 Claudia Wagner

using CommunityToolkit.Mvvm.ComponentModel;
using Loginator.Application.Model;
using Loginator.Infrastructure.Option;
using System;

namespace Loginator.Application.ViewModel {

    public partial class ConnectionViewModel : ObservableObject {

        private readonly Connection connection;

        public ConnectionViewModel(Connection connection) {
            ArgumentNullException.ThrowIfNull(connection);

            this.connection = connection;
        }

        public ConnectionType ConnectionType => connection.ConnectionType;
        public LogType LogType => connection.LogType;
        public string? IpAddress => connection.IpAddress;
        public string Port => connection.Port.ToString();
    }
}
