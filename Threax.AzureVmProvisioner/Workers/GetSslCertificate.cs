using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;
using Threax.ProcessHelper;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Workers
{
    record GetSslCertificate
    (
        EnvironmentConfiguration EnvironmentConfiguration,
        IPathHelper PathHelper,
        IShellRunner ShellRunner,
        IKeyVaultManager KeyVaultManager
    )
        : IWorker<GetSslCertificate>
    {
        public async Task ExecuteAsync()
        {
            if (! await KeyVaultManager.Exists(EnvironmentConfiguration.ExternalKeyVaultName))
            {
                throw new InvalidOperationException($"You must create a key vault named '{EnvironmentConfiguration.ExternalKeyVaultName}' before getting a ssl cert. CreateCommon will do this for you.");
            }

            var keyVaultUnlockTask = KeyVaultManager.UnlockSecrets(EnvironmentConfiguration.ExternalKeyVaultName, EnvironmentConfiguration.UserId);

            var baseUrl = EnvironmentConfiguration.BaseUrl ?? throw new InvalidOperationException($"You must include a '{nameof(EnvironmentConfiguration.BaseUrl)}' property when making ssl certificates.");
            var commonName = $"*.{baseUrl}";
            var email = EnvironmentConfiguration.SslEmail ?? throw new InvalidOperationException($"You must include a '{nameof(EnvironmentConfiguration.SslEmail)}' property when making ssl certificates.");
            var path = PathHelper.ConfigDirectory;

            var time = DateTime.Now.ToString("yyyyMMddhhmmss");
            var certTempPath = Path.GetFullPath(Path.Combine(path, ".files", $"cert_{time}"));
            if (!Directory.Exists(certTempPath))
            {
                Directory.CreateDirectory(certTempPath);
            }

            try
            {
                //Get cert with certbot container
                ShellRunner.RunProcessVoid($"docker run -it --rm -v {certTempPath}:/etc/letsencrypt certbot/certbot certonly --manual --server https://acme-v02.api.letsencrypt.org/directory --preferred-challenges dns --agree-tos --manual-public-ip-logging-ok --no-eff-email --email {email} -d {commonName}");

                var finalCertPath = Path.Combine(certTempPath, "archive", baseUrl);
                var publicKeyPath = Path.Combine(finalCertPath, "fullchain1.pem");
                var privateKeyPath = Path.Combine(finalCertPath, "privkey1.pem");

                var publicKey = File.ReadAllText(publicKeyPath);
                var privateKey = File.ReadAllText(privateKeyPath);

                //Copy certs to key vaults
                await keyVaultUnlockTask;
                await Task.WhenAll
                (
                    KeyVaultManager.SetSecret(EnvironmentConfiguration.ExternalKeyVaultName, EnvironmentConfiguration.SslPublicKeyName, publicKey),
                    KeyVaultManager.SetSecret(EnvironmentConfiguration.ExternalKeyVaultName, EnvironmentConfiguration.SslPrivateKeyName, privateKey)
                );
            }
            finally
            {
                if (Directory.Exists(certTempPath))
                {
                    Directory.Delete(certTempPath, true);
                }
            }
        }
    }
}
