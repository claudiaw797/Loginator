// Copyright (C) 2025 Claudia Wagner

using FluentAssertions;
using Loginator.Application.Common;
using Loginator.Application.ViewModel;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using static Loginator.Application.UnitTests.ViewModel.TestData;
using static Loginator.Domain.Common.Constants;
using LogLevel = Loginator.Domain.Model.LogLevel;

namespace Loginator.Application.UnitTests.ViewModel {

    /// <summary>
    /// Represents unit tests for <see cref="NamespaceViewModel"/>.
    /// </summary>
    public partial class NamespaceViewModelTests {

        private const string APP_NAME = "TestApp";
        private const string NAMESPACE_NAME = "TestNs";

        private readonly ObservableCollection<NamespaceViewModel> namespaces = [];
        private readonly ApplicationViewModel applicationViewModel;
        private readonly TestData testData;

        public NamespaceViewModelTests() {
            var logs = new OrderedObservableCollection();
            applicationViewModel = new ApplicationViewModel(APP_NAME, logs, namespaces, LogLevel.NOT_SET);

            testData = Sut();
        }

        [Test]
        public void Can_create_sut_active_and_expanded() {
            testData.Sut.IsActive.Should().BeTrue();
            testData.Sut.IsExpanded.Should().BeTrue();
        }

        [Test]
        public void Cannot_create_sut_without_application() {
            var action = () => new NamespaceViewModel(NAMESPACE_NAME, null!);

            action.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void Can_set_active_state_for_all_children() {
            testData.Sut.IsActive.Should().BeTrue();
            testData.Sut.IsActive = false;

            testData.Sut.IsActive.Should().BeFalse();
            foreach (var nspace in testData.Namespaces) {
                nspace.IsActive.Should().BeFalse();
            }
        }

        [Test]
        public void Can_build_full_name() {
            var nspace = testData.Namespaces.Last();

            nspace.Name.Should().Be(testData.ExpectedLastName);
            nspace.Fullname.Should().Be(testData.ExpectedLastFullName);
        }

        [Test]
        public void Can_update_log_count_by_level() {
            var sut = testData.Sut;

            SetUpdateLogCount(LogLevel.TRACE);
            sut.CountTrace.Should().Be(1);
            sut.Count.Should().Be(1);

            SetUpdateLogCount(LogLevel.DEBUG);
            sut.CountDebug.Should().Be(1);
            sut.Count.Should().Be(2);

            SetUpdateLogCount(LogLevel.INFO);
            sut.CountInfo.Should().Be(1);
            sut.Count.Should().Be(3);

            SetUpdateLogCount(LogLevel.WARN);
            sut.CountWarn.Should().Be(1);
            sut.Count.Should().Be(4);

            SetUpdateLogCount(LogLevel.ERROR);
            sut.CountError.Should().Be(1);
            sut.Count.Should().Be(5);

            SetUpdateLogCount(LogLevel.FATAL);
            sut.CountFatal.Should().Be(1);
            sut.Count.Should().Be(6);

            sut.ClearLogData();
            sut.Count.Should().Be(0);
            sut.CountTrace.Should().Be(0);
            sut.CountDebug.Should().Be(0);
            sut.CountInfo.Should().Be(0);
            sut.CountWarn.Should().Be(0);
            sut.CountError.Should().Be(0);
            sut.CountFatal.Should().Be(0);
        }

        private void SetUpdateLogCount(LogLevel logLevel) {
            var logViewModel = new LogViewModel(MinLog(logLevel));
            testData.Sut.UpdateLogCounts(logViewModel);
        }

        private TestData Sut() {
            var sut = new NamespaceViewModel(APP_NAME, applicationViewModel);
            var nsFirst = "FirstLayerNs";
            var nsSecond = "SecondLayerNs";
            var nsThird = "ThirdLayerNs";
            var ns1 = new NamespaceViewModel(nsFirst, applicationViewModel) { Parent = sut };
            var ns2 = new NamespaceViewModel(nsSecond, applicationViewModel) { Parent = ns1 };
            var ns3 = new NamespaceViewModel(nsThird, applicationViewModel) { Parent = ns2 };
            sut.Children.AddRange(ns1);
            ns1.Children.Add(ns2);
            ns2.Children.Add(ns3);
            var expected = $"{APP_NAME}{NamespaceSplitter}{nsFirst}{NamespaceSplitter}{nsSecond}{NamespaceSplitter}{nsThird}";

            return new TestData(sut, [ns1, ns2, ns3], nsThird, expected);
        }

        private record TestData(NamespaceViewModel Sut, IReadOnlyCollection<NamespaceViewModel> Namespaces, string ExpectedLastName, string ExpectedLastFullName) { }
    }
}