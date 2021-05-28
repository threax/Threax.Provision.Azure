using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Threax.ProcessHelper;

namespace Threax.AzureVmProvisioner.Services
{
    public interface IImageManager
    {
        string FindLatestImage(string image, string baseTag, string currentTag);
    }

    public class ImageManager : IImageManager
    {
        private readonly IShellRunner shellRunner;

        public ImageManager(IShellRunner shellRunner)
        {
            this.shellRunner = shellRunner;
        }

        public string FindLatestImage(string image, string baseTag, string currentTag)
        {
            //Get the tags from docker
            var args = $"";
            var searchTag = $"{image}:{currentTag}";
            var format = "{{json .RepoTags}}";
            var tags = shellRunner.RunProcess<List<String>>($"docker inspect --format={format} {searchTag}");

            //Remove any tags that weren't set by this software
            tags.Remove($"{image}:{currentTag}");
            var tagFilter = $"{image}:{baseTag}";
            tags = tags.Where(i => i.StartsWith(tagFilter)).ToList();
            tags.Sort(); //Docker seems to store these in order, but sort them by their names, the tags are date based and the latest will always be last

            var latestDateTag = tags.LastOrDefault();

            if (latestDateTag == null)
            {
                throw new InvalidOperationException($"Cannot find a tag in the format '{tagFilter}' on image '{image}'.");
            }

            return latestDateTag;
        }
    }
}
