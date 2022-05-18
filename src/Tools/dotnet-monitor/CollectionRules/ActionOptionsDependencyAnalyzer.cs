// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
        private const string SubstitutionPrefix = "$(";
        private const string SubstitutionSuffix = ")";
        private const string Separator = ".";
        private const string ActionsReference = "Actions";
        private const string ProcessInfoReference = "ProcessInfo";
        private static readonly string ActionReferencePrefix = FormattableString.Invariant($"{SubstitutionPrefix}{ActionsReference}{Separator}");
        public static readonly string RuntimeIdReference = FormattableString.Invariant($"{SubstitutionPrefix}{ProcessInfoReference}{Separator}InstanceId{SubstitutionSuffix}");

        private readonly CollectionRuleContext _ruleContext;

        //Use action index instead of name, since it's possible for an unnamed action to have named dependencies.
        private Dictionary<int, Dictionary<string, PropertyDependency>> _dependencies;

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
                Action.Name, Separator, ResultName, SubstitutionSuffix);
        }

        public static ActionOptionsDependencyAnalyzer Create(CollectionRuleContext context)
        {
            var analyzer = new ActionOptionsDependencyAnalyzer(context);
            analyzer.EnsureDependencies();
            return analyzer;
        }

        private ActionOptionsDependencyAnalyzer(CollectionRuleContext context)
        {
            _ruleContext = context ?? throw new ArgumentNullException(nameof(context));
        }

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

        public object SubstituteOptionValues(IDictionary<string, CollectionRuleActionResult> actionResults, int actionIndex, object settings)
        {
            //Attempt to substitute context properties.
            object originalSettings = settings;

            foreach(PropertyInfo propertyInfo in GetPropertiesFromSettings(settings))
            {
                string originalPropertyValue = (string)propertyInfo.GetValue(settings);
                //If we don't have an Endpoint info (such as test scenarios) we cannot perform this substitution.
                if (string.IsNullOrEmpty(originalPropertyValue) || (_ruleContext.EndpointInfo == null))
                {
                    continue;
                }

                string replacement = originalPropertyValue.Replace(RuntimeIdReference, _ruleContext.EndpointInfo.RuntimeInstanceCookie.ToString("D"), StringComparison.Ordinal);

                if (!ReferenceEquals(replacement, originalPropertyValue))
                {
                    if (!TryCloneSettings(originalSettings, ref settings))
                    {
                        return settings;
                    }
                    propertyInfo.SetValue(settings, replacement);
                }
            }
            
            if (!_dependencies.TryGetValue(actionIndex, out Dictionary<string, PropertyDependency> properties))
            {
                return settings;
            }

            if (!TryCloneSettings(originalSettings, ref settings))
            {
                return settings;
            }

            foreach ((_, PropertyDependency property) in properties)
            {
                StringBuilder builder = property.ActionDependencies.Any() ? new() : null;
                int offset = 0;
                foreach(ActionDependency actionDependency in property.ActionDependencies)
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

            return settings;
        }

        private bool TryCloneSettings(object originalSettings, ref object settings)
        {
            if (originalSettings == null)
            {
                return false;
            }

            if (ReferenceEquals(originalSettings, settings))
            {
                if (originalSettings is BaseRecordOptions baseRecord)
                {
                    //Creates a copy using record's Clone method.
                    settings = baseRecord with { };
                    return true;
                }
                else
                {
                    _ruleContext.Logger.ActionSettingsTokenizationNotSupported(settings.GetType().FullName);
                    settings = originalSettings;
                    return false;
                }
            }
            return true;
        }

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

        private void EnsureDependencies(CollectionRuleActionOptions options, int actionIndex)
        {
            foreach (PropertyInfo property in GetDependencyPropertiesFromSettings(options))
            {
                string originalValue = (string)property.GetValue(options.Settings);
                string newValue = originalValue;
                if (string.IsNullOrEmpty(originalValue))
                {
                    continue;
                }

                int foundIndex = 0;
                int startIndex = 0;

                PropertyDependency propertyDependency = null;

                while ((foundIndex = newValue.IndexOf(ActionReferencePrefix, startIndex, StringComparison.OrdinalIgnoreCase)) >= 0)
                {
                    int suffixIndex = newValue.IndexOf(SubstitutionSuffix, foundIndex, StringComparison.OrdinalIgnoreCase);
                    if (suffixIndex == -1)
                    {
                        _ruleContext.Logger.InvalidActionReferenceToken(options.Name ?? actionIndex.ToString(), property.Name);
                        break;
                    }
                    startIndex = suffixIndex;

                    string actionAndResult = newValue[(foundIndex + ActionReferencePrefix.Length)..suffixIndex];

                    if (!GetActionResultReference(actionAndResult, actionIndex, out CollectionRuleActionOptions dependencyOptions, out string actionResultName))
                    {
                        continue;
                    }

                    if (propertyDependency == null)
                    {
                        propertyDependency = GetOrCreateDependency(actionIndex, property, originalValue);
                    }

                    var dependency = new ActionDependency(dependencyOptions, actionResultName, foundIndex, suffixIndex);
                    propertyDependency.ActionDependencies.Add(dependency);
                }
            }
        }

        private PropertyDependency GetOrCreateDependency(int actionIndex, PropertyInfo propertyInfo, string originalValue)
        {
            if (!_dependencies.TryGetValue(actionIndex, out Dictionary<string, PropertyDependency> properties))
            {
                properties = new Dictionary<string, PropertyDependency>(StringComparer.Ordinal);
                _dependencies.Add(actionIndex, properties);
            }
            if (!properties.TryGetValue(propertyInfo.Name, out PropertyDependency dependency))
            {
                dependency = new PropertyDependency(propertyInfo, originalValue);
                properties.Add(propertyInfo.Name, dependency);
            }
            return dependency;
        }

        private bool GetActionResultReference(string actionReference, int actionIndex,
            out CollectionRuleActionOptions action, out string actionResultName)
        {
            action = null;
            actionResultName = null;

            string[] parts = actionReference.Split(Separator);
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
            CollectionRuleActionOptions dependencyOptions = _ruleContext.Options.Actions.Take(actionIndex)
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

        private IEnumerable<PropertyInfo> GetDependencyPropertiesFromSettings(CollectionRuleActionOptions options)
        {
            return GetPropertiesFromSettings(options.Settings, p => p.GetCustomAttributes(typeof(ActionOptionsDependencyPropertyAttribute), inherit: true).Any());
        }

        private IEnumerable<PropertyInfo> GetPropertiesFromSettings(object settings, Predicate<PropertyInfo> predicate = null) =>
            //CONSIDER
            //In the future we may want to do additional substitutions, such as $(Environment.Value)
            //or $(Process.Id). We would likely remove the attribute in this case.
            //Note settings could be null, although we do not have any options like this currently.
            settings?.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(string) && (predicate?.Invoke(p) ?? true)) ??
            Enumerable.Empty<PropertyInfo>();
    }
}
