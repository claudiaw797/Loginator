// Copyright (C) 2024 Claudia Wagner

using FakeItEasy;
using FluentAssertions;
using Loginator.Domain.Model;
using Loginator.Domain.Option;
using Loginator.Infrastructure.Converter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Xml.Linq;
using static Loginator.Infrastructure.UnitTests.Converter.Log4jConversionFactoryTestData;

namespace Loginator.Infrastructure.UnitTests.Converter {

    /// <summary>
    /// Represents unit tests for <see cref="Log4jConversionFactory"/>.
    /// </summary>
    [FixtureLifeCycle(LifeCycle.SingleInstance)]
    public class Log4jConversionFactoryTests {

        private readonly Log4jConversionFactory sut;

        public Log4jConversionFactoryTests() {
            sut = Sut();
        }

        [TestCaseSource(typeof(Log4jConversionFactoryTestData), nameof(ValidLog4jDataOptions))]
        public Log Can_convert_valid_log4j_xml_to_log(bool hasPrefix, bool hasNamespace, bool isMixed, SaveOptions formatOptions) {
            var input = Log4JDefault(hasPrefix, hasNamespace, isMixed, formatOptions);
            TestContext.Out.WriteLine($"Input is{Environment.NewLine}{input}");

            var actual = sut.Convert(input);

            actual.Should().HaveCount(1);
            actual.First().Should().Be(LogFromValidLog4jXml, new LogComparer());
            return actual.First();
        }

        private static Log4jConversionFactory Sut() {
            var config = new LogProcessingOptions {
                AllowAnonymousMessages = true,
                ApplicationFormat = ApplicationFormat.Consolidate
            };
            var configDao = A.Fake<IOptionsMonitor<LogProcessingOptions>>();
            A.CallTo(() => configDao.CurrentValue).Returns(config);

            var logger = A.Fake<ILogger<Log4jConversionFactory>>();
            return new Log4jConversionFactory(configDao, logger);
        }
    }
}