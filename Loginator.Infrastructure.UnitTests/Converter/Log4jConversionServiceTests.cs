// Copyright (C) 2024 Claudia Wagner

using FakeItEasy;
using FluentAssertions;
using Loginator.Domain.Model;
using Loginator.Domain.Option;
using Loginator.Infrastructure.Converter;
using Loginator.UnitTests.Infrastructure;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Xml.Linq;
using static Loginator.Infrastructure.UnitTests.Converter.Log4jConversionServiceTestData;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Loginator.Infrastructure.UnitTests.Converter {

    /// <summary>
    /// Represents unit tests for <see cref="Log4jConversionService"/>.
    /// </summary>
    [FixtureLifeCycle(LifeCycle.SingleInstance)]
    public class Log4jConversionServiceTests {

        private readonly IOptionsMonitor<LogProcessingOptions> configDao;
        private readonly LogListener logListener = new();
        private readonly Log4jConversionService sut;

        public Log4jConversionServiceTests() {
            configDao = A.Fake<IOptionsMonitor<LogProcessingOptions>>();
            sut = Sut();
        }

        [SetUp]
        public void Setup() {
            logListener.Reset();
        }

        [TestCaseSource(typeof(Log4jConversionServiceTestData), nameof(Log4jFullDataOptions))]
        public Log Can_convert_full_log4j_xml_to_log(bool hasPrefix, bool hasNamespace, bool isMixed, SaveOptions formatOptions) {
            var input = Log4jFull(hasPrefix, hasNamespace, isMixed, formatOptions);
            TestContext.Out.WriteLine($"Input is{Environment.NewLine}{input}");

            var actual = sut.Convert(input);

            actual.Should().HaveCount(1);
            actual.First().Should().Be(LogFromFullLog4jXml, new LogComparer());
            return actual.First();
        }

        [TestCaseSource(typeof(Log4jConversionServiceTestData), nameof(Log4jMessageAndPropertiesDataOptions))]
        public Log Can_convert_minimum_log4j_xml_to_log(bool isMixed, bool addAppProps, bool addMachineProps) {
            SetupConfig(ApplicationFormat.DoNotChange, once: true);
            var input = Log4jMessageAndPropertiesOnly(true, false, isMixed, addAppProps, addMachineProps, SaveOptions.None);
            TestContext.Out.WriteLine($"Input is{Environment.NewLine}{input}");
            var expected = LogFromMessageAndPropertiesOnlyLog4jXml(isMixed, addAppProps, addMachineProps);

            var actual = sut.Convert(input);

            actual.Should().HaveCount(1);
            actual.First().Should().Be(expected, new LogComparer());
            return actual.First();
        }

        [Test]
        public void Can_return_default_log_if_error_occurs() {
            A.CallTo(() => configDao.CurrentValue).Throws(new InvalidOperationException("test error")).Once();

            var actual = sut.Convert("test value");

            actual.First().Should().Be(Log.DEFAULT);
            logListener.Contains(LogLevel.Error).Should().BeTrue();
        }

        [Test]
        public void Can_return_empty_logs_if_format_is_unreadable() {
            var actual = sut.Convert("test value");

            actual.Should().BeEmpty();
            logListener.LogCalls.Should().BeEmpty();
        }

        private void SetupConfig(ApplicationFormat applicationFormat = ApplicationFormat.Consolidate, bool once = false) {
            var config = new LogProcessingOptions {
                AllowAnonymousMessages = true,
                ApplicationFormat = applicationFormat
            };

            var fake = A.CallTo(() => configDao.CurrentValue).Returns(config);
            if (once) fake.Once();
        }

        private Log4jConversionService Sut() {
            SetupConfig();

            var logger = logListener.Setup<Log4jConversionService>();
            return new Log4jConversionService(configDao, logger);
        }
    }
}