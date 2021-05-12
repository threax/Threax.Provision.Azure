namespace Threax.AzureVmProvisioner.Services
{
    interface IPathHelper
    {
        string Path { get; }

        string Directory { get; }
        string UserSshFolder { get; }
        string AppUserFolder { get; }

        string GetTempProvisionPath();
    }
}