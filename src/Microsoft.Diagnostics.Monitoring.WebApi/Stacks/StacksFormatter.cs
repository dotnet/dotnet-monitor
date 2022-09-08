// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Stacks
{
    internal abstract class StacksFormatter
    {
        protected const string UnknownModule = "UnknownModule";
        protected const string UnknownClass = "UnknownClass";
        protected const string UnknownFunction = "UnknownFunction";

        protected const string ArrayType = "_ArrayType_";
        protected const string CompositeType = "_CompositeType_";

        protected const char NestedSeparator = '+';
        protected const char GenericStart = '[';
        protected const char GenericSeparator = ',';
        protected const char GenericEnd = ']';

        protected Stream OutputStream { get; }

        public StacksFormatter(Stream outputStream)
        {
            OutputStream = outputStream;
        }

        public abstract Task FormatStack(StackResult stackResult, CancellationToken token);

        protected string GetModuleName(NameCache cache, ulong moduleId)
        {
            string moduleName = UnknownModule;
            if (cache.ModuleData.TryGetValue(moduleId, out ModuleData moduleData))
            {
                moduleName = moduleData.Name;
            }
            return moduleName;
        }

        protected void BuildClassName(StringBuilder builder, NameCache cache, ulong classId)
        {
            string className = UnknownClass;
            if (cache.ClassData.TryGetValue(classId, out ClassData classData))
            {
                if (classData.Flags != ClassFlags.None)
                {
                    switch (classData.Flags)
                    {
                        case ClassFlags.Array:
                            className = ArrayType;
                            break;
                        case ClassFlags.Composite:
                            className = CompositeType;
                            break;
                        default:
                            //All other cases default to UnknownClass
                            break;
                    }
                }
                else
                {
                    BuildClassName(builder, cache, classData.ModuleId, classData.Token);
                }
                BuildGenericParameters(builder, cache, classData.TypeArgs);
            }
            else
            {
                builder.Append(className);
            }
        }

        protected void BuildGenericParameters(StringBuilder builder, NameCache cache, ulong[] parameters)
        {
            for (int i = 0; i < parameters?.Length; i++)
            {
                if (i == 0)
                {
                    builder.Append(GenericStart);
                }
                BuildClassName(builder, cache, parameters[i]);
                if (i < parameters.Length - 1)
                {
                    builder.Append(GenericSeparator);
                }
                else if (i == parameters.Length - 1)
                {
                    builder.Append(GenericEnd);
                }
            }
        }

        protected void BuildClassName(StringBuilder builder, NameCache cache, ulong moduleId, uint token)
        {
            var classNames = new Stack<string>();

            uint currentToken = token;
            while (currentToken != 0 && cache.TokenData.TryGetValue((moduleId, currentToken), out TokenData tokenData))
            {
                classNames.Push(tokenData.Name);
                currentToken = tokenData.OuterToken;
            }

            if (classNames.Count == 0)
            {
                builder.Append(UnknownClass);
            }

            while (classNames.Count > 0)
            {
                string className = classNames.Pop();
                builder.Append(className);
                if (classNames.Count > 0)
                {
                    builder.Append(NestedSeparator);
                }
            }
        }
    }
}
