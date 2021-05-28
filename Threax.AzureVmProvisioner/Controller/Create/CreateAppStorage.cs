using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.ArmTemplates.StorageAccount;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Controller
{
    interface ICreateAppStorage : IController
    {
        Task Run(Configuration config);
    }

    [HelpInfo(HelpCategory.Create, "Create a storage account for the given app.")]
    record CreateAppStorage
    (
        ILogger<CreateAppStorage> logger,
        IArmTemplateManager armTemplateManager,
        IStorageManager storageManager,
        IKeyVaultAccessManager keyVaultAccessManager,
        IKeyVaultManager keyVaultManager
    )
    : ICreateAppStorage
    {
        public async Task Run(Configuration config)
        {
            var envConfig = config.Environment;
            var resources = config.Resources;
            var azureKeyVaultConfig = config.KeyVault;
            var azureStorageConfig = config.Storage;

            var resource = resources.Storage;

            if(resource == null)
            {
                return;
            }

            logger.LogInformation($"Processing storage account '{azureStorageConfig.StorageAccount}'");

            var nameCheck = azureStorageConfig.StorageAccount ?? throw new InvalidOperationException("You must provide a name for storage resources.");

            var storage = new ArmStorageAccount(azureStorageConfig.StorageAccount, envConfig.Location);
            await armTemplateManager.ResourceGroupDeployment(envConfig.ResourceGroup, storage);

            if (!String.IsNullOrWhiteSpace(resource.AccessKeySecretName))
            {
                logger.LogInformation($"Setting up storage connection string '{resource.AccessKeySecretName}' in Key Vault '{azureKeyVaultConfig.VaultName}'.");

                await keyVaultAccessManager.Unlock(azureKeyVaultConfig.VaultName, envConfig.UserId);

                var accessKey = await storageManager.GetAccessKey(azureStorageConfig.StorageAccount, envConfig.ResourceGroup);

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
