using Common.Configuration;

namespace Loginator.ViewModels {

    public class ViewModelLocator {

        public LoginatorViewModel LoginatorViewModel =>
            IoC.Get<LoginatorViewModel>();

        public ConfigurationViewModel ConfigurationViewModel =>
            IoC.Get<ConfigurationViewModel>();
    }
}
