// Copyright (C) 2025 Claudia Wagner

using FakeItEasy;
using FakeItEasy.Configuration;
using Loginator.Domain.Channel;
using Loginator.Domain.Option;
using System;
using System.Threading.Tasks;

namespace Loginator.Application.UnitTests.ViewModel {

    internal class TestConnection : IDisposable {

        protected readonly ILogService logService;

        private TaskCompletionSource? tcs;

        public TestConnection(Connection connection, ILogService? logService = null) {
            Connection = connection;
            LogWriter = A.Fake<ILogWriter>();
            this.logService = logService ?? A.Fake<ILogService>();

            CallToLogWriterConnection().Returns(connection);
            CallToLogWriterTask().ReturnsLazily(c => tcs is null ? Task.CompletedTask : tcs.Task);

            CallToLogServiceCreateWriter().Returns(LogWriter);
            CallToLogServiceStartWriter().Invokes(c => tcs = new TaskCompletionSource());
            CallToLogServiceStopWriter().Invokes(c => tcs?.SetCanceled());
        }

        public Connection Connection { get; private init; }

        public ILogWriter LogWriter { get; private init; }

        public bool LogWriterTaskRunning {
            get => !LogWriter.Task.IsCompleted;
            set {
                if (value)
                    tcs = new TaskCompletionSource();
                else if (tcs is not null && !tcs.Task.IsCompleted)
                    tcs?.SetCanceled();
            }
        }

        public void Dispose() => LogWriterTaskRunning = false;

        public IReturnValueArgumentValidationConfiguration<Connection> CallToLogWriterConnection() =>
            A.CallTo(() => LogWriter.Connection);

        public IReturnValueArgumentValidationConfiguration<Task> CallToLogWriterTask() =>
            A.CallTo(() => LogWriter.Task);

        public IReturnValueArgumentValidationConfiguration<ILogWriter> CallToLogServiceCreateWriter() =>
            A.CallTo(() => logService.CreateWriter(Connection));

        public IReturnValueArgumentValidationConfiguration<Task> CallToLogServiceStartWriter() =>
            A.CallTo(() => logService.StartWriterAsync(LogWriter));

        public IReturnValueArgumentValidationConfiguration<Task> CallToLogServiceStopWriter() =>
            A.CallTo(() => logService.StopWriterAsync(LogWriter));

        public IReturnValueArgumentValidationConfiguration<Task> CallToLogServiceRemoveWriter() =>
            A.CallTo(() => logService.RemoveWriterAsync(LogWriter));
    }
}