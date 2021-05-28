using System;

namespace Threax.AzureVmProvisioner
{
    enum HelpCategory
    {
        Unknown,
        Primary,
        Create,
        CreateCommon,
        Deploy
    }

    [AttributeUsage(AttributeTargets.Class)]
    class HelpInfoAttribute : Attribute
    {
        public HelpInfoAttribute(HelpCategory category, String description)
        {
            this.Category = category;
            this.Description = description;
        }

        public HelpCategory Category { get; private set; }

        public String Description { get; private set; }
    }
}
