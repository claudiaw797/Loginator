using FluentAssertions;
using Loginator.Collections;
using Loginator.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Loginator.UnitTests.Collections {

    /// <summary>
    /// Represents unit tests for <see cref="OrderedObservableCollection"/>.
    /// </summary>
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public class OrderedObservableCollectionTests {

        private readonly OrderedObservableCollection sut = [];
        private readonly SutEvents sutEvents = [];

        private readonly LogViewModel item1;
        private readonly LogViewModel item2;
        private readonly LogViewModel item3;

        public OrderedObservableCollectionTests() {
            DateTime ts = DateTime.Now;
            item1 = LogVM(ts);
            item2 = LogVM(ts.AddSeconds(5));
            item3 = LogVM(ts.AddSeconds(-5));
        }

        [SetUp]
        public void Setup() {
            sut.CollectionChanged += OnNotifyCollectionChanged;
        }

        [TearDown]
        public void TearDown() {
            sut.CollectionChanged -= OnNotifyCollectionChanged;
        }

        [Test]
        public void Cannot_insert_item() {
            var action = () => sut.Insert(0, new LogViewModel());

            action.Should().Throw<InvalidOperationException>();
            sutEvents.Should().BeEmpty();
        }

        [Test]
        public void Cannot_add_null() {
            var action = () => sut.Add(null!);

            action.Should().Throw<ArgumentNullException>();
            sutEvents.Should().BeEmpty();
        }

        [Test]
        public void Can_add_leading_item_without_sorting() {
            Can_add_multiple_items_sorted_by_timestamp_descending();
            sutEvents.Clear();

            var item4 = new LogViewModel { Timestamp = DateTime.Now.AddMinutes(-5) };
            sut.AddLeading(item4);

            sut.Should()
                .ContainInOrder(item4, item2, item1, item3)
                .And.HaveCount(4);
            sutEvents.Actions.Should()
                .ContainSingle(a => a == NotifyCollectionChangedAction.Add);
        }

        [Test]
        public void Can_add_single_item_sorted_by_timestamp_descending() {
            sut.Add(item1);
            sut.Should()
                .Contain(item1)
                .And.HaveCount(1);

            sut.Add(item2);
            sut.Should()
                .ContainInOrder(item2, item1)
                .And.HaveCount(2);

            sut.Add(item3);

            AssertSutContainsTestItems();
            sutEvents.Actions.Should()
                .AllBeEquivalentTo(NotifyCollectionChangedAction.Add)
                .And.HaveCount(3);
        }

        [Test]
        public void Can_add_multiple_items_sorted_by_timestamp_descending() {
            sut.Add([item1, item2, item3]);

            AssertSutContainsTestItems();
            sutEvents.Actions.Should()
                .ContainSingle(a => a == NotifyCollectionChangedAction.Reset);
        }

        [Test]
        public void Cannot_add_duplicate_items() {
            sut.Add(item1);
            sut.Add([item2, item3]);

            AssertSutContainsTestItems();
            sutEvents.Actions.Should()
                .ContainInConsecutiveOrder(NotifyCollectionChangedAction.Add, NotifyCollectionChangedAction.Reset)
                .And.HaveCount(2);
            sutEvents.Clear();

            sut.Add([item1, item2, item3]);

            AssertSutContainsTestItems();
            sutEvents.Should().BeEmpty();

            sut.Add(item1);
            sut.Add(item2);
            sut.Add(item3);

            AssertSutContainsTestItems();
            sutEvents.Should().BeEmpty();
        }

        [Test]
        public void Can_remove_single_item() {
            Can_add_multiple_items_sorted_by_timestamp_descending();
            sutEvents.Clear();

            var actual = sut.Remove(item1);

            actual.Should().BeTrue();
            sut.Should()
                .ContainInOrder(item2, item3)
                .And.HaveCount(2);
            sutEvents.Actions.Should()
                .ContainSingle(a => a == NotifyCollectionChangedAction.Remove);
            sutEvents.Clear();

            sut.RemoveAt(0);

            sut.Should()
                .ContainSingle(it => it == item3);
            sutEvents.Actions.Should()
                .ContainSingle(a => a == NotifyCollectionChangedAction.Remove);
        }

        [Test]
        public void Can_remove_multiple_items() {
            Can_add_multiple_items_sorted_by_timestamp_descending();
            sutEvents.Clear();

            var item4 = new LogViewModel { Timestamp = DateTime.Now };
            var actual = sut.Remove([item1, item2, item3, item4]);

            actual.Should().BeTrue();
            sut.Should().BeEmpty();
            sutEvents.Actions.Should()
                .ContainSingle(a => a == NotifyCollectionChangedAction.Reset);
        }

        [Test]
        public void Can_prevent_removing_multiple_items_with_condition() {
            Can_add_multiple_items_sorted_by_timestamp_descending();
            sutEvents.Clear();

            var item4 = new LogViewModel { Timestamp = DateTime.Now };
            var actual = sut.Remove([item1, item2, item3, item4], _ => false);

            actual.Should().BeFalse();
            AssertSutContainsTestItems();
            sutEvents.Should().BeEmpty();
        }

        private void AssertSutContainsTestItems() {
            sut.Should()
                .ContainInOrder(item2, item1, item3)
                .And.HaveCount(3);
        }

        private void OnNotifyCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
            sutEvents.Add(e);

        private class SutEvents : List<NotifyCollectionChangedEventArgs> {

            public IEnumerable<NotifyCollectionChangedAction> Actions =>
                this.Select(e => e.Action);
        }

        private static LogViewModel LogVM(DateTime timestamp) =>
            new() { Timestamp = timestamp };
    }
}