// Copyright (C) 2025 Claudia Wagner

using FakeItEasy;
using FakeItEasy.Configuration;
using FakeItEasy.Core;
using FluentAssertions;
using Loginator.Application.Option;
using Loginator.Application.ViewModel;
using Loginator.Domain.Option;
using Loginator.UnitTests.Infrastructure;
using System;
using System.Drawing;
using static Loginator.UnitTests.Infrastructure.DelegateHandlerExtensions;

namespace Loginator.Application.UnitTests.ViewModel {

    /// <summary>
    /// Represents unit tests for <see cref="ConfigurationViewModel"/>.
    /// </summary>
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public class ConfigurationViewModelTests {

        private static readonly Exception TestException = new InvalidOperationException("test error");

        private readonly IOptionsRepository<ApplicationOptions> optionsRepository;
        private readonly IDelegateHandler delegateHandler;
        private readonly ApplicationOptions applicationOptions = new();
        private Action<ApplicationOptions, string?>? applicationOptionsChanged;

        private readonly ConfigurationViewModel sut;

        public ConfigurationViewModelTests() {
            optionsRepository = A.Fake<IOptionsRepository<ApplicationOptions>>();
            delegateHandler = A.Fake<IDelegateHandler>();
            sut = Sut();
        }

        [TearDown]
        public void TearDown() {
            sut.Dispose();
        }

        [Test]
        public void Can_register_repository_change_handler_when_created() {
            CallToOptionsRepositoryOnChanged().MustHaveHappened();
        }

        [Test]
        public void Can_call_close_handler_without_saving_changes() {
            TestCancelChanges();

            delegateHandler.CallToClose().MustHaveHappened();
            delegateHandler.CallToError().MustNotHaveHappened();
        }

        [Test]
        public void Can_do_nothing_if_no_close_handler_is_set_when_canceling() {
            sut.OnClose = null;

            TestCancelChanges();

            delegateHandler.CallToClose().MustNotHaveHappened();
            delegateHandler.CallToError().MustNotHaveHappened();
        }

        [Test]
        public void Can_call_error_handler_if_close_handler_throws_when_canceling() {
            TestCloseHandlerThrowsOnCancel();

            delegateHandler.CallToError(TestException).MustHaveHappened();
        }

        [Test]
        public void Can_swallow_error_if_close_handler_throws_and_no_error_handler_is_set_when_canceling() {
            sut.OnError = null;

            TestCloseHandlerThrowsOnCancel();

            delegateHandler.CallToError().MustNotHaveHappened();
        }

        [Test]
        public void Can_only_execute_save_if_fields_are_changed() {
            TestProperty(nameof(sut.Language), KnownCulture.German);
            TestProperty(nameof(sut.CheckForUpdateOnStartup), true);
            TestProperty(nameof(sut.TracePerformance), true);
            TestProperty(nameof(sut.LogTimeFormat), LogTimeFormat.ConvertToLocalTime);
            TestProperty(nameof(sut.ApplicationFormat), ApplicationFormat.Consolidate);
            TestProperty(nameof(sut.AllowAnonymousMessages), true);
            TestProperty(nameof(sut.TraceMessages), true);
            TestProperty(nameof(sut.ColorLevelTrace), Color.Coral);
            TestProperty(nameof(sut.ColorLevelDebug), Color.Coral);
            TestProperty(nameof(sut.ColorLevelInfo), Color.Coral);
            TestProperty(nameof(sut.ColorLevelWarn), Color.Coral);
            TestProperty(nameof(sut.ColorLevelError), Color.Coral);
            TestProperty(nameof(sut.ColorLevelFatal), Color.Coral);
            TestProperty(nameof(sut.ColorLogHighlight), Color.Coral);
        }

        [Test]
        public void Can_save_changes_and_call_close_handler() {
            TestSaveChanges();

            delegateHandler.CallToClose().MustHaveHappened();
            delegateHandler.CallToError().MustNotHaveHappened();
        }

        [Test]
        public void Can_save_changes_if_no_close_handler_is_set_when_saving() {
            sut.OnClose = null;

            TestSaveChanges();

            delegateHandler.CallToClose().MustNotHaveHappened();
            delegateHandler.CallToError().MustNotHaveHappened();
        }

        [Test]
        public void Cannot_save_changes_but_call_error_handler_if_repository_throws_when_saving() {
            TestRepositoryThrowsOnSave();

            delegateHandler.CallToError(TestException).MustHaveHappened();
        }

        [Test]
        public void Cannot_save_changes_but_swallow_error_if_repository_throws_and_no_error_handler_is_set_when_saving() {
            sut.OnError = null;

            TestRepositoryThrowsOnSave();

            delegateHandler.CallToError().MustNotHaveHappened();
        }

        [Test]
        public void Can_call_error_handler_if_close_handler_throws_after_saving() {
            TestCloseHandlerThrowsOnSave();

            delegateHandler.CallToError(TestException).MustHaveHappened();
        }

        [Test]
        public void Can_swallow_error_if_close_handler_throws_and_no_error_handler_is_set_when_saving() {
            sut.OnError = null;

            TestCloseHandlerThrowsOnSave();

            delegateHandler.CallToError().MustNotHaveHappened();
        }

        [Test]
        public void Can_update_fields_if_repository_changes() {
            var expected = new ApplicationOptions {
                Language = KnownCulture.German,
                CheckForUpdateOnStartup = true,
                TracePerformance = true,
                LogTimeFormat = LogTimeFormat.ConvertToLocalTime,
                LogProcessing = new() {
                    ApplicationFormat = ApplicationFormat.Consolidate,
                    AllowAnonymousMessages = true,
                    TraceMessages = true,
                },
                Colors = new() {
                    LevelTrace = Color.Coral,
                    LevelDebug = Color.Coral,
                    LevelInfo = Color.Coral,
                    LevelWarn = Color.Coral,
                    LevelError = Color.Coral,
                    LevelFatal = Color.Coral,
                    LogHighlight = Color.Coral,
                }
            };

            applicationOptionsChanged?.Invoke(expected, string.Empty);

            sut.Language.Should().Be(expected.Language);
            sut.CheckForUpdateOnStartup.Should().Be(expected.CheckForUpdateOnStartup);
            sut.TracePerformance.Should().Be(expected.TracePerformance);
            sut.LogTimeFormat.Should().Be(expected.LogTimeFormat);
            sut.ApplicationFormat.Should().Be(expected.LogProcessing.ApplicationFormat);
            sut.AllowAnonymousMessages.Should().Be(expected.LogProcessing.AllowAnonymousMessages);
            sut.TraceMessages.Should().Be(expected.LogProcessing.TraceMessages);
            sut.ColorLevelTrace.Should().Be(expected.Colors.LevelTrace);
            sut.ColorLevelDebug.Should().Be(expected.Colors.LevelDebug);
            sut.ColorLevelInfo.Should().Be(expected.Colors.LevelInfo);
            sut.ColorLevelWarn.Should().Be(expected.Colors.LevelWarn);
            sut.ColorLevelError.Should().Be(expected.Colors.LevelError);
            sut.ColorLevelFatal.Should().Be(expected.Colors.LevelFatal);
            sut.ColorLogHighlight.Should().Be(expected.Colors.LogHighlight);
        }

        private void TestCancelChanges() {
            // Act
            sut.CancelChangesCommand.Execute(null);

            // Assert
            CallToOptionsRepositorySave().MustNotHaveHappened();
        }

        private void TestCloseHandlerThrowsOnCancel() {
            delegateHandler.CallToClose().Throws(TestException).Once();

            TestCancelChanges();

            delegateHandler.CallToClose().MustHaveHappened();
        }

        private void TestRepositoryThrowsOnSave() {
            CallToOptionsRepositorySave().Throws(TestException).Once();

            TestSaveChanges(assertOptionChanges: false);

            delegateHandler.CallToClose().MustNotHaveHappened();
        }

        private void TestCloseHandlerThrowsOnSave() {
            delegateHandler.CallToClose().Throws(TestException).Once();

            TestSaveChanges();

            delegateHandler.CallToClose().MustHaveHappened();
        }

        private void TestProperty(string name, object value) {
            var property = typeof(ConfigurationViewModel).GetProperty(name);
            property.Should().NotBeNull();

            var org = property.GetValue(sut);
            value.Should().NotBe(org);

            sut.AcceptChangesCommand.CanExecute(null).Should().BeFalse();
            property.SetValue(sut, value);
            sut.AcceptChangesCommand.CanExecute(null).Should().BeTrue();
            property.SetValue(sut, org);
            sut.AcceptChangesCommand.CanExecute(null).Should().BeFalse();
        }

        private void TestSaveChanges(bool assertOptionChanges = true) {
            // Arrange
            sut.Language = KnownCulture.German;
            sut.CheckForUpdateOnStartup = true;
            sut.TracePerformance = true;
            sut.LogTimeFormat = LogTimeFormat.ConvertToLocalTime;
            sut.ApplicationFormat = ApplicationFormat.Consolidate;
            sut.AllowAnonymousMessages = true;
            sut.TraceMessages = true;
            sut.ColorLevelTrace = Color.Coral;
            sut.ColorLevelDebug = Color.Coral;
            sut.ColorLevelInfo = Color.Coral;
            sut.ColorLevelWarn = Color.Coral;
            sut.ColorLevelError = Color.Coral;
            sut.ColorLevelFatal = Color.Coral;
            sut.ColorLogHighlight = Color.Coral;

            // Act
            sut.AcceptChangesCommand.Execute(null);

            // Assert
            CallToOptionsRepositorySave().MustHaveHappened();

            if (assertOptionChanges) {
                applicationOptions.Language.Should().Be(sut.Language);
                applicationOptions.CheckForUpdateOnStartup.Should().Be(sut.CheckForUpdateOnStartup);
                applicationOptions.TracePerformance.Should().Be(sut.TracePerformance);
                applicationOptions.LogTimeFormat.Should().Be(sut.LogTimeFormat);
                applicationOptions.LogProcessing.ApplicationFormat.Should().Be(sut.ApplicationFormat);
                applicationOptions.LogProcessing.AllowAnonymousMessages.Should().Be(sut.AllowAnonymousMessages);
                applicationOptions.LogProcessing.TraceMessages.Should().Be(sut.TraceMessages);
                applicationOptions.Colors.LevelTrace.Should().Be(sut.ColorLevelTrace);
                applicationOptions.Colors.LevelDebug.Should().Be(sut.ColorLevelDebug);
                applicationOptions.Colors.LevelInfo.Should().Be(sut.ColorLevelInfo);
                applicationOptions.Colors.LevelWarn.Should().Be(sut.ColorLevelWarn);
                applicationOptions.Colors.LevelError.Should().Be(sut.ColorLevelError);
                applicationOptions.Colors.LevelFatal.Should().Be(sut.ColorLevelFatal);
                applicationOptions.Colors.LogHighlight.Should().Be(sut.ColorLogHighlight);
            }
        }

        private IReturnValueArgumentValidationConfiguration<ApplicationOptions> CallToOptionsRepositoryGet() =>
            A.CallTo(() => optionsRepository.Get());

        private IReturnValueArgumentValidationConfiguration<IDisposable?> CallToOptionsRepositoryOnChanged() =>
            A.CallTo(() => optionsRepository.OnChanged(A<Action<ApplicationOptions, string?>>._));

        private IVoidArgumentValidationConfiguration CallToOptionsRepositorySave() =>
            A.CallTo(() => optionsRepository.Save(A<Action<ApplicationOptions>>._));

        private void OnOptionsRepositoryChanged(IFakeObjectCall call) {
            applicationOptionsChanged = call.Arguments.Get<Action<ApplicationOptions, string?>>(0);
        }

        private void OnOptionsRepositorySave(IFakeObjectCall call) {
            var changeMethod = call.Arguments.Get<Action<ApplicationOptions>>(0);
            changeMethod?.Invoke(applicationOptions);
        }

        private ConfigurationViewModel Sut() {
            CallToOptionsRepositoryGet().Returns(applicationOptions);
            CallToOptionsRepositoryOnChanged().Invokes(OnOptionsRepositoryChanged);
            CallToOptionsRepositorySave().Invokes(OnOptionsRepositorySave);

            return new ConfigurationViewModel(optionsRepository) {
                OnClose = delegateHandler.Close,
                OnError = delegateHandler.Error
            };
        }
    }
}