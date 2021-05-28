using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threax.AzureVmProvisioner
{
    enum HelpCategory
    {
        Unknown,
        Primary,
        Create,
        CreateCommon,
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
