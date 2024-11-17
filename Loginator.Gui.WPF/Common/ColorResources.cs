// Copyright (C) 2024 Claudia Wagner

using Loginator.Application.Option;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;
using SystemColor = System.Drawing.Color;

namespace Loginator.Gui.WPF.Common {

    internal class ColorResources {

        private ResourceDictionary colorResources;
        private ColorsOptions colorOptions;

        public ColorResources(IOptionsMonitor<ApplicationOptions> applicationOptionsMonitor) {
            colorOptions = applicationOptionsMonitor.CurrentValue.Colors;
            colorResources = InitializeDefaultColors(colorOptions);

            applicationOptionsMonitor.OnChange(SetColors);
        }

        private static SolidColorBrush GetColorResource(SystemColor sysColor) {
            var color = MediaColor.FromArgb(sysColor.A, sysColor.R, sysColor.G, sysColor.B);
            return new SolidColorBrush(color);
        }

        private static void SetColorResource(ResourceDictionary resources, string key, SystemColor sysColor) =>
            resources[key] = GetColorResource(sysColor);

        private static ResourceDictionary CreateColorResources(ColorsOptions options) {
            var resources = new ResourceDictionary();

            SetColorResource(resources, "brush.level.Trace", options.LevelTrace);
            SetColorResource(resources, "brush.level.Debug", options.LevelDebug);
            SetColorResource(resources, "brush.level.Info", options.LevelInfo);
            SetColorResource(resources, "brush.level.Warn", options.LevelWarn);
            SetColorResource(resources, "brush.level.Error", options.LevelError);
            SetColorResource(resources, "brush.level.Fatal", options.LevelFatal);
            SetColorResource(resources, "brush.LogHighlight", options.LogHighlight);

            return resources;
        }

        private static string GetColorResourcesPath() => @"Resources\Colors.xaml";

        private static ResourceDictionary InitializeDefaultColors(ColorsOptions options) {
            var mergedResources = App.Current.Resources.MergedDictionaries;
            var defaultPath = GetColorResourcesPath();
            var defaultResources = mergedResources.FirstOrDefault(d => d.Source.OriginalString.Equals(defaultPath));
            if (defaultResources is null) {
                defaultResources = CreateColorResources(options);
                mergedResources.Add(defaultResources);
            }

            return defaultResources;
        }

        private void SetColorResource(string key, SystemColor sysColor) =>
            SetColorResource(colorResources, key, sysColor);

        private void SetColors(ApplicationOptions applicationOptions) {
            var options = applicationOptions.Colors;

            if (colorOptions.LevelTrace != options.LevelTrace) {
                SetColorResource("brush.level.Trace", options.LevelTrace);
            }
            if (colorOptions.LevelDebug != options.LevelDebug) {
                SetColorResource("brush.level.Debug", options.LevelDebug);
            }
            if (colorOptions.LevelInfo != options.LevelInfo) {
                SetColorResource("brush.level.Info", options.LevelInfo);
            }
            if (colorOptions.LevelWarn != options.LevelWarn) {
                SetColorResource("brush.level.Warn", options.LevelWarn);
            }
            if (colorOptions.LevelError != options.LevelError) {
                SetColorResource("brush.level.Error", options.LevelError);
            }
            if (colorOptions.LevelFatal != options.LevelFatal) {
                SetColorResource("brush.level.Fatal", options.LevelFatal);
            }
            if (colorOptions.LogHighlight != options.LogHighlight) {
                SetColorResource("brush.LogHighlight", options.LogHighlight);
            }

            colorOptions = options;
        }
    }
}
