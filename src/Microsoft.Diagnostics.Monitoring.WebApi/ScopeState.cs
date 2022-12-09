// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class ScopeState : IEnumerable<IReadOnlyList<KeyValuePair<string, object>>>
    {
        private readonly Stack<IReadOnlyList<KeyValuePair<string, object>>> _scopes = new Stack<IReadOnlyList<KeyValuePair<string, object>>>();

        private sealed class ScopeEntry : IDisposable
        {
            private readonly Stack<IReadOnlyList<KeyValuePair<string, object>>> _scopes;

            public ScopeEntry(Stack<IReadOnlyList<KeyValuePair<string, object>>> scopes, IReadOnlyList<KeyValuePair<string, object>> scope)
            {
                _scopes = scopes;
                _scopes.Push(scope);
            }

            public void Dispose()
            {
                _scopes.Pop();
            }
        }

        public IDisposable Push(IReadOnlyList<KeyValuePair<string, object>> scope)
        {
            return new ScopeEntry(_scopes, scope);
        }

        public IEnumerator<IReadOnlyList<KeyValuePair<string, object>>> GetEnumerator()
        {
            return _scopes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool HasScopes => _scopes.Count > 0;
    }
}
