// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;

namespace CollectionRuleActions.UnitTests
{
    internal sealed class PipeEgressOptions
    {
        public PipeWriter Writer { get; set; }
    }
}
