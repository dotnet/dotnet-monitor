// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Microsoft.Diagnostics.Monitoring.WebApi.Controllers
{
    public class MinimalControllerBase
    {
        internal HttpContext HttpContext { get; }

        internal HttpRequest Request => HttpContext.Request;

        public MinimalControllerBase(HttpContext httpContext)
        {
            HttpContext = httpContext;
        }
    }

    internal static class ControllerExtensions
    {
        public static ProblemHttpResult FeatureNotEnabled(this MinimalControllerBase controller, string featureName)
        {
            return TypedResults.Problem(new ProblemDetails()
            {
                Detail = string.Format(Strings.Message_FeatureNotEnabled, featureName),
                Status = StatusCodes.Status400BadRequest
            });
        }

        public static IResult NotAcceptable(this MinimalControllerBase controller)
        {
            return TypedResults.StatusCode((int)HttpStatusCode.NotAcceptable);
        }

        public static IResult InvokeService<T>(this MinimalControllerBase controller, Func<T> serviceCall, ILogger logger)
            where T : IResult
        {
            //Convert from IResult to Task<IResult>
            //and safely convert back.
            return controller.InvokeService(() => Task.FromResult(serviceCall()), logger).Result;
        }

        public static async Task<IResult> InvokeService<T>(this MinimalControllerBase controller, Func<Task<T>> serviceCall, ILogger logger)
            where T : IResult
        {
            CancellationToken token = controller.HttpContext.RequestAborted;
            // Exceptions are logged in the "when" clause in order to preview the exception
            // from the point of where it was thrown. This allows capturing of the log scopes
            // that were active when the exception was thrown. Waiting to log during the exception
            // handler will miss any scopes that were added during invocation of serviceCall.
            try
            {
                return await serviceCall();
            }
            catch (ArgumentException e) when (LogError(logger, e))
            {
                return controller.Problem(e);
            }
            catch (DiagnosticsClientException e) when (LogError(logger, e))
            {
                return controller.Problem(e);
            }
            catch (InvalidOperationException e) when (LogError(logger, e))
            {
                return controller.Problem(e);
            }
            catch (OperationCanceledException e) when (token.IsCancellationRequested && LogInformation(logger, e))
            {
                return controller.Problem(e);
            }
            catch (OperationCanceledException e) when (LogError(logger, e))
            {
                return controller.Problem(e);
            }
            catch (TooManyRequestsException e) when (LogError(logger, e))
            {
                return controller.Problem(e, statusCode: StatusCodes.Status429TooManyRequests);
            }
            catch (MonitoringException e) when (LogError(logger, e))
            {
                return controller.Problem(e);
            }
            catch (ValidationException e) when (LogError(logger, e))
            {
                return controller.Problem(e);
            }
        }

        public static ProblemHttpResult Problem(this MinimalControllerBase controller, Exception ex, int statusCode = StatusCodes.Status400BadRequest)
        {
            return TypedResults.Problem(ex.ToProblemDetails(statusCode));
        }

        private static bool LogError(ILogger logger, Exception ex)
        {
            logger.RequestFailed(ex);
            return true;
        }

        private static bool LogInformation(ILogger logger, Exception ex)
        {
            logger.RequestCanceled();
            return true;
        }
    }
}
