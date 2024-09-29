// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Backend.Model;
using System.Collections.Generic;
using System.IO;

namespace Backend.Converter {

    public interface ILogConverter {

        IReadOnlyCollection<Log> Convert(string text);

        IReadOnlyCollection<Log> Convert(Stream stream);
    }
}
