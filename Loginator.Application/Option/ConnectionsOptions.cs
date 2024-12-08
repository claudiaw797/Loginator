// Copyright (C) 2024 Claudia Wagner

using Loginator.Application.Model;
using System.Collections.Generic;

namespace Loginator.Application.Option {

    public sealed class ConnectionsOptions : List<Connection> {

        public const string SectionName = "Connections";
    }
}
