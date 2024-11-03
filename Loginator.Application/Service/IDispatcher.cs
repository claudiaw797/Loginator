// Copyright (C) 2024 Claudia Wagner

using System;

namespace Loginator.Application.Service {

    public interface IDispatcher {

        /// <summary>
        /// Executes an action on the UI thread.
        /// If this method is called from the UI thread, the action is executed immediately.
        /// If the method is called from another thread, the action will be enqueued on the UI thread's dispatcher and executed asynchronously.
        /// </summary>
        /// <param name="action">The action that will be executed on the UI thread.</param>
        void BeginInvokeOnUIThread(Action action);

        /// <summary>
        /// This method should be called once on the UI thread.
        /// </summary>
        void Initialize();
    }
}
