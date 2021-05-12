namespace Threax.AzureVmProvisioner.Services
{
    interface IPathHelper
    {
        string Path { get; }

        string Directory { get; }
    }
}