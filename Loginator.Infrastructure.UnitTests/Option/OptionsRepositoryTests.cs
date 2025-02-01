// Copyright (C) 2024 Claudia Wagner

using FakeItEasy;
using FluentAssertions;
using Loginator.Infrastructure.Option;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text.Json;
using static Loginator.Infrastructure.UnitTests.Option.OptionsRepositoryTestData;

namespace Loginator.Infrastructure.UnitTests.Option {

    /// <summary>
    /// Represents unit tests for <see cref="OptionsRepository"/>.
    /// </summary>
    public class OptionsRepositoryTests {

        [SetUp]
        public void Setup() {
            foreach (var file in Directory.EnumerateFiles("Resources", "*.tmp.json")) {
                File.Delete(file);
            }
        }

        [TestCaseSource(typeof(OptionsRepositoryTestData), nameof(FirstLayerTestCases))]
        public void Can_edit_options_in_first_layer(string infix) {
            AssertOptions("Person", "Person", infix, CreatePerson);
        }

        [TestCaseSource(typeof(OptionsRepositoryTestData), nameof(SecondLayerTestCases))]
        public void Can_edit_options_in_second_layer(string infix) {
            AssertOptions("Person:Address", "Address", infix, CreateAddress);
        }

        [TestCaseSource(typeof(OptionsRepositoryTestData), nameof(ThirdLayerTestCases))]
        public void Can_edit_options_in_third_layer(string infix) {
            AssertOptions("Person:Address:Street", "Street", infix, CreateStreet);
        }

        [TestCaseSource(typeof(OptionsRepositoryTestData), nameof(FourthLayerTestCases))]
        public void Can_edit_options_in_fourth_layer(string infix) {
            AssertOptions("Person:Address:Street:Note", "Note", infix, CreateNote);
        }

        [TestCaseSource(typeof(OptionsRepositoryTestData), nameof(FourthLayerListTestCases))]
        public void Can_edit_options_list_in_fourth_layer(string infix) {
            AssertOptions("Person:Address:Street:Notes", "Notes", infix, CreateNotes);
        }

        private static AppOptions? Deserialize(string file) =>
            JsonSerializer.Deserialize<AppOptions>(File.ReadAllText(file));

        private static string Source(string infix) =>
            $"Resources/options.{infix}.test.json";

        private static (string, bool) Copy(string infix) {
            var source = Source(infix);
            var dest = source.Replace("test", "tmp");
            var exists = File.Exists(source);
            if (exists) File.Copy(source, dest, true);
            return (dest, exists);
        }

        private static void AssertOptions<TOptions>(string path, string key, string infix, Func<IAppOptions<TOptions>> testCreator)
            where TOptions : class, new() {

            (var file, var exists) = Copy(infix);
            var sut = Sut<TOptions>(path, key, file);
            var testOptions = testCreator();
            var source = exists ? Deserialize(Source(infix)) : new AppOptions();
            var expected = testOptions.Merge(source);

            sut.Save(o => testOptions.CopyTo(o));
            var actual = Deserialize(file);

            actual.Should().BeEquivalentTo(expected);
        }

        private static OptionsRepository<TOptions> Sut<TOptions>(string path, string key, string file)
            where TOptions : class, new() {

            var configuration = A.Fake<IConfigurationRoot>();

            var fileInfo = A.Fake<IFileInfo>();
            A.CallTo(() => fileInfo.PhysicalPath).Returns(file);
            var fileProvider = A.Fake<IFileProvider>();
            A.CallTo(() => fileProvider.GetFileInfo(A<string>._)).Returns(fileInfo);
            var environment = A.Fake<IHostEnvironment>();
            A.CallTo(() => environment.ContentRootFileProvider).Returns(fileProvider);

            var options = A.Fake<IOptionsMonitor<TOptions>>();

            var section = A.Fake<IConfigurationSection>();
            A.CallTo(() => section.Key).Returns(key);
            A.CallTo(() => section.Path).Returns(path);

            return new OptionsRepository<TOptions>(environment, options, configuration, section, file);
        }
    }
}