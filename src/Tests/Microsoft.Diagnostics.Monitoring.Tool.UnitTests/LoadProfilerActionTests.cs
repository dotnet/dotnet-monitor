// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class LoadProfilerActionTests
    {
        private readonly ITestOutputHelper _outputHelper;
        private const string DefaultRuleName = nameof(LoadProfilerActionTests);

        public LoadProfilerActionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task TranslateSingleStringToAny()
        {
            const string TargetPath = @"C:\My\Test\Path";
            await TestHostHelper.CreateCollectionRulesHost(
                outputHelper: _outputHelper, 
                setup: rootOptions =>
                {
                    CollectionRuleOptions options = rootOptions.CreateCollectionRule(DefaultRuleName)
                        .AddLoadProfilerAction(
                            configureOptions: actionSettings =>
                            {
                                actionSettings.Path = TargetPath;
                                actionSettings.ProfilerGuid = Guid.NewGuid();
                            })
                        .SetStartupTrigger();
                }, 
                hostCallback: host =>
                {
                    IOptionsMonitor<CollectionRuleOptions> collectionRuleOptsMonitor = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>();
                    CollectionRuleOptions ruleOptions = collectionRuleOptsMonitor.Get(DefaultRuleName);

                    LoadProfilerOptions configuredOptions = ruleOptions.Actions.FirstOrDefault()?.Settings as LoadProfilerOptions;

                    Assert.NotNull(configuredOptions);
                    Assert.False(string.IsNullOrEmpty(configuredOptions.Path));
                    Assert.Equal(TargetPath, configuredOptions.Path);
                });
        }
    }
}
