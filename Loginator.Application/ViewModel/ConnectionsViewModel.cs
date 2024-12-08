// Copyright (C) 2024 Claudia Wagner

using CommunityToolkit.Mvvm.ComponentModel;
using Loginator.Application.Model;
using Loginator.Application.Option;
using Loginator.Domain.Option;
using System.Collections.ObjectModel;
using System.Linq;

namespace Loginator.Application.ViewModel {

    public partial class ConnectionsViewModel : ObservableObject {

        // repository is only used for saving, not for loading changes made to the file directly
        // connections can only be changed by the application
        private readonly IOptionsRepository<ConnectionsOptions> optionsRepository;
        private readonly ConnectionsOptions connections;

        public ConnectionsViewModel(IOptionsRepository<ConnectionsOptions> optionsRepository) {
            this.optionsRepository = optionsRepository;
            connections = optionsRepository.Get();

            foreach (var connection in connections) {
                Connections.Add(new(connection));
            }
        }

        public ObservableCollection<ConnectionViewModel> Connections { get; init; } = [];

        /// <summary>
        /// Only checks internally, so that the same port cannot be used twice.
        /// </summary>
        public bool IsAvailablePort(int port) {
            var isAvailable = connections.All(x => x.Port != port);
            return isAvailable;
        }

        internal void AddConnection(Connection connection) {
            connections.Add(connection);
            Connections.Add(new(connection));

            optionsRepository.Save(options => {
                options.Clear();
                options.AddRange(connections);
            });
        }
    }
}
