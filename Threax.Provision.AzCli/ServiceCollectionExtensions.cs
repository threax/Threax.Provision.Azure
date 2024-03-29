﻿using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using Threax.Provision.AzCli;
using Threax.Provision.AzCli.Managers;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddThreaxProvisionAzCli(this IServiceCollection services, Action<ThreaxAzCliOptions>? configure = null)
        {
            var options = new ThreaxAzCliOptions();
            configure?.Invoke(options);

            services.TryAddScoped<IAccountManager, AccountManager>();
            services.TryAddScoped<IArmTemplateManager, ArmTemplateManager>();

            return services;
        }
    }
}
