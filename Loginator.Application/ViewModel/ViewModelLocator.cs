// Copyright (C) 2024 Claudia Wagner, Daniel Kuster

using Loginator.Application.Model;
using Loginator.Application.Service;

namespace Loginator.Application.ViewModel {

    public class ViewModelLocator {

        public LoginatorViewModel LoginatorViewModel =>
            IoC.Get<LoginatorViewModel>();

        public ConfigurationViewModel ConfigurationViewModel =>
            IoC.Get<ConfigurationViewModel>();

        public AssemblyInfo AssemblyInfo =>
            IoC.Get<AssemblyInfo>();
    }
}
