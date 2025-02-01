// Copyright (C) 2025 Claudia Wagner

using FluentAssertions;
using Loginator.Domain.Model;
using Loginator.UnitTests.Infrastructure;

namespace Loginator.Domain.UnitTests.Model {

    /// <summary>
    /// Represents unit tests for <see cref="LocationInfo"/>.
    /// </summary>
    public class LocationInfoTests {

        [Test]
        public void Can_determine_equality_with_same_values() {
            var sutA = CreateLocationInfo();
            var sutB = CreateLocationInfo();

            sutA.Should().BeSymmetricallyAndValueEqualTo(sutB);
        }

        [Test]
        public void Can_determine_equality_with_same_instance() {
            var sut = CreateLocationInfo();

            sut.Should().BeSymmetricallyAndValueEqualTo(sut);
        }

        [Test]
        public void Can_determine_inequality_with_different_class() {
            var sutA = CreateLocationInfo(@class: "TestClass1");
            var sutB = CreateLocationInfo(@class: "TestClass2");

            sutA.Should().NotBeSymmetricallyAndValueEqualTo(sutB);
        }

        [Test]
        public void Can_determine_inequality_with_different_file() {
            var sutA = CreateLocationInfo(file: "TestFile1");
            var sutB = CreateLocationInfo(file: "TestFile2");

            sutA.Should().NotBeSymmetricallyAndValueEqualTo(sutB);
        }

        [Test]
        public void Can_determine_inequality_with_different_method() {
            var sutA = CreateLocationInfo(method: "TestMethod1");
            var sutB = CreateLocationInfo(method: "TestMethod2");

            sutA.Should().NotBeSymmetricallyAndValueEqualTo(sutB);
        }

        [Test]
        public void Can_determine_inequality_with_different_line() {
            var sutA = CreateLocationInfo(line: 1);
            var sutB = CreateLocationInfo(line: 2);

            sutA.Should().NotBeSymmetricallyAndValueEqualTo(sutB);
        }

        [Test]
        public void Can_determine_inequality_with_null() {
            var sut = CreateLocationInfo();

            sut.Should().NotBeSymmetricallyAndValueEqualTo(null);
        }

        [Test]
        public void Can_determine_if_is_empty() {
            var sutEmptyA = CreateLocationInfo(null, null, null);
            var sutEmptyB = CreateLocationInfo(string.Empty, string.Empty, string.Empty, -1);
            var sutA = CreateLocationInfo("Test", null, null);
            var sutB = CreateLocationInfo(null, "Test", null);
            var sutC = CreateLocationInfo(null, null, "Test");
            var sutD = CreateLocationInfo(null, null, null, 1);

            sutEmptyA.IsEmpty().Should().BeTrue();
            sutEmptyB.IsEmpty().Should().BeTrue();
            sutA.IsEmpty().Should().BeFalse();
            sutB.IsEmpty().Should().BeFalse();
            sutC.IsEmpty().Should().BeFalse();
            sutD.IsEmpty().Should().BeFalse();
        }

        private static LocationInfo CreateLocationInfo(
            string? @class = "TestClass",
            string? file = "TestFile",
            string? method = "TestMethod",
            int line = 0) =>
           new() {
               ClassName = @class,
               FileName = file,
               MethodName = method,
               LineNumber = line,
           };
    }
}