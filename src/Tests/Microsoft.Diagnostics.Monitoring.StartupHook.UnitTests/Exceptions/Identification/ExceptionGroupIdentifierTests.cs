// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Identification
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class ExceptionGroupIdentifierTests
    {
        [Fact]
        public void ExceptionGroupIdentifier_NotThrownException()
        {
            Exception ex = new();
            ExceptionGroupIdentifier id = new(ex);

            Assert.Equal(ex.GetType(), id.ExceptionType);
            Assert.Null(id.ThrowingMethod);
            Assert.Equal(0, id.ILOffset);
        }

        [Fact]
        public void ExceptionGroupIdentifier_ThrownException()
        {
            Exception ex = new();
            ExceptionGroupIdentifier id = ThrowAndCreate(ex);

            Assert.Equal(ex.GetType(), id.ExceptionType);
            Assert.NotNull(id.ThrowingMethod);
        }

        [Fact]
        public void ExceptionGroupIdentifier_SameOrigination_Equal()
        {
            ExceptionGroupIdentifier id1 = ThrowAndCreate();
            ExceptionGroupIdentifier id2 = ThrowAndCreate();

            Assert.Equal(id1, id2);
            Assert.True(id1 == id2);
        }

        [Fact]
        public void ExceptionGroupIdentifier_SameOrigination_EqualProperties()
        {
            ExceptionGroupIdentifier id1 = ThrowAndCreate();
            ExceptionGroupIdentifier id2 = ThrowAndCreate();

            Assert.Equal(id1.ExceptionType, id2.ExceptionType);
            Assert.NotNull(id1.ThrowingMethod);
            Assert.NotNull(id2.ThrowingMethod);
            Assert.Equal(id1.ThrowingMethod, id2.ThrowingMethod);
            Assert.Equal(id1.ILOffset, id2.ILOffset);
        }

        [Fact]
        public void ExceptionGroupIdentifier_DifferentType_NotEqual()
        {
            (ExceptionGroupIdentifier id1, ExceptionGroupIdentifier id2) = ThrowAndCreatePair(differentTypes: true);

            Assert.NotEqual(id1, id2);
            Assert.True(id1 != id2);
        }

        [Fact]
        public void ExceptionGroupIdentifier_DifferentType_NotEqualProperties()
        {
            (ExceptionGroupIdentifier id1, ExceptionGroupIdentifier id2) = ThrowAndCreatePair(differentTypes: true);

            Assert.NotEqual(id1.ExceptionType, id2.ExceptionType);
            Assert.NotNull(id1.ThrowingMethod);
            Assert.NotNull(id2.ThrowingMethod);
            Assert.Equal(id1.ThrowingMethod, id2.ThrowingMethod);
            Assert.NotEqual(id1.ILOffset, id2.ILOffset);
        }

        [Fact]
        public void ExceptionGroupIdentifier_DifferentMethod_NotEqual()
        {
            (ExceptionGroupIdentifier id1, ExceptionGroupIdentifier id2) = ThrowAndCreatePair(differentMethods: true);

            Assert.NotEqual(id1, id2);
            Assert.True(id1 != id2);
        }

        [Fact]
        public void ExceptionGroupIdentifier_DifferentMethod_NotEqualProperties()
        {
            (ExceptionGroupIdentifier id1, ExceptionGroupIdentifier id2) = ThrowAndCreatePair(differentMethods: true);

            Assert.Equal(id1.ExceptionType, id2.ExceptionType);
            Assert.NotNull(id1.ThrowingMethod);
            Assert.NotNull(id2.ThrowingMethod);
            Assert.NotEqual(id1.ThrowingMethod, id2.ThrowingMethod);
        }

        [Fact]
        public void ExceptionGroupIdentifier_DifferentOffset_NotEqual()
        {
            (ExceptionGroupIdentifier id1, ExceptionGroupIdentifier id2) = ThrowAndCreatePair();

            Assert.NotEqual(id1, id2);
            Assert.True(id1 != id2);
        }

        [Fact]
        public void ExceptionGroupIdentifier_DifferentOffset_NotEqualProperties()
        {
            (ExceptionGroupIdentifier id1, ExceptionGroupIdentifier id2) = ThrowAndCreatePair();

            Assert.Equal(id1.ExceptionType, id2.ExceptionType);
            Assert.NotNull(id1.ThrowingMethod);
            Assert.NotNull(id2.ThrowingMethod);
            Assert.Equal(id1.ThrowingMethod, id2.ThrowingMethod);
            Assert.NotEqual(id1.ILOffset, id2.ILOffset);
        }

        private static ExceptionGroupIdentifier ThrowAndCreate()
        {
            return ThrowAndCreate(new InvalidOperationException());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ExceptionGroupIdentifier ThrowAndCreate(Exception ex)
        {
            try
            {
                throw ex;
            }
            catch (Exception caughtEx)
            {
                return new ExceptionGroupIdentifier(caughtEx);
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static (ExceptionGroupIdentifier, ExceptionGroupIdentifier) ThrowAndCreatePair(bool differentTypes = false, bool differentMethods = false)
        {
            ExceptionGroupIdentifier id1;
            try
            {
                throw new InvalidOperationException();
            }
            catch (Exception ex)
            {
                id1 = new ExceptionGroupIdentifier(ex);
            }

            Exception ex2 = differentTypes ? new OperationCanceledException() : new InvalidOperationException();

            ExceptionGroupIdentifier id2;
            if (differentMethods)
            {
                id2 = ThrowAndCreate(ex2);
            }
            else
            {
                try
                {
                    throw ex2;
                }
                catch (Exception ex)
                {
                    id2 = new ExceptionGroupIdentifier(ex);
                }
            }

            return (id1, id2);
        }
    }
}
