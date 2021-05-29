using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.Controller;
using Threax.AzureVmProvisioner.Services;
using Threax.ConsoleApp;
using Threax.ProcessHelper;

namespace Threax.AzureVmProvisioner
{
    class Program
    {
        public static Task<int> Main(string[] args)
        {
            string command = args.Length > 0 ? args[0] : null;
            var rootDir = args.Length > 1 ? args[1] : null;
            var includeGlob = args.Length > 2 ? args[2] : null;

            return AppHost
            .Setup<IController, Help>(command, services =>
            {
                services.AddSingleton<IArgsProvider>(s => new ArgsProvider(args));

                services.AddSingleton<IPathHelper, PathHelper>();
                services.AddSingleton<IConfigLoader, ConfigLoader>();
                services.AddSingleton<IConfigProvider, ConfigProvider>();

                //Use the first defined environment as the environment
                //It should always be the same in all files
                services.AddSingleton<EnvironmentConfiguration>(s => s.GetRequiredService<IConfigProvider>().LoadConfigPhases(rootDir, includeGlob).First().First().Environment);

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

                RegisterControllers(services, typeof(Program).Assembly);
            })
            .Run(async (c, s) =>
            {
                var type = c.GetType();
                var runFunc = type.GetMethod("Run");
                if (runFunc == null)
                {
                    throw new InvalidOperationException($"Cannot find a 'Run' function on type '{type.FullName}'");
                }

                //var parms = 
                //    .Select(i =>
                //    {
                //        return s.ServiceProvider.GetRequiredService(i.ParameterType);
                //    })
                //    .ToArray();

                if (runFunc.GetParameters().Any(i => i.ParameterType == typeof(Configuration)))
                {
                    var configProvider = s.ServiceProvider.GetRequiredService<IConfigProvider>();
                    foreach (var configs in configProvider.LoadConfigPhases(rootDir, includeGlob))
                    {
                        var taskList = new List<Task>();
                        foreach (var config in configs)
                        {
                            var parms = new Object[] { config };
                            taskList.Add(runFunc.Invoke(c, parms) as Task);
                        }
                        await Task.WhenAll(taskList);
                    }
                }
                else
                {
                    await (runFunc.Invoke(c, null) as Task);
                }
            });
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
