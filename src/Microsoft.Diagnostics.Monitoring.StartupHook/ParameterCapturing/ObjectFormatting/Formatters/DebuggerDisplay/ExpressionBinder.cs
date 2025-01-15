// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using static Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.ObjectFormatting.Formatters.DebuggerDisplay.DebuggerDisplayParser;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.ObjectFormatting.Formatters.DebuggerDisplay
{
    internal static class ExpressionBinder
    {
        internal delegate object? BoundEvaluator(object instanceObj);
        internal record struct ExpressionEvaluator(BoundEvaluator Evaluate, Type ReturnType);

        internal static ObjectFormatterFunc? BindParsedDebuggerDisplay(Type objType, ParsedDebuggerDisplay debuggerDisplay)
        {
            //
            // To construct an ObjectFormatterFunc from a parsed debugger display:
            // - Successfully bind all expressions in the debugger display.
            // - Get object formatters for the results of each bound expression.
            //
            // At this point the ObjectFormatterFunc simply needs to evaluate each bound expression
            // and format its results using the debugger display's format string.
            //

            if (debuggerDisplay.Expressions.Count == 0)
            {
                return (_, _) => new(debuggerDisplay.FormatString);
            }

            ExpressionEvaluator[] evaluators = new ExpressionEvaluator[debuggerDisplay.Expressions.Count];
            ObjectFormatterFunc[] evaluatorFormatters = new ObjectFormatterFunc[debuggerDisplay.Expressions.Count];
            for (int i = 0; i < debuggerDisplay.Expressions.Count; i++)
            {
                ExpressionEvaluator? evaluator = BindExpression(objType, debuggerDisplay.Expressions[i].ExpressionString.Span);
                if (evaluator == null)
                {
                    return null;
                }

                evaluators[i] = evaluator.Value;
                evaluatorFormatters[i] = ObjectFormatterFactory.GetFormatter(evaluator.Value.ReturnType, useDebuggerDisplayAttribute: false).Formatter;
            }

            return (obj, formatSpecifier) =>
            {
                string[] evaluationResults = new string[evaluators.Length];
                for (int i = 0; i < evaluators.Length; i++)
                {
                    object? evaluationResult = evaluators[i].Evaluate(obj);
                    if (evaluationResult == null)
                    {
                        evaluationResults[i] = ObjectFormatter.Tokens.Null;
                        continue;
                    }

                    evaluationResults[i] = ObjectFormatter.FormatObject(
                        evaluatorFormatters[i],
                        evaluationResult,
                        debuggerDisplay.Expressions[i].FormatSpecifier).FormattedValue;
                }

                return new(ObjectFormatter.WrapValue(string.Format(debuggerDisplay.FormatString, evaluationResults)));
            };
        }

        internal static ExpressionEvaluator? BindExpression(Type objType, ReadOnlySpan<char> expression)
        {
            //
            // An expression consists of one or more chained evaluators e.g. "a.b.c.d()", where the chain is [ "a", "b", "c", "d()"].
            // Each segment of the chain must be bound individually and the segments composed into a single evaluator.
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

                evaluatorChain.Add(evaluator.Value);
                chainedContextType = evaluator.Value.ReturnType;
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
            // - A field.
            // - A method without parameters.
            //

            try
            {
                if (expression.EndsWith("()", StringComparison.Ordinal))
                {
                    return BindMethod(objType, expression[..^2].ToString());
                }

                string expressionStr = expression.ToString();

                return BindProperty(objType, expressionStr) ?? BindField(objType, expressionStr);
            }
            catch
            {
            }

            return null;
        }

        private static ExpressionEvaluator? BindMethod(Type objType, string methodName)
        {
            MethodInfo? method = objType.GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static,
                Array.Empty<Type>());

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

        private static ExpressionEvaluator? BindProperty(Type objType, string propertyName)
        {
            //
            // Use the property's Get method instead of directly using ".GetValue(..)"
            // so that we can identify if it's static or not.
            //
            MethodInfo? getterMethod = objType.GetProperty(propertyName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
                )?.GetGetMethod(nonPublic: true);

            if (getterMethod == null)
            {
                return null;
            }

            return new ExpressionEvaluator(
                getterMethod.HasImplicitThis()
                    ? (obj) => getterMethod.Invoke(obj, parameters: null)
                    : (_) => getterMethod.Invoke(obj: null, parameters: null),
                getterMethod.ReturnType);
        }

        private static ExpressionEvaluator? BindField(Type objType, string fieldName)
        {
            FieldInfo? field = objType.GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            if (field == null)
            {
                return null;
            }

            return new ExpressionEvaluator(
                field.IsStatic
                    ? (_) => field.GetValue(obj: null)
                    : field.GetValue,
                field.FieldType);
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
