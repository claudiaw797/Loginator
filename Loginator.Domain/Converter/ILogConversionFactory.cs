// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Loginator.Domain.Model;
using System.Collections.Generic;
using System.IO;

namespace Loginator.Domain.Converter {

    public interface ILogConversionFactory {

        IReadOnlyCollection<Log> Convert(string text);

        IReadOnlyCollection<Log> Convert(Stream stream);
    }
}
