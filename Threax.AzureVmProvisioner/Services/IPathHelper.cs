namespace Threax.AzureVmProvisioner.Services
{
    interface IPathHelper
    {
        string ConfigPath { get; }
        string ConfigDirectory { get; }
        string UserSshFolder { get; }
        string AppUserFolder { get; }

        string GetTempProvisionPath();
    }
}