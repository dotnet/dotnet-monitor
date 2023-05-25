﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

// Use a shorter namespace to keep the expected strings in tests a more manageable length.
namespace SampleMethods
{
    internal record struct MyRecordStruct { }
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
        internal struct SampleNestedStruct
        {
#pragma warning disable CA1822 // Mark members as static
            public void DoWork(int i) { }
#pragma warning restore CA1822 // Mark members as static
        }

        public delegate int MyDelegate(int i, int j);

        public static void Arrays(int[] intArray, bool[,] multidimensionalArray) { }

        public static void BuiltInReferenceTypes(object arg1, string arg2, dynamic arg3) { }

        public static void NoArgs() { }

        public static void ExplicitThis(this object thisObj) { }

        public static void RefStruct(ref MyRefStruct myRefStruct) { }

        public static void RecordStruct(MyRecordStruct myRecordStruct) { }

        public static unsafe void Pointer(byte* test) { }

        public static void Delegate(MyDelegate func) { }

        public static void InParam(in int i) { }

        public static void RefParam(ref int i) { }

        public static void OutParam(out int i)
        {
            i = 0;
        }

        public static void Primitives(
            bool arg1,
            char arg2,
            sbyte arg3,
            byte arg4,
            short arg5,
            ushort arg6,
            int arg7,
            uint arg8,
            long arg9,
            ulong arg10,
            float arg11,
            double arg12) { }

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
