using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.ArmTemplates.AppInsights;
using Threax.AzureVmProvisioner.ArmTemplates.ArmVm;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Controller.CreateCommon
{
    class CreateCommonCompute
    {
        private readonly EnvironmentConfiguration config;
        private readonly IAcrManager acrManager;
        private readonly IArmTemplateManager armTemplateManager;
        private readonly IKeyVaultManager keyVaultManager;
        private readonly IKeyVaultAccessManager keyVaultAccessManager;
        private readonly ILogger<CreateCommonCompute> logger;
        private readonly ICredentialLookup credentialLookup;
        private readonly IVmManager vmManager;
        private readonly IVmCommands vmCommands;
        private readonly ISshCredsManager sshCredsManager;

        public CreateCommonCompute(
            EnvironmentConfiguration config,
            IAcrManager acrManager,
            IArmTemplateManager armTemplateManager,
            IKeyVaultManager keyVaultManager,
            IKeyVaultAccessManager keyVaultAccessManager,
            ILogger<CreateCommonCompute> logger,
            ICredentialLookup credentialLookup,
            IVmManager vmManager,
            IVmCommands vmCommands,
            ISshCredsManager sshCredsManager)
        {
            this.config = config;
            this.acrManager = acrManager;
            this.armTemplateManager = armTemplateManager;
            this.keyVaultManager = keyVaultManager;
            this.keyVaultAccessManager = keyVaultAccessManager;
            this.logger = logger;
            this.credentialLookup = credentialLookup;
            this.vmManager = vmManager;
            this.vmCommands = vmCommands;
            this.sshCredsManager = sshCredsManager;
        }

        public async Task Execute()
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
