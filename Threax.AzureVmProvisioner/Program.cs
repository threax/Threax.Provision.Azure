using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Threax.Azure.Abstractions;
using Threax.AzureVmProvisioner.Controller;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;
using Threax.AzureVmProvisioner.Workers;
using Threax.ConsoleApp;

namespace Threax.AzureVmProvisioner
{
    class Program
    {
        private const String EnvironmentSectionName = "Environment";
        private const String ResourcesSectionName = "Resources";
        private const String KeyVaultSectionName = "KeyVault";
        private const String StorageSectionName = "Storage";

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

                services.AddThreaxProvisionAzPowershell();

                services.AddScoped<IStringGenerator, StringGenerator>();
                services.AddScoped<ICredentialLookup, CredentialLookup>();
                services.AddScoped<IVmCommands, VmCommands>();
                services.AddScoped<ISshCredsManager, SshCredsManager>();

                RegisterWorkers(services, typeof(Program).Assembly);
            })
            .Run(c => c.Run());
        }

        private static void RegisterWorkers(IServiceCollection services, Assembly assembly)
        {
            var genericWorkerType = typeof(IWorker<>);

            foreach(var type in assembly.GetTypes())
            {
                var concreteType = genericWorkerType.MakeGenericType(type);
                if (concreteType.IsAssignableFrom(type) && type != concreteType)
                {
                    services.AddScoped(concreteType, type);
                }
            }
        }
    }
}
