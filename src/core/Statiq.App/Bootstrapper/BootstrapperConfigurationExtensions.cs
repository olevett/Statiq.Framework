﻿using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using Statiq.Common;

namespace Statiq.App
{
    public static class BootstrapperConfigurationExtensions
    {
        public static Bootstrapper Configure<TConfigurable>(this Bootstrapper bootstrapper, Action<TConfigurable> action)
            where TConfigurable : IConfigurable
        {
            bootstrapper.ThrowIfNull(nameof(bootstrapper));
            bootstrapper.Configurators.Add(action);
            return bootstrapper;
        }

        public static Bootstrapper AddConfigurator<TConfigurable, TConfigurator>(
            this Bootstrapper bootstrapper)
            where TConfigurable : IConfigurable
            where TConfigurator : Common.IConfigurator<TConfigurable>
        {
            bootstrapper.ThrowIfNull(nameof(bootstrapper));
            bootstrapper.Configurators.Add<TConfigurable, TConfigurator>();
            return bootstrapper;
        }

        public static Bootstrapper AddConfigurator<TConfigurable>(
            this Bootstrapper bootstrapper,
            Common.IConfigurator<TConfigurable> configurator)
            where TConfigurable : IConfigurable
        {
            bootstrapper.ThrowIfNull(nameof(bootstrapper));
            bootstrapper.Configurators.Add(configurator);
            return bootstrapper;
        }
    }
}