using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using Threax.Azure.Abstractions;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Workers
{
    record LoadExternalSecretsWorker
    (
        ResourceConfiguration ResourceConfiguration,
        IKeyVaultManager KeyVaultManager,
        EnvironmentConfiguration EnvironmentConfiguration,
        AzureKeyVaultConfig AzureKeyVaultConfig,
        IPathHelper PathHelper,
        IVmCommands VmCommands,
        ILogger<LoadExternalSecretsWorker> Logger
    ) 
        : IWorker<LoadExternalSecretsWorker>
    {
        public async Task ExecuteAsync()
        {
            if(ResourceConfiguration.ExternalSecrets == null)
            {
                return;
            }

            Logger.LogInformation($"Managing external secrets.");

            if(AzureKeyVaultConfig?.VaultName != null)
            {
                await KeyVaultManager.UnlockSecrets(AzureKeyVaultConfig.VaultName, EnvironmentConfiguration.UserId);
            }

            await KeyVaultManager.UnlockSecrets(EnvironmentConfiguration.ExternalKeyVaultName, EnvironmentConfiguration.UserId);

            foreach (var externalSecret in ResourceConfiguration.ExternalSecrets)
            {
                Logger.LogInformation($"Copying secret '{externalSecret.Source}' to '{externalSecret.Destination}' of type '{externalSecret.Type}'");

                var secret = await KeyVaultManager.GetSecret(EnvironmentConfiguration.ExternalKeyVaultName, externalSecret.Source);
                switch (externalSecret.Type)
                {
                    case ExternalSecretDestinationType.AppKeyVault:
                        if (AzureKeyVaultConfig?.VaultName == null)
                        {
                            throw new InvalidOperationException($"You must define a '{nameof(AzureKeyVaultConfig.VaultName)}' property in your KeyVault section to store external secrets in a key vault.");
                        }
                        await KeyVaultManager.SetSecret(AzureKeyVaultConfig.VaultName, externalSecret.Destination, secret);
                        break;
                    case ExternalSecretDestinationType.Local:
                        var fileName = Path.GetFileName(PathHelper.ConfigPath);
                        var serverConfigFilePath = $"/app/{ResourceConfiguration.Compute.Name}/{fileName}";
                        await VmCommands.SetSecretFromString(EnvironmentConfiguration.VmName, EnvironmentConfiguration.ResourceGroup, PathHelper.ConfigPath, serverConfigFilePath, externalSecret.Destination, secret);
                        break;
                    default:
                        throw new InvalidOperationException($"Destination type '{externalSecret.Destination}' is not supported for loading external secrets.");
                }
            }
        }
    }
}
