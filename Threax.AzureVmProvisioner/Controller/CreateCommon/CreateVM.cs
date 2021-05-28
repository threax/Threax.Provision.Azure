using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.ArmTemplates.AppInsights;
using Threax.AzureVmProvisioner.ArmTemplates.ArmVm;
using Threax.AzureVmProvisioner.Services;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Controller
{
    interface ICreateVM : IController
    {
        Task Run(EnvironmentConfiguration config);
    }

    [HelpInfo(HelpCategory.CreateCommon, "Create the common vm for apps to run on.")]
    record CreateVM
    (
        ILogger<CreateVM> logger,
        IArmTemplateManager armTemplateManager,
        IAcrManager acrManager,
        IKeyVaultAccessManager keyVaultAccessManager,
        ICredentialLookup credentialLookup,
        IVmCommands vmCommands,
        ISshCredsManager sshCredsManager
    ) : ICreateVM
    {
        public async Task Run(EnvironmentConfiguration config)
        {
            logger.LogInformation("Creating common compute resources.");

            if (await this.acrManager.IsNameAvailable(config.AcrName))
            {
                logger.LogInformation($"Creating ACR '{config.AcrName}'.");

                await this.acrManager.Create(config.AcrName, config.ResourceGroup, config.Location, "Basic");
            }
            else
            {
                logger.LogInformation($"Find existing acr '{config.AcrName}'.");

                //This will fail if this acr isn't under our control
                await this.acrManager.GetAcr(config.AcrName, config.ResourceGroup);
            }

            logger.LogInformation($"Get acr credentials '{config.AcrName}'.");
            var acrCreds = await acrManager.GetAcrCredential(config.AcrName, config.ResourceGroup);

            //Setup Vm
            logger.LogInformation($"Setup vm credentials in key vault '{config.VmName}'.");
            await keyVaultAccessManager.Unlock(config.InfraKeyVaultName, config.UserId);
            var vmCreds = await credentialLookup.GetOrCreateCredentials(config.InfraKeyVaultName, config.VmAdminBaseKey);

            if (String.IsNullOrEmpty(config.VmName))
            {
                throw new InvalidOperationException($"You must supply a '{nameof(config.VmName)}' property in your config file.");
            }

            logger.LogInformation($"Creating virtual machine '{config.VmName}'.");
            var publicKey = await sshCredsManager.LoadPublicKey();
            var vm = new ArmVm(config.VmName, config.ResourceGroup, vmCreds.User, publicKey)
            {
                publicIpAddressName = config.PublicIpName,
                networkSecurityGroupName = config.NsgName,
                virtualNetworkName = config.VnetName
            };
            await armTemplateManager.ResourceGroupDeployment(config.ResourceGroup, vm);

            //Save Ssh Key
            logger.LogInformation($"Saving known hosts secret.");
            await sshCredsManager.SaveSshKnownHostsSecret();

            logger.LogInformation("Running setup script on server.");
            await vmCommands.RunSetupScript(config.VmName, config.ResourceGroup, $"{config.AcrName}.azurecr.io", acrCreds);

            //Setup App Insights
            logger.LogInformation($"Creating App Insights '{config.AppInsightsName}' in Resource Group '{config.ResourceGroup}'");

            var armAppInsights = new ArmAppInsights(config.AppInsightsName, config.Location);
            await armTemplateManager.ResourceGroupDeployment(config.ResourceGroup, armAppInsights);
        }
    }
}
