// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules
{
    /// <summary>
    /// Analyzes actions to determine their dependencies based
    /// on token expressions in their options.
    /// </summary>
    /// <remarks>
    /// - Properties that are eligible for substitution are determined using an attribute.
    /// - The syntax is $(Actions.ActionName.ResultName). Note that only previous actions can be referenced.
    /// - A single property can reference multiple actions.
    /// - Long term, it may be worthwhile to create a graph of all the actions based on overall dependency analysis that
    ///   also determines the execution order.
    /// - Note that property substitution uses the results of the dependency analysis, rather than recalculating the tokens.
    /// Dependencies are organized as follows:
    /// Action -> (Property, OriginalValue) -> (PreviousActionReference, PreviousActionResultReference)
    /// </remarks>
    internal sealed class ActionOptionsDependencyAnalyzer
    {
        private const string ActionsReference = "Actions";
        private static readonly string ActionReferencePrefix = FormattableString.Invariant($"{ConfigurationTokenParser.SubstitutionPrefix}{ActionsReference}{ConfigurationTokenParser.Separator}");

        private readonly CollectionRuleContext _ruleContext;
        private readonly ConfigurationTokenParser _tokenParser;

        //Use action index instead of name, since it's possible for an unnamed action to have named dependencies.
#nullable disable
        private Dictionary<int, Dictionary<string, PropertyDependency>> _dependencies;
#nullable restore

        private sealed class PropertyDependency
        {
            public PropertyDependency(PropertyInfo property, string originalValue)
            {
                Property = property;
                OriginalValue = originalValue;
            }

            public PropertyInfo Property { get; }

            public string OriginalValue { get; }

            public List<ActionDependency> ActionDependencies = new();
        }

        private sealed class ActionDependency
        {
            public ActionDependency(CollectionRuleActionOptions action, string resultName, int startIndex, int endIndex)
            {
                Action = action;
                ResultName = resultName;
                StartIndex = startIndex;
                EndIndex = endIndex;
            }

            public CollectionRuleActionOptions Action { get; }

            public string ResultName { get; }

            public int StartIndex { get; }

            public int EndIndex { get; }

            public string GetActionResultToken() => string.Concat(ActionReferencePrefix,
                Action.Name, ConfigurationTokenParser.Separator, ResultName, ConfigurationTokenParser.SubstitutionSuffix);
        }

        public static ActionOptionsDependencyAnalyzer Create(CollectionRuleContext context)
        {
            var analyzer = new ActionOptionsDependencyAnalyzer(context, new ConfigurationTokenParser(context.Logger));
            analyzer.EnsureDependencies();
            return analyzer;
        }

        private ActionOptionsDependencyAnalyzer(CollectionRuleContext context, ConfigurationTokenParser tokenParser)
        {
            _ruleContext = context ?? throw new ArgumentNullException(nameof(context));
            _tokenParser = tokenParser ?? throw new ArgumentNullException(nameof(tokenParser));
        }

#nullable disable
        public IList<CollectionRuleActionOptions> GetActionDependencies(int actionIndex)
        {
            if (_dependencies.TryGetValue(actionIndex, out Dictionary<string, PropertyDependency> properties))
            {
                HashSet<string> actionNames = new(StringComparer.Ordinal);
                List<CollectionRuleActionOptions> actionOptions = new();

                foreach (ActionDependency dependency in properties.SelectMany(p => p.Value.ActionDependencies))
                {
                    if (actionNames.Add(dependency.Action.Name))
                    {
                        actionOptions.Add(dependency.Action);
                    }
                }

                return actionOptions;
            }
            return Array.Empty<CollectionRuleActionOptions>();
        }
#nullable restore

        public object? SubstituteOptionValues(IDictionary<string, CollectionRuleActionResult> actionResults, int actionIndex, object? settings)
        {
            //Attempt to substitute context properties.
            object? originalSettings = settings;

            if (_dependencies.TryGetValue(actionIndex, out Dictionary<string, PropertyDependency>? properties) && (properties.Count > 0))
            {
                if (!_tokenParser.TryCloneSettings(originalSettings, ref settings))
                {
                    return settings;
                }

#nullable disable
                foreach ((_, PropertyDependency property) in properties)
                {
                    StringBuilder builder = property.ActionDependencies.Any() ? new() : null;
                    int offset = 0;
                    foreach (ActionDependency actionDependency in property.ActionDependencies)
                    {
                        if (!actionResults.TryGetValue(actionDependency.Action.Name, out CollectionRuleActionResult results))
                        {
                            _ruleContext.Logger.InvalidActionResultReference(actionDependency.GetActionResultToken());
                            continue;
                        }
                        if (!results.OutputValues.TryGetValue(actionDependency.ResultName, out string result))
                        {
                            _ruleContext.Logger.InvalidActionResultReference(actionDependency.GetActionResultToken());
                            continue;
                        }
                        builder.Append(property.OriginalValue, offset, actionDependency.StartIndex - offset);
                        builder.Append(result);
                        offset = actionDependency.EndIndex + 1;
                    }
                    if (builder != null)
                    {
                        //It's possible there are trailing values after the last dependency or we simply couldn't process any tokens.
                        if (offset < property.OriginalValue.Length)
                        {
                            builder.Append(property.OriginalValue, offset, property.OriginalValue.Length - offset);
                        }

                        property.Property.SetValue(settings, builder.ToString());
                    }
                }
#nullable restore
            }
            string? commandLine = _ruleContext.EndpointInfo.CommandLine;

            settings = _tokenParser.SubstituteOptionValues(settings, new TokenContext
            {
                CloneOnSubstitution = ReferenceEquals(originalSettings, settings),
                RuntimeId = _ruleContext.EndpointInfo.RuntimeInstanceCookie,
                ProcessId = _ruleContext.EndpointInfo.ProcessId,
                CommandLine = commandLine ?? string.Empty,
                ProcessName = _ruleContext.ProcessInfo.ProcessName ?? string.Empty,
                MonitorHostName = _ruleContext.HostInfo.HostName,
                Timestamp = _ruleContext.HostInfo.TimeProvider.GetUtcNow(),
            });

            return settings;
        }

#nullable disable
        private void EnsureDependencies()
        {
            if (_dependencies == null)
            {
                _dependencies = new Dictionary<int, Dictionary<string, PropertyDependency>>(_ruleContext.Options.Actions.Count);
                for (int i = 0; i < _ruleContext.Options.Actions.Count; i++)
                {
                    CollectionRuleActionOptions options = _ruleContext.Options.Actions[i];
                    EnsureDependencies(options, i);
                }
            }
        }
#nullable restore

        private void EnsureDependencies(CollectionRuleActionOptions options, int actionIndex)
        {
            foreach (PropertyInfo property in GetDependencyPropertiesFromSettings(options))
            {
                string? originalValue = (string?)property.GetValue(options.Settings);
                if (string.IsNullOrEmpty(originalValue))
                {
                    continue;
                }
                string newValue = originalValue;

                int foundIndex;
                int startIndex = 0;

                PropertyDependency? propertyDependency = null;

                while ((foundIndex = newValue.IndexOf(ActionReferencePrefix, startIndex, StringComparison.OrdinalIgnoreCase)) >= 0)
                {
                    int suffixIndex = newValue.IndexOf(ConfigurationTokenParser.SubstitutionSuffix, foundIndex, StringComparison.OrdinalIgnoreCase);
                    if (suffixIndex == -1)
                    {
                        _ruleContext.Logger.InvalidActionReferenceToken(options.Name ?? actionIndex.ToString(), property.Name);
                        break;
                    }
                    startIndex = suffixIndex;

                    string actionAndResult = newValue[(foundIndex + ActionReferencePrefix.Length)..suffixIndex];

                    if (!GetActionResultReference(actionAndResult, actionIndex, out CollectionRuleActionOptions? dependencyOptions, out string? actionResultName))
                    {
                        continue;
                    }

                    propertyDependency ??= GetOrCreateDependency(actionIndex, property, originalValue);

                    var dependency = new ActionDependency(dependencyOptions, actionResultName, foundIndex, suffixIndex);
                    propertyDependency.ActionDependencies.Add(dependency);
                }
            }
        }

        private PropertyDependency GetOrCreateDependency(int actionIndex, PropertyInfo propertyInfo, string originalValue)
        {
            if (!_dependencies.TryGetValue(actionIndex, out Dictionary<string, PropertyDependency>? properties))
            {
                properties = new Dictionary<string, PropertyDependency>(StringComparer.Ordinal);
                _dependencies.Add(actionIndex, properties);
            }
            if (!properties.TryGetValue(propertyInfo.Name, out PropertyDependency? dependency))
            {
                dependency = new PropertyDependency(propertyInfo, originalValue);
                properties.Add(propertyInfo.Name, dependency);
            }
            return dependency;
        }

        private bool GetActionResultReference(string actionReference, int actionIndex,
            [NotNullWhen(true)] out CollectionRuleActionOptions? action, [NotNullWhen(true)] out string? actionResultName)
        {
            action = null;
            actionResultName = null;

            string[] parts = actionReference.Split(ConfigurationTokenParser.Separator);
            if (parts.Length != 2)
            {
                _ruleContext.Logger.InvalidActionReference(actionReference);
                return false;
            }

            string name = parts[0];
            if (string.IsNullOrEmpty(name))
            {
                _ruleContext.Logger.InvalidActionReference(actionReference);
                return false;
            }

            //We only check previous actions for our dependencies.
            CollectionRuleActionOptions? dependencyOptions = _ruleContext.Options.Actions?.Take(actionIndex)
                .FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.Ordinal));

            if (dependencyOptions == null)
            {
                _ruleContext.Logger.InvalidActionReference(actionReference);
                return false;
            }

            string resultName = parts[1];
            if (string.IsNullOrEmpty(resultName))
            {
                _ruleContext.Logger.InvalidActionReference(actionReference);
                return false;
            }

            action = dependencyOptions;
            actionResultName = resultName;
            return true;
        }

        private static IEnumerable<PropertyInfo> GetDependencyPropertiesFromSettings(CollectionRuleActionOptions options)
        {
            return ConfigurationTokenParser.GetPropertiesFromSettings(options.Settings, p => p.GetCustomAttributes(typeof(ActionOptionsDependencyPropertyAttribute), inherit: true).Any());
        }
    }
}
