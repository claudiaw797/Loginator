// Copyright (C) 2025 Claudia Wagner

using FluentAssertions;
using FluentAssertions.Numeric;
using System;
using static FluentAssertions.FluentActions;
using static Loginator.UnitTests.Infrastructure.ObjectAssertionsExtensions;

namespace Loginator.UnitTests.Infrastructure {

    public static class ComparableTypeAssertionsExtensions {

        public static AndConstraint<ComparableTypeAssertions<T>> BeSymmetricallyEqualAndRankedTo<T>(
            this ComparableTypeAssertions<T> actual,
            T? expected,
            object expectedToThrow,
            bool expectedEquals,
            string because = "",
            params object[] becauseArgs)
            where T : class, IComparable {
            actual.Subject.ObjectShould().BeSymmetricallyAndValueEqualTo(expected, expectedEquals, because, becauseArgs);

            return actual
                .BeSymmetricallyRankedEqualTo(expected, expectedEquals, because, becauseArgs).And
                .BeEdgeCaseComparableTo(expectedToThrow, because, becauseArgs);
        }

        public static AndConstraint<ComparableTypeAssertions<T>> BeSymmetricallyEqualAndRankedTo<T>(
            this ComparableTypeAssertions<T> actual,
            T? expected,
            object expectedToThrow,
            string because = "",
            params object[] becauseArgs)
            where T : class, IComparable {
            return actual.BeSymmetricallyEqualAndRankedTo(expected, expectedToThrow, true, because, becauseArgs);
        }

        public static AndConstraint<ComparableTypeAssertions<T>> NotBeSymmetricallyEqualAndRankedTo<T>(
            this ComparableTypeAssertions<T> actual,
            T? expected,
            object expectedToThrow,
            string because = "",
            params object[] becauseArgs)
            where T : class, IComparable {
            return actual.BeSymmetricallyEqualAndRankedTo(expected, expectedToThrow, false, because, becauseArgs);
        }

        public static AndConstraint<ComparableTypeAssertions<T>> BeSymmetricallyEqualAndRankedToEqualValue<T>(
            this ComparableTypeAssertions<T> actual,
            T expected,
            object expectedToThrow,
            string because = "",
            params object[] becauseArgs)
            where T : class, IComparable {
            return actual
                .BeSymmetricallyEqualAndRankedTo(expected, expectedToThrow, true, because, becauseArgs).And
                .BeSymmetricallyComparableWithEqualValue(expected, because, becauseArgs);
        }

        public static AndConstraint<ComparableTypeAssertions<T>> BeSymmetricallyEqualAndRankedToLowerValue<T>(
            this ComparableTypeAssertions<T> actual,
            T expected,
            object expectedToThrow,
            string because = "",
            params object[] becauseArgs)
            where T : class, IComparable {
            ForArguments(actual, because, becauseArgs);

            return actual
                .BeSymmetricallyEqualAndRankedTo(expected, expectedToThrow, false, because, becauseArgs).And
                .BeSymmetricallyComparableWithLowerValue(expected, because, becauseArgs);
        }

        public static AndConstraint<ComparableTypeAssertions<T>> BeSymmetricallyEqualTo<T>(
            this ComparableTypeAssertions<T> actual,
            T? expected,
            string because = "",
            params object[] becauseArgs) {
            actual.Subject.ObjectShould().BeSymmetricallyEqualTo(expected, because, becauseArgs);

            return new AndConstraint<ComparableTypeAssertions<T>>(actual);
        }

        public static AndConstraint<ComparableTypeAssertions<T>> NotBeSymmetricallyEqualTo<T>(
            this ComparableTypeAssertions<T> actual,
            T? expected,
            string because = "",
            params object[] becauseArgs) {
            actual.Subject.ObjectShould().NotBeSymmetricallyEqualTo(expected, because, becauseArgs);

            return new AndConstraint<ComparableTypeAssertions<T>>(actual);
        }

        public static AndConstraint<ComparableTypeAssertions<T>> BeSymmetricallyRankedEqualTo<T>(
            this ComparableTypeAssertions<T> actual,
            T? expected,
            bool expectedEquals,
            string because = "",
            params object[] becauseArgs)
            where T : IComparable {
            if (expectedEquals) {
                return actual.BeSymmetricallyRankedEqualTo(expected, because, becauseArgs);
            }
            else {
                return actual.NotBeSymmetricallyRankedEqualTo(expected, because, becauseArgs);
            }
        }

        public static AndConstraint<ComparableTypeAssertions<T>> BeSymmetricallyRankedEqualTo<T>(
            this ComparableTypeAssertions<T> actual,
            T? expected,
            string because = "",
            params object[] becauseArgs)
            where T : IComparable {
            const string message = "Expected {0} to be ranked equally to {1}{reason}.";

            ForArguments(actual, expected, because, becauseArgs)
                .ForCondition(actual.Subject.CompareTo(expected) == 0)
                .Fail(message, actual.Subject, expected)
                .Then
                .ForCondition(expected!.CompareTo(actual.Subject) == 0)
                .Fail(message, expected, actual.Subject);

            return new AndConstraint<ComparableTypeAssertions<T>>(actual);
        }

