using Backend.Model;
using FakeItEasy;
using FluentAssertions;
using Loginator.Controls;
using Loginator.ViewModels;
using System;

namespace Loginator.UnitTests.ViewModels {

    /// <summary>
    /// Represents unit tests for <see cref="SearchViewModel"/>.
    /// </summary>
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public class SearchViewModelTests {

        private const string CRITERIA = "Test criteria";

        private readonly SearchViewModel sut;
        private readonly EventHandler<EventArgs> updateHandler;

        public SearchViewModelTests() {
            updateHandler = A.Fake<EventHandler<EventArgs>>();
            sut = Sut(updateHandler);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Can_raise_update_event_on_search_command(bool isInverted) {
            AssertAndArrangeSut(CRITERIA, isInverted);

            sut.UpdateCommand.Execute(SearchViewModel.UpdateCommandSearch);

            sut.ToOptions().Should().Be(Search(CRITERIA, isInverted));
            AssertCanExecuteClear(true);
            AssertCalledUpdateEvent();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Can_raise_update_event_on_clear_command(bool isInverted) {
            AssertAndArrangeSut(CRITERIA, isInverted);

            sut.UpdateCommand.Execute(SearchViewModel.UpdateCommandClear);

            sut.ToOptions().Should().Be(Search(isInverted: isInverted));
            AssertCanExecuteClear(true);
            AssertCalledUpdateEvent();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Can_raise_update_event_on_invert_command(bool isInverted) {
            AssertAndArrangeSut(CRITERIA, isInverted);

            sut.UpdateCommand.Execute("Invert");

            sut.ToOptions().Should().Be(Search(CRITERIA, isInverted));
            AssertCanExecuteSearch(true);
            AssertCalledUpdateEvent();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Can_determine_search_command_from_criteria(bool isInverted) {
            AssertAndArrangeSut(CRITERIA, isInverted);

            sut.UpdateCommand.Execute(null);

            sut.ToOptions().Should().Be(Search(CRITERIA, isInverted));
            AssertCanExecuteClear(true);
            AssertCalledUpdateEvent();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Can_determine_clear_command_from_criteria(bool isInverted) {
            AssertAndArrangeSut(string.Empty, isInverted);

            sut.UpdateCommand.Execute(null);

            sut.ToOptions().Should().Be(Search(isInverted: isInverted));
            AssertCanExecuteClear(true);
            AssertCalledUpdateEvent();
        }

        private void AssertAndArrangeSut(string criteria, bool isInverted) {
            AssertCanExecuteUpdateCommand(false);
            sut.UpdateCommandName.Should().Be(SearchViewModel.UpdateCommandSearch);

            sut.Criteria = criteria;
            sut.IsInverted = isInverted;

            AssertCanExecuteUpdateCommand(true);
        }

        private void AssertCanExecuteClear(bool expected) {
            sut.UpdateCommandName.Should().Be(SearchViewModel.UpdateCommandClear);
            AssertCanExecuteUpdateCommand(expected);
        }

        private void AssertCanExecuteSearch(bool expected) {
            sut.UpdateCommandName.Should().Be(SearchViewModel.UpdateCommandSearch);
            AssertCanExecuteUpdateCommand(expected);
        }

        private void AssertCanExecuteUpdateCommand(bool expected) {
            sut.UpdateCommand.CanExecute(SearchViewModel.UpdateCommandSearch).Should().Be(expected);
            sut.UpdateCommand.CanExecute(SearchViewModel.UpdateCommandClear).Should().Be(expected);
            sut.UpdateCommand.CanExecute("Invert").Should().Be(expected);
            sut.UpdateCommand.CanExecute(null).Should().Be(expected);
            sut.UpdateCommand.CanExecute("xy").Should().Be(expected);
        }

        private void AssertCalledUpdateEvent() =>
            A.CallTo(() => updateHandler.Invoke(sut, A<EventArgs>._)).MustHaveHappened();

        private static SearchViewModel Sut(EventHandler<EventArgs> updateHandler) {
            DispatcherHelper.Initialize();

            var sut = new SearchViewModel();
            sut.UpdateSearch += updateHandler;

            return sut;
        }

        private static SearchOptions Search(string? criteria = null, bool isInverted = false) =>
            new() { Criteria = criteria, IsInverted = isInverted };
    }
}