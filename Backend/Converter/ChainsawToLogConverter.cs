// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Backend.Model;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using static Common.Constants;

namespace Backend.Converter {

    public class ChainsawToLogConverter : ILogConverter {

        private readonly ILogger<ChainsawToLogConverter> logger;
        private readonly IOptionsMonitor<Configuration> configuration;

        public ChainsawToLogConverter(IOptionsMonitor<Configuration> configuration, ILogger<ChainsawToLogConverter> logger) {
            this.logger = logger;
            this.configuration = configuration;
        }

        /*
            <log4j:event logger="WorldDirect.ChimneySweeper.Server.ChimneyService.BaseApplication" level="INFO" timestamp="1439817232886" thread="1">
	            <log4j:message>Starting Rauchfangkehrer</log4j:message>
                <log4j:throwable>System.InvalidOperationException: test exception...</log4j:throwable>
                <log4j:NDC/>
                <log4j:locationInfo class="Example.Runner" method="Void TrySomething(System.String)" file="C:\temp\Solution\Project\Folders\Program.cs" line="75"/>
	            <log4j:properties>
		            <log4j:data name="log4japp" value="Server.ChimneyService.exe(8428)" />
		            <log4j:data name="log4jmachinename" value="DKU" />
	            </log4j:properties>
            </log4j:event>
        */

        public IReadOnlyCollection<Log> Convert(string text) {
            try {
                var byteArray = Encoding.UTF8.GetBytes(text);
                using var memoryStream = new MemoryStream(byteArray);

                // read with namespace check
                var logs = ReadEvents(memoryStream, checkNamespace: true);

                // if no log was found and settings allow it => read without namespace check
                if (logs.Length == 0 && configuration.CurrentValue.AllowAnonymousLogs) {
                    logs = ReadEvents(memoryStream, checkNamespace: false);
                }

                return logs;
            }
            catch (Exception e) {
                logger.LogError(e, "Could not read Log4j data");
            }
            return [Log.DEFAULT];
        }

        private Log[] ReadEvents(Stream stream, bool checkNamespace) {
            stream.Position = 0;
            using var xmlReader = new ChainSawLogReader(stream, checkNamespace, configuration.CurrentValue.ApplicationFormat);
            return xmlReader.ReadLogs().ToArray();
        }

        private class ChainSawLogReader : IDisposable {

            private const string NS_URI_LOG4J = "https://logging.apache.org/xml/ns";
            private const string NS_PFX_LOG4J = "log4j";
            private const string NS_URI_NLOG = "https://nlog-project.org";
            private const string NS_PFX_NLOG = "nlog";
            private const string EVENT_TAG = "event";
            private const string DATA_TAG = "data";
            private const string LOG4J_APP = "log4japp";
            private const string LOG4J_HOST = "log4jmachinename";

            private static readonly XmlReaderSettings settings;
            private static readonly XmlParserContext context;

            private readonly XmlReader xmlReader;
            private readonly bool checkNamespace;
            private readonly ApplicationFormat applicationFormat;

            static ChainSawLogReader() {
                settings = new XmlReaderSettings {
                    NameTable = new NameTable(),
                    ConformanceLevel = ConformanceLevel.Fragment
                };
                var xmlns = new XmlNamespaceManager(settings.NameTable);
                xmlns.AddNamespace(NS_PFX_LOG4J, NS_URI_LOG4J);
                xmlns.AddNamespace(NS_PFX_NLOG, NS_URI_NLOG);
                context = new XmlParserContext(null, xmlns, string.Empty, XmlSpace.Default);
            }

            public ChainSawLogReader(Stream stream, bool checkNamespace, ApplicationFormat applicationFormat) {
                stream.Position = 0;
                xmlReader = XmlReader.Create(stream, settings, context);

                this.checkNamespace = checkNamespace;
                this.applicationFormat = applicationFormat;
            }

            public void Dispose() => xmlReader.Dispose();

            public IEnumerable<Log> ReadLogs() {
                while (HasNext(EVENT_TAG)) {
                    var log = new Log();

                    ReadEventTag(log);
                    ReadEventContent(log);

                    yield return log;
                }
            }

            private static string? InsertIntoMessage(string? message, string? text, bool append = true, string separator = " ") =>
                string.IsNullOrWhiteSpace(text)
                    ? message
                    : string.IsNullOrWhiteSpace(message)
                    ? text
                    : append ? $"{message}{separator}{text}" : $"{text}{separator}{message}";

