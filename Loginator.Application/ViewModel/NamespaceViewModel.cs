// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using CommunityToolkit.Mvvm.ComponentModel;
using Loginator.Domain.Model;
using System;
using System.Collections.ObjectModel;
using System.Text;
using static Loginator.Domain.Common.Constants;

namespace Loginator.Application.ViewModel {

    public partial class NamespaceViewModel : ObservableObject {

        private readonly ApplicationViewModel applicationViewModel;
        private readonly Lazy<string> fullName;

        public NamespaceViewModel(string name, ApplicationViewModel applicationViewModel) {
            ArgumentNullException.ThrowIfNull(applicationViewModel);

            this.applicationViewModel = applicationViewModel;
            this.Name = name;

            isActive = true;
            isExpanded = true;
            fullName = new(this.GetFullName);
        }

        [ObservableProperty]
        private bool isActive;
        partial void OnIsActiveChanged(bool value) {
            lock (Constants.SyncObject) {
                applicationViewModel.UpdateIsActive(this);
            }

            foreach (var child in Children) {
                child.IsActive = value;
            }
        }

        [ObservableProperty]
        private bool isExpanded;

        [ObservableProperty]
        private bool isHighlighted;

        public int Count { get; private set; }
        public int CountTrace { get; private set; }
        public int CountDebug { get; private set; }
        public int CountInfo { get; private set; }
        public int CountWarn { get; private set; }
        public int CountError { get; private set; }
        public int CountFatal { get; private set; }

        public NamespaceViewModel? Parent { get; init; }

        public ObservableCollection<NamespaceViewModel> Children { get; private set; } = [];

        public string Name { get; private set; }

        public string Fullname => fullName.Value;

        internal void ClearLogData() {
            this.Count = 0;
            this.CountTrace = 0;
            this.CountDebug = 0;
            this.CountInfo = 0;
            this.CountWarn = 0;
            this.CountError = 0;
            this.CountFatal = 0;
            this.IsHighlighted = false;
        }

        internal void UpdateLogCounts(LogViewModel log) {
            this.Count++;

            if (log.Level == LogLevel.TRACE) {
                this.CountTrace++;
            }
            else if (log.Level == LogLevel.DEBUG) {
                this.CountDebug++;
            }
            else if (log.Level == LogLevel.INFO) {
                this.CountInfo++;
            }
            else if (log.Level == LogLevel.WARN) {
                this.CountWarn++;
            }
            else if (log.Level == LogLevel.ERROR) {
                this.CountError++;
            }
            else if (log.Level == LogLevel.FATAL) {
                this.CountFatal++;
            }
        }

        private string GetFullName() {
            var fullname = new StringBuilder(this.Name);
            var parent = this.Parent;
            while (parent is not null) {
                fullname.Insert(0, NamespaceSplitter);
                fullname.Insert(0, parent.Name);
                parent = parent.Parent;
            }
            return fullname.ToString();
        }
    }
}
