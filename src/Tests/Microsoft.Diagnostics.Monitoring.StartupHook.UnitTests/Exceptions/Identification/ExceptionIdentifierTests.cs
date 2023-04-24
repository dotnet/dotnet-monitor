// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Identification
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class ExceptionIdentifierTests
    {
        [Fact]
        public void ExceptionIdentifier_NotThrownException()
        {
            Exception ex = new();
            ExceptionIdentifier id = new(ex);

            Assert.Equal(ex.GetType(), id.ExceptionType);
            Assert.Null(id.ThrowingMethod);
            Assert.Equal(0, id.ILOffset);
        }

        [Fact]
        public void ExceptionIdentifier_ThrownException()
        {
            Exception ex = new();
            ExceptionIdentifier id = ThrowAndCreate(ex);

            Assert.Equal(ex.GetType(), id.ExceptionType);
            Assert.NotNull(id.ThrowingMethod);
        }

        [Fact]
        public void ExceptionIdentifier_SameOrigination_Equal()
        {
            ExceptionIdentifier id1 = ThrowAndCreate();
            ExceptionIdentifier id2 = ThrowAndCreate();

            Assert.Equal(id1, id2);
            Assert.True(id1 == id2);
        }

        [Fact]
        public void ExceptionIdentifier_SameOrigination_EqualProperties()
        {
            ExceptionIdentifier id1 = ThrowAndCreate();
            ExceptionIdentifier id2 = ThrowAndCreate();

            Assert.Equal(id1.ExceptionType, id2.ExceptionType);
            Assert.NotNull(id1.ThrowingMethod);
            Assert.NotNull(id2.ThrowingMethod);
            Assert.Equal(id1.ThrowingMethod, id2.ThrowingMethod);
            Assert.Equal(id1.ILOffset, id2.ILOffset);
        }

        [Fact]
        public void ExceptionIdentifier_DifferentType_NotEqual()
        {
            (ExceptionIdentifier id1, ExceptionIdentifier id2) = ThrowAndCreatePair(differentTypes: true);

            Assert.NotEqual(id1, id2);
            Assert.True(id1 != id2);
        }

        [Fact]
        public void ExceptionIdentifier_DifferentType_NotEqualProperties()
        {
            (ExceptionIdentifier id1, ExceptionIdentifier id2) = ThrowAndCreatePair(differentTypes: true);

            Assert.NotEqual(id1.ExceptionType, id2.ExceptionType);
            Assert.NotNull(id1.ThrowingMethod);
            Assert.NotNull(id2.ThrowingMethod);
            Assert.Equal(id1.ThrowingMethod, id2.ThrowingMethod);
            Assert.NotEqual(id1.ILOffset, id2.ILOffset);
        }

        [Fact]
        public void ExceptionIdentifier_DifferentMethod_NotEqual()
        {
            (ExceptionIdentifier id1, ExceptionIdentifier id2) = ThrowAndCreatePair(differentMethods: true);

            Assert.NotEqual(id1, id2);
            Assert.True(id1 != id2);
        }

        [Fact]
        public void ExceptionIdentifier_DifferentMethod_NotEqualProperties()
        {
            (ExceptionIdentifier id1, ExceptionIdentifier id2) = ThrowAndCreatePair(differentMethods: true);

            Assert.Equal(id1.ExceptionType, id2.ExceptionType);
            Assert.NotNull(id1.ThrowingMethod);
            Assert.NotNull(id2.ThrowingMethod);
            Assert.NotEqual(id1.ThrowingMethod, id2.ThrowingMethod);
        }

        [Fact]
        public void ExceptionIdentifier_DifferentOffset_NotEqual()
        {
            (ExceptionIdentifier id1, ExceptionIdentifier id2) = ThrowAndCreatePair();

            Assert.NotEqual(id1, id2);
            Assert.True(id1 != id2);
        }

        [Fact]
        public void ExceptionIdentifier_DifferentOffset_NotEqualProperties()
        {
            (ExceptionIdentifier id1, ExceptionIdentifier id2) = ThrowAndCreatePair();

            Assert.Equal(id1.ExceptionType, id2.ExceptionType);
            Assert.NotNull(id1.ThrowingMethod);
            Assert.NotNull(id2.ThrowingMethod);
            Assert.Equal(id1.ThrowingMethod, id2.ThrowingMethod);
            Assert.NotEqual(id1.ILOffset, id2.ILOffset);
        }

        private static ExceptionIdentifier ThrowAndCreate()
        {
            return ThrowAndCreate(new InvalidOperationException());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ExceptionIdentifier ThrowAndCreate(Exception ex)
        {
            try
            {
                throw ex;
            }
            catch (Exception caughtEx)
            {
                return new ExceptionIdentifier(caughtEx);
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static (ExceptionIdentifier, ExceptionIdentifier) ThrowAndCreatePair(bool differentTypes = false, bool differentMethods = false)
        {
            ExceptionIdentifier id1;
            try
            {
                throw new InvalidOperationException();
            }
            catch (Exception ex)
            {
                id1 = new ExceptionIdentifier(ex);
            }

            Exception ex2 = differentTypes ? new OperationCanceledException() : new InvalidOperationException();

            ExceptionIdentifier id2;
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
                    id2 = new ExceptionIdentifier(ex);
                }
            }

            return (id1, id2);
        }
    }
}
