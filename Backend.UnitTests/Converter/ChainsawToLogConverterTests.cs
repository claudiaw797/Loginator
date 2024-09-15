// Copyright (C) 2024 Claudia Wagner

using Backend.Converter;
using Backend.Model;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
            var elem = Log4JDefault(hasNamespace, hasPrefix, isMixed);
            var input = elem.ToString(formatOptions, !hasNamespace);
            TestContext.Out.WriteLine($"Input is{Environment.NewLine}{input}");

            var actual = sut.Convert(input);

            actual.Should().HaveCount(1);
            actual.First().Should().Be(LogFromValidLog4jXml, new LogComparer());
            return actual.First();
        }

        private static ChainsawToLogConverter Sut() {
            var config = new Configuration { AllowAnonymousLogs = true };
            var configDao = A.Fake<IOptionsMonitor<Configuration>>();
            A.CallTo(() => configDao.CurrentValue).Returns(config);

            var logger = A.Fake<ILogger<ChainsawToLogConverter>>();
            return new ChainsawToLogConverter(configDao, logger);
        }

        private class LogComparer : IEqualityComparer<Log> {

            public bool Equals(Log? x, Log? y) {
                if (ReferenceEquals(x, y))
                    return true;

                if (y is null || x is null)
                    return false;

                return x.Timestamp == y.Timestamp &&
                    x.Level == y.Level &&
                    x.Message?.ReplaceLineEndings() == y.Message?.ReplaceLineEndings() &&
                    x.Exception?.ReplaceLineEndings() == y.Exception?.ReplaceLineEndings() &&
                    x.MachineName == y.MachineName &&
                    x.Namespace == y.Namespace &&
                    x.Application == y.Application &&
                    x.Process == y.Process &&
                    x.Thread == y.Thread &&
                    x.Context?.ReplaceLineEndings() == y.Context?.ReplaceLineEndings() &&
                    x.Location == y.Location &&
                    Enumerable.SequenceEqual(x.Properties.OrderBy(p => p.Name), y.Properties.OrderBy(p => p.Name));
            }

            public int GetHashCode([DisallowNull] Log obj) {
                var hash = new HashCode();
                hash.Add(obj.Timestamp);
                hash.Add(obj.Level);
                hash.Add(obj.Message);
                hash.Add(obj.Exception);
                hash.Add(obj.MachineName);
                hash.Add(obj.Namespace);
                hash.Add(obj.Application);
                hash.Add(obj.Process);
                hash.Add(obj.Thread);
                hash.Add(obj.Context);
                hash.Add(obj.Location);
                foreach (var property in obj.Properties) {
                    hash.Add(property);
                }
                hash.Add(obj.Properties);
                return hash.ToHashCode();
            }
        }
    }
}