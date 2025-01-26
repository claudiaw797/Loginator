// Copyright (C) 2025 Claudia Wagner

using FluentAssertions;
using Loginator.Application.ViewModel;
using Loginator.Domain.Model;
using NUnit.Framework.Internal;
using System.Collections;
using static Loginator.Application.UnitTests.ViewModel.TestData;

namespace Loginator.Application.UnitTests.ViewModel {

    /// <summary>
    /// Represents unit tests for <see cref="LogViewModel"/>.
    /// </summary>
    public partial class LogViewModelTests {

        private const string TEST_APPLICATION_PROCESS = $"{TEST_APPLICATION}({TEST_PROCESS})";

        [Test]
        public void Can_create_sut() {
            var log = FullLog();
            var sut = TestSut(log);

            sut.ClassName.Should().Be(log.Location?.ClassName);
            sut.FileName.Should().Be(log.Location?.FileName);
            sut.MethodName.Should().Be(log.Location?.MethodName);
            sut.LineNumber.Should().Be($"{log.Location?.LineNumber}");
        }

        [Test]
        public void Can_create_sut_without_location() {
            var log = MinLog();
            var sut = TestSut(log);

            sut.ClassName.Should().BeNull();
            sut.FileName.Should().BeNull();
            sut.MethodName.Should().BeNull();
            sut.LineNumber.Should().BeNull();
        }

        [Test]
        [TestCase(null, TEST_APPLICATION, false)]
        [TestCase(null, TEST_APPLICATION_PROCESS, false)]
        [TestCase("", TEST_APPLICATION, false)]
        [TestCase("", TEST_APPLICATION_PROCESS, false)]
        [TestCase(TEST_PROCESS, TEST_APPLICATION, true)]
        [TestCase(TEST_PROCESS, TEST_APPLICATION_PROCESS, false)]
        public void Can_display_process_appended_to_application(string? process, string application, bool expectBoth) {
            var sut = new LogViewModel(MinLog(process: process, application: application));

            var actual = sut.ApplicationProcess;

            if (expectBoth)
                actual.Should().Contain(application).And.Contain(process);
            else
                actual.Should().Be(application);
        }

        [Test]
        [TestCaseSource(typeof(LocationInfoTests))]
        public void Can_display_location_info(LocationInfo? locationInfo, bool? expectedExtend) {
            var sut = new LogViewModel(MinLog(locationInfo: locationInfo));

            var actual = sut.Location;

            if (!expectedExtend.HasValue) {
                actual.Should().BeNullOrEmpty();
            }
            else if (!expectedExtend.Value) {
                actual
                    .Should().Contain(locationInfo?.ClassName)
                    .And.Contain(locationInfo?.MethodName)
                    .And.NotContainEquivalentOf("File")
                    .And.NotContainEquivalentOf("Line");
            }
            else {
                actual
                    .Should().Contain(locationInfo?.ClassName)
                    .And.Contain(locationInfo?.MethodName)
                    .And.Contain(locationInfo?.FileName)
                    .And.Contain($"{locationInfo?.LineNumber}");
            }
        }

        [Test]
        [TestCaseSource(typeof(PropertiesTests))]
        public void Can_display_properties(Log log, bool expectedExtend) {
            var sut = new LogViewModel(log);

            var actual = sut.Properties;

            if (expectedExtend) {
                AssertProperties(actual, log);
            }
            else {
                actual.Should().BeNullOrEmpty();
            }
        }

        [Test]
        public void Can_display_all_values_in_string_representation() {
            var log = FullLog();
            var sut = new LogViewModel(log);

            var actual = sut.ToString();

            actual
                .Should().Contain(log.Timestamp.ToString())
                .And.Contain(log.Level.ToString())
                .And.Contain(log.Message)
                .And.Contain(log.Exception)
                .And.Contain(log.MachineName)
                .And.Contain(log.Namespace)
                .And.Contain(log.Application)
                .And.Contain(log.Process)
                .And.Contain(log.Thread)
                .And.Contain(log.Context)
                .And.Contain(log.Location?.ClassName)
                .And.Contain(log.Location?.FileName)
                .And.Contain(log.Location?.MethodName)
                .And.Contain($"{log.Location?.LineNumber}");
            AssertProperties(actual, log);
        }

        [Test]
        public void Can_hide_missing_values_in_string_representation() {
            var log = MinLog();
            var sut = new LogViewModel(log);

            var actual = sut.ToString();

            actual
                .Should().NotContainEquivalentOf("Process")
                .And.NotContainEquivalentOf("Context")
                .And.NotContainEquivalentOf("Thread")
                .And.NotContainEquivalentOf("Message")
                .And.NotContainEquivalentOf("Exception")
                .And.NotContainEquivalentOf("Class")
                .And.NotContainEquivalentOf("Method")
                .And.NotContainEquivalentOf("File")
                .And.NotContainEquivalentOf("Line");
        }

        private static LogViewModel TestSut(Log log) {
            var sut = new LogViewModel(log);

            sut.Timestamp.Should().Be(log.Timestamp);
            sut.Level.Should().Be(log.Level);
            sut.Message.Should().Be(log.Message);
            sut.Exception.Should().Be(log.Exception);
            sut.MachineName.Should().Be(log.MachineName);
            sut.Namespace.Should().Be(log.Namespace);
            sut.Application.Should().Be(log.Application);
            sut.Process.Should().Be(log.Process);
            sut.Thread.Should().Be(log.Thread);
            sut.Context.Should().Be(log.Context);

            return sut;
        }

        private static void AssertProperties(string? actual, Log log) {
            foreach (var property in log.Properties) {
                actual
                    .Should().Contain(property.Name)
                    .And.Contain(property.Value);
            }
        }

        private class LocationInfoTests : IEnumerable {

            public IEnumerator GetEnumerator() {
                yield return new object[] { null!, null! };
                yield return new object[] { LocationInfo(), null! };
                yield return new object[] { LocationInfo("testClass", null, "testMethod", 15), false };
                yield return new object[] { LocationInfo("testClass", string.Empty, "testMethod", 15), false };
                yield return new object[] { LocationInfo("testClass", "testFile", "testMethod", 15), true };
            }
        }

        private class PropertiesTests : IEnumerable {

            public IEnumerator GetEnumerator() {
                yield return new object[] { MinLog(), false };
                yield return new object[] { FullLog(), true };
            }
        }
    }
}