        public static AndConstraint<ComparableTypeAssertions<T>> NotBeSymmetricallyRankedEqualTo<T>(
            this ComparableTypeAssertions<T> actual,
            T? expected,
            string because = "",
            params object[] becauseArgs)
            where T : IComparable {
            const string message = "Expected {0} not to be ranked equally to {1}{reason}.";

            ForArguments(actual, because, becauseArgs)
                .ForCondition(actual.Subject is null || actual.Subject.CompareTo(expected) != 0)
                .Fail(message, actual.Subject, expected)
                .Then
                .ForCondition(expected is null || expected.CompareTo(actual.Subject) != 0)
                .Fail(message, expected, actual.Subject);

            return new AndConstraint<ComparableTypeAssertions<T>>(actual);
        }

        public static AndConstraint<ComparableTypeAssertions<T>> BeSymmetricallyRankedPreceding<T>(
            this ComparableTypeAssertions<T> actual,
            T expected,
            string because = "",
            params object[] becauseArgs)
            where T : IComparable {
            ForArguments(actual, expected, because, becauseArgs)
                .ForCondition(actual.Subject.CompareTo(expected) < 0)
                .FailWith("Expected {context:object} {0} to precede {1} in the sort order{reason}.", actual.Subject, expected)
                .Then
                .ForCondition(expected!.CompareTo(actual.Subject) > 0)
                .FailWith("Expected {context:object} {0} to follow {1} in the sort order{reason}.", expected, actual.Subject);

            return new AndConstraint<ComparableTypeAssertions<T>>(actual);
        }

        public static AndConstraint<ComparableTypeAssertions<T>> BeSymmetricallyRankedFollowing<T>(
            this ComparableTypeAssertions<T> actual,
            T expected,
            string because = "",
            params object[] becauseArgs)
            where T : IComparable {
            ForArguments(actual, expected, because, becauseArgs)
                .ForCondition(actual.Subject.CompareTo(expected) > 0)
                .FailWith("Expected {context:object} {0} to follow {1} in the sort order{reason}.", actual.Subject, expected)
                .Then
                .ForCondition(expected!.CompareTo(actual.Subject) < 0)
                .FailWith("Expected {context:object} {0} to precede {1} in the sort order{reason}.", expected, actual.Subject);

            return new AndConstraint<ComparableTypeAssertions<T>>(actual);
        }

        public static AndConstraint<ComparableTypeAssertions<T>> BeEdgeCaseComparableTo<T>(
            this ComparableTypeAssertions<T> actual,
            object expectedToThrow,
            string because = "",
            params object[] becauseArgs)
            where T : IComparable {
            ForArguments(actual, because, becauseArgs);

            ((T)actual.Subject).CompareTo((object?)null).Should()
                .BePositive();

            Invoking(() => ((T)actual.Subject).CompareTo(expectedToThrow)).Should()
                .Throw<ArgumentException>()
                .WithMessage($"*{GetNameOfT<T>()}*");

            return new AndConstraint<ComparableTypeAssertions<T>>(actual);
        }

        public static AndConstraint<ComparableTypeAssertions<T>> BeSymmetricallyComparableWithEqualValue<T>(
            this ComparableTypeAssertions<T> actual,
            T expected,
            string because = "",
            params object[] becauseArgs)
            where T : IComparable {
            ForArguments(actual, expected, because, becauseArgs);

            Gte((T)actual.Subject, expected).Should().BeTrue();
            Gte(expected, (T)actual.Subject).Should().BeTrue();
            Lte((T)actual.Subject, expected).Should().BeTrue();
            Lte(expected, (T)actual.Subject).Should().BeTrue();

            return new AndConstraint<ComparableTypeAssertions<T>>(actual);
        }

        public static AndConstraint<ComparableTypeAssertions<T>> BeSymmetricallyComparableWithLowerValue<T>(
            this ComparableTypeAssertions<T> actual,
            T expected,
            string because = "",
            params object[] becauseArgs)
            where T : IComparable {
            ForArguments(actual, expected, because, becauseArgs);

            Gt((T)actual.Subject, expected).Should().BeTrue();
            Gte((T)actual.Subject, expected).Should().BeTrue();
            Lt(expected, (T)actual.Subject).Should().BeTrue();
            Lte(expected, (T)actual.Subject).Should().BeTrue();

            return new AndConstraint<ComparableTypeAssertions<T>>(actual);
        }

        private static bool Lt(dynamic left, dynamic right) =>
            left < right;

        private static bool Lte(dynamic left, dynamic right) =>
            left <= right;

        private static bool Gt(dynamic left, dynamic right) =>
            left > right;

        private static bool Gte(dynamic left, dynamic right) =>
            left >= right;

        private static string GetNameOfT<T>() {
            var type = typeof(T);
            var name = type.IsGenericType && type.BaseType is not null ? type.BaseType.Name : type.Name;
            return name;
        }
    }
}
