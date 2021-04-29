using System;
using System.Collections.Generic;
using System.Text;
using Threax.Provision.Azure.Core;

namespace Threax.Provision.AzCli.Managers
{
    public interface IArmTemplateManager
    {
        void ResourceGroupDeployment(string resourceGroupName, ArmTemplate armTemplate);
        void ResourceGroupDeployment(string resourceGroupName, string templateFile);
        void ResourceGroupDeployment(string resourceGroupName, string templateFile, object? args);
        void ResourceGroupDeployment(string resourceGroupName, string templateFile, string? templateParametersFile);
        void ResourceGroupDeployment(string resourceGroupName, string templateFile, string? templateParameterFile, object? args);
        void SubscriptionDeployment(string resourceGroupName, ArmTemplate armTemplate);
        void SubscriptionDeployment(string location, string templateFile);
        void SubscriptionDeployment(string location, string templateFile, object? args);
        void SubscriptionDeployment(string location, string templateFile, string? templateParametersFile);
        void SubscriptionDeployment(string location, string templateFile, string? templateParameterFile, object? args);
    }
}
