using System.Threading.Tasks;

namespace Threax.AzureVmProvisioner.Services
{
    public interface IMachineIpManager
    {
        Task<string> GetExternalIp();
    }
}