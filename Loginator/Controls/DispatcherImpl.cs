// Copyright (C) 2024 Claudia Wagner

using System;
using System.Windows.Threading;

namespace Loginator.Controls {

    /// <summary>
    /// Helper class for dispatcher operations on the UI thread.
    /// </summary>
    /// <remarks>This is a copy of <c>Threading.DispatcherHelper</c> from the deprecated GalaSoft.MvvmLight package.</remarks>
    public class DispatcherImpl : IDispatcher {

        /// <inheritdoc/>
        public Dispatcher? UIDispatcher { get; private set; }

        /// <inheritdoc/>
        public void CheckBeginInvokeOnUI(Action action) {
            if (action is not null) {
                CheckDispatcher();
                if (UIDispatcher!.CheckAccess()) {
                    action();
                }
                else {
                    UIDispatcher.BeginInvoke(action);
                }
            }
        }

        /// <inheritdoc/>
        public DispatcherOperation RunAsync(Action action) {
            CheckDispatcher();
            return UIDispatcher!.BeginInvoke(action);
        }

        /// <inheritdoc/>
        public void Initialize() {
            if (UIDispatcher is null || !UIDispatcher.Thread.IsAlive) {
                UIDispatcher = Dispatcher.CurrentDispatcher;
            }
        }

        /// <inheritdoc/>
        public void Reset() {
            UIDispatcher = null;
        }

        private void CheckDispatcher() {
            if (UIDispatcher is null) {
                var message = ($"The DispatcherHelper is not initialized.{Environment.NewLine}Call DispatcherHelper.Initialize() in the static App constructor.");
                throw new InvalidOperationException(message);
            }
        }
    }
}
