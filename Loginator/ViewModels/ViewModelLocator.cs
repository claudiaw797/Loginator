// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Common.Configuration;
using Loginator.Model;

namespace Loginator.ViewModels {

    public class ViewModelLocator {

        public LoginatorViewModel LoginatorViewModel =>
            IoC.Get<LoginatorViewModel>();

        public ConfigurationViewModel ConfigurationViewModel =>
            IoC.Get<ConfigurationViewModel>();

        public AssemblyInfo AssemblyInfo =>
            IoC.Get<AssemblyInfo>();
    }
}
