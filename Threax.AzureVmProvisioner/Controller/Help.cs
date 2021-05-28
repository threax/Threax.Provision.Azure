using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threax.AzureVmProvisioner.Controller
{
    class Help : IController
    {
        private static readonly String ControllerSuffix = "Controller";

        public Task Run()
        {
            var assembly = typeof(Help).Assembly;
            var controllerType = typeof(IController);

            foreach (var type in assembly.GetTypes().Where(i => !i.IsInterface))
            {
                if (controllerType.IsAssignableFrom(type) && type != controllerType)
                {
                    var baseType = type.GetInterfaces().Where(i => i != controllerType && controllerType.IsAssignableFrom(i)).FirstOrDefault();
                    if (baseType != null)
                    {
                        var helpInfo = type
                            .GetCustomAttributes(typeof(HelpInfoAttribute), false)
                            .Select(i => i as HelpInfoAttribute)
                            .FirstOrDefault() ?? new HelpInfoAttribute(HelpCategory.Unknown, "No description");

                        var name = type.Name;
                        if (name.EndsWith(ControllerSuffix))
                        {
                            name = name.Substring(0, name.Length - ControllerSuffix.Length);
                        }
                        Console.WriteLine($"{name} - {helpInfo.Description}");
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
