#define ENABLE_ARM_TESTS

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Threax.AspNetCore.Tests;
using Threax.ProcessHelper;
using Threax.Provision.AzCli.Managers;
using Threax.Provision.AzCli.Tests.ArmTemplates.KeyVaultTemplate;
using Xunit;
using Xunit.Abstractions;

namespace Threax.Provision.AzCli.Tests
{
    public class ArmTemplateManagerTests
    {
        const string TestRg = "threax-prov-rg";
        const string TestOverrideRg = "threax-prov-override-rg";
        const string TestLoc = "Central US";

        Mockup mockup = new Mockup();

        public ArmTemplateManagerTests(ITestOutputHelper output)
        {
            mockup.AddCommonMockups(output);
        }

        [Fact
#if !ENABLE_ARM_TESTS
         (Skip = "Arm Tests Disabled")
#endif
        ]
        public void DeployRg()
        {
            var manager = mockup.Get<IArmTemplateManager>();
            manager.SubscriptionDeployment(
                TestLoc, 
                "ArmTemplates/ResourceGroupTemplate/template.json", 
                "ArmTemplates/ResourceGroupTemplate/parameters.json");
        }

        [Fact
#if !ENABLE_ARM_TESTS
         (Skip = "Arm Tests Disabled")
#endif
        ]
        public void DeployRgOverride()
        {
            var manager = mockup.Get<IArmTemplateManager>();
            manager.SubscriptionDeployment(
                TestLoc, 
                "ArmTemplates/ResourceGroupTemplate/template.json",
                "ArmTemplates/ResourceGroupTemplate/parameters.json", 
                new { rgName = TestOverrideRg });
        }

        [Fact
#if !ENABLE_ARM_TESTS
         (Skip = "Arm Tests Disabled")
#endif
        ]
        public void DeployKeyVault()
        {
            var manager = mockup.Get<IArmTemplateManager>();
            manager.ResourceGroupDeployment(TestRg, 
                "ArmTemplates/KeyVaultTemplate/template.json", 
                "ArmTemplates/KeyVaultTemplate/parameters.json",
                new { tenant = Config.Tenant });
        }

        [Fact
#if !ENABLE_ARM_TESTS
         (Skip = "Arm Tests Disabled")
#endif
        ]
        public void DeployKeyVaultOverride()
        {
            var manager = mockup.Get<IArmTemplateManager>();
            manager.ResourceGroupDeployment(TestOverrideRg,
                "ArmTemplates/KeyVaultTemplate/template.json",
                "ArmTemplates/KeyVaultTemplate/parameters.json", 
                new { name = "threax-prov-override-kv", tenant = Config.Tenant });
        }

        [Fact
#if !ENABLE_ARM_TESTS
         (Skip = "Arm Tests Disabled")
#endif
        ]
        public void DeployKeyVaultObj()
        {
            var manager = mockup.Get<IArmTemplateManager>();
            manager.ResourceGroupDeployment(TestRg, new ArmKeyVault("threax-kv-obj", TestLoc, Config.Tenant));
        }
    }
}
