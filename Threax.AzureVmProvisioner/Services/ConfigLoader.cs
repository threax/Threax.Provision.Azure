using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Threax.AzureVmProvisioner.Services
{
    interface IConfigLoader
    {
        /// <summary>
        /// Load the config. You will get a fresh JObject each time this is called, so the result is safe to modify.
        /// </summary>
        /// <returns></returns>
        JObject LoadConfig(String path);
    }

    class ConfigLoader : IConfigLoader
    {
        private const String IncludeKey = "$include";

        public ConfigLoader()
        {
            
        }

        public JObject LoadConfig(String path)
        {
            return LoadWithInclude(path);
        }

        private static JObject LoadWithInclude(String path)
        {
            var fullPath = Path.GetFullPath(path);
            var rootFolder = Path.GetDirectoryName(fullPath);
            //Load the file to see if there are any includes
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Cannot find include file {path}.\nFullPath: {fullPath}", fullPath);
            }

            JObject jObj;
            using (var reader = new StreamReader(File.OpenRead(fullPath)))
            {
                using (var jsonReader = new JsonTextReader(reader))
                {
                    jObj = JToken.ReadFrom(jsonReader) as JObject;
                }
            }

            var include = jObj[IncludeKey];
            if(include != null)
            {
                var includePath = Path.Combine(rootFolder, include.Value<String>());
                var parent = LoadWithInclude(includePath);
                parent.Merge(jObj, new JsonMergeSettings()
                {
                    MergeArrayHandling = MergeArrayHandling.Concat,
                    MergeNullValueHandling = MergeNullValueHandling.Merge,
                    PropertyNameComparison = StringComparison.Ordinal
                });
                jObj = parent;
            }

            return jObj;
        }
    }
}
