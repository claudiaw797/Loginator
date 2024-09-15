// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Backend;
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
        private LogType logType;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private string portChainsaw;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private string portLogcat;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private LogTimeFormat logTimeFormat;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AcceptChangesCommand))]
        private ApplicationFormat applicationFormat;

        public Action? CloseAction { get; set; }

        public ConfigurationViewModel(IWritableOptions<Configuration> configurationDao) {
            this.configurationDao = configurationDao;

            var configuration = configurationDao.Value;
            logType = configuration.LogType;
            portChainsaw = configuration.PortChainsaw.ToString();
            portLogcat = configuration.PortLogcat.ToString();
            logTimeFormat = configuration.LogTimeFormat;
            applicationFormat = configuration.ApplicationFormat;
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
                    c.LogType = LogType;
                    c.PortChainsaw = Convert.ToInt32(PortChainsaw);
                    c.PortLogcat = Convert.ToInt32(PortLogcat);
                    c.LogTimeFormat = LogTimeFormat;
                    c.ApplicationFormat = ApplicationFormat;
                });
                IoC.Get<IReceiver>().Initialize(configurationDao.Value);

                CloseAction?.Invoke();
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, "Error saving configuration changes", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK);
            }
        }

        private bool CanAcceptChanges() {
            var configuration = configurationDao.Value;
            var result = LogType != configuration.LogType ||
                PortChainsaw != configuration.PortChainsaw.ToString() ||
                PortLogcat != configuration.PortLogcat.ToString() ||
                LogTimeFormat != configuration.LogTimeFormat ||
                ApplicationFormat != configuration.ApplicationFormat;
            return result;
        }
    }
}
