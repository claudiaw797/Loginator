using Backend.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace Loginator.ViewModels {

    public partial class SearchViewModel : ObservableObject {

        public static string UpdateCommandClear => "Clear";
        public static string UpdateCommandSearch => "Search";

        public event EventHandler<EventArgs>? UpdateSearch;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(UpdateCommand))]
        private string? criteria;
        partial void OnCriteriaChanged(string? value) {
            UpdateCommandName = GetUpdateCommandNameForCriteria(value);
        }

        [ObservableProperty]
        private bool isInverted;

        [ObservableProperty]
        private string updateCommandName = UpdateCommandSearch;

        [RelayCommand(CanExecute = nameof(CanUpdateSearch))]
        private void Update(string? command) {
            lock (ViewModelConstants.SYNC_OBJECT) {
                command ??= GetUpdateCommandNameForCriteria(Criteria);

                if (command == UpdateCommandClear) {
                    Criteria = null;
                }
                else if (command == UpdateCommandSearch) {
                    UpdateCommandName = UpdateCommandClear;
                }

                UpdateSearch?.Invoke(this, EventArgs.Empty);
            }
        }

        public SearchOptions ToOptions() =>
            new() {
                Criteria = Criteria,
                IsInverted = IsInverted
            };

        private static string GetUpdateCommandNameForCriteria(string? criteria) =>
            string.IsNullOrEmpty(criteria)
                ? UpdateCommandClear
                : UpdateCommandSearch;

        private bool CanUpdateSearch(string? command) {
            return !string.IsNullOrEmpty(Criteria) || UpdateCommandName == UpdateCommandClear;
        }
    }
}
