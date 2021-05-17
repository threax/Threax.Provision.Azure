using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Threax.AspNetCore.Tests;
using Threax.ProcessHelper;
using Xunit.Abstractions;

namespace Threax.Provision.AzCli.Tests
{
    static class MockupExtensions
    {
        public static Mockup AddCommonMockups(this Mockup mockup, ITestOutputHelper output)
        {
            mockup.MockServiceCollection.AddThreaxProcessHelper(o =>
            {
                o.IncludeLogOutput = false;
                o.DecorateProcessRunner = r => new SpyProcessRunner(r)
                {
                    Events = new ProcessEvents()
                    {
                        ErrorDataReceived = (o, e) => { if (e.DataReceivedEventArgs.Data != null) output.WriteLine(e.DataReceivedEventArgs.Data); },
                        OutputDataReceived = (o, e) => { if (e.DataReceivedEventArgs.Data != null) output.WriteLine(e.DataReceivedEventArgs.Data); },
                    }
                };
            });
            mockup.MockServiceCollection.AddThreaxPwshShellRunner();
            mockup.MockServiceCollection.AddThreaxProvisionAzCli();

            return mockup;
        }
    }
}
