// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Monitoring.UnitTestWebApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;

            _logger.LogWarning("IN INDEX MODEL");

            //OnGet();
            //RedirectToPage("./Privacy");
        }

        
        public void OnGet()
        {
            _logger.LogWarning("IN ONGET");

            //return RedirectToPage("./Privacy");
        }
    }
}
