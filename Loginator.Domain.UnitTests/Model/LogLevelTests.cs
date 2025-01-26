// Copyright (C) 2024 Claudia Wagner

using FluentAssertions;
using Loginator.Domain.Model;
using Loginator.UnitTests.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace Loginator.Domain.UnitTests.Model {

    /// <summary>
    /// Represents unit tests for <see cref="LogLevel"/>.
    /// </summary>
    public class LogLevelTests {

        [TestCaseSource(typeof(LogLevelTestData), nameof(LogLevelTestData.ValidLevels))]
        public void Cannot_create_new_instance_from_id(LogLevel org) {
            var sut = LogLevel.FromId(org.Id);

            sut.Should().BeSameAs(org);
        }

        [TestCaseSource(typeof(LogLevelTestData), nameof(LogLevelTestData.ValidLevels))]
        public void Cannot_create_new_instance_from_name(LogLevel org) {
            var sut = LogLevel.FromName(org.Name);

            sut.Should().BeSameAs(org);
        }

        [TestCaseSource(typeof(LogLevelTestData), nameof(LogLevelTestData.ValidLevels))]
        public void Cannot_create_new_instance_from_short_name(LogLevel org) {
            var sut = LogLevel.FromShortName(org.ShortName);

            sut.Should().BeSameAs(org);
        }

        [TestCaseSource(typeof(LogLevelTestData), nameof(LogLevelTestData.ValidLevels))]
        public void Can_determine_valid_level_by_id(LogLevel expected) {
            var actual = LogLevel.FromId(expected.Id);

            actual.Should().Be(expected);
        }

        [TestCaseSource(typeof(LogLevelTestData), nameof(LogLevelTestData.ValidLevels))]
        public void Can_determine_valid_level_by_name(LogLevel expected) {
            var actual = LogLevel.FromName(expected.Name.ToLowerInvariant());

            actual.Should().Be(expected);
        }

        [TestCaseSource(typeof(LogLevelTestData), nameof(LogLevelTestData.ValidLevels))]
        public void Can_determine_valid_level_by_short_name(LogLevel expected) {
            var actual = LogLevel.FromShortName(expected.ShortName);

            actual.Should().Be(expected);
        }

        [TestCaseSource(typeof(LogLevelTestData), nameof(LogLevelTestData.ValidLevels))]
        public void Can_determine_equality_with_same_values(LogLevel expected) {
            var sut = LogLevel.FromId(expected.Id);

            sut.Should().BeSymmetricallyEqualAndRankedTo(expected, 1);
        }

        [TestCaseSource(typeof(LogLevelTestData), nameof(LogLevelTestData.FirstLowerThanSecond))]
        public void Can_determine_inequality_with_different_values(LogLevel sutLower, LogLevel sutHigher) {
            sutLower.Should().NotBeSymmetricallyEqualAndRankedTo(sutHigher, 1);
        }

        [TestCaseSource(typeof(LogLevelTestData), nameof(LogLevelTestData.ValidLevels))]
        public void Can_determine_inequality_with_null(LogLevel sut) {
            sut.Should().NotBeSymmetricallyEqualAndRankedTo(null, 1);
        }

        [TestCaseSource(typeof(LogLevelTestData), nameof(LogLevelTestData.FirstLowerThanSecond))]
        public void Can_determine_order_based_on_id(LogLevel sutLower, LogLevel sutHigher) {
            sutLower.Should().BeSymmetricallyRankedPreceding(sutHigher);
            sutHigher.Should().BeSymmetricallyRankedFollowing(sutLower);
        }

        [TestCaseSource(typeof(LogLevelTestData), nameof(LogLevelTestData.ValidLevels))]
        public void Can_determine_greater_and_less_than_or_equal_for_equal_ids(LogLevel sut) {
            sut.Should().BeSymmetricallyEqualAndRankedToEqualValue(sut, 1);
        }

        [TestCaseSource(typeof(LogLevelTestData), nameof(LogLevelTestData.FirstLowerThanSecond))]
        public void Can_determine_greater_and_less_than_or_equal_for_unequal_ids(LogLevel sutLower, LogLevel sutHigher) {
            sutHigher.Should().BeSymmetricallyEqualAndRankedToLowerValue(sutLower, 1);
        }

        [TestCaseSource(typeof(LogLevelTestData), nameof(LogLevelTestData.ValidLevels))]
        public void Cannot_determine_greater_and_less_than_or_equal_if_other_is_null(LogLevel sut) {
            AssertNotGreaterAndLessThanOrEqualWithNull(sut, null);
            AssertNotGreaterAndLessThanOrEqualWithNull(null, sut);
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

        private static void AssertNotGreaterAndLessThanOrEqualWithNull(LogLevel? sutA, LogLevel? sutB) {
            (sutA < sutB).Should().BeFalse();
            (sutA > sutB).Should().BeFalse();
            (sutA <= sutB).Should().BeFalse();
            (sutA >= sutB).Should().BeFalse();
        }

        private static void AssertLevelsBetween(LogLevel? sutFrom, LogLevel? sutTo, IEnumerable<LogLevel> expected) {
            var actual = LogLevel.GetLogLevelsBetween(ref sutFrom, ref sutTo);

            actual.Should().BeEquivalentTo(expected);
        }
    }
}