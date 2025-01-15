// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable CA1822 // Mark members as static

// Use a shorter namespace to keep the expected strings in tests a more manageable length.
namespace SampleMethods
{
    internal record struct MyRecordStruct { }
    internal ref struct MyRefStruct { }
    internal enum MyEnum
    {
        ValueA = 1
    }

    internal sealed class GenericTestMethodSignatures<T1, T2>
    {
        public void GenericParameters<T3>(T1 t1, T2 t2, T3 t3) { }
    }

    internal sealed class TestMethodSignatures
    {
        public void ImplicitThis() { }
    }

    internal abstract class TestAbstractSignatures
    {
        public const string PrivateStaticBaseMethodName = nameof(PrivateStaticBaseMethod);
        public const string PrivateBaseMethodName = nameof(PrivateBaseMethod);
        public const string ProtectedBaseMethodName = nameof(ProtectedBaseMethod);

        private static void PrivateStaticBaseMethod() { }
        private void PrivateBaseMethod() { }
        protected void ProtectedBaseMethod() { }
        public void BaseMethod() { }
        public abstract void DerivedMethod();
    }

    internal sealed class TestDerivedSignatures : TestAbstractSignatures
    {
        public void NonInheritedMethod() { }
        public override void DerivedMethod() { }
    }

    internal sealed class TestAmbiguousGenericSignatures<T1>
    {
        public void AmbiguousMethod<T2>(T1 t, T2 t2) { }
        public void AmbiguousMethod<T2, T3>(T1 t, T2 t2, T3 t3) { }
    }

    internal static class StaticTestMethodSignatures
    {
        public static bool FieldWithGetterAndSetter { get; set; }

        internal struct SampleNestedStruct
        {
            public void DoWork(int i) { }
        }

        public delegate int MyDelegate(int i, int j);

        public static void AmbiguousMethod() { }
        public static bool AmbiguousMethod(int i) { return true; }
        public static int AmbiguousMethod(int i, int j) { return i + j; }

        public static void Arrays(int[] intArray, bool[,] multidimensionalArray) { }

        public static async Task AsyncMethod(int delay) { await Task.Delay(delay); }

        public static void BuiltInReferenceTypes(object arg1, string arg2, dynamic arg3) { }

        public static void NoArgs() { }

        public static string ExceptionRegionAtBeginningOfMethod(object myObject)
        {
            try
            {
                return myObject.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

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
            double arg12)
        { }

        public static void NativeIntegers(IntPtr intPtr, UIntPtr uintPtr) { }

        public static void GenericParameters<T, K>(T t, K k) { }

        public static void TypeRef(Uri uri) { }

        public static void TypeDef(TestMethodSignatures t) { }

        public static void TypeSpec(IList<IEnumerable<bool>> list) { }


        public static void ValueType_TypeDef(MyEnum myEnum) { }

        public static void ValueType_TypeRef(TypeCode typeCode) { }

        public static void ValueType_TypeSpec(bool? b, (int? i, bool? b) tuple) { }

        public static void VarArgs(bool b, params int[] myInts) { }

        public static void Unicode_ΦΨ(bool δ) { }

    }
}
#pragma warning restore CA1822 // Mark members as static
