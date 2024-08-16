using Backend.Model;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Backend.UnitTests.Model {

    /// <summary>
    /// Represents unit tests for <see cref="LoggingLevel"/>.
    /// </summary>
    public class LoggingLevelTests {

        [TestCaseSource(typeof(LoggingLevelTestData), nameof(LoggingLevelTestData.ValidLevels))]
        public void Can_determine_equality(LoggingLevel sut) {
            AssertEqualityAndSameOrder(sut, sut, true);
        }

        [TestCaseSource(typeof(LoggingLevelTestData), nameof(LoggingLevelTestData.ValidLevels))]
        public void Can_compare_with_null(LoggingLevel sut) {
            AssertUnequalityAndOrderWithNull(sut);
        }

        [TestCaseSource(typeof(LoggingLevelTestData), nameof(LoggingLevelTestData.ValidLevels))]
        public void Can_determine_greater_and_less_than_or_equal_for_equal_levels(LoggingLevel sut) {
            AssertGreaterAndLessThanOrEqualForEqualLevels(sut, sut);
        }

        [TestCaseSource(typeof(LoggingLevelTestData), nameof(LoggingLevelTestData.FirstLowerThanSecond))]
        public void Can_determine_greater_and_less_than_or_equal_for_unequal_levels(LoggingLevel sutLower, LoggingLevel sutHigher) {
            AssertGreaterAndLessThanOrEqualForUnequalLevels(sutLower, sutHigher);
        }

        [Test]
        public void Can_determine_all_valid_levels_sortable() {
            LoggingLevel[] expected = [LoggingLevel.TRACE, LoggingLevel.DEBUG, LoggingLevel.INFO, LoggingLevel.WARN, LoggingLevel.ERROR, LoggingLevel.FATAL];
            var actual = LoggingLevel.GetAllLogLevels().Order();

            actual.Should().BeEquivalentTo(expected, c => c.WithStrictOrdering());
        }

        [TestCaseSource(typeof(LoggingLevelTestData), nameof(LoggingLevelTestData.LevelsBetweenEmptyResult))]
        public void Can_determine_empty_levels_between(LoggingLevel? sutFrom, LoggingLevel? sutTo) {
            var actual = LoggingLevel.GetLogLevelsBetween(ref sutFrom, ref sutTo);

            actual.Should().NotBeNull();
            actual.Should().BeEmpty();
        }

        [TestCaseSource(typeof(LoggingLevelTestData), nameof(LoggingLevelTestData.LevelsBetween))]
        public void Can_determine_levels_between(LoggingLevel sutFrom, LoggingLevel sutTo, IEnumerable<LoggingLevel> expected) {
            AssertLevelsBetween(sutFrom, sutTo, expected);
            AssertLevelsBetween(sutTo, sutFrom, expected);
        }

        [TestCaseSource(typeof(LoggingLevelTestData), nameof(LoggingLevelTestData.ValidLevelsByName))]
        public void Can_determine_valid_levels_by_name(string name, LoggingLevel expected) {
            var actual = LoggingLevel.FromName(name);

            actual.Should().Be(expected);
        }

        [TestCaseSource(typeof(LoggingLevelTestData), nameof(LoggingLevelTestData.ValidLevelsByShortName))]
        public void Can_determine_valid_levels_by_short_name(char shortName, LoggingLevel expected) {
            var actual = LoggingLevel.FromShortName(shortName);

            actual.Should().Be(expected);
        }

        private static void AssertEqualityAndSameOrder(LoggingLevel levelLeft, LoggingLevel levelRight, bool isExpectedEqual) {
            AssertEquality(levelLeft, levelRight, isExpectedEqual);
            AssertOrder(levelLeft, levelRight, isExpectedEqual);

            (levelLeft == levelRight).Should().Be(isExpectedEqual);
            (levelRight == levelLeft).Should().Be(isExpectedEqual);

            (levelLeft != levelRight).Should().Be(!isExpectedEqual);
            (levelRight != levelLeft).Should().Be(!isExpectedEqual);
        }

        private static void AssertGreaterAndLessThanOrEqualForEqualLevels(LoggingLevel levelLeft, LoggingLevel levelRight) {
            (levelLeft >= levelRight).Should().BeTrue();
            (levelLeft <= levelRight).Should().BeTrue();
            (levelRight >= levelLeft).Should().BeTrue();
            (levelRight <= levelLeft).Should().BeTrue();

            AssertEqualityAndSameOrder(levelLeft, levelRight, true);
        }

        private static void AssertGreaterAndLessThanOrEqualForUnequalLevels(LoggingLevel lower, LoggingLevel higher) {
            (higher > lower).Should().BeTrue();
            (higher >= lower).Should().BeTrue();
            (lower < higher).Should().BeTrue();
            (lower <= higher).Should().BeTrue();

            AssertEqualityAndSameOrder(higher, lower, false);
        }

        private static void AssertUnequalityAndOrderWithNull(LoggingLevel level) {
            var nullLevel = (LoggingLevel?)null;

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

        private static void AssertLevelsBetween(LoggingLevel? sutFrom, LoggingLevel? sutTo, IEnumerable<LoggingLevel> expected) {
            var actual = LoggingLevel.GetLogLevelsBetween(ref sutFrom, ref sutTo);

            actual.Should().BeEquivalentTo(expected);
        }
    }
}