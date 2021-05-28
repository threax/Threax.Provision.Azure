using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using Threax.Azure.Abstractions;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Controller
{
    interface ILoadExternalSecretsWorker : IController
    {
        Task Run(EnvironmentConfiguration config, ResourceConfiguration resources, AzureKeyVaultConfig azureKeyVaultConfig);
    }

    [HelpInfo(HelpCategory.Create, "Load secrets from the external secrets vault for the given app.")]
    record LoadExternalSecretsWorker
    (
        IKeyVaultManager KeyVaultManager,
        IPathHelper PathHelper,
        IVmCommands VmCommands,
        ILogger<LoadExternalSecretsWorker> Logger
    ) 
        : ILoadExternalSecretsWorker
    {
        public async Task Run(EnvironmentConfiguration config, ResourceConfiguration resources, AzureKeyVaultConfig azureKeyVaultConfig)
        {
            if(resources.ExternalSecrets == null)
            {
                return;
            }

            Logger.LogInformation($"Managing external secrets.");

            if(azureKeyVaultConfig?.VaultName != null)
            {
                await KeyVaultManager.UnlockSecrets(azureKeyVaultConfig.VaultName, config.UserId);
            }

            await KeyVaultManager.UnlockSecrets(config.ExternalKeyVaultName, config.UserId);

            foreach (var externalSecret in resources.ExternalSecrets)
            {
                Logger.LogInformation($"Copying secret '{externalSecret.Source}' to '{externalSecret.Destination}' of type '{externalSecret.Type}'");

                var secret = await KeyVaultManager.GetSecret(config.ExternalKeyVaultName, externalSecret.Source);
                switch (externalSecret.Type)
                {
                    case ExternalSecretDestinationType.AppKeyVault:
                        if (azureKeyVaultConfig?.VaultName == null)
                        {
                            throw new InvalidOperationException($"You must define a '{nameof(azureKeyVaultConfig.VaultName)}' property in your KeyVault section to store external secrets in a key vault.");
                        }
                        await KeyVaultManager.SetSecret(azureKeyVaultConfig.VaultName, externalSecret.Destination, secret);
                        break;
                    case ExternalSecretDestinationType.Local:
                        var fileName = Path.GetFileName(PathHelper.ConfigPath);
                        var serverConfigFilePath = $"/app/{resources.Compute.Name}/{fileName}";
                        await VmCommands.SetSecretFromString(config.VmName, config.ResourceGroup, PathHelper.ConfigPath, serverConfigFilePath, externalSecret.Destination, secret);
                        break;
                    default:
                        throw new InvalidOperationException($"Destination type '{externalSecret.Destination}' is not supported for loading external secrets.");
                }
            }
        }
    }
}
