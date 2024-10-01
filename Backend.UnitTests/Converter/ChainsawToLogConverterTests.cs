// Copyright (C) 2024 Claudia Wagner

using Backend.Converter;
using Backend.Model;
using Common;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Xml.Linq;
using static Backend.UnitTests.Converter.ChainsawToLogConverterTestData;

namespace Backend.UnitTests.Converter {

    /// <summary>
    /// Represents unit tests for <see cref="ChainsawToLogConverter"/>.
    /// </summary>
    [FixtureLifeCycle(LifeCycle.SingleInstance)]
    public class ChainsawToLogConverterTests {

        private readonly ChainsawToLogConverter sut;

        public ChainsawToLogConverterTests() {
            sut = Sut();
        }

        [TestCaseSource(typeof(ChainsawToLogConverterTestData), nameof(ValidLog4jDataOptions))]
        public Log Can_convert_valid_log4j_xml_to_log(bool hasPrefix, bool hasNamespace, bool isMixed, SaveOptions formatOptions) {
            var input = Log4JDefault(hasPrefix, hasNamespace, isMixed, formatOptions);
            TestContext.Out.WriteLine($"Input is{Environment.NewLine}{input}");

            var actual = sut.Convert(input);

            actual.Should().HaveCount(1);
            actual.First().Should().Be(LogFromValidLog4jXml, new LogComparer());
            return actual.First();
        }

        private static ChainsawToLogConverter Sut() {
            var config = new Configuration {
                AllowAnonymousLogs = true,
                ApplicationFormat = ApplicationFormat.Consolidate
            };
            var configDao = A.Fake<IOptionsMonitor<Configuration>>();
            A.CallTo(() => configDao.CurrentValue).Returns(config);

            var logger = A.Fake<ILogger<ChainsawToLogConverter>>();
            return new ChainsawToLogConverter(configDao, logger);
        }
    }
}