using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Threax.Azure.Abstractions;
using Threax.AzureVmProvisioner.ArmTemplates.StorageAccount;
using Threax.AzureVmProvisioner.Resources;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Workers
{
    record CreateAppStorage
    (
        ILogger<CreateAppStorage> logger,
        EnvironmentConfiguration config,
        IArmTemplateManager armTemplateManager,
        IStorageManager storageManager,
        IKeyVaultAccessManager keyVaultAccessManager,
        IKeyVaultManager keyVaultManager,
        AzureKeyVaultConfig azureKeyVaultConfig,
        AzureStorageConfig azureStorageConfig,
        ResourceConfiguration resources
    )
    : IWorker<CreateAppStorage>
    {
        public async Task ExecuteAsync()
        {
            var resource = resources.Storage;

            if(resource == null)
            {
                return;
            }

            var nameCheck = azureStorageConfig.StorageAccount ?? throw new InvalidOperationException("You must provide a name for storage resources.");

            var storage = new ArmStorageAccount(azureStorageConfig.StorageAccount, config.Location);
            await armTemplateManager.ResourceGroupDeployment(config.ResourceGroup, storage);

            if (!String.IsNullOrWhiteSpace(resource.AccessKeySecretName))
            {
                logger.LogInformation($"Setting up connection string in Key Vault '{azureKeyVaultConfig.VaultName}'.");

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
