// Copyright (C) 2024 Claudia Wagner

namespace Loginator.Model {

    public record AssemblyInfo {

        public required string Product { get; init; }

        public required string Copyright { get; init; }

        public required string Description { get; init; }

        public required string VersionName { get; init; }

        public required int VersionCode { get; init; }

        public required string License { get; init; }

        public required string DownloadUrl { get; init; }

        public required string SourceUrl { get; init; }

        public required string RawUrl { get; init; }
    }
}
