using System.Threading.Tasks;

namespace Threax.AzureVmProvisioner.Services
{
    interface IRunInfoLogger
    {
        Task Log();
    }
}