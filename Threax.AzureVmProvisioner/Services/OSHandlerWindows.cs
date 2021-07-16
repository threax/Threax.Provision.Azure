using System;
using System.Collections.Generic;
using System.Text;

namespace Threax.AzureVmProvisioner.Services
{
    public class OSHandlerWindows : IOSHandler
    {
        public string GetUser()
        {
            return "";
        }

        public string GetGroup()
        {
            return "";
        }
        public string CreateDockerPath(string path)
        {
            return "/" + path.Replace("\\", "/").Remove(1, 1);
        }

        public void SetPermissions(string path, string user, string group)
        {

        }

        public void MakeExecutable(string path)
        {

        }
    }
}
