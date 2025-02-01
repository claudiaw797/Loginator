// Copyright (C) 2025 Claudia Wagner

using FluentAssertions;
using Loginator.Application.Common;
using Loginator.UnitTests.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace Loginator.Application.UnitTests.Common {

    /// <summary>
    /// Represents unit tests for <see cref="EnumerableExtensions"/>.
    /// </summary>
    public class EnumerableExtensionsTests {

        [Test]
        public void Can_flatten_tree_of_same_type() {
            var sut = new TestClass[] {
                Outer(1, [ Inner(1), Inner(2), Inner(3) ]),
                Outer(2, [ Inner(4), Inner(5), Inner(6) ]),
                Outer(3, [ Inner(7), Inner(8), Inner(9) ]),
            };
            var expected = sut[0].Children
                .Concat(sut[1].Children)
                .Concat(sut[2].Children)
                .Concat([sut[0], sut[1], sut[2]]);

            var actual = sut.Flatten(outer => outer.Children);

            actual.Should().BeEquivalentTo(expected, c => c.WithStrictOrdering());
        }

        private static TestClass Outer(int postfix, IEnumerable<TestClass> children) =>
            new($"Outer{postfix}", children);

        private static TestClass Inner(int postfix) =>
            new($"Inner{postfix}", []);

        private record TestClass(string Name, IEnumerable<TestClass> Children) { }
    }
}