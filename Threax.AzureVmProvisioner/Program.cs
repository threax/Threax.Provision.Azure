using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.Controller;
using Threax.AzureVmProvisioner.Controller.CreateCommon;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;
using Threax.ConsoleApp;
using Threax.Extensions.Configuration.SchemaBinder;
using Threax.ProcessHelper;

namespace Threax.AzureVmProvisioner
{
    class Program
    {
        private const String EnvironmentSectionName = "Environment";
        private const String ResourcesSectionName = "Resources";

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
                    return new ConfigLoader(pathHelper.Path);
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

                services.AddScoped<CreateCommonCompute>();
                services.AddScoped<CreateCommonKeyVault>();
                services.AddScoped<CreateCommonResourceGroup>();
                services.AddScoped<CreateCommonSqlDatabase>();
            })
            .Run(c => c.Run());
        }
    }
}
