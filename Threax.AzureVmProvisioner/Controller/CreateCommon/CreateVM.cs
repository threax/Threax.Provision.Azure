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
        Task Run(Configuration config);
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
        public async Task Run(Configuration config)
        {
            var envConfig = config.Environment;

            logger.LogInformation("Creating common compute resources.");

            if (await this.acrManager.IsNameAvailable(envConfig.AcrName))
            {
                logger.LogInformation($"Creating ACR '{envConfig.AcrName}'.");

                await this.acrManager.Create(envConfig.AcrName, envConfig.ResourceGroup, envConfig.Location, "Basic");
            }
            else
            {
                logger.LogInformation($"Find existing acr '{envConfig.AcrName}'.");

                //This will fail if this acr isn't under our control
                await this.acrManager.GetAcr(envConfig.AcrName, envConfig.ResourceGroup);
            }

            logger.LogInformation($"Get acr credentials '{envConfig.AcrName}'.");
            var acrCreds = await acrManager.GetAcrCredential(envConfig.AcrName, envConfig.ResourceGroup);

            //Setup Vm
            logger.LogInformation($"Setup vm credentials in key vault '{envConfig.VmName}'.");
            await keyVaultAccessManager.Unlock(envConfig.InfraKeyVaultName, envConfig.UserId);
            var vmCreds = await credentialLookup.GetOrCreateCredentials(envConfig.InfraKeyVaultName, envConfig.VmAdminBaseKey);

            if (String.IsNullOrEmpty(envConfig.VmName))
            {
                throw new InvalidOperationException($"You must supply a '{nameof(envConfig.VmName)}' property in your config file.");
            }

            logger.LogInformation($"Creating virtual machine '{envConfig.VmName}'.");
            var publicKey = await sshCredsManager.LoadPublicKey();
            var vm = new ArmVm(envConfig.VmName, envConfig.ResourceGroup, vmCreds.User, publicKey)
            {
                publicIpAddressName = envConfig.PublicIpName,
                networkSecurityGroupName = envConfig.NsgName,
                virtualNetworkName = envConfig.VnetName
            };
            await armTemplateManager.ResourceGroupDeployment(envConfig.ResourceGroup, vm);

            //Save Ssh Key
            logger.LogInformation($"Saving known hosts secret.");
            await sshCredsManager.SaveSshKnownHostsSecret();

            logger.LogInformation("Running setup script on server.");
            await vmCommands.RunSetupScript(envConfig.VmName, envConfig.ResourceGroup, $"{envConfig.AcrName}.azurecr.io", acrCreds);

            //Setup App Insights
            logger.LogInformation($"Creating App Insights '{envConfig.AppInsightsName}' in Resource Group '{envConfig.ResourceGroup}'");

            var armAppInsights = new ArmAppInsights(envConfig.AppInsightsName, envConfig.Location);
            await armTemplateManager.ResourceGroupDeployment(envConfig.ResourceGroup, armAppInsights);
        }
    }
}
