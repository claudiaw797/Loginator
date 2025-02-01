// Copyright (C) 2025 Claudia Wagner

using FluentAssertions;
using Loginator.Domain.Model;
using Loginator.UnitTests.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using static Loginator.Domain.UnitTests.Model.LogTestData;

namespace Loginator.Domain.UnitTests.Model {

    /// <summary>
    /// Represents unit tests for <see cref="Log"/>.
    /// </summary>
    public class LogTests {

        [Test]
        public void Can_create_default_log_from_no_values() {
            var sut = new Log();

            sut.Should().BeEquivalentTo(Log.DEFAULT, c => c.Using(new LogComparer()));
        }

        [Test]
        public void Can_determine_equality_from_restricted_set_of_values() {
            var sutA = CreateLog();
            var sutB = CreateLog();

            sutA.Should().BeSymmetricallyAndValueEqualTo(sutB);
        }

        [Test]
        public void Can_determine_equality_with_same_instance() {
            var sut = CreateLog();

            sut.Should().BeSymmetricallyAndValueEqualTo(sut);
        }

        [Test]
        public void Can_determine_inequality_with_different_timestamp() {
            var sutA = CreateLog(timestamp: Now);
            var sutB = CreateLog(timestamp: Now.AddTicks(1));

            sutA.Should().NotBeSymmetricallyAndValueEqualTo(sutB);
        }

        [Test]
        public void Can_determine_inequality_with_different_level() {
            var sutA = CreateLog(level: LogLevel.DEBUG);
            var sutB = CreateLog(level: LogLevel.WARN);

            sutA.Should().NotBeSymmetricallyAndValueEqualTo(sutB);
        }

        [Test]
        public void Can_determine_inequality_with_different_message() {
            var sutA = CreateLog(message: "message1");
            var sutB = CreateLog(message: "message2");

            sutA.Should().NotBeSymmetricallyAndValueEqualTo(sutB);
        }

        [Test]
        public void Can_determine_inequality_with_different_application() {
            var sutA = CreateLog(application: "application1");
            var sutB = CreateLog(application: "application2");

            sutA.Should().NotBeSymmetricallyAndValueEqualTo(sutB);
        }

        [Test]
        public void Can_determine_inequality_with_different_process() {
            var sutA = CreateLog(process: "process1");
            var sutB = CreateLog(process: "process2");

            sutA.Should().NotBeSymmetricallyAndValueEqualTo(sutB);
        }

        [Test]
        public void Can_determine_inequality_with_different_namespace() {
            var sutA = CreateLog(nspace: "namespace1");
            var sutB = CreateLog(nspace: "namespace2");

            sutA.Should().NotBeSymmetricallyAndValueEqualTo(sutB);
        }

        [Test]
        public void Can_determine_inequality_with_different_thread() {
            var sutA = CreateLog(thread: "thread1");
            var sutB = CreateLog(thread: "thread2");

            sutA.Should().NotBeSymmetricallyAndValueEqualTo(sutB);
        }

        [Test]
        public void Can_determine_inequality_with_null() {
            var sut = CreateLog();

            sut.Should().NotBeSymmetricallyAndValueEqualTo(null);
        }

        [Test]
        public void Can_internally_add_properties_sorted() {
            IEnumerable<Property> expected = [
                new("TestProperty1", "TestValue1"),
                new("TestProperty2", "TestValue2"),
                new("TestProperty3", "TestValue3"),
                new("TestProperty4", "TestValue4"),
                ];
            var shuffled = expected.Shuffle().ToArray();
            var sut = CreateLog();

            sut.AddProperties(shuffled.Take(2));
            sut.AddProperties(shuffled.Skip(2));

            sut.Properties.Should().BeEquivalentTo(expected, c => c.WithStrictOrdering());
        }
    }
}