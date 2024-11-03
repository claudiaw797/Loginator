// Copyright (C) 2024 Claudia Wagner

using Loginator.Application.Model;

namespace Loginator.Application.ViewModel {

    public class AboutViewModel(AssemblyInfo assemblyInfo) {

        private readonly AssemblyInfo assemblyInfo = assemblyInfo;

        public string Product => assemblyInfo.Product;
        public string Copyright => assemblyInfo.Copyright;
        public string Description => assemblyInfo.Description;
        public string VersionName => assemblyInfo.VersionName;
        public string License => assemblyInfo.License;
        public string DownloadUrl => assemblyInfo.DownloadUrl;
        public string SourceUrl => assemblyInfo.SourceUrl;
    }
}