            private static void ParseApplication(Log log, IEnumerable<Property> properties, bool dontChange) {
                var property = properties.FirstOrDefault(m => m.Name == LOG4J_APP)?.Value;
                if (property is not null) {
                    var application = RegexLog4jApp().Match(property);
                    if (application.Success) {
                        log.Application = application.Groups["app"].Value.Trim();
                        log.Process = application.Groups["pid"].Value.Trim();
                    }

                    if (!application.Success || dontChange) {
                        log.Application = property.Trim();
                    }
                }
            }

            private static void ParseMachineName(Log log, IEnumerable<Property> properties) {
                log.MachineName = properties.FirstOrDefault(m => m.Name == LOG4J_HOST)?.Value;
            }

            private void ReadEventTag(Log log) {
                while (xmlReader.MoveToNextAttribute()) {
                    switch (xmlReader.LocalName) {
                        case "logger":
                            log.Namespace = xmlReader.Value;
                            break;
                        case "level":
                            log.Level = LoggingLevel.FromName(xmlReader.Value);
                            break;
                        case "timestamp":
                            var timestamp = long.Parse(xmlReader.Value);
                            log.Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                            break;
                        case "thread":
                            log.Thread = xmlReader.Value;
                            break;
                    }
                }
            }

            private void ReadEventContent(Log log) {
                do {
                    if (xmlReader.MoveToContent() == XmlNodeType.Element) {
                        switch (xmlReader.LocalName) {
                            case "message":
                                log.Message = xmlReader.ReadElementContentAsString();
                                break;
                            case "throwable":
                                log.Exception = xmlReader.ReadElementContentAsString();
                                break;
                            case "NDC":
                                log.Context = xmlReader.ReadElementContentAsString();
                                break;
                            case "MDC":
                                log.AddProperties(ReadDataTags(log));
                                break;
                            case "properties":
                                ReadPropertiesContent(log);
                                break;
                            case "locationInfo":
                                ReadLocationTag(log);
                                goto default;
                            default:
                                xmlReader.Read();
                                break;
                        }
                    }
                    else if (xmlReader.NodeType == XmlNodeType.EndElement &&
                             xmlReader.LocalName == EVENT_TAG &&
                             (xmlReader.NamespaceURI == NS_URI_LOG4J || !checkNamespace)) {
                        break;
                    }
                    else {
                        xmlReader.Read();
                    }
                } while (true);
            }

            private IEnumerable<Property> ReadDataTags(Log log) {
                var properties = new List<Property>();
                if (HasDescendant(DATA_TAG)) {
                    do {
                        var name = xmlReader.GetAttribute("name");
                        if (!string.IsNullOrEmpty(name)) {
                            properties.Add(new Property(name, xmlReader.GetAttribute("value") ?? string.Empty));
                        }
                    } while (HasNext(DATA_TAG));
                }
                return properties;
            }

            private void ReadPropertiesContent(Log log) {
                var properties = ReadDataTags(log);

                ParseApplication(log, properties, applicationFormat != ApplicationFormat.Consolidate);
                ParseMachineName(log, properties);

                log.AddProperties(properties.ExceptBy([LOG4J_APP, LOG4J_HOST], p => p.Name));
            }

            private void ReadLocationTag(Log log) {
                var locationInfo = new LocationInfo();
                while (xmlReader.MoveToNextAttribute()) {
                    switch (xmlReader.LocalName) {
                        case "class":
                            locationInfo.ClassName = xmlReader.Value;
                            break;
                        case "method":
                            locationInfo.MethodName = xmlReader.Value;
                            break;
                        case "file":
                            locationInfo.FileName = xmlReader.Value;
                            break;
                        case "line":
                            if (int.TryParse(xmlReader.Value, out var line)) locationInfo.LineNumber = line;
                            break;
                    }
                }
                if (!locationInfo.IsEmpty()) log.Location = locationInfo;
            }

            private bool HasDescendant(string localName) =>
                checkNamespace
                    ? xmlReader.ReadToDescendant(localName, NS_URI_LOG4J)
                    : xmlReader.ReadToDescendant(localName);

            private bool HasNext(string localName) =>
                checkNamespace
                    ? xmlReader.ReadToNextSibling(localName, NS_URI_LOG4J)
                    : xmlReader.ReadToNextSibling(localName);
        }
    }
}
