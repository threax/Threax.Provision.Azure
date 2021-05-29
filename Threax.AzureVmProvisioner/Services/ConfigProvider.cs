using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threax.AzureVmProvisioner.Services
{
    interface IConfigProvider
    {
        IEnumerable<IEnumerable<Configuration>> LoadConfigPhases(String rootDir, String includeGlob);
    }

    class ConfigProvider : IConfigProvider
    {
        private readonly IConfigLoader configLoader;

        public ConfigProvider(IConfigLoader configLoader)
        {
            this.configLoader = configLoader;
        }

        public IEnumerable<IEnumerable<Configuration>> LoadConfigPhases(String rootDir, String includeGlob)
        {
            rootDir = Path.GetFullPath(rootDir);

            var matcher = new Matcher();
            matcher.AddInclude(includeGlob);
            var results = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(rootDir)));

            //Load all configs and find named configs
            var noNameConfigs = new List<Configuration>();
            var namedConfigs = new Dictionary<String, Configuration>();
            foreach (var result in results.Files)
            {
                var fullPath = Path.Combine(rootDir, result.Path);

                var config = new Configuration(fullPath);
                var configJobj = configLoader.LoadConfig(fullPath);
                if (configJobj != null)
                {
                    using var reader = configJobj.CreateReader();
                    Newtonsoft.Json.JsonSerializer.CreateDefault().Populate(reader, config);
                }

                var name = config.Resources?.Name;
                if (name != null)
                {
                    namedConfigs.Add(name, config);
                }
                else
                {
                    noNameConfigs.Add(config);
                }
            }

            //Find all configs that have a loaded dependency
            var independentConfigs = new List<Configuration>();
            var dependentConfigs = new Dictionary<String, List<Configuration>>();
            foreach(var config in noNameConfigs.Concat(namedConfigs.Values))
            {
                //If this config has a dependency and the dependency is loaded defer it to later.
                var dependency = config.Resources?.DependsOn;
                if (dependency != null && namedConfigs.ContainsKey(dependency))
                {
                    if(!dependentConfigs.TryGetValue(dependency, out var configs))
                    {
                        configs = new List<Configuration>();
                        dependentConfigs.Add(dependency, configs);
                    }
                    configs.Add(config);
                }
                else
                {
                    independentConfigs.Add(config);
                }
            }

            //First return all the independent configs
            yield return independentConfigs;

            //Next walk through all the processed configs, find the dependencies and return those
            //Continue in this manner until the dependent configs have all been processed
            var processedConfigs = independentConfigs;
            while (dependentConfigs.Count > 0)
            {
                var nextConfigReturnValue = new List<Configuration>();
                foreach (var config in processedConfigs)
                {
                    var name = config.Resources.Name;
                    if (name != null && dependentConfigs.TryGetValue(name, out var items))
                    {
                        nextConfigReturnValue.AddRange(items);
                        dependentConfigs.Remove(name);
                    }
                }

                yield return nextConfigReturnValue;
                processedConfigs = nextConfigReturnValue;
            }
        }
    }
}
