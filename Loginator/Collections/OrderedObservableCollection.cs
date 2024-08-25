using Loginator.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Loginator.Collections {

    public class OrderedObservableCollection : ObservableCollection<LogViewModel> {

        public void AddLeading(LogViewModel item) {
            ArgumentNullException.ThrowIfNull(item, nameof(item));

            InsertItem(() => 0, item);
        }

        public new void Add(LogViewModel item) {
            ArgumentNullException.ThrowIfNull(item, nameof(item));

            InsertItem(() => FindNewIndex(item), item);
        }

        public void Add(IEnumerable<LogViewModel> items, Predicate<LogViewModel>? predicate = null) {
            ArgumentNullException.ThrowIfNull(items, nameof(items));
            predicate ??= _ => true;

            CheckReentrancy();
            bool isChanged = false;
            foreach (var item in items) {
                if (!Items.Contains(item) && predicate(item)) {
                    Items.Insert(FindNewIndex(item), item);
                    isChanged = true;
                }
            }

            if (isChanged) OnReset();
        }

        public bool Remove(IEnumerable<LogViewModel> items, Predicate<LogViewModel>? predicate = null) {
            ArgumentNullException.ThrowIfNull(items, nameof(items));
            predicate ??= l => true;

            CheckReentrancy();
            bool isChanged = false;
            foreach (var item in items) {
                if (predicate(item)) {
                    base.Remove(item);
                    isChanged = true;
                }
            }

            if (isChanged) OnReset();
            return isChanged;
        }

        protected override void InsertItem(int index, LogViewModel item) =>
            throw new InvalidOperationException("Only adding items controlled is allowed");

        internal void RaiseReset() {
            OnCollectionChanged(EventArgsCache.ResetCollectionChanged);
        }

        private int FindNewIndex(LogViewModel item) {
            for (int i = 0; i < Items.Count; i++) {
                if (Items.ElementAt(i).Timestamp < item.Timestamp) {
                    return i;
                }
            }
            return Items.Count;
        }

        private void InsertItem(Func<int> index, LogViewModel item) {
            if (Items.Contains(item)) {
                return;
            }

            base.InsertItem(index(), item);
        }

        private void OnReset() {
            OnPropertyChanged(EventArgsCache.CountPropertyChanged);
            OnPropertyChanged(EventArgsCache.IndexerPropertyChanged);
            OnCollectionChanged(EventArgsCache.ResetCollectionChanged);
        }

        private static class EventArgsCache {
            internal static readonly PropertyChangedEventArgs CountPropertyChanged = new("Count");
            internal static readonly PropertyChangedEventArgs IndexerPropertyChanged = new("Item[]");
            internal static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged = new(NotifyCollectionChangedAction.Reset);
        }
    }
}
