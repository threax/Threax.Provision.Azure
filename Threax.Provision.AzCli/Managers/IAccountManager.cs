using Threax.Provision.AzCli.Models;

namespace Threax.Provision.AzCli.Managers
{
    public interface IAccountManager
    {
        void SetSubscription(string nameOrId);
        AccountShowOutput? Show();
    }
}