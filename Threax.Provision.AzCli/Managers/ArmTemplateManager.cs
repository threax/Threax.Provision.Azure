using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Threax.ProcessHelper;
using Threax.Provision.Azure.Core;

namespace Threax.Provision.AzCli.Managers
{
    class ArmTemplateManager : IArmTemplateManager
    {
        private readonly IShellRunner shellRunner;

        public ArmTemplateManager(IShellRunner shellRunner)
        {
            this.shellRunner = shellRunner;
        }

        public void ResourceGroupDeployment(string resourceGroupName, ArmTemplate armTemplate)
        {
            ResourceGroupDeployment(resourceGroupName, armTemplate.GetTemplatePath(), armTemplate.GetParametersPath(), armTemplate);
        }

        public void ResourceGroupDeployment(string resourceGroupName, string templateFile)
        {
            ResourceGroupDeployment(resourceGroupName, templateFile, null);
        }

        public void ResourceGroupDeployment(string resourceGroupName, string templateFile, object? args)
        {
            ResourceGroupDeployment(resourceGroupName, templateFile, null, args);
        }

        public void ResourceGroupDeployment(string resourceGroupName, string templateFile, string? templateParametersFile)
        {
            ResourceGroupDeployment(resourceGroupName, templateFile, templateParametersFile, null);
        }

        public void ResourceGroupDeployment(string resourceGroupName, string templateFile, string? templateParameterFile, object? args)
        {
            IEnumerable<FormattableString> command = new FormattableString[] { $"az deployment group create --resource-group {resourceGroupName}" };
            command = command.Concat(SetupArgs(templateFile, templateParameterFile, args));
            shellRunner.RunProcessVoid(command, invalidExitCodeMessage: $"Error on resource group deploy for arm template '{templateFile}' to resource group '{resourceGroupName}'.");
        }

        public void SubscriptionDeployment(string resourceGroupName, ArmTemplate armTemplate)
        {
            SubscriptionDeployment(resourceGroupName, armTemplate.GetTemplatePath(), armTemplate.GetParametersPath(), armTemplate);
        }

        public void SubscriptionDeployment(string location, string templateFile)
        {
            SubscriptionDeployment(location, templateFile, null);
        }

        public void SubscriptionDeployment(string location, string templateFile, object args)
        {
            SubscriptionDeployment(location, templateFile, null, args);
        }

        public void SubscriptionDeployment(string location, string templateFile, string templateParametersFile)
        {
            SubscriptionDeployment(location, templateFile, templateParametersFile, new Object());
        }

        public void SubscriptionDeployment(string location, string templateFile, string templateParameterFile, object args)
        {
            IEnumerable<FormattableString> command = new FormattableString[] { $"az deployment sub create --location {location}" };
            command = command.Concat(SetupArgs(templateFile, templateParameterFile, args));
            shellRunner.RunProcessVoid(command, invalidExitCodeMessage: $"Error on subscripton deploy for arm template '{templateFile}' to location '{location}'.");
        }

        private static IEnumerable<FormattableString> SetupArgs(string templateFile, string? templateParameterFile, Object? args)
        {
            var argList = new List<FormattableString>();
            var name = Guid.NewGuid();

            templateFile = Path.GetFullPath(templateFile);
            argList.Add($" --template-file {templateFile} --name {name}");

            if (templateParameterFile != null)
            {
                templateParameterFile = Path.GetFullPath(templateParameterFile);
                argList.Add($" --parameters @{templateParameterFile}");
            }

            if (args != null)
            {
                var passProps = GetPropertiesAndValues(args).Where(i => i.Value != null).ToList();

                if (passProps.Count > 0)
                {
                    argList.Add($" --parameters");

                    foreach (var prop in passProps)
                    {
                        argList.Add($" {new RawProcessString(prop.Key)}={prop.Value}");
                    }
                }
            }

            return argList;
        }

        private static IEnumerable<KeyValuePair<String, Object?>> GetPropertiesAndValues(Object instance)
        {
            foreach (var prop in instance.GetType().GetTypeInfo().DeclaredProperties)
            {
                var getMethod = prop.GetGetMethod();
                if (getMethod != null)
                {
                    yield return KeyValuePair.Create<String, Object?>(prop.Name, getMethod.Invoke(instance, new object[0]));
                }
            }
        }
    }
}
