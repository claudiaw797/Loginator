// Copyright (C) 2025 Claudia Wagner

using FluentAssertions;
using Loginator.Domain.Model;
using Loginator.Infrastructure.Converter;
using Loginator.UnitTests.Infrastructure;
using System.Linq;
using static Loginator.Infrastructure.UnitTests.Converter.LogcatConversionServiceTestData;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Loginator.Infrastructure.UnitTests.Converter {

    /// <summary>
    /// Represents unit tests for <see cref="Log4jConversionService"/>.
    /// </summary>
    [FixtureLifeCycle(LifeCycle.SingleInstance)]
    public class LogcatConversionServiceTests {

        private readonly LogListener logListener = new();
        private readonly LogcatComparer logComparer = new();
        private readonly LogcatConversionService sut;

        public LogcatConversionServiceTests() {
            sut = Sut();
        }

        [SetUp]
        public void Setup() {
            logListener.Reset();
        }

        [TestCaseSource(typeof(LogcatConversionServiceTestData), nameof(SingleLineLogcatDataOptions))]
        public void Can_convert_single_line_logcat_to_log(string input, Log expected) {
            TestContext.Out.WriteLine($"Input is «{input}»");

            var actual = sut.Convert(input);

            actual.Should().HaveCount(1);
            actual.First().Should().Be(expected, logComparer);
        }

        [TestCaseSource(typeof(LogcatConversionServiceTestData), nameof(MultiLineLogcatDataOptions))]
        public void Can_convert_multi_line_logcat_to_logs(string input) {
            var actual = sut.Convert(input);

            actual.Should()
                .HaveCount(7).And
                .BeEquivalentTo(Logs, c => c.Using(logComparer));
        }

        [Test]
        public void Can_return_default_log_if_error_occurs() {
            var actual = sut.Convert(default(string)!);

            actual.First().Should().Be(Log.DEFAULT);
            logListener.Contains(LogLevel.Error).Should().BeTrue();
        }

        [Test]
        public void Can_return_empty_logs_if_format_is_unreadable() {
            var actual = sut.Convert("test value");

            actual.Should().BeEmpty();
            logListener.LogCalls.Should().BeEmpty();
        }

        private LogcatConversionService Sut() {
            var logger = logListener.Setup<LogcatConversionService>();
            return new LogcatConversionService(logger);
        }
    }
}