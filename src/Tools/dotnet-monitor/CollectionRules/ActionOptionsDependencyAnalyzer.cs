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
        private const string ActionReferencePrefix = SubstitutionPrefix + "Actions.";

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
            public ActionDependency(CollectionRuleActionOptions action, string resultName)
            {
                Action = action;
                ResultName = resultName;
            }

            public CollectionRuleActionOptions Action { get; }

            public string ResultName { get; }

            public string GetActionResultToken() => string.Concat(ActionReferencePrefix,
                Action.Name, Separator, ResultName, SubstitutionSuffix);
        }

        public ActionOptionsDependencyAnalyzer(CollectionRuleContext context)
        {
            _ruleContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IList<CollectionRuleActionOptions> GetActionDependencies(int actionIndex)
        {
            EnsureDependencies();

            if (_dependencies.TryGetValue(actionIndex, out Dictionary<string, PropertyDependency> properties))
            {
                return properties
                    .SelectMany(p => p.Value.ActionDependencies)
                    .DistinctBy(a => a.Action.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(a => a.Action).ToArray();
            }
            return Array.Empty<CollectionRuleActionOptions>();
        }

        public object SubstituteOptionValues(int actionIndex, object settings)
        {
            EnsureDependencies();
            if (!_dependencies.TryGetValue(actionIndex, out Dictionary<string, PropertyDependency> properties))
            {
                return settings;
            }

            if (settings is ICloneable cloneable)
            {
                settings = cloneable.Clone();
            }
            else
            {
                _ruleContext.Logger.InvalidSettings(settings.GetType().FullName);
                return settings;
            }

            foreach (KeyValuePair<string, PropertyDependency> property in properties)
            {
                string newValue = property.Value.OriginalValue;
                foreach(ActionDependency actionDependency in property.Value.ActionDependencies)
                {
                    string actionToken = actionDependency.GetActionResultToken();
                    if (!_ruleContext.ActionResults.TryGetValue(actionDependency.Action.Name, out CollectionRuleActionResult results))
                    {
                        _ruleContext.Logger.InvalidResultReference(actionToken);
                        continue;
                    }
                    if (!results.OutputValues.TryGetValue(actionDependency.ResultName, out string result))
                    {
                        _ruleContext.Logger.InvalidResultReference(actionToken);
                        continue;
                    }
                    newValue = newValue.Replace(actionToken, result, StringComparison.OrdinalIgnoreCase);
                }
                property.Value.Property.SetValue(settings, newValue);
            }

            return settings;
        }

        private void EnsureDependencies()
        {
            if (_dependencies == null)
            {
                _dependencies = new Dictionary<int, Dictionary<string, PropertyDependency>>();
                for (int i = 0; i < _ruleContext.Options.Actions.Count; i++)
                {
                    CollectionRuleActionOptions options = _ruleContext.Options.Actions[i];
                    EnsureDependencies(options, i);
                }
            }
        }

        private void EnsureDependencies(CollectionRuleActionOptions options, int actionIndex)
        {
            foreach (PropertyInfo property in GetPropertiesFromSettings(options))
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
                        _ruleContext.Logger.InvalidTokenReference(options.Name, property.Name);
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

                    var dependency = new ActionDependency(dependencyOptions, actionResultName);
                    propertyDependency.ActionDependencies.Add(dependency);
                }
            }
        }

        private PropertyDependency GetOrCreateDependency(int actionIndex, PropertyInfo propertyInfo, string originalValue)
        {
            if (!_dependencies.TryGetValue(actionIndex, out Dictionary<string, PropertyDependency> properties))
            {
                properties = new Dictionary<string, PropertyDependency>();
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
            //We only check previous actions for our dependencies.
            CollectionRuleActionOptions dependencyOptions = _ruleContext.Options.Actions.Take(actionIndex)
                .FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

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

        private IEnumerable<PropertyInfo> GetPropertiesFromSettings(CollectionRuleActionOptions options) =>
            //CONSIDER
            //In the future we may want to do additional substitutions, such as $(Environment.Value)
            //or $(Process.Id). We would likely remove the attribute in this case.
            //Note settings could be null, although we do not have any options like this currently.
            options.Settings?.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(string) &&
                            p.GetCustomAttributes(typeof(ActionOptionsDependencyPropertyAttribute), inherit: true).Any()) ??
            Enumerable.Empty<PropertyInfo>();
    }
}
