using System;
using System.IO;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.ConfigurationSchema
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidOperationException("Missing output path");
            }

            var generator = new SchemaGenerator();
            string result = generator.GenerateSchema();

            File.WriteAllText(args[0], result);
        }
    }
}
