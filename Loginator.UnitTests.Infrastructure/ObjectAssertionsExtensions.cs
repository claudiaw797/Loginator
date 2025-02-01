// Copyright (C) 2025 Claudia Wagner

using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace Loginator.UnitTests.Infrastructure {

    public static class ObjectAssertionsExtensions {

        public static AndConstraint<ObjectAssertions> BeSymmetricallyAndValueEqualTo(
            this ObjectAssertions actual,
            object? expected,
            bool expectedEquals,
            string because = "",
            params object[] becauseArgs) {
            return actual
                .BeSymmetricallyEqualTo(expected, expectedEquals, because, becauseArgs).And
                .BeSymmetricallyValueEqualTo(expected, expectedEquals, because, becauseArgs);
        }

        public static AndConstraint<ObjectAssertions> BeSymmetricallyAndValueEqualTo(
            this ObjectAssertions actual,
            object? expected,
            string because = "",
            params object[] becauseArgs) {
            return actual
                .BeSymmetricallyEqualTo(expected, true, because, becauseArgs).And
                .BeSymmetricallyValueEqualTo(expected, true, because, becauseArgs);
        }

        public static AndConstraint<ObjectAssertions> NotBeSymmetricallyAndValueEqualTo(
            this ObjectAssertions actual,
            object? expected,
            string because = "",
            params object[] becauseArgs) {
            return actual
                .BeSymmetricallyEqualTo(expected, false, because, becauseArgs).And
                .BeSymmetricallyValueEqualTo(expected, false, because, becauseArgs);
        }

        public static AndConstraint<ObjectAssertions> BeSymmetricallyEqualTo(
            this ObjectAssertions actual,
            object? expected,
            bool expectedEquals,
            string because = "",
            params object[] becauseArgs) {
            if (expectedEquals) {
                return actual.BeSymmetricallyEqualTo(expected, because, becauseArgs);
            }
            else {
                return actual.NotBeSymmetricallyEqualTo(expected, because, becauseArgs);
            }
        }

        public static AndConstraint<ObjectAssertions> BeSymmetricallyEqualTo(
            this ObjectAssertions actual,
            object? expected,
            string because = "",
            params object[] becauseArgs) {
            const string messageObjectEquals = "Expected Equals({0}, {1}) to be true{reason}, but it was false.";
            const string messageActualEquals = "Expected {0}.Equals({1}) to be true{reason}, but it was false.";

            ForArguments(actual, expected, because, becauseArgs)
                .ForCondition(Equals(actual.Subject, expected))
                .Fail(messageObjectEquals, actual.Subject, expected)
                .Then
                .ForCondition(Equals(expected, actual.Subject))
                .Fail(messageObjectEquals, expected, actual.Subject)
                .Then
                .ForCondition(actual.Subject.Equals(expected))
                .Fail(messageActualEquals, actual.Subject, expected)
                .Then
                .ForCondition(expected!.Equals(actual.Subject))
                .Fail(messageActualEquals, expected, actual.Subject)
                .Then
                .ForCondition(actual.Subject.GetHashCode() == expected.GetHashCode())
                .Fail("Expected {0}.GetHashCode() == {1}.GetHashCode() to be true{reason}, but it was false.", actual.Subject, expected);

            return new AndConstraint<ObjectAssertions>(actual);
        }

        public static AndConstraint<ObjectAssertions> NotBeSymmetricallyEqualTo(
            this ObjectAssertions actual,
            object? expected,
            string because = "",
            params object[] becauseArgs) {
            const string messageObjectEquals = "Expected Equals({0}, {1}) to be false{reason}, but it was true.";
            const string messageActualEquals = "Expected {0}.Equals({1}) to be false{reason}, but it was true.";

            ForArguments(actual, because, becauseArgs)
                .ForCondition(!Equals(actual.Subject, expected))
                .Fail(messageObjectEquals, actual.Subject, expected)
                .Then
                .ForCondition(!Equals(expected, actual.Subject))
                .Fail(messageObjectEquals, expected, actual.Subject)
                .Then
                .ForCondition(actual.Subject is null || !actual.Subject.Equals(expected))
                .Fail(messageActualEquals, actual.Subject, expected)
                .Then
                .ForCondition(expected is null || !expected.Equals(actual.Subject))
                .Fail(messageActualEquals, expected, actual.Subject)
                .Then
                .ForCondition((actual.Subject is null ? 0 : actual.Subject.GetHashCode()) != (expected is null ? 0 : expected.GetHashCode()))
                .Fail("Expected {0}.GetHashCode() != {1}.GetHashCode()) to be true{reason}, but it was false.", actual.Subject, expected);

            return new AndConstraint<ObjectAssertions>(actual);
        }

        public static AndConstraint<ObjectAssertions> BeSymmetricallyValueEqualTo<T>(
            this ObjectAssertions actual,
            T? expected,
            bool expectedEquals,
            string because = "",
            params object[] becauseArgs)
            where T : class {
            ForArguments(actual, because, becauseArgs);

            Eq((T)actual.Subject, expected).Should().Be(expectedEquals);
            Eq(expected, (T)actual.Subject).Should().Be(expectedEquals);
            Neq((T)actual.Subject, expected).Should().Be(!expectedEquals);
            Neq(expected, (T)actual.Subject).Should().Be(!expectedEquals);

            return new AndConstraint<ObjectAssertions>(actual);
        }

        public static AndConstraint<ObjectAssertions> BeSymmetricallyValueEqualTo<T>(
            this ObjectAssertions actual,
            T? expected,
            string because = "",
            params object[] becauseArgs)
            where T : class {
            return actual.BeSymmetricallyValueEqualTo(expected, true, because, becauseArgs);
        }

        public static AndConstraint<ObjectAssertions> NotBeSymmetricallyValueEqualTo<T>(
            this ObjectAssertions actual,
            T? expected,
            string because = "",
            params object[] becauseArgs)
            where T : class {
            return actual.BeSymmetricallyValueEqualTo(expected, false, because, becauseArgs);
        }

        internal static Continuation Fail(this AssertionChain chain, string message, object? actual, object? expected) =>
            chain.FailWith(message, actual?.ToString(), expected?.ToString());

        internal static AssertionChain ForArguments(object? actual, string because, object[] becauseArgs) =>
            AssertionChain.GetOrCreate()
                .BecauseOf(because, becauseArgs)
                .ForCondition(actual is not null)
                .FailWith("Assertion cannot be null, but it is")
                .Then;

        internal static AssertionChain ForArguments(object? actual, object? expected, string because, object[] becauseArgs) =>
            ForArguments(actual, because, becauseArgs)
                .ForCondition(actual is not null)
                .FailWith("Expected actual to be not null, but it is")
                .Then
                .ForCondition(expected is not null)
                .FailWith("Expected expected to be not null, but it is")
                .Then;

        internal static ObjectAssertions ObjectShould(this object actual) =>
            new(actual, AssertionChain.GetOrCreate());

        private static bool Eq(dynamic? left, dynamic? right) =>
            left == right;

        private static bool Neq(dynamic? left, dynamic? right) =>
            left != right;
    }
}
