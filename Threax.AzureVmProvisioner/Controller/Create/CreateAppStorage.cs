using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Threax.Azure.Abstractions;
using Threax.AzureVmProvisioner.ArmTemplates.StorageAccount;
using Threax.AzureVmProvisioner.Resources;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Workers
{
    interface ICreateAppStorage : IController
    {
        Task Run(EnvironmentConfiguration config, ResourceConfiguration resources, AzureKeyVaultConfig azureKeyVaultConfig, AzureStorageConfig azureStorageConfig);
    }

    [HelpInfo(HelpCategory.Create, "Create a storage account for the given app.")]
    record CreateAppStorage
    (
        ILogger<CreateAppStorage> logger,
        EnvironmentConfiguration config,
        IArmTemplateManager armTemplateManager,
        IStorageManager storageManager,
        IKeyVaultAccessManager keyVaultAccessManager,
        IKeyVaultManager keyVaultManager
    )
    : ICreateAppStorage
    {
        public async Task Run(EnvironmentConfiguration config, ResourceConfiguration resources, AzureKeyVaultConfig azureKeyVaultConfig, AzureStorageConfig azureStorageConfig)
        {
            var resource = resources.Storage;

            if(resource == null)
            {
                return;
            }

            logger.LogInformation($"Processing storage account '{azureStorageConfig.StorageAccount}'");

            var nameCheck = azureStorageConfig.StorageAccount ?? throw new InvalidOperationException("You must provide a name for storage resources.");

            var storage = new ArmStorageAccount(azureStorageConfig.StorageAccount, config.Location);
            await armTemplateManager.ResourceGroupDeployment(config.ResourceGroup, storage);

            if (!String.IsNullOrWhiteSpace(resource.AccessKeySecretName))
            {
                logger.LogInformation($"Setting up storage connection string '{resource.AccessKeySecretName}' in Key Vault '{azureKeyVaultConfig.VaultName}'.");

                await keyVaultAccessManager.Unlock(azureKeyVaultConfig.VaultName, config.UserId);

                var accessKey = await storageManager.GetAccessKey(azureStorageConfig.StorageAccount, config.ResourceGroup);

                if (accessKey == null)
                {
                    throw new InvalidOperationException("The access key returned from the server was null.");
                }

                //Need to double check format here, assuming key is valid for now
                await keyVaultManager.SetSecret(azureKeyVaultConfig.VaultName, resource.AccessKeySecretName, accessKey);
            }
        }
    }
}
