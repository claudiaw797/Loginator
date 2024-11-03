// Copyright (C) 2024 Claudia Wagner

using FluentAssertions;
using Loginator.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Loginator.Domain.UnitTests.Model {

    /// <summary>
    /// Represents unit tests for <see cref="LogLevel"/>.
    /// </summary>
    public class LogLevelTests {

        [TestCaseSource(typeof(LogLevelTestData), nameof(LogLevelTestData.ValidLevels))]
        public void Can_determine_equality(LogLevel sut) {
            AssertEqualityAndSameOrder(sut, sut, true);
        }

        [TestCaseSource(typeof(LogLevelTestData), nameof(LogLevelTestData.ValidLevels))]
        public void Can_compare_with_null(LogLevel sut) {
            AssertUnequalityAndOrderWithNull(sut);
        }

        [TestCaseSource(typeof(LogLevelTestData), nameof(LogLevelTestData.ValidLevels))]
        public void Can_determine_greater_and_less_than_or_equal_for_equal_levels(LogLevel sut) {
            AssertGreaterAndLessThanOrEqualForEqualLevels(sut, sut);
        }

        [TestCaseSource(typeof(LogLevelTestData), nameof(LogLevelTestData.FirstLowerThanSecond))]
        public void Can_determine_greater_and_less_than_or_equal_for_unequal_levels(LogLevel sutLower, LogLevel sutHigher) {
            AssertGreaterAndLessThanOrEqualForUnequalLevels(sutLower, sutHigher);
        }

        [Test]
        public void Can_determine_all_valid_levels_sortable() {
            LogLevel[] expected = [LogLevel.TRACE, LogLevel.DEBUG, LogLevel.INFO, LogLevel.WARN, LogLevel.ERROR, LogLevel.FATAL];
            var actual = LogLevel.AllLogLevels.Order();

            actual.Should().BeEquivalentTo(expected, c => c.WithStrictOrdering());
        }

        [TestCaseSource(typeof(LogLevelTestData), nameof(LogLevelTestData.LevelsBetweenEmptyResult))]
        public void Can_determine_empty_levels_between(LogLevel? sutFrom, LogLevel? sutTo) {
            var actual = LogLevel.GetLogLevelsBetween(ref sutFrom, ref sutTo);

            actual.Should().NotBeNull();
            actual.Should().BeEmpty();
        }

        [TestCaseSource(typeof(LogLevelTestData), nameof(LogLevelTestData.LevelsBetween))]
        public void Can_determine_levels_between(LogLevel sutFrom, LogLevel sutTo, IEnumerable<LogLevel> expected) {
            AssertLevelsBetween(sutFrom, sutTo, expected);
            AssertLevelsBetween(sutTo, sutFrom, expected);
        }

        [TestCaseSource(typeof(LogLevelTestData), nameof(LogLevelTestData.ValidLevelsByName))]
        public void Can_determine_valid_levels_by_name(string name, LogLevel expected) {
            var actual = LogLevel.FromName(name);

            actual.Should().Be(expected);
        }

        [TestCaseSource(typeof(LogLevelTestData), nameof(LogLevelTestData.ValidLevelsByShortName))]
        public void Can_determine_valid_levels_by_short_name(char shortName, LogLevel expected) {
            var actual = LogLevel.FromShortName(shortName);

            actual.Should().Be(expected);
        }

        private static void AssertEqualityAndSameOrder(LogLevel levelLeft, LogLevel levelRight, bool isExpectedEqual) {
            AssertEquality(levelLeft, levelRight, isExpectedEqual);
            AssertOrder(levelLeft, levelRight, isExpectedEqual);

            (levelLeft == levelRight).Should().Be(isExpectedEqual);
            (levelRight == levelLeft).Should().Be(isExpectedEqual);

            (levelLeft != levelRight).Should().Be(!isExpectedEqual);
            (levelRight != levelLeft).Should().Be(!isExpectedEqual);
        }

        private static void AssertGreaterAndLessThanOrEqualForEqualLevels(LogLevel levelLeft, LogLevel levelRight) {
            (levelLeft >= levelRight).Should().BeTrue();
            (levelLeft <= levelRight).Should().BeTrue();
            (levelRight >= levelLeft).Should().BeTrue();
            (levelRight <= levelLeft).Should().BeTrue();

            AssertEqualityAndSameOrder(levelLeft, levelRight, true);
        }

        private static void AssertGreaterAndLessThanOrEqualForUnequalLevels(LogLevel lower, LogLevel higher) {
            (higher > lower).Should().BeTrue();
            (higher >= lower).Should().BeTrue();
            (lower < higher).Should().BeTrue();
            (lower <= higher).Should().BeTrue();

            AssertEqualityAndSameOrder(higher, lower, false);
        }

        private static void AssertUnequalityAndOrderWithNull(LogLevel level) {
            var nullLevel = (LogLevel?)null;

            level.Equals(nullLevel!).Should().BeFalse();

            (level == nullLevel).Should().BeFalse();
            (nullLevel == level).Should().BeFalse();

            (level != nullLevel).Should().BeTrue();
            (nullLevel != level).Should().BeTrue();

            level.CompareTo(nullLevel).Should().BePositive();
        }

        private static void AssertEquality<T>(T left, T right, bool isExpectedEqual) where T : notnull {
            Equals(left, right).Should().Be(isExpectedEqual);
            Equals(right, left).Should().Be(isExpectedEqual);

            left.Equals(right).Should().Be(isExpectedEqual);
            right.Equals(left).Should().Be(isExpectedEqual);

            var hash1 = left.GetHashCode();
            var hash2 = right.GetHashCode();
            Equals(hash1, hash2).Should().Be(isExpectedEqual);
        }

        private static void AssertOrder<T>(T left, T right, bool isExpectedSame) where T : IComparable<T> {
            if (isExpectedSame) {
                left.CompareTo(right).Should().Be(0);
                right.CompareTo(left).Should().Be(0);
            }
            else {
                left.CompareTo(right).Should().NotBe(0);
                right.CompareTo(left).Should().NotBe(0);
            }
        }

        private static void AssertLevelsBetween(LogLevel? sutFrom, LogLevel? sutTo, IEnumerable<LogLevel> expected) {
            var actual = LogLevel.GetLogLevelsBetween(ref sutFrom, ref sutTo);

            actual.Should().BeEquivalentTo(expected);
        }
    }
}