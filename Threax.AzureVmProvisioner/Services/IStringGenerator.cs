namespace Threax.AzureVmProvisioner.Services
{
    interface IStringGenerator
    {
        string CreateBase64String(int numBytes);
        void Dispose();
    }
}