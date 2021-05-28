﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Threax.Azure.Abstractions;
using Threax.AzureVmProvisioner.Controller;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;
using Threax.AzureVmProvisioner.Workers;
using Threax.ConsoleApp;
using Threax.DeployConfig;
using Threax.DockerBuildConfig;
using Threax.ProcessHelper;

namespace Threax.AzureVmProvisioner
{
    class Program
    {
        private const String EnvironmentSectionName = "Environment";
        private const String ResourcesSectionName = "Resources";
        private const String KeyVaultSectionName = "KeyVault";
        private const String StorageSectionName = "Storage";
        private const String BuildSectionName = "Build";
        private const String DeploySectionName = "Deploy";

        public static Task<int> Main(string[] args)
        {
            string command = args.Length > 0 ? args[0] : null;

            return AppHost
            .Setup<IController, HelpController>(command, services =>
            {
                services.AddSingleton<IArgsProvider>(s => new ArgsProvider(args));

                services.AddSingleton<IPathHelper>(s => args.Length > 1 ? new PathHelper(args[1])
                    : throw new InvalidOperationException("No config file path provided."));

                services.AddSingleton<IConfigLoader>(s =>
                {
                    var pathHelper = s.GetRequiredService<IPathHelper>();
                    return new ConfigLoader(pathHelper.ConfigPath);
                });

                services.AddSingleton<EnvironmentConfiguration>(s =>
                {
                    var configBinder = s.GetRequiredService<IConfigLoader>();
                    var config = configBinder.SharedConfigInstance[EnvironmentSectionName]?.ToObject<EnvironmentConfiguration>()
                        ?? throw new InvalidOperationException($"No '{EnvironmentSectionName}' property defined.");
                    return config;
                });

                services.AddSingleton<ResourceConfiguration>(s =>
                {
                    var configBinder = s.GetRequiredService<IConfigLoader>();
                    var config = configBinder.SharedConfigInstance[ResourcesSectionName]?.ToObject<ResourceConfiguration>()
                        ?? throw new InvalidOperationException($"No '{ResourcesSectionName}' property defined.");
                    return config;
                });

                services.AddSingleton<AzureKeyVaultConfig>(s =>
                {
                    var configBinder = s.GetRequiredService<IConfigLoader>();
                    var config = configBinder.SharedConfigInstance[KeyVaultSectionName]?.ToObject<AzureKeyVaultConfig>()
                        ?? new AzureKeyVaultConfig();
                    return config;
                });

                services.AddSingleton<AzureStorageConfig>(s =>
                {
                    var configBinder = s.GetRequiredService<IConfigLoader>();
                    var config = configBinder.SharedConfigInstance[StorageSectionName]?.ToObject<AzureStorageConfig>()
                        ?? new AzureStorageConfig();
                    return config;
                });

                services.AddSingleton<BuildConfig>(s =>
                {
                    var pathHelper = s.GetRequiredService<IPathHelper>();
                    var configBinder = s.GetRequiredService<IConfigLoader>();
                    var result = new BuildConfig(pathHelper.ConfigPath);
                    var config = configBinder.SharedConfigInstance[BuildSectionName];
                    if (config != null)
                    {
                        using var reader = config.CreateReader();
                        Newtonsoft.Json.JsonSerializer.CreateDefault().Populate(reader, result);
                    }
                    return result;
                });

                services.AddSingleton<DeploymentConfig>(s =>
                {
                    var pathHelper = s.GetRequiredService<IPathHelper>();
                    var configBinder = s.GetRequiredService<IConfigLoader>();
                    var result = new DeploymentConfig(pathHelper.ConfigPath);
                    var config = configBinder.SharedConfigInstance[DeploySectionName];
                    if (config != null)
                    {
                        using var reader = config.CreateReader();
                        Newtonsoft.Json.JsonSerializer.CreateDefault().Populate(reader, result);
                    }
                    return result;
                });

                services.AddSingleton<RandomNumberGenerator>(services => RandomNumberGenerator.Create());

                services.AddHttpClient();
                services.AddLogging(o =>
                {
                    o.AddConsole().AddSimpleConsole(co =>
                    {
                        co.IncludeScopes = false;
                        co.SingleLine = true;
                    });
                });

                services.AddThreaxPwshShellRunner(o =>
                {
                    //o.IncludeLogOutput = false;
                    //o.DecorateProcessRunner = r => new SpyProcessRunner(r)
                    //{
                    //    Events = new ProcessEvents()
                    //    {
                    //        ErrorDataReceived = (o, e) => { if (e.DataReceivedEventArgs.Data != null) Console.WriteLine(e.DataReceivedEventArgs.Data); },
                    //        OutputDataReceived = (o, e) => { if (e.DataReceivedEventArgs.Data != null) Console.WriteLine(e.DataReceivedEventArgs.Data); },
                    //    }
                    //};
                });

                services.AddScoped<IOSHandler>(s =>
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        return new OSHandlerWindows();
                    }
                    return new OSHandlerUnix(s.GetRequiredService<IProcessRunner>());
                });

                services.AddThreaxProcessHelper();
                services.AddThreaxProvisionAzPowershell();

                services.AddScoped<IMachineIpManager, MachineIpManager>();
                services.AddScoped<IStringGenerator, StringGenerator>();
                services.AddScoped<ICredentialLookup, CredentialLookup>();
                services.AddScoped<IVmCommands, VmCommands>();
                services.AddScoped<ISshCredsManager, SshCredsManager>();
                services.AddScoped<IImageManager, ImageManager>();
                services.AddScoped<IRunInfoLogger, RunInfoLogger>();
                services.AddSingleton<IAppSecretCreator, AppSecretCreator>();

                RegisterWorkers(services, typeof(Program).Assembly);
                RegisterControllers(services, typeof(Program).Assembly);
            })
            .Run((c, s) =>
            {
                var type = c.GetType();
                var runFunc = type.GetMethod("Run");
                if (runFunc == null)
                {
                    throw new InvalidOperationException($"Cannot find a 'Run' function on type '{type.FullName}'");
                }

                var parms = runFunc.GetParameters()
                    .Select(i =>
                    {
                        return s.ServiceProvider.GetRequiredService(i.ParameterType);
                    })
                    .ToArray();

                return runFunc.Invoke(c, parms) as Task;
            });
        }

        private static void RegisterWorkers(IServiceCollection services, Assembly assembly)
        {
            var genericWorkerType = typeof(IWorker<>);

            foreach (var type in assembly.GetTypes())
            {
                var concreteType = genericWorkerType.MakeGenericType(type);
                if (concreteType.IsAssignableFrom(type) && type != concreteType)
                {
                    services.AddScoped(concreteType, type);
                }
            }
        }

        private static void RegisterControllers(IServiceCollection services, Assembly assembly)
        {
            var controllerType = typeof(IController);

            foreach (var type in assembly.GetTypes().Where(i => !i.IsInterface))
            {
                if (controllerType.IsAssignableFrom(type) && type != controllerType)
                {
                    var baseType = type.GetInterfaces().Where(i => i != controllerType && controllerType.IsAssignableFrom(i)).FirstOrDefault();
                    if (baseType != null)
                    {
                        services.AddScoped(baseType, type);
                    }
                }
            }
        }
    }
}
