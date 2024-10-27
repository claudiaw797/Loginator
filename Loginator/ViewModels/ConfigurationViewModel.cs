// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Backend.Model;
using Common;
using Common.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;

namespace Loginator.ViewModels {

    public partial class ConfigurationViewModel : ObservableObject {

        private readonly IWritableOptions<Configuration> configurationDao;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private ConnectionType connectionType;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private LogType logType;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private string port;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private LogTimeFormat logTimeFormat;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private ApplicationFormat applicationFormat;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private KnownCulture language;

        public Action? CloseAction { get; set; }

        public ConfigurationViewModel(IWritableOptions<Configuration> configurationDao) {
            this.configurationDao = configurationDao;

            var configuration = configurationDao.Value;
            connectionType = configuration.ConnectionType;
            logType = configuration.LogType;
            port = configuration.Port.ToString();
            logTimeFormat = configuration.LogTimeFormat;
            applicationFormat = configuration.ApplicationFormat;
            language = configuration.Language;
        }

        [RelayCommand]
        private void CancelChanges() {
            try {
                CloseAction?.Invoke();
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, "Error canceling configuration changes", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
            }
        }

        [RelayCommand(CanExecute = nameof(CanAcceptChanges))]
        private void AcceptChanges() {
            try {
                configurationDao.Update(c => {
                    c.ConnectionType = ConnectionType;
                    c.LogType = LogType;
                    c.Port = Convert.ToInt32(Port);
                    c.LogTimeFormat = LogTimeFormat;
                    c.ApplicationFormat = ApplicationFormat;
                    c.Language = Language;
                });

                CloseAction?.Invoke();
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, "Error saving configuration changes", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
            }
        }

        private bool CanAcceptChanges() {
            var configuration = configurationDao.Value;
            var result =
                ConnectionType != configuration.ConnectionType ||
                LogType != configuration.LogType ||
                Port != configuration.Port.ToString() ||
                LogTimeFormat != configuration.LogTimeFormat ||
                ApplicationFormat != configuration.ApplicationFormat ||
                Language != configuration.Language;
            return result;
        }
    }
}
