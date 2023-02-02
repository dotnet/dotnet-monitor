// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;


namespace Microsoft.Diagnostics.Monitoring.WebApi.Controllers
{
    [Route("/swagger/v1/")]
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SwaggerFilesController : ControllerBase
    {
        private readonly ILogger<SwaggerFilesController> _logger;

        public SwaggerFilesController(ILogger<SwaggerFilesController> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get the swagger files embedded into the assembly
        /// </summary>
        [HttpGet("{filename}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public FileStreamResult GetFile(string filename)
        {
            if (filename != "swagger.json") { throw new Exception(string.Format(Strings.ErrorMessage_ResourceNotFound, filename)); }
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Microsoft.Diagnostics.Monitoring.WebApi.swagger.v1." + filename);
            string mimetype = "text/json";

            return new FileStreamResult(stream, mimetype)
            {
                FileDownloadName = filename
            };
        }
    }
}
