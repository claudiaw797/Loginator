// Copyright (C) 2025 Claudia Wagner

using FluentAssertions;
using Loginator.Application.Model;
using Loginator.Application.ViewModel;

namespace Loginator.Application.UnitTests.ViewModel {

    /// <summary>
    /// Represents unit tests for <see cref="AboutViewModel"/>.
    /// </summary>
    public class AboutViewModelTests {

        private readonly AssemblyInfo assemblyInfo;

        public AboutViewModelTests() {
            assemblyInfo = new() {
                Product = "TestProduct",
                Copyright = "TestCopyright",
                Description = "TestDescription",
                VersionName = "TestVersionName",
                VersionCode = 139,
                License = "TestLicense",
                DownloadUrl = "TestDownloadUrl",
                SourceUrl = "TestSourceUrl",
                AssemblyInfoPath = "TestAssemblyInfoPath"
            };
        }

        [Test]
        public void Can_create_sut() {
            var sut = new AboutViewModel(assemblyInfo);

            sut.Product.Should().Be(assemblyInfo.Product);
            sut.Copyright.Should().Be(assemblyInfo.Copyright);
            sut.Description.Should().Be(assemblyInfo.Description);
            sut.VersionName.Should().Be(assemblyInfo.VersionName);
            sut.License.Should().Be(assemblyInfo.License);
            sut.DownloadUrl.Should().Be(assemblyInfo.DownloadUrl);
            sut.SourceUrl.Should().Be(assemblyInfo.SourceUrl);
        }
    }
}