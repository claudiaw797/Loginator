// Copyright (C) 2024 Claudia Wagner

using Loginator.Domain.Model;
using Loginator.Domain.Option;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Loginator.Application.UnitTests.ViewModel {

    /// <summary>
    /// Represents test data for <see cref="ViewModel"/> tests.
    /// </summary>
    internal static class TestData {

        public const string TEST_PROCESS = "testProcess";
        public const string TEST_APPLICATION = "testApplication";
        public const int TEST_PORT = 7071;

        public static readonly object[] AllLogLevels = [
            new object[] { LogLevel.NOT_SET },
            new object[] { LogLevel.TRACE },
            new object[] { LogLevel.DEBUG },
            new object[] { LogLevel.INFO },
            new object[] { LogLevel.WARN },
            new object[] { LogLevel.ERROR },
            new object[] { LogLevel.FATAL },
        ];

        public static readonly object[] ValidLogLevels =
            AllLogLevels.Skip(1).ToArray();

        public static IEnumerable<object[]> ValidLogLevelsAndBool(bool isParentSet) {
            yield return new object[] { LogLevel.TRACE, isParentSet };
            yield return new object[] { LogLevel.DEBUG, isParentSet };
            yield return new object[] { LogLevel.INFO, isParentSet };
            yield return new object[] { LogLevel.WARN, isParentSet };
            yield return new object[] { LogLevel.ERROR, isParentSet };
            yield return new object[] { LogLevel.FATAL, isParentSet };
        }

        public static IEnumerable<object[]> DifferentLogLevels(bool isParentSet) {
            yield return new object[] { LogLevel.TRACE, LogLevel.DEBUG, isParentSet };
            yield return new object[] { LogLevel.DEBUG, LogLevel.INFO, isParentSet };
            yield return new object[] { LogLevel.INFO, LogLevel.WARN, isParentSet };
            yield return new object[] { LogLevel.WARN, LogLevel.ERROR, isParentSet };
            yield return new object[] { LogLevel.ERROR, LogLevel.FATAL, isParentSet };
            yield return new object[] { LogLevel.FATAL, LogLevel.TRACE, isParentSet };
        }

        public static Connection Connection(
            ConnectionType connectionType = ConnectionType.Udp,
            LogType logType = LogType.Log4j,
            int port = TEST_PORT,
            string? ipAddress = null,
            ConnectionState state = ConnectionState.Stopped) =>
            new() {
                ConnectionType = connectionType,
                LogType = logType,
                Port = port,
                IpAddress = ipAddress,
                State = state
            };

        public static LocationInfo LocationInfo(
            string? className = null,
            string? fileName = null,
            string? methodName = null,
            int? lineNumber = null) =>
            new() {
                ClassName = className,
                FileName = fileName,
                MethodName = methodName,
                LineNumber = lineNumber ?? 0,
            };

        public static Log Log(
            LogLevel level,
            DateTimeOffset ts,
            string application,
            string nspace,
            string? message,
            string? exception,
            string? process,
            string? thread,
            string? context,
            LocationInfo? locationInfo,
            bool addDefaultProperties = false) {
            var log = new Log() {
                Application = application,
                Namespace = nspace,
                Level = level,
                Timestamp = ts,
                Message = message,
                Exception = exception,
                MachineName = nspace,
                Process = process,
                Thread = thread,
                Context = context,
                Location = locationInfo
            };
            if (addDefaultProperties) {
                log.AddProperties([
                    new("TestProperty1", "TestValue1"),
                    new("TestProperty2", "TestValue2"),
                    new("TestProperty3", "TestValue3"),
                    new("TestProperty4", "TestValue4"),
                ]);
            }
            return log;
        }

        public static Log MinLog(
            LogLevel? level = null,
            DateTimeOffset? ts = null,
            string? application = null,
            string? nspace = null,
            string? message = null,
            string? exception = null,
            string? process = null,
            string? thread = null,
            string? context = null,
            LocationInfo? locationInfo = null) =>
            Log(
                level ?? LogLevel.INFO,
                ts ?? DateTimeOffset.Now,
                application ?? TEST_APPLICATION,
                nspace ?? "test.namespace",
                message,
                exception,
                process,
                thread,
                context,
                locationInfo);

        public static Log FullLog(
            LogLevel? level = null,
            DateTimeOffset? ts = null,
            string? application = null,
            string? nspace = null,
            string? message = null,
            string? exception = null,
            string? process = null,
            string? thread = null,
            string? context = null,
            LocationInfo? locationInfo = null) =>
            Log(
                level ?? LogLevel.INFO,
                ts ?? DateTimeOffset.Now,
                application ?? TEST_APPLICATION,
                nspace ?? "test.namespace",
                message ?? "testMessage",
                exception ?? "testException",
                process ?? TEST_PROCESS,
                thread ?? "testThread",
                context ?? "testContext",
                locationInfo ?? LocationInfo("testClass", "testFile", "testMethod", 15),
                true);

        public static void AddRange<T>(this ObservableCollection<T> collection, params T[] items) {
            foreach (var item in items) {
                collection.Add(item);
            }
        }
    }
}