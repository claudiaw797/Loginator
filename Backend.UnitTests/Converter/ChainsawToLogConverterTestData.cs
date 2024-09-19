// Copyright (C) 2024 Claudia Wagner

using Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Backend.UnitTests.Converter {

    /// <summary>
    /// Represents test data for <see cref="ChainsawToLogConverterTests"/>.
    /// </summary>
    internal class ChainsawToLogConverterTestData {

        private const string NSP_URI = "https://logging.apache.org/xml/ns";
        private const string NSP_PFX = "log4j";

        private static readonly XNamespace Nspace = NSP_URI;

        private static readonly XElementDictionary XElems = new();

        internal static readonly Log LogFromValidLog4jXml = new() {
            Timestamp = Value.Timestamp,
            Level = Value.Level,
            Message = Value.Message,
            Exception = Value.Exception,
            MachineName = Value.MachineName,
            Namespace = Value.Nspace,
            Application = Value.Application,
            Process = Value.Process,
            Thread = Value.Thread,
            Context = Value.Context,
            Location = new() {
                ClassName = Value.ClassName,
                FileName = Value.FileName,
                MethodName = Value.MethodName,
                LineNumber = Value.LineNumber,
            }
        };

        static ChainsawToLogConverterTestData() {
            LogFromValidLog4jXml.AddProperties([
                new(Name.FromMdc, Value.FromMdc),
                new(Name.Action, Value.Action),
                new(Name.EventId, Value.EventId)
            ]);
        }

        public static IEnumerable<TestCaseData> ValidLog4jDataOptions() {
            SaveOptions[] options = [
                SaveOptions.None,
                SaveOptions.DisableFormatting,
                SaveOptions.OmitDuplicateNamespaces,
                SaveOptions.DisableFormatting | SaveOptions.OmitDuplicateNamespaces
            ];
            bool[] booleans = [true, false];

            foreach (var hasPrefix in booleans) {
                foreach (var hasNamespace in booleans) {
                    foreach (var isMixed in booleans) {
                        foreach (var option in options) {
                            yield return new TestCaseData(hasPrefix, hasNamespace, isMixed, option)
                                .SetName("{m}{p}")
                                .Returns(LogFromValidLog4jXml);
                        }
                    }
                }
            }
        }

        internal static XElement Log4JDefault(bool withNs, bool withPrefix, bool mixed) {
            var @event = XElement(Tag.Event, withNs, withPrefix, mixed, XAtt.Logger, XAtt.Level, XAtt.Timestamp, XAtt.Thread);
            var mdc = Properties(withNs, withPrefix, mixed, Tag.Mdc, [XElems[Key(Name.FromMdc, withNs, withPrefix, mixed)]]);
            var locationInfo = XElement(Tag.Locationinfo, withNs, withPrefix, mixed, XAtt.Class, XAtt.Method, XAtt.File, XAtt.Line);
            var properties = Properties(withNs, withPrefix, mixed, Tag.Properties,
                    XElems[Key(Name.Action, withNs, withPrefix, mixed)],
                    XElems[Key(Name.EventId, withNs, withPrefix, mixed)],
                    XElems[Key(Name.Log4jApp, withNs, withPrefix, mixed)],
                    XElems[Key(Name.Log4jMachine, withNs, withPrefix, mixed)]);

            @event.Add(
                XElems[Key(Tag.Message, withNs, withPrefix)],
                XElems[Key(Tag.Throwable, withNs, withPrefix)],
                XElems[Key(Tag.Ndc, withNs, withPrefix)],
                mdc,
                locationInfo,
                properties);

            if (mixed) ShuffleNodes(@event);

            return @event;
        }

        private static string Key(string name, bool withNs, bool withPrefix, bool reversed = false) =>
            withNs
            ? withPrefix
                ? reversed ? $"{name}-Ns-Pfx-Rev" : $"{name}-Ns-Pfx"
                : reversed ? $"{name}-Ns-Rev" : $"{name}-Ns"
            : withPrefix
                ? reversed ? $"{name}-Pfx-Rev" : $"{name}-Pfx"
                : reversed ? $"{name}-Rev" : name;

        private static XElement XElement(string name, bool withNs, bool withPrefix, bool mixed, params XAttribute[] attributes) {
            var e = new XElement(XElems[Key(name, withNs, withPrefix)]);
            e.ReplaceAttributes(withPrefix
                ? mixed ? attributes.Append(XAtt.Log4j).Shuffle() : attributes.Prepend(XAtt.Log4j)
                : mixed ? attributes.Shuffle() : attributes);
            return e;
        }

        private static XElement Properties(bool withNs, bool withPrefix, bool mixed, string key, params XElement[] elements) {
            var p = new XElement(XElems[Key(key, withNs, withPrefix)]);
            p.ReplaceNodes(mixed ? elements.Shuffle() : elements);
            return p;
        }

        private static void ShuffleNodes(XElement element) {
            var elements = element.Nodes();
            var shuffled = elements.Shuffle();
            element.ReplaceNodes(shuffled);
        }

        internal class XAtt {
            public static readonly XAttribute Log4j = new(XNamespace.Xmlns + NSP_PFX, NSP_URI);
            public static readonly XAttribute Logger = new(Att.Logger, Value.Nspace);
            public static readonly XAttribute Level = new(Att.Level, Value.Level.Name);
            public static readonly XAttribute Timestamp = new(Att.Timestamp, Value.Timestamp.ToUnixTimeMilliseconds());
            public static readonly XAttribute Thread = new(Att.Thread, Value.Thread);
            public static readonly XAttribute Class = new(Att.Class, Value.ClassName);
            public static readonly XAttribute Method = new(Att.Method, Value.MethodName);
            public static readonly XAttribute File = new(Att.File, Value.FileName);
            public static readonly XAttribute Line = new(Att.Line, Value.LineNumber);
            public static readonly XAttribute Action_Name = new(Att.Name, Name.Action);
            public static readonly XAttribute Action_Value = new(Att.Value, Value.Action);
            public static readonly XAttribute EventId_Name = new(Att.Name, Name.EventId);
            public static readonly XAttribute EventId_Value = new(Att.Value, Value.EventId);
            public static readonly XAttribute Log4jApp_Name = new(Att.Name, Name.Log4jApp);
            public static readonly XAttribute Log4jApp_Value = new(Att.Value, Value.Log4jApp);
            public static readonly XAttribute Log4jMachine_Name = new(Att.Name, Name.Log4jMachine);
            public static readonly XAttribute Log4jMachine_Value = new(Att.Value, Value.Log4jMachine);
            public static readonly XAttribute Mdc_Name = new(Att.Name, Name.FromMdc);
            public static readonly XAttribute Mdc_Value = new(Att.Value, Value.FromMdc);
        }

        private class Tag {
            public const string Event = "event";
            public const string Message = "message";
            public const string Throwable = "throwable";
            public const string Ndc = "NDC";
            public const string Mdc = "MDC";
            public const string Locationinfo = "locationInfo";
            public const string Properties = "properties";
            public const string Data = "data";
        }

        private class Att {
            public const string Logger = "logger";
            public const string Level = "level";
            public const string Timestamp = "timestamp";
            public const string Thread = "thread";
            public const string Class = "class";
            public const string Method = "method";
            public const string File = "file";
            public const string Line = "line";
            public const string Name = "name";
            public const string Value = "value";
        }

        private class Name {
            public const string Action = "Action";
            public const string EventId = "EventId";
            public const string Log4jApp = "log4japp";
            public const string Log4jMachine = "log4jmachinename";
            public const string FromMdc = "MdcName";
        }

        private class Value {
            public static readonly DateTimeOffset Timestamp = DateTimeOffset.Parse("2024-08-26 16:13:21.964 +0");
            public static readonly LoggingLevel Level = LoggingLevel.ERROR;
            public const string Message = "Something really important happened ;)";
            public const string Exception = """
            System.InvalidOperationException: test exception
             ---> System.InvalidCastException: test inner exception
               --- End of inner exception stack trace ---
               at Example.Runner.DoSomething() in C:\temp\Solution\Project\Folders\Program.cs:line 80
               at Example.Runner.TrySomething(String action) in C:\temp\Solution\Project\Folders\Program.cs:line 71
            """;
            public const string MachineName = "TEST-MACHINE";
            public const string Nspace = "Example.Runner";
            public const string Application = "ChainsawRunnerExample";
            public const string Process = "10888";
            public const string Thread = "19";
            public const string Context = "ScopeNested1_ScopeNested2_ScopeLocal";
            public const string ClassName = "Example.Runner";
            public const string FileName = @"C:\temp\Solution\Project\Folders\Program.cs";
            public const string MethodName = "Void TrySomething(System.String)";
            public const int LineNumber = 71;
            public const string Action = "What action?";
            public const string EventId = "23504";
            public const string Log4jApp = "ChainsawRunnerExample(10888)";
            public const string Log4jMachine = "TEST-MACHINE";
            public const string FromMdc = "MdcValue";
        }

        private class XElementDictionary : Dictionary<string, XElement> {

            public XElementDictionary() {
                AddXElements(withNs: false, withPrefix: false);
                AddXElements(withNs: false, withPrefix: true);
                AddXElements(withNs: true, withPrefix: false);
                AddXElements(withNs: true, withPrefix: true);

                AddXData(withNs: false, withPrefix: false, reversed: false);
                AddXData(withNs: false, withPrefix: false, reversed: true);
                AddXData(withNs: false, withPrefix: true, reversed: false);
                AddXData(withNs: false, withPrefix: true, reversed: true);
                AddXData(withNs: true, withPrefix: false, reversed: false);
                AddXData(withNs: true, withPrefix: false, reversed: true);
                AddXData(withNs: true, withPrefix: true, reversed: false);
                AddXData(withNs: true, withPrefix: true, reversed: true);
            }

            private void AddXData(bool withNs, bool withPrefix, bool reversed) {
                this.AddXData(Name.Action, XAtt.Action_Name, XAtt.Action_Value, withNs, withPrefix, reversed);
                this.AddXData(Name.EventId, XAtt.EventId_Name, XAtt.EventId_Value, withNs, withPrefix, reversed);
                this.AddXData(Name.Log4jApp, XAtt.Log4jApp_Name, XAtt.Log4jApp_Value, withNs, withPrefix, reversed);
                this.AddXData(Name.Log4jMachine, XAtt.Log4jMachine_Name, XAtt.Log4jMachine_Value, withNs, withPrefix, reversed);
                this.AddXData(Name.FromMdc, XAtt.Mdc_Name, XAtt.Mdc_Value, withNs, withPrefix, reversed);
            }

            private void AddXElements(bool withNs, bool withPrefix) {
                this.AddXElement(Tag.Event, withNs, withPrefix);
                this.AddXElement(Tag.Message, withNs, withPrefix, Value.Message);
                this.AddXElement(Tag.Throwable, withNs, withPrefix, Value.Exception);
                this.AddXElement(Tag.Ndc, withNs, withPrefix, Value.Context);
                this.AddXElement(Tag.Mdc, withNs, withPrefix);
                this.AddXElement(Tag.Locationinfo, withNs, withPrefix);
                this.AddXElement(Tag.Properties, withNs, withPrefix);
            }

            private void AddXData(string key, XAttribute name, XAttribute value, bool withNs, bool withPrefix, bool reversed) =>
                this.Add(Key(key, withNs, withPrefix, reversed), XData(name, value, withNs, withPrefix, reversed));

            private void AddXElement(string name, bool withNs, bool withPrefix, params object[] content) =>
                this.Add(Key(name, withNs, withPrefix), XElement(name, withNs, withPrefix, content));

            private static XElement XElement(string name, bool withNs, bool withPrefix, params object[] content) =>
                withPrefix
                ? new(Nspace + name, XAtt.Log4j, content) // ns-attribute must be removed manually from string representation
                : (withNs ? new(Nspace + name, content) : new(name, content));

            private static XElement XData(XAttribute name, XAttribute value, bool withNs, bool withPrefix, bool reversed) {
                var element = reversed
                    ? XElement(Tag.Data, withNs, withPrefix, value, name)
                    : XElement(Tag.Data, withNs, withPrefix, name, value);
                return element;
            }
        }
    }

    internal static class ChainsawToLogConverterExtensions {

        private static readonly Random Random = new();

        public static string ToString(this XElement element, SaveOptions options, bool removeNs) {
            var result = element.ToString(options);

            if (removeNs) {
                result = result.Replace($" {ChainsawToLogConverterTestData.XAtt.Log4j}", string.Empty);
            }
            return result;
        }

        public static T[] Shuffle<T>(this IEnumerable<T>? enumerable) =>
            [.. enumerable?.OrderBy(x => Random.Next(1, 100))];
    }
}