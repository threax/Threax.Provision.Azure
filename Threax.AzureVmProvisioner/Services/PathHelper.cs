using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Threax.AzureVmProvisioner.Services
{
    class PathHelper : IPathHelper
    {
        public PathHelper(String baseFile)
        {
            ConfigPath = baseFile = Path.GetFullPath(baseFile);
            this.ConfigDirectory = Path.GetDirectoryName(baseFile);
        }

        public String ConfigPath { get; }

        public String ConfigDirectory { get; }

        public String AppUserFolder => Path.Combine(GetUserHomePath(), ".threaxprovision");

        public String UserSshFolder => Path.Combine(GetUserHomePath(), ".ssh");

        /// <summary>
        /// Get a temporary file path in the user's ~/.threaxprovision/temp folder.
        /// </summary>
        /// <returns></returns>
        public String GetTempProvisionPath()
        {
            var tempFolder = Path.Combine(AppUserFolder, "temp");
            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }

            return Path.Combine(tempFolder, Guid.NewGuid().ToString());
        }

        private String GetUserHomePath()
        {
            //Thanks to MiffTheFox at https://stackoverflow.com/questions/1143706/getting-the-path-of-the-home-directory-in-c

            return (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
                ? Environment.GetEnvironmentVariable("HOME")
                : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
        }
    }
}
