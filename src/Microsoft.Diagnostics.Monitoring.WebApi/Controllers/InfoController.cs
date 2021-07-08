// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Controllers
{
    [Route("")]
    [ApiController]
#if NETCOREAPP3_1_OR_GREATER
    [ProducesErrorResponseType(typeof(ValidationProblemDetails))]
#endif
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public class InfoController : ControllerBase
    {
        private readonly ILogger<InfoController> _logger;

        public InfoController(ILogger<InfoController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets the Dotnet Monitor Version (other configuration information may be added in the future).
        /// </summary>
        [HttpGet("info", Name = nameof(GetInfo))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(Models.DotnetMonitorInfo), StatusCodes.Status200OK)]
        public ActionResult<Models.DotnetMonitorInfo> GetInfo()
        {
            return this.InvokeService(() =>
            {
                string version = GetDotnetMonitorVersion();

                Models.DotnetMonitorInfo dotnetMonitorInfo = new Models.DotnetMonitorInfo()
                {
                    Version = version
                };

                _logger.WrittenToHttpStream();
                return new ActionResult<Models.DotnetMonitorInfo>(dotnetMonitorInfo);
            }, _logger);
        }

        private static string GetDotnetMonitorVersion()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

            var assemblyVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            if (assemblyVersionAttribute is null)
            {
                return assembly.GetName().Version.ToString();
            }
            else
            {
                return assemblyVersionAttribute.InformationalVersion;
            }
        }
    }
}
