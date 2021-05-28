using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threax.Azure.Abstractions;
using Threax.AzureVmProvisioner.Resources;
using Threax.DeployConfig;
using Threax.DockerBuildConfig;

namespace Threax.AzureVmProvisioner
{
    class Configuration
    {
        private String path;

        public Configuration(String path)
        {
            this.path = path;
            this.Build = new BuildConfig(path);
            this.Deploy = new DeploymentConfig(path);
        }

        public String GetConfigPath()
        {
            return path;
        }

        public String GetConfigDirectory()
        {
            return Path.GetDirectoryName(path);
        }

        public EnvironmentConfiguration Environment { get; set; } = new EnvironmentConfiguration();

        public ResourceConfiguration Resources { get; set; } = new ResourceConfiguration();

        public AzureKeyVaultConfig KeyVault { get; set; } = new AzureKeyVaultConfig();

        public AzureStorageConfig Storage { get; set; } = new AzureStorageConfig();
        
        public BuildConfig Build { get; set; }

        public DeploymentConfig Deploy { get; set; }
}
}
