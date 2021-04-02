using Microsoft.Diagnostics.Monitoring.UnitTests.Options;
using NJsonSchema;
using NJsonSchema.Generation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.ConfigurationSchema
{
    internal sealed class SchemaGenerator
    {
        public string GenerateSchema()
        {
            var settings = new JsonSchemaGeneratorSettings();
            JsonSchema schema = JsonSchema.FromType<RootOptions>(settings);
            schema.Id = @"https://www.github.com/dotnet/dotnet-monitor";
            schema.Title = "DotnetMonitorConfiguration";

            //HACK Even though Properties is defined as JsonDataExtension, it still emits a property
            //called 'Properties' into the schema, instead of just specifying additionalProperties.
            //There is likely a more elegant way to fix this.
            schema.Definitions[nameof(EgressProvider)].Properties.Remove(nameof(EgressProvider.Properties));

            string schemaPayload = schema.ToJson();

            //Normalize newlines embedded into json
            schemaPayload = schemaPayload.Replace(@"\r\n", @"\n", StringComparison.Ordinal);
            return schemaPayload;
        }
    }
}
