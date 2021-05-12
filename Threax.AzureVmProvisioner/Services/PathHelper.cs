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
            Path = baseFile = System.IO.Path.GetFullPath(baseFile);
            this.Directory = System.IO.Path.GetDirectoryName(baseFile);
        }

        public String Path { get; }

        public String Directory { get; }

        public String AppUserFolder => System.IO.Path.Combine(GetUserHomePath(), ".threaxprovision");

        public String UserSshFolder => System.IO.Path.Combine(GetUserHomePath(), ".ssh");

        /// <summary>
        /// Get a temporary file path in the user's ~/.threaxprovision/temp folder.
        /// </summary>
        /// <returns></returns>
        public String GetTempProvisionPath()
        {
            var tempFolder = System.IO.Path.Combine(AppUserFolder, "temp");
            if (!System.IO.Directory.Exists(tempFolder))
            {
                System.IO.Directory.CreateDirectory(tempFolder);
            }

            return System.IO.Path.Combine(tempFolder, Guid.NewGuid().ToString());
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
