// Copyright (C) 2024 Claudia Wagner

using CommunityToolkit.Mvvm.ComponentModel;
using Loginator.Application.Option;
using Loginator.Domain.Channel;
using Loginator.Domain.Option;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace Loginator.Application.ViewModel {

    public partial class ConnectionsViewModel : ObservableObject, IAsyncDisposable {

        // repository is only used for saving, not for loading changes made to the file directly
        // connections can only be changed by the application
        private readonly IOptionsRepository<ConnectionsOptions> optionsRepository;
        private readonly ILogService logService;
        private readonly ILogger<ConnectionsViewModel> logger;

        public ConnectionsViewModel(
            IOptionsRepository<ConnectionsOptions> optionsRepository,
            ILogService logService,
            ILogger<ConnectionsViewModel> logger) {
            this.optionsRepository = optionsRepository;
            this.logService = logService;
            this.logger = logger;

            foreach (var connection in optionsRepository.Get()) {
                AddConnection(connection);
            }
        }

        public ObservableCollection<ConnectionViewModel> Connections { get; private set; } = [];

        public async ValueTask DisposeAsync() {
            if (Connections is not null) {
                AcceptChanges();
                Connections.Clear();
                Connections = null!;

                await logService.DisposeAsync().ConfigureAwait(false);
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Only checks internally, so that the same port cannot be used twice.
        /// </summary>
        public bool IsAvailablePort(int port) {
            var isAvailable = Connections.All(x => x.Port != port);
            return isAvailable;
        }

        internal void AddConnection(Connection connection) {
            Connections.Add(new(this, logService.CreateWriter(connection), logger));
        }

        internal void Start(ILogWriter logWriter) =>
            logService.StartWriter(logWriter);

        internal void Stop(ILogWriter logWriter) =>
            logService.StopWriter(logWriter);

        internal void Remove(ConnectionViewModel connectionViewModel, ILogWriter logWriter) {
            using TransactionScope scope = new TransactionScope();
            logService.RemoveWriter(logWriter);
            Connections.Remove(connectionViewModel);
            scope.Complete();
        }

        private void AcceptChanges() {
            optionsRepository.Save(options => {
                options.Clear();
                options.AddRange(Connections.Select(c => c.Connection));
            });
        }
    }
}
