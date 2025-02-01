// Copyright (C) 2025 Claudia Wagner

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Loginator.UnitTests.Infrastructure {

    public static class ServiceProviderExtensions {

        public static TService TestRegistration<TService>(this IServiceProvider sp)
            where TService : notnull {
            var actual = sp.GetRequiredService<TService>();
            actual.Should().BeAssignableTo<TService>();
            return actual;
        }

        public static TService TestRegistration<TService>(this IServiceProvider sp, object serviceKey) {
            var actual = sp.GetKeyedService<TService>(serviceKey);
            actual.Should().BeAssignableTo<TService>();
            return actual;
        }
    }
}