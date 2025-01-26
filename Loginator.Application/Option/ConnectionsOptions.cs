// Copyright (C) 2024 Claudia Wagner

using Loginator.Domain.Option;
using System.Collections.Generic;

namespace Loginator.Application.Option {

    public sealed class ConnectionsOptions : List<Connection> {

        public const string SectionName = "Connections";

        public ConnectionsOptions(IEnumerable<Connection> collection) : base(collection) { }

        public ConnectionsOptions() { }
    }
}
