// Copyright (C) 2024 Claudia Wagner

using System;
using System.Windows.Markup;

namespace Loginator.Gui.WPF.Control {

    public class ServiceProviderExtension(Type type) : MarkupExtension {

        public override object ProvideValue(IServiceProvider serviceProvider) =>
            App.GetService(type);
    }
}
