﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using static Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.ObjectFormatting.Formatters.DebuggerDisplay.DebuggerDisplayParser;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.ObjectFormatting.Formatters.DebuggerDisplay
{
    internal static class ExpressionBinder
    {
        internal delegate object? BoundEvaluator(object instanceObj);
        internal record ExpressionEvaluator(BoundEvaluator Evaluate, Type ReturnType);

        internal static ObjectFormatterFunc? BindParsedDebuggerDisplay(Type objType, ParsedDebuggerDisplay debuggerDisplay)
        {
            //
            // To construct an ObjectFormatterFunc from a parsed debugger display:
            // - Succesfully bind all expressions in the debugger display.
            // - Get object formatters for the results of each bound expression.
            //
            // At this point the ObjectFormatterFunc simply needs to evaluate each bound expression
            // and format its results using the debugger display's format string.
            //

            if (debuggerDisplay.Expressions.Length == 0)
            {
                return (_, _) => debuggerDisplay.FormatString;
            }

            ExpressionEvaluator[] boundExpressions = new ExpressionEvaluator[debuggerDisplay.Expressions.Length];
            ObjectFormatterFunc[] evaluatorFormatters = new ObjectFormatterFunc[debuggerDisplay.Expressions.Length];
            for (int i = 0; i < debuggerDisplay.Expressions.Length; i++)
            {
                ExpressionEvaluator? evaluator = BindExpression(objType, debuggerDisplay.Expressions[i].ExpressionString.Span);
                if (evaluator == null)
                {
                    return null;
                }

                boundExpressions[i] = evaluator;
                evaluatorFormatters[i] = ObjectFormatterFactory.GetFormatter(evaluator.ReturnType, useDebuggerDisplayAttribute: false).Formatter;
            }

            return (obj, formatSpecifier) =>
            {
                string[] evaluationResults = new string[boundExpressions.Length];
                for (int i = 0; i < boundExpressions.Length; i++)
                {
                    object? evaluationResult = boundExpressions[i].Evaluate(obj);
                    if (evaluationResult == null)
                    {
                        evaluationResults[i] = ObjectFormatter.Tokens.Null;
                        continue;
                    }

                    evaluationResults[i] = ObjectFormatter.FormatObject(
                        evaluatorFormatters[i],
                        evaluationResult,
                        debuggerDisplay.Expressions[i].FormatSpecifier);
                }

                return string.Format(debuggerDisplay.FormatString, evaluationResults);
            };
        }

        internal static ExpressionEvaluator? BindExpression(Type objType, ReadOnlySpan<char> expression)
        {
            //
            // An expression consists of one or more chained evaluators e.g. "a.b.c.d()", where the chain is [ "a", "b", "c", "d()"].
            // Each segment of the chain must be bound indivually and the segments composed into a single evaluator.
            //
            List<ExpressionEvaluator> evaluatorChain = new();

            //
            // Keep track of the current chain context type, which represents the type of the object
            // that the current segment of the chain binds against.
            //
            Type chainedContextType = objType;

            // Read the expression chain one segment at a time until the entire chain has been processed.
            ReadOnlySpan<char> unboundExpression = expression;
            while (!unboundExpression.IsEmpty)
            {
                int chainedExpressionIndex = unboundExpression.IndexOf('.');
                ExpressionEvaluator? evaluator;
                if (chainedExpressionIndex != -1)
                {
                    evaluator = BindSingleExpression(unboundExpression[..chainedExpressionIndex], chainedContextType);
                    unboundExpression = unboundExpression[(chainedExpressionIndex + 1)..];
                }
                else
                {
                    evaluator = BindSingleExpression(unboundExpression, chainedContextType);
                    unboundExpression = ReadOnlySpan<char>.Empty;
                }

                if (evaluator == null)
                {
                    return null;
                }

                evaluatorChain.Add(evaluator);
                chainedContextType = evaluator.ReturnType;
            }

            return CollapseChainedEvaluators(evaluatorChain);
        }

        private static ExpressionEvaluator? BindSingleExpression(ReadOnlySpan<char> expression, Type objType)
        {
            //
            // NOTE: Complex expressions are not supported (e.g. "Count + 1").
            // This includes expressions with additional parenthesis that are otherwise supported, e.g. "(Count)".
            //
            // The following expressions are supported:
            // - A property.
            // - A method without parameters.
            //

            try
            {
                //
                // Check if the expression is a parameter-less method. If not, try to bind it as a property.
                // This means any complex expressions, or methods with parameters, will try to bind as a property
                // and fail.
                //
                MethodInfo? method = null;
                if (expression.EndsWith("()", StringComparison.Ordinal))
                {
                    method = objType.GetMethod(
                        expression[..^2].ToString(),
                        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static,
                        Array.Empty<Type>());
                }
                else
                {
                    PropertyInfo? property = objType.GetProperty(expression.ToString());
                    if (property == null)
                    {
                        return null;
                    }

                    //
                    // Directly use the property's Get method instead of directly using ".GetValue(..)"
                    // so that we can identify if it's static or not.
                    //
                    method = property.GetGetMethod(nonPublic: true);
                }

                if (method == null)
                {
                    return null;
                }

                return new ExpressionEvaluator(
                    method.HasImplicitThis()
                        ? (obj) => method.Invoke(obj, parameters: null)
                        : (_) => method.Invoke(obj: null, parameters: null),
                    method.ReturnType);
            }
            catch
            {
                return null;
            }
        }

        private static ExpressionEvaluator? CollapseChainedEvaluators(List<ExpressionEvaluator> chain)
        {
            //
            // Collapse a chain of one or more evaluators into a single evaluator
            // - The input to the chain is the implicit this of the object (if applicable).
            // - The output of the chain is the result of the last evaluator.
            //

            if (chain.Count == 0)
            {
                return null;
            }

            if (chain.Count == 1)
            {
                return chain[0];
            }

            return new ExpressionEvaluator(
                (obj) =>
                {
                    object? chainedResult = obj;
                    foreach (ExpressionEvaluator chainedEvaluator in chain)
                    {
                        if (chainedResult == null)
                        {
                            return null;
                        }

                        chainedResult = chainedEvaluator.Evaluate(chainedResult);
                    }

                    return chainedResult;
                },
                chain[^1].ReturnType);
        }
    }
}
