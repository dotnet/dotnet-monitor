// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public sealed class ExecutionResult<T>
    {
        private static readonly ExecutionResult<T> _empty = new ExecutionResult<T>();

        public Exception? Exception { get; private set; }
        public T? Result { get; private set; }
        public ProblemDetails? ProblemDetails { get; private set; }

        private ExecutionResult() { }

        public static ExecutionResult<T> Succeeded(T result) => new ExecutionResult<T> { Result = result };

        public static ExecutionResult<T> Failed(Exception exception) =>
            new ExecutionResult<T> { Exception = exception, ProblemDetails = exception.ToProblemDetails((int)HttpStatusCode.BadRequest) };

        public static ExecutionResult<T> Empty() => _empty;
    }

    internal static class ExecutionHelper
    {
        public static async Task<ExecutionResult<T>> InvokeAsync<T>(Func<CancellationToken, Task<ExecutionResult<T>>> action, ILogger logger,
            CancellationToken token)
        {
            // Exceptions are logged in the "when" clause in order to preview the exception
            // from the point of where it was thrown. This allows capturing of the log scopes
            // that were active when the exception was thrown. Waiting to log during the exception
            // handler will miss any scopes that were added during invocation of action.
            try
            {
                return await action(token);
            }
            catch (ArgumentException ex) when (LogError(logger, ex))
            {
                return ExecutionResult<T>.Failed(ex);
            }
            catch (DiagnosticsClientException ex) when (LogError(logger, ex))
            {
                return ExecutionResult<T>.Failed(ex);
            }
            catch (InvalidOperationException ex) when (LogError(logger, ex))
            {
                return ExecutionResult<T>.Failed(ex);
            }
            catch (OperationCanceledException ex) when (token.IsCancellationRequested && LogInformation(logger, ex))
            {
                return ExecutionResult<T>.Failed(ex);
            }
            catch (OperationCanceledException ex) when (LogError(logger, ex))
            {
                return ExecutionResult<T>.Failed(ex);
            }
            catch (MonitoringException ex) when (LogError(logger, ex))
            {
                return ExecutionResult<T>.Failed(ex);
            }
            catch (ValidationException ex) when (LogError(logger, ex))
            {
                return ExecutionResult<T>.Failed(ex);
            }
            catch (UnauthorizedAccessException ex) when (LogError(logger, ex))
            {
                return ExecutionResult<T>.Failed(ex);
            }
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

    internal static class ActionContextExtensions
    {
        public static Task ProblemAsync(this ActionContext context, BadRequestObjectResult result)
        {
            if (context.HttpContext.Features.Get<IHttpResponseFeature>()?.HasStarted == true)
            {
                // If already started writing response, do not rewrite
                // as this will throw an InvalidOperationException.
                return Task.CompletedTask;
            }
            else
            {
                // Need to manually add this here because this ObjectResult is not processed by the filter pipeline,
                // which would look up the applicable content types and apply them before executing the result.
                result.ContentTypes.Add(ContentTypes.ApplicationProblemJson);
                return result.ExecuteResultAsync(context);
            }
        }

        public static async Task InvokeAsync(this ActionContext context, Func<CancellationToken, Task> action, ILogger logger)
        {
            //We want to handle two scenarios:
            //1) These functions are part of an ExecuteResultAsync implementation, in which case
            //they don't need to return anything. (The delegate below handles this case).
            //2) They are part of deferred processing and return a result.
            Func<CancellationToken, Task<ExecutionResult<object>>> innerAction = async (CancellationToken token) =>
            {
                await action(token);
                return ExecutionResult<object>.Empty();
            };

            ExecutionResult<object> result = await ExecutionHelper.InvokeAsync(innerAction, logger, context.HttpContext.RequestAborted);
            if (result.ProblemDetails != null)
            {
                await context.ProblemAsync(new BadRequestObjectResult(result.ProblemDetails));
            }
        }
    }
}
