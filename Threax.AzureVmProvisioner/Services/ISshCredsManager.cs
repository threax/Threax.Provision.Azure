using System.Threading.Tasks;

namespace Threax.AzureVmProvisioner.Services
{
    interface ISshCredsManager
    {
        string PrivateKeySecretName { get; }
        string PublicKeySecretName { get; }

        Task CopySshFile(string file, string dest);
        Task CopyStringToSshFile(string input, string dest);
        void Dispose();
        Task<string> LoadPublicKey();
        Task<int> RunSshCommand(string command);
        Task SaveSshKnownHostsSecret();
    }
}