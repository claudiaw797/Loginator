// Copyright (C) 2024 Claudia Wagner

using Backend.Model;
using Common;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;

namespace Loginator.Bootstrapper {

    internal class StringResources {

        private readonly Dictionary<KnownCulture, ResourceDictionary> stringResources = [];
        private readonly IDisposable? configurationChangeListener;

        public StringResources(IOptionsMonitor<Configuration> configurationDao) {
            InitializeDefaultCulture();
            SetCulture(configurationDao.CurrentValue.Language);

            configurationChangeListener = configurationDao.OnChange(o => this.SetCulture(o.Language));
        }

        public KnownCulture CurrentCulture { get; private set; }

        internal void SetSystemCulture() {
            var culture = Thread.CurrentThread.CurrentCulture.ToString();
            this.SetCulture(GetKnownCulture(culture));
        }

        internal void SetCulture(KnownCulture nextCulture) {
            if (CurrentCulture == nextCulture)
                return;

            if (!stringResources.TryGetValue(nextCulture, out var nextStringResources)) {
                nextStringResources = LoadStringResources(nextCulture);
                stringResources.Add(nextCulture, nextStringResources);
            }

            if (CurrentCulture != KnownCulture.English) {
                var currentStringResources = stringResources[CurrentCulture];
                Application.Current.Resources.MergedDictionaries.Remove(currentStringResources);
            }

            if (nextCulture != KnownCulture.English)
                Application.Current.Resources.MergedDictionaries.Add(nextStringResources);

            CurrentCulture = nextCulture;
        }

        private static KnownCulture GetKnownCulture(string culture) =>
            culture switch {
                var c when c.StartsWith("de") => KnownCulture.German,
                _ => KnownCulture.English,
            };

        private static ResourceDictionary LoadStringResources(KnownCulture culture) =>
            LoadStringResources(GetStringResourcesPath(culture));

        private static ResourceDictionary LoadStringResources(string path) =>
            new() { Source = new Uri($@"..\{path}", UriKind.Relative) };

        private static string GetStringResourcesPath(KnownCulture culture) {
            var infix = culture switch {
                KnownCulture.German => ".de",
                _ => string.Empty,
            };
            return $@"Resources\Strings{infix}.xaml";
        }

        private void InitializeDefaultCulture() {
            CurrentCulture = KnownCulture.English;

            var mergedResources = Application.Current.Resources.MergedDictionaries;
            var defaultPath = GetStringResourcesPath(CurrentCulture);
            var defaultStringResources = mergedResources.FirstOrDefault(d => d.Source.OriginalString.Equals(defaultPath));
            if (defaultStringResources is null) {
                defaultStringResources = LoadStringResources(defaultPath);
                mergedResources.Add(defaultStringResources);
            }

            stringResources.Add(CurrentCulture, defaultStringResources);
        }
    }
}
