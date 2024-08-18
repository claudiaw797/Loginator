using AutoMapper;
using Backend.Bootstrapper;
using Backend.Model;
using Common.Configuration;
using Loginator.ViewModels;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loginator.Bootstrapper {
    public static class DiBootstrapperFrontend {
        public static void Initialize(IContainer container) {
            Console.WriteLine("Bootstrapping DI: Frontend");
            container.Configure(m => {
                m.For<TimeProvider>().Singleton().Use(c => TimeProvider.System);
                m.For<LoginatorViewModel>().Singleton().Use<LoginatorViewModel>();
                m.For<ConfigurationViewModel>().Use<ConfigurationViewModel>();
                // TODO: Move this to separate bootstrapper
                m.For<IApplicationConfiguration>().Use<ApplicationConfiguration>();

                var config = new MapperConfiguration(cfg => cfg.CreateMap<Log, LogViewModel>());
                m.For<IMapper>().Singleton().Use(c => new Mapper(config));
            });

            DiBootstrapperBackend.Initialize(container);
        }
    }
}
