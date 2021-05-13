namespace Threax.AzureVmProvisioner.Services
{
    public interface IImageManager
    {
        string FindLatestImage(string image, string baseTag, string currentTag);
    }
}