// Copyright (C) 2024 Claudia Wagner

using System;

namespace Loginator.Domain.Option {

    public interface IOptionsRepository<out TOptions>
        where TOptions : class, new() {

        TOptions Get();

        void Save(Action<TOptions> applyChanges);

        IDisposable? OnChanged(Action<TOptions, string?> listener);
    }
}
