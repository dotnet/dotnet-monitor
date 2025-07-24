// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal sealed class TestOptionsFactory
    {
        private Type TriggerOptionsType { get; }
        private Type ActionOptionsType { get; }
        private string TriggerTypeName { get; }
        private string ActionTypeName { get; }
        
        // Cache property info for polymorphic types that need special handling
        private static readonly PropertyInfo CollectionRuleTriggerOptionsSettingsProperty = typeof(CollectionRuleTriggerOptions).GetProperty(nameof(CollectionRuleTriggerOptions.Settings))!;
        private static readonly PropertyInfo CollectionRuleActionOptionsSettingsProperty = typeof(CollectionRuleActionOptions).GetProperty(nameof(CollectionRuleActionOptions.Settings))!;

        public TestOptionsFactory(Type triggerOptionsType, Type actionOptionsType, string triggerTypeName, string actionTypeName)
        {
            TriggerOptionsType = triggerOptionsType;
            ActionOptionsType = actionOptionsType;
            TriggerTypeName = triggerTypeName;
            ActionTypeName = actionTypeName;
        }

        public RootOptions CreateRootOptions()
        {
            return (RootOptions)CreateObject(typeof(RootOptions));
        }

        public object CreateObject(Type type, PropertyInfo? propertyInfo = null)
        {
            if (TryCreateBuiltInObject(type, propertyInfo, out object? obj))
            {
                return obj!;
            }

            obj = Activator.CreateInstance(type)!;

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.GetIndexParameters().Any())
                {                   
                    object value = CreateValue(property.PropertyType, property);
                    property.SetValue(obj, value);
                }
            }
            
            return obj;
        }

        public bool TryCreateBuiltInObject(Type type, PropertyInfo? propertyInfo, out object? obj)
        {
            // Handle special cases for polymorphic types
            if (type == typeof(CollectionRuleTriggerOptions))
            {
                obj = CreateCollectionRuleTriggerOptions();
                return true;
            }

            if (type == typeof(CollectionRuleActionOptions))
            {
                obj = CreateCollectionRuleActionOptions();
                return true;
            }

            obj = null;
            return false;
        }

        public object CreateValue(Type type, PropertyInfo? propertyInfo = null)
        {
            if (Nullable.GetUnderlyingType(type) is Type underlyingType)
            {
                type = underlyingType;
            }

            if (type.IsPrimitive ||
                type.IsEnum ||
                typeof(Guid) == type ||
                typeof(string) == type ||
                typeof(TimeSpan) == type ||
                typeof(Uri) == type)
            {
                return CreateBuiltInValue(type);
            }
            else
            {
                if (TryCreateDictionary(type, out IDictionary dictionary))
                {
                    return dictionary;
                }
                else if (TryCreateList(type, out IList list))
                {
                    return list;
                }
                else
                {
                    return CreateObject(type, propertyInfo);
                }
            }
        }

        public static object CreateBuiltInValue(Type type)
        {
            if (type == typeof(string))
            {
                return "SomeString";
            }
            else if (type == typeof(Uri))
            {
                return new Uri("http://localhost:5000");
            }
            
            return Activator.CreateInstance(type)!;
        }

        CollectionRuleTriggerOptions CreateCollectionRuleTriggerOptions()
        {
            var triggerOptions = new CollectionRuleTriggerOptions();
            triggerOptions.Type = TriggerTypeName;
            triggerOptions.Settings = CreateValue(TriggerOptionsType, CollectionRuleTriggerOptionsSettingsProperty);
            return triggerOptions;
        }

        CollectionRuleActionOptions CreateCollectionRuleActionOptions()
        {
            var actionOptions = new CollectionRuleActionOptions();
            actionOptions.Type = ActionTypeName;
            actionOptions.Settings = CreateValue(ActionOptionsType, CollectionRuleActionOptionsSettingsProperty);
            return actionOptions;
        }

        bool TryCreateDictionary(Type type, out IDictionary dictionary)
        {
            dictionary = null!;

            foreach (var interfaceType in type.GetInterfaces().Concat([type]))
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    Type[] genericArguments = interfaceType.GetGenericArguments();
                    Type keyType = genericArguments[0];
                    Type valueType = genericArguments[1];
                    
                    Type dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
                    var dictionaryInstance = (IDictionary)Activator.CreateInstance(dictionaryType)!;
                    object key = CreateValue(keyType);
                    object value = CreateValue(valueType);
                    dictionaryInstance.Add(key, value);
                    
                    dictionary = dictionaryInstance;
                    return true;
                }
            }
            
            return false;
        }

        bool TryCreateList(Type type, out IList list)
        {
            list = null!;

            if (type.IsArray)
            {
                Type elementType = type.GetElementType()!;
                Array array = Array.CreateInstance(elementType, 1);
                array.SetValue(CreateValue(elementType), 0);
                list = array;
                return true;
            }
            
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type valueType = type.GetGenericArguments()[0];
                var listInstance = (IList)Activator.CreateInstance(type)!;
                listInstance.Add(CreateValue(valueType));
                
                list = listInstance;
                return true;
            }
            
            return false;
        }
    }
}
