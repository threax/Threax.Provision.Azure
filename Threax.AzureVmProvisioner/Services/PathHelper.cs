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
    }
}
