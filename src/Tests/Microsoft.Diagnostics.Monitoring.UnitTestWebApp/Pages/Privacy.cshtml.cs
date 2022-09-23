// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.UnitTestApp;
//using Microsoft.Diagnostics.Monitoring.UnitTestApp;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestWebApp.Pages
{
    public class PrivacyModel : PageModel
    {
        private readonly ILogger<PrivacyModel> _logger;

        public PrivacyModel(ILogger<PrivacyModel> logger)
        {
            _logger = logger;

            _logger.LogWarning("IN PRIVACY");
        }

        public void OnGet()
        {
            _logger.LogWarning("IN PRIVACY ONGET");

            //await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue, _logger);

            //_logger.LogWarning("After waiting for command");

            //Process p = Process.GetCurrentProcess();
            //p.Kill();

            /*
            _logger.LogWarning("IN PRIVACY ONGET");

            long sum = 0; // just burning through the loop
            for (int index = 0; index < 1000000000; ++index)
            {
                ++sum;
            }

            return RedirectToPage("./Error");*/
        }
    }
}
