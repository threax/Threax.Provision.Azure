using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.ArmTemplates.AppInsights;
using Threax.AzureVmProvisioner.ArmTemplates.ArmVm;
using Threax.AzureVmProvisioner.ArmTemplates.KeyVault;
using Threax.AzureVmProvisioner.ArmTemplates.ResourceGroup;
using Threax.AzureVmProvisioner.ArmTemplates.SqlDb;
using Threax.AzureVmProvisioner.ArmTemplates.SqlServer;
using Threax.AzureVmProvisioner.Services;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Controller
{
    record CreateCommonController
    (
        ILogger<CreateCommonController> logger,
        EnvironmentConfiguration config,
        IArmTemplateManager armTemplateManager,
        IKeyVaultManager keyVaultManager,
        IAcrManager acrManager,
        IKeyVaultAccessManager keyVaultAccessManager,
        ICredentialLookup credentialLookup,
        IVmCommands vmCommands,
        ISshCredsManager sshCredsManager
    )
    : IController
    {
        public async Task Run()
        {
            logger.LogInformation("Creating common resources.");

            await CreateResourceGroup();
            await CreateKeyVaults();
            await CreateCompute();
            await CreateSql();
        }

        private async Task CreateResourceGroup()
        {
            //Resource Group
            logger.LogInformation($"Creating resource group '{config.ResourceGroup}'.");

            var armResourceGroup = new ArmResourceGroup(config.ResourceGroup);
            await armTemplateManager.SubscriptionDeployment(config.Location, armResourceGroup);
        }

        private async Task CreateKeyVaults()
        {
            //Key Vaults
            logger.LogInformation($"Setting up infra key vault '{config.InfraKeyVaultName}'.");

            if (!await keyVaultManager.Exists(config.InfraKeyVaultName))
            {
                logger.LogInformation($"Creating infra key vault '{config.InfraKeyVaultName}'.");

                var keyVaultArm = new ArmKeyVault(config.InfraKeyVaultName, config.Location, config.TenantId.ToString());
                await armTemplateManager.ResourceGroupDeployment(config.ResourceGroup, keyVaultArm);
            }

            //Allow AzDo user in the key vault if one is set.
            if (config.AzDoUser != null)
            {
                logger.LogInformation($"Adding AzDo user to infra key vault '{config.InfraKeyVaultName}'.");

                await keyVaultManager.UnlockSecretsRead(config.InfraKeyVaultName, config.AzDoUser.Value);
            }
        }

        private async Task CreateCompute()
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


        public async Task CreateSql()
        {
            //In this setup there is actually only 1 db to save money.
            //So both the sql server and the db will be provisioned in this step.
            //You would want to have separate dbs in a larger setup.
            await keyVaultAccessManager.Unlock(config.InfraKeyVaultName, config.UserId);

            var saCreds = await credentialLookup.GetOrCreateCredentials(config.InfraKeyVaultName, config.SqlSaBaseKey);

            //Setup logical server
            logger.LogInformation($"Setting up SQL Logical Server '{config.SqlServerName}' in Resource Group '{config.ResourceGroup}'.");
            await this.armTemplateManager.ResourceGroupDeployment(config.ResourceGroup, new ArmSqlServer(config.SqlServerName, saCreds.User, saCreds.Pass, config.VnetName, config.VnetSubnetName));

            //Setup shared sql db
            logger.LogInformation($"Setting up Shared SQL Database '{config.SqlDbName}' on SQL Logical Server '{config.SqlServerName}'.");
            await this.armTemplateManager.ResourceGroupDeployment(config.ResourceGroup, new ArmSqlDb(config.SqlServerName, config.SqlDbName));
        }
    }
}
