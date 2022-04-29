// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal abstract class ExtensionRepository : IExtensionRepository
    {
        private readonly int _resolvePriority;
        private readonly string _name;

        public ExtensionRepository(int resolvePriority, string name)
        {
            _resolvePriority = resolvePriority;
            _name = name;
        }

        public int ResolvePriority => _resolvePriority;

        public string Name => _name;

        public abstract IExtension FindExtension(string extensionMoniker);
    }
}
