// Copyright (C) 2025 Claudia Wagner

using Loginator.Domain.Model;
using System.Collections.Generic;
using System.IO;

namespace Loginator.Domain.Converter {

    public interface ILogConversionService {

        IReadOnlyCollection<Log> Convert(string text);

        IReadOnlyCollection<Log> Convert(Stream stream);
    }
}
