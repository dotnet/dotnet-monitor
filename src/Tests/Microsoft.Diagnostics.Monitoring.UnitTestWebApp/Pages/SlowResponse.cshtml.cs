// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc.RazorPages;
//using Microsoft.Diagnostics.Monitoring.UnitTestApp;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestWebApp.Pages
{
    public class SlowResponseModel : PageModel
    {
        private readonly ILogger<SlowResponseModel> _logger;

        public SlowResponseModel(ILogger<SlowResponseModel> logger)
        {
            _logger = logger;
        }

        public async Task OnGet()
        {
            await Task.Delay(1000); // Wait one second to return
        }
    }
}
