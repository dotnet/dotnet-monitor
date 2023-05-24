// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

// Use a shorter namespace to keep the expected strings in tests a more manageable length.
namespace SampleMethods
{
    internal struct MyTestStruct
    {
        public static void DoWork(int i) { }
    }
    internal ref struct MyRefStruct { }
    internal enum MyEnum
    {
        ValueA = 1
    }

#pragma warning disable CA1822 // Mark members as static
    internal sealed class GenericTestMethodSignatures<T1, T2>
    {
        public void GenericParameters<T3>(T1 t1, T2 t2, T3 t3) { }
    }
#pragma warning restore CA1822 // Mark members as static


#pragma warning disable CA1822 // Mark members as static
    internal sealed class TestMethodSignatures
    {
        public void ImplicitThis() { }
    }
#pragma warning restore CA1822 // Mark members as static

    internal static class StaticTestMethodSignatures
    {
        internal struct SampleClassStruct
        {
            public static void DoWork(int i) { }
        }

        public delegate int MyDelegate(int i, int j);

        public static void BasicTypes(string s, int[] intArray, bool[,] multidimensionalArray, uint uInt) { }

        public static void NoArgs() { }

        public static void ExplicitThis(this object thisObj) { }

        public static void RefStruct(ref MyRefStruct myRefStruct) { }

        public static unsafe void Pointer(byte* test) { }

        public static void Delegate(MyDelegate func) { }

        public static void InParam(in int i) { }

        public static void RefParam(ref int i) { }

        public static void OutParam(out int i)
        {
            i = 0;
        }

        public static void GenericParameters<T, K>(T t, K k) { }

        public static void TypeRef(Uri uri) { }

        public static void TypeDef(TestMethodSignatures t) { }

        public static void TypeSpec(IList<IEnumerable<bool>> list) { }


        public static void ValueType_TypeDef(MyEnum myEnum) { }

        public static void ValueType_TypeRef(TypeCode typeCode) { }

        public static void ValueType_TypeSpec(bool? b) { }

        public static void VarArgs(bool b, params int[] myInts) { }

        public static void Unicode_ΦΨ(bool δ) { }

    }
}
