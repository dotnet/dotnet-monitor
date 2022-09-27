// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.Options;
using NJsonSchema.Generation;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.ConfigurationSchema
{
    internal class ExperimentalSchemaProcessor : ISchemaProcessor
    {
        private const string ExperimentalPrefix = "[Experimental]";

        public void Process(SchemaProcessorContext context)
        {
            foreach (PropertyInfo property in context.Type.GetProperties())
            {
                if (null != property.GetCustomAttribute<ExperimentalAttribute>())
                {
                    string description = context.Schema.Properties[property.Name].Description;
                    if (string.IsNullOrEmpty(description))
                    {
                        description = ExperimentalPrefix;
                    }
                    else
                    {
                        description = $"{ExperimentalPrefix} {description}";
                    }
                    context.Schema.Properties[property.Name].Description = description;
                }
            }
        }
    }
}
