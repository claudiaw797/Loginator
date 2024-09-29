// Copyright (C) 2024 Claudia Wagner

using System;
using System.Windows.Threading;

namespace Loginator.Controls {

    public interface IDispatcher {

        /// <summary>
        /// Gets a reference to the UI thread's dispatcher, after the <see cref="Initialize"/> method has been called on the UI thread.
        /// </summary>
        Dispatcher? UIDispatcher { get; }

        /// <summary>
        /// Executes an action on the UI thread.
        /// If this method is called from the UI thread, the action is executed immediately.
        /// If the method is called from another thread, the action will be enqueued on the UI thread's dispatcher and executed asynchronously.
        /// For additional operations on the UI thread, you can get a reference to the UI thread's dispatcher thanks to the property <see cref="UIDispatcher"/>.
        /// </summary>
        /// <param name="action">The action that will be executed on the UI thread.</param>
        void CheckBeginInvokeOnUI(Action action);

        /// <summary>
        /// Invokes an action asynchronously on the UI thread.
        /// </summary>
        /// <param name="action">The action that must be executed.</param>
        /// <returns>An object, which is returned immediately after BeginInvoke is called, that can
        /// be used to interact with the delegate as it is pending execution in the event queue.</returns>
        DispatcherOperation RunAsync(Action action);

        /// <summary>
        /// This method should be called once on the UI thread to ensure that the <see cref="UIDispatcher"/> property is initialized.
        /// In WPF, call this method on the static App() constructor.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Resets the class by deleting the <see cref="UIDispatcher"/> property.
        /// </summary>
        void Reset();
    }
}
