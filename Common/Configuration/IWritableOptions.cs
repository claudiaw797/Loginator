using Microsoft.Extensions.Options;
using System;

namespace Common.Configuration {

    public interface IWritableOptions<out T> : IOptions<T> where T : class, new() {

        IDisposable? OnChange(Action<T, string?> listener);

        void Update(Action<T> applyChanges);
    }
}
