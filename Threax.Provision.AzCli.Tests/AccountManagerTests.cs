using Microsoft.Extensions.DependencyInjection;
using System;
using Threax.AspNetCore.Tests;
using Xunit;
using Threax.Provision.AzCli.Managers;

namespace Threax.Provision.AzCli.Tests
{
    public class AccountManagerTests
    {
        Mockup mockup = new Mockup();

        public AccountManagerTests()
        {
            mockup.MockServiceCollection.AddLogging();
            mockup.MockServiceCollection.AddThreaxProvisionAzCli();
        }

        [Fact]
        public void SetSubscription()
        {
            var accountManager = mockup.Get<IAccountManager>();
            accountManager.SetSubscription("Visual Studio Professional");
        }

        [Fact]
        public void SetSubscriptionFail()
        {
            var accountManager = mockup.Get<IAccountManager>();
            Assert.Throws<InvalidOperationException>(() => accountManager.SetSubscription("If someone names their subscription this and tries to run this test they are stupid."));
        }

        [Fact]
        public void Show()
        {
            var accountManager = mockup.Get<IAccountManager>();
            accountManager.SetSubscription("Visual Studio Professional");
            var result = accountManager.Show();
            Assert.Equal("Visual Studio Professional", result.name);
        }
    }
}
