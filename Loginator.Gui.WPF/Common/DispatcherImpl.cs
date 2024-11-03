// Copyright (C) 2024 Claudia Wagner

using Loginator.Application.Service;
using System;
using System.Windows.Threading;

namespace Loginator.Gui.WPF.Common {

    /// <summary>
    /// Helper class for dispatcher operations on the UI thread.
    /// </summary>
    /// <remarks>This is a copy of <c>Threading.DispatcherHelper</c> from the deprecated GalaSoft.MvvmLight package.</remarks>
    internal class DispatcherImpl : IDispatcher {

        /// <summary>
        /// Gets a reference to the UI thread's dispatcher, after the <see cref="Initialize"/> method has been called on the UI thread.
        /// </summary>
        public Dispatcher? UIDispatcher { get; private set; }

        /// <summary>
        /// Executes an action on the UI thread.
        /// If this method is called from the UI thread, the action is executed immediately.
        /// If the method is called from another thread, the action will be enqueued on the UI thread's dispatcher and executed asynchronously.
        /// For additional operations on the UI thread, you can get a reference to the UI thread's dispatcher thanks to the property <see cref="UIDispatcher"/>.
        /// </summary>
        /// <param name="action">The action that will be executed on the UI thread.</param>
        public void BeginInvokeOnUIThread(Action action) {
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

        /// <summary>
        /// Invokes an action asynchronously on the UI thread.
        /// </summary>
        /// <param name="action">The action that must be executed.</param>
        /// <returns>An object, which is returned immediately after BeginInvoke is called, that can
        /// be used to interact with the delegate as it is pending execution in the event queue.</returns>
        public DispatcherOperation RunAsync(Action action) {
            CheckDispatcher();
            return UIDispatcher!.BeginInvoke(action);
        }

        /// <summary>
        /// This method should be called once on the UI thread to ensure that the <see cref="UIDispatcher"/> property is initialized.
        /// In WPF, call this method on the static App() constructor.
        /// </summary>
        public void Initialize() {
            if (UIDispatcher is null || !UIDispatcher.Thread.IsAlive) {
                UIDispatcher = Dispatcher.CurrentDispatcher;
            }
        }

        /// <summary>
        /// Resets the class by deleting the <see cref="UIDispatcher"/> property.
        /// </summary>
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
