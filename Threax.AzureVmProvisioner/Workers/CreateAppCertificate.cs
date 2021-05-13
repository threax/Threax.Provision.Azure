using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Threax.Azure.Abstractions;
using Threax.AzureVmProvisioner.Resources;
using Threax.AzureVmProvisioner.Services;
using Threax.Provision.AzPowershell;

namespace Threax.AzureVmProvisioner.Workers
{
    record CreateAppCertificate
    (
        IStringGenerator stringGenerator, 
        IKeyVaultManager keyVaultManager, 
        EnvironmentConfiguration config, 
        IKeyVaultAccessManager keyVaultAccessManager, 
        AzureKeyVaultConfig azureKeyVaultConfig,
        ResourceConfiguration resources
    ) : IWorker<CreateAppCertificate>
    {
        public async Task ExecuteAsync()
        {
            var resource = resources.Certificate;

            if(resource == null)
            {
                return;
            }

            if (String.IsNullOrEmpty(resource.Name))
            {
                throw new InvalidOperationException($"You must provide a value for '{nameof(Certificate.Name)}' in your '{nameof(Certificate)}' types.");
            }

            if (String.IsNullOrEmpty(resource.CN))
            {
                throw new InvalidOperationException($"You must provide a value for '{nameof(Certificate.CN)}' in your '{nameof(Certificate)}' types.");
            }

            await keyVaultAccessManager.Unlock(azureKeyVaultConfig.VaultName, config.UserId);

            var existingCert = await keyVaultManager.GetCertificate(azureKeyVaultConfig.VaultName, resource.Name);
            if (existingCert == null)
            {
                using (var rsa = RSA.Create()) // generate asymmetric key pair
                {
                    var request = new CertificateRequest($"cn={resource.CN}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                    //Thanks to Muscicapa Striata for these settings at
                    //https://stackoverflow.com/questions/42786986/how-to-create-a-valid-self-signed-x509certificate2-programmatically-not-loadin
                    request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));
                    request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));

                    //Create the cert
                    var password = stringGenerator.CreateBase64String(32);
                    byte[] certBytes;
                    using (var cert = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-2)), new DateTimeOffset(DateTime.UtcNow.AddMonths(resource.ExpirationMonths))))
                    {
                        certBytes = cert.Export(X509ContentType.Pfx, password);
                    }
                    await keyVaultManager.ImportCertificate(azureKeyVaultConfig.VaultName, resource.Name, certBytes, password);
                }
            }
        }
    }
}
