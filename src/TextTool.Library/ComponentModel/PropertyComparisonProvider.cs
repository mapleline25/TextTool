using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using TextTool.Library.Utils;

namespace TextTool.Library.ComponentModel;

public class PropertyComparisonProvider : IPropertyComparisonProvider<object>
{
    private static readonly Dictionary<Type, PropertyComparisonProvider> _ProviderTable = [];
    private readonly Dictionary<string, PropertyComparison<object>?> _comparisonTable = [];
    private readonly Type _targetType;

    private PropertyComparisonProvider(Type type)
    {
        _targetType = type;
    }

    public static PropertyComparisonProvider GetProvider(Type type)
    {
        if (type.IsGenericTypeDefinition)
        {
            throw new NotSupportedException("Generic type definition is not supported");
        }

        if (type.IsPointer || type.IsFunctionPointer)
        {
            throw new NotSupportedException("Pointer type is not supported");
        }

        if (type.IsByRef || type.IsByRefLike)
        {
            throw new NotSupportedException("Byref or byref-like type is not supported");
        }

        if (Nullable.GetUnderlyingType(type) != null)
        {
            throw new NotSupportedException("Nullable<T> is not supported");
        }
        
        if (!_ProviderTable.TryGetValue(type, out PropertyComparisonProvider? provider))
        {
            provider = new(type);
            _ProviderTable[type] = provider;
        }

        return provider;
    }

    public PropertyComparison<object>? GetComparison(string propertyName)
    {
        if (!_comparisonTable.TryGetValue(propertyName, out PropertyComparison<object>? comparison))
        {
            try
            {
                comparison = GetComparisonDelegate(propertyName);
            }
            catch
            {
                comparison = null;
            }

            _comparisonTable[propertyName] = comparison;
        }

        return comparison;
    }

    private PropertyComparison<object>? GetComparisonDelegate(string propertyName)
    {
        if (TypeAccess.GetPublicProperty(_targetType, propertyName) is not PropertyInfo propertyInfo)
        {
            return null;
        }

        if (propertyInfo!.GetIndexParameters().Length == 1) // ignore this[]
        {
            return null;
        }

        Type propertyType = propertyInfo.PropertyType; 
        if (propertyType == typeof(object)
            || propertyType.IsArray
            || propertyType.IsSZArray
            || propertyType.IsPointer
            || propertyType.IsFunctionPointer
            || propertyType.IsByRef
            || propertyType.IsByRefLike
            || propertyType.IsCOMObject)
        {
            return null;
        }

        bool isNullable;
        if (Nullable.GetUnderlyingType(propertyType) is Type actualType)
        {
            isNullable = true;
        }
        else
        {
            actualType = propertyType;
            isNullable = false;
        }

        if (actualType == typeof(string))
        {
            return FromString(propertyInfo);
        }
        if (actualType == typeof(bool))
        {
            return isNullable ? FromNullableBoolean(propertyInfo) : FromBoolean(propertyInfo);
        }
        if (actualType == typeof(DateTime))
        {
            return isNullable ? FromNullableDateTime(propertyInfo) : FromDateTime(propertyInfo);
        }
        if (actualType.IsEnum)
        {
            return isNullable ? FromNullableEnum(propertyInfo, actualType) : FromEnum(propertyInfo, actualType);
        }
        if (actualType == typeof(char))
        {
            return isNullable ? FromNullableNumeric<char>(propertyInfo) : FromNumeric<char>(propertyInfo);
        }
        if (actualType == typeof(decimal))
        {
            return isNullable ? FromNullableNumeric<decimal>(propertyInfo) : FromNumeric<decimal>(propertyInfo);
        }
        if (actualType == typeof(double))
        {
            return isNullable ? FromNullableNumeric<double>(propertyInfo) : FromNumeric<double>(propertyInfo);
        }
        if (actualType == typeof(short))
        {
            return isNullable ? FromNullableNumeric<short>(propertyInfo) : FromNumeric<short>(propertyInfo);
        }
        if (actualType == typeof(ushort))
        {
            return isNullable ? FromNullableNumeric<ushort>(propertyInfo) : FromNumeric<ushort>(propertyInfo);
        }
        if (actualType == typeof(int))
        {
            return isNullable ? FromNullableNumeric<int>(propertyInfo) : FromNumeric<int>(propertyInfo);
        }
        if (actualType == typeof(uint))
        {
            return isNullable ? FromNullableNumeric<uint>(propertyInfo) : FromNumeric<uint>(propertyInfo);
        }
        if (actualType == typeof(long))
        {
            return isNullable ? FromNullableNumeric<long>(propertyInfo) : FromNumeric<long>(propertyInfo);
        }
        if (actualType == typeof(ulong))
        {
            return isNullable ? FromNullableNumeric<ulong>(propertyInfo) : FromNumeric<ulong>(propertyInfo);
        }
        if (actualType == typeof(float))
        {
            return isNullable ? FromNullableNumeric<float>(propertyInfo) : FromNumeric<float>(propertyInfo);
        }
        if (actualType == typeof(byte))
        {
            return isNullable ? FromNullableNumeric<byte>(propertyInfo) : FromNumeric<byte>(propertyInfo);
        }
        if (actualType == typeof(sbyte))
        {
            return isNullable ? FromNullableNumeric<sbyte>(propertyInfo) : FromNumeric<sbyte>(propertyInfo);
        }
        if (actualType == typeof(Int128))
        {
            return isNullable ? FromNullableNumeric<Int128>(propertyInfo) : FromNumeric<Int128>(propertyInfo);
        }
        if (actualType == typeof(UInt128))
        {
            return isNullable ? FromNullableNumeric<UInt128>(propertyInfo) : FromNumeric<UInt128>(propertyInfo);
        }
        if (actualType == typeof(BigInteger))
        {
            return isNullable ? FromNullableNumeric<BigInteger>(propertyInfo) : FromNumeric<BigInteger>(propertyInfo);
        }
        if (actualType == typeof(NFloat))
        {
            return isNullable ? FromNullableNumeric<NFloat>(propertyInfo) : FromNumeric<NFloat>(propertyInfo);
        }
        if (actualType == typeof(Half))
        {
            return isNullable ? FromNullableIComparableT<Half>(propertyInfo) : FromIComparableT<Half>(propertyInfo);
        }
        if (actualType == typeof(IntPtr))
        {
            return isNullable ? FromNullableNumeric<nint>(propertyInfo) : FromNumeric<nint>(propertyInfo);
        }
        if (actualType == typeof(UIntPtr))
        {
            return isNullable ? FromNullableNumeric<nuint>(propertyInfo) : FromNumeric<nuint>(propertyInfo);
        }
        if (TypeAccess.IsIComparable(actualType, out bool isIComparableT) && isIComparableT)
        {
            return _targetType.IsValueType
                ? FromIComparableTOfValueType(_targetType, propertyInfo, actualType)
                : FromIComparableTOfReferenceType(_targetType, propertyInfo, actualType);
        }

        return FromObject(propertyInfo);
    }

    private PropertyComparison<object> FromObject(PropertyInfo propertyInfo)
    {
        Func<object, object> getter = TypeAccess.CreateUntypedPropertyGetter<object>(_targetType, propertyInfo);
        string propertyName = propertyInfo.Name;

        return Compare;

        int Compare(object x, object y, CompareInfo info)
        {
            // when a/b is value type, boxing/unboxing will exist
            object? a = getter(x);
            object? b = getter(y);
            if (a == null) return b == null ? 0 : -1;
            if (b == null) return 1;
            if (a is IComparable ia) return ia.CompareTo(b);
            throw new NotImplementedException(propertyName);
        }
    }

    private PropertyComparison<object> FromString(PropertyInfo propertyInfo)
    {
        Func<object, string> getter = TypeAccess.CreateUntypedPropertyGetter<string>(_targetType, propertyInfo);
        return Compare;

        int Compare(object x, object y, CompareInfo compareInfo)
        {
            string? a = getter(x);
            string? b = getter(y);
            if (a == null) return b == null ? 0 : -1;
            if (b == null) return 1;
            return compareInfo.Compare(a.AsSpan(), b.AsSpan());
        }
    }

    private PropertyComparison<object> FromNumeric<TNumeric>(PropertyInfo propertyInfo)
        where TNumeric : IComparisonOperators<TNumeric, TNumeric, bool>
    {
        Func<object, TNumeric> getter = TypeAccess.CreateUntypedPropertyGetter<TNumeric>(_targetType, propertyInfo);
        return Compare;

        int Compare(object x, object y, CompareInfo compareInfo)
        {
            // X : IComparisonOperators<X, X, bool>
            // when X is value type, null check will be meaningless
            TNumeric a = getter(x);
            TNumeric b = getter(y);
            if (a == null) return b == null ? 0 : -1;
            if (b == null) return 1;
            return a < b ? -1 : a > b ? 1 : 0;
        }
    }

    private PropertyComparison<object> FromNullableNumeric<TNumeric>(PropertyInfo propertyInfo)
        where TNumeric : struct, IComparisonOperators<TNumeric, TNumeric, bool>
    {
        Func<object, TNumeric?> getter = TypeAccess.CreateUntypedPropertyGetter<TNumeric?>(_targetType, propertyInfo);
        return Compare;

        int Compare(object x, object y, CompareInfo compareInfo)
        {
            // X? : IComparisonOperators<X?, X?, bool>
            TNumeric? na = getter(x);
            TNumeric? nb = getter(y);
            if (na == null) return nb == null ? 0 : -1;
            if (nb == null) return 1;
            var a = na.Value;
            var b = nb.Value;
            return a < b ? -1 : a > b ? 1 : 0;
        }
    }

    private PropertyComparison<object> FromBoolean(PropertyInfo propertyInfo)
    {
        Func<object, bool> getter = TypeAccess.CreateUntypedPropertyGetter<bool>(_targetType, propertyInfo);
        return Compare;

        int Compare(object x, object y, CompareInfo compareInfo)
        {
            bool a = getter(x);
            bool b = getter(y);
            return a == b ? 0 : (!a ? -1 : 1);
        }
    }

    private PropertyComparison<object> FromNullableBoolean(PropertyInfo propertyInfo)
    {
        Func<object, bool?> getter = TypeAccess.CreateUntypedPropertyGetter<bool?>(_targetType, propertyInfo);
        return Compare;

        int Compare(object x, object y, CompareInfo compareInfo)
        {
            bool? na = getter(x);
            bool? nb = getter(y);
            if (na == null) return nb == null ? 0 : -1;
            if (nb == null) return 1;
            var a = na.Value;
            var b = nb.Value;
            return a == b ? 0 : (!a ? -1 : 1);
        }
    }

    private PropertyComparison<object> FromDateTime(PropertyInfo propertyInfo)
    {
        Func<object, DateTime> getter = TypeAccess.CreateUntypedPropertyGetter<DateTime>(_targetType, propertyInfo);
        return Compare;

        int Compare(object x, object y, CompareInfo compareInfo)
        {
            DateTime a = getter(x);
            DateTime b = getter(y);
            return a < b ? -1 : a > b ? 1 : 0;
        }
    }

    private PropertyComparison<object> FromNullableDateTime(PropertyInfo propertyInfo)
    {
        Func<object, DateTime?> getter = TypeAccess.CreateUntypedPropertyGetter<DateTime?>(_targetType, propertyInfo);
        return Compare;

        int Compare(object x, object y, CompareInfo compareInfo)
        {
            DateTime? na = getter(x);
            DateTime? nb = getter(y);
            if (na == null) return nb == null ? 0 : -1;
            if (nb == null) return 1;
            var a = na.Value;
            var b = nb.Value;
            return a < b ? -1 : a > b ? 1 : 0;
        }
    }

    private PropertyComparison<object> FromEnum(PropertyInfo enumProperty, Type enumType)
    {
        Type underlyingType = Enum.GetUnderlyingType(enumType);
        
        if (underlyingType == typeof(int))
        {
            return GetEnumComparison<int>(enumProperty);
        }
        else if (underlyingType == typeof(uint))
        {
            return GetEnumComparison<uint>(enumProperty);
        }
        else if (underlyingType == typeof(sbyte))
        {
            return GetEnumComparison<sbyte>(enumProperty);
        }
        else if (underlyingType == typeof(byte))
        {
            return GetEnumComparison<byte>(enumProperty);
        }
        else if (underlyingType == typeof(short))
        {
            return GetEnumComparison<short>(enumProperty);
        }
        else if (underlyingType == typeof(ushort))
        {
            return GetEnumComparison<ushort>(enumProperty);
        }
        else if (underlyingType == typeof(long))
        {
            return GetEnumComparison<long>(enumProperty);
        }
        else
        {
            return GetEnumComparison<ulong>(enumProperty);
        }
    }

    private PropertyComparison<object> GetEnumComparison<TInteger>(PropertyInfo enumProperty)
        where TInteger : struct, IComparisonOperators<TInteger, TInteger, bool>
    {
        Func<object, TInteger> integerGetter = 
            TypeAccess.CreateEnumAsIntegerGetter<TInteger>(_targetType, enumProperty);
        
        return Compare;

        int Compare(object x, object y, CompareInfo compareInfo)
        {
            TInteger a = integerGetter(x);
            TInteger b = integerGetter(y);
            return a < b ? -1 : a > b ? 1 : 0;
        }
    }

    private PropertyComparison<object> FromNullableEnum(PropertyInfo enumProperty, Type enumType)
    {
        Type underlyingType = Enum.GetUnderlyingType(enumType);

        if (underlyingType == typeof(int))
        {
            return GetNullableEnumComparison<int>(enumProperty);
        }
        else if (underlyingType == typeof(uint))
        {
            return GetNullableEnumComparison<uint>(enumProperty);
        }
        else if (underlyingType == typeof(sbyte))
        {
            return GetNullableEnumComparison<sbyte>(enumProperty);
        }
        else if (underlyingType == typeof(byte))
        {
            return GetNullableEnumComparison<byte>(enumProperty);
        }
        else if (underlyingType == typeof(short))
        {
            return GetNullableEnumComparison<short>(enumProperty);
        }
        else if (underlyingType == typeof(ushort))
        {
            return GetNullableEnumComparison<ushort>(enumProperty);
        }
        else if (underlyingType == typeof(long))
        {
            return GetNullableEnumComparison<long>(enumProperty);
        }
        else
        {
            return GetNullableEnumComparison<ulong>(enumProperty);
        }
    }

    public PropertyComparison<object> GetNullableEnumComparison<TInteger>(PropertyInfo enumProperty)
        where TInteger : struct, IComparisonOperators<TInteger, TInteger, bool>
    {
        Func<object, TInteger?> integerGetter = 
            TypeAccess.CreateNullableEnumAsIntegerGetter<TInteger?>(_targetType, enumProperty);

        return Compare;

        int Compare(object x, object y, CompareInfo compareInfo)
        {
            TInteger? na = integerGetter(x);
            TInteger? nb = integerGetter(y);
            if (na == null) return nb == null ? 0 : -1;
            if (nb == null) return 1;
            var a = na.Value;
            var b = nb.Value;
            return a < b ? -1 : a > b ? 1 : 0;
        }
    }

    private PropertyComparison<object> FromIComparable<TComparable>(PropertyInfo propertyInfo)
        where TComparable : IComparable
    {
        Func<object, TComparable> getter = TypeAccess.CreateUntypedPropertyGetter<TComparable>(_targetType, propertyInfo);
        return Compare;

        int Compare(object x, object y, CompareInfo compareInfo)
        {
            TComparable a = getter(x);
            TComparable b = getter(y);
            if (a == null) return b == null ? 0 : -1;
            if (b == null) return 1;
            return a.CompareTo(b);
        }
    }

    private PropertyComparison<object> FromNullableIComparable<TComparable>(PropertyInfo propertyInfo)
        where TComparable : struct, IComparable
    {
        Func<object, TComparable?> getter = TypeAccess.CreateUntypedPropertyGetter<TComparable?>(_targetType, propertyInfo);
        return Compare;

        int Compare(object x, object y, CompareInfo compareInfo)
        {
            TComparable? na = getter(x);
            TComparable? nb = getter(y);
            if (na == null) return nb == null ? 0 : -1;
            if (nb == null) return 1;
            var a = na.Value;
            var b = nb.Value;
            return a.CompareTo(b);
        }
    }

    private PropertyComparison<object> FromIComparableT<TComparable>(PropertyInfo propertyInfo)
        where TComparable : IComparable<TComparable>
    {
        Func<object, TComparable> getter = TypeAccess.CreateUntypedPropertyGetter<TComparable>(_targetType, propertyInfo);
        return Compare;

        int Compare(object x, object y, CompareInfo compareInfo)
        {
            TComparable a = getter(x);
            TComparable b = getter(y);
            if (a == null) return b == null ? 0 : -1;
            if (b == null) return 1;
            return a.CompareTo(b);
        }
    }

    private PropertyComparison<object> FromNullableIComparableT<TComparable>(PropertyInfo propertyInfo)
        where TComparable : struct, IComparable<TComparable>
    {
        Func<object, TComparable?> getter = TypeAccess.CreateUntypedPropertyGetter<TComparable?>(_targetType, propertyInfo);
        return Compare;

        int Compare(object x, object y, CompareInfo compareInfo)
        {
            TComparable? na = getter(x);
            TComparable? nb = getter(y);
            if (na == null) return nb == null ? 0 : -1;
            if (nb == null) return 1;
            var a = na.Value;
            var b = nb.Value;
            return a.CompareTo(b);
        }
    }

    public static PropertyComparison<object> FromIComparableTOfReferenceType(Type type, PropertyInfo propertyInfo, Type compareType)
    {
        Type propertyType = propertyInfo.PropertyType;
        bool isNullable = propertyType != compareType;

        MethodInfo getPropertyValueMethod = propertyInfo.GetGetMethod(true);
        MethodInfo compareMethod = compareType.GetMethod("CompareTo", BindingFlags.Instance | BindingFlags.Public, [compareType]);

        DynamicMethod method = new($"_{type.FullName}_Comparison_{propertyType.FullName}_", typeof(int), [typeof(object), typeof(object), typeof(CompareInfo)], type, true);
        ILGenerator gen = method.GetILGenerator();
        if (isNullable)
        {
            MethodInfo hasValueMethod = propertyType.GetProperty("HasValue", BindingFlags.Instance | BindingFlags.Public).GetGetMethod(true);
            MethodInfo getValueMethod = propertyType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public).GetGetMethod(true);

            Label label1 = gen.DefineLabel();
            Label label2 = gen.DefineLabel();
            Label label3 = gen.DefineLabel();

            gen.DeclareLocal(propertyType);
            gen.DeclareLocal(propertyType);
            gen.DeclareLocal(compareType);
            gen.DeclareLocal(compareType);

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Castclass, type);
            gen.Emit(OpCodes.Callvirt, getPropertyValueMethod);
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Castclass, type);
            gen.Emit(OpCodes.Callvirt, getPropertyValueMethod);
            gen.Emit(OpCodes.Stloc_1);
            gen.Emit(OpCodes.Ldloca_S, 0);
            gen.Emit(OpCodes.Call, hasValueMethod);
            gen.Emit(OpCodes.Brtrue_S, label2);

            gen.Emit(OpCodes.Ldloca_S, 1);
            gen.Emit(OpCodes.Call, hasValueMethod);
            gen.Emit(OpCodes.Brfalse_S, label1);

            gen.Emit(OpCodes.Ldc_I4_M1);
            gen.Emit(OpCodes.Ret);

            gen.MarkLabel(label1);
            gen.Emit(OpCodes.Ldc_I4_0);
            gen.Emit(OpCodes.Ret);

            gen.MarkLabel(label2);
            gen.Emit(OpCodes.Ldloca_S, 1);
            gen.Emit(OpCodes.Call, hasValueMethod);
            gen.Emit(OpCodes.Brtrue_S, label3);

            gen.Emit(OpCodes.Ldc_I4_1);
            gen.Emit(OpCodes.Ret);

            gen.MarkLabel(label3);
            gen.Emit(OpCodes.Ldloca_S, 0);
            gen.Emit(OpCodes.Call, getValueMethod);
            gen.Emit(OpCodes.Stloc_2);
            gen.Emit(OpCodes.Ldloca_S, 1);
            gen.Emit(OpCodes.Call, getValueMethod);
            gen.Emit(OpCodes.Stloc_3);
            gen.Emit(OpCodes.Ldloca_S, 2);
            gen.Emit(OpCodes.Ldloc_3);
            gen.Emit(OpCodes.Call, compareMethod);
            gen.Emit(OpCodes.Ret);
        }
        else if (propertyType.IsValueType)
        {
            gen.DeclareLocal(propertyType);
            gen.DeclareLocal(propertyType);

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Castclass, type);
            gen.Emit(OpCodes.Callvirt, getPropertyValueMethod);
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Castclass, type);
            gen.Emit(OpCodes.Callvirt, getPropertyValueMethod);
            gen.Emit(OpCodes.Stloc_1);
            gen.Emit(OpCodes.Ldloca_S, 0);
            gen.Emit(OpCodes.Ldloc_1);
            gen.Emit(OpCodes.Call, compareMethod);
            gen.Emit(OpCodes.Ret);
        }
        else
        {
            Label label1 = gen.DefineLabel();
            Label label2 = gen.DefineLabel();

            gen.DeclareLocal(propertyType);
            gen.DeclareLocal(propertyType);

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Castclass, type);
            gen.Emit(OpCodes.Callvirt, getPropertyValueMethod);
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Castclass, type);
            gen.Emit(OpCodes.Callvirt, getPropertyValueMethod);
            gen.Emit(OpCodes.Stloc_1);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Brtrue_S, label2);

            gen.Emit(OpCodes.Ldloc_1);
            gen.Emit(OpCodes.Brfalse_S, label1);

            gen.Emit(OpCodes.Ldc_I4_M1);
            gen.Emit(OpCodes.Ret);

            gen.MarkLabel(label1);
            gen.Emit(OpCodes.Ldc_I4_0);
            gen.Emit(OpCodes.Ret);

            gen.MarkLabel(label2);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Ldloc_1);
            gen.Emit(OpCodes.Callvirt, compareMethod);
            gen.Emit(OpCodes.Ret);
        }

        return method.CreateDelegate<PropertyComparison<object>>();
    }

    public static PropertyComparison<object> FromIComparableTOfValueType(Type type, PropertyInfo propertyInfo, Type compareType)
    {
        Type propertyType = propertyInfo.PropertyType;
        bool isNullable = propertyType != compareType;

        MethodInfo getPropertyValueMethod = propertyInfo.GetGetMethod(true);
        MethodInfo compareMethod = compareType.GetMethod("CompareTo", BindingFlags.Instance | BindingFlags.Public, [compareType]);

        DynamicMethod method = new($"_{type.FullName}_Comparison_{propertyType.FullName}_", typeof(int), [typeof(object), typeof(object), typeof(CompareInfo)], type, true);
        ILGenerator gen = method.GetILGenerator();
        if (isNullable)
        {
            MethodInfo hasValueMethod = propertyType.GetProperty("HasValue", BindingFlags.Instance | BindingFlags.Public).GetGetMethod(true);
            MethodInfo getValueMethod = propertyType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public).GetGetMethod(true);

            Label label1 = gen.DefineLabel();
            Label label2 = gen.DefineLabel();
            Label label3 = gen.DefineLabel();

            gen.DeclareLocal(propertyType);
            gen.DeclareLocal(propertyType);
            gen.DeclareLocal(compareType);
            gen.DeclareLocal(compareType);
            gen.DeclareLocal(type);

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Unbox_Any, type);
            gen.Emit(OpCodes.Stloc_S, 4);
            gen.Emit(OpCodes.Ldloca_S, 4);
            gen.Emit(OpCodes.Call, getPropertyValueMethod);
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Unbox_Any, type);
            gen.Emit(OpCodes.Stloc_S, 4);
            gen.Emit(OpCodes.Ldloca_S, 4);
            gen.Emit(OpCodes.Call, getPropertyValueMethod);
            gen.Emit(OpCodes.Stloc_1);
            gen.Emit(OpCodes.Ldloca_S, 0);
            gen.Emit(OpCodes.Call, hasValueMethod);
            gen.Emit(OpCodes.Brtrue_S, label2);

            gen.Emit(OpCodes.Ldloca_S, 1);
            gen.Emit(OpCodes.Call, hasValueMethod);
            gen.Emit(OpCodes.Brfalse_S, label1);

            gen.Emit(OpCodes.Ldc_I4_M1);
            gen.Emit(OpCodes.Ret);

            gen.MarkLabel(label1);
            gen.Emit(OpCodes.Ldc_I4_0);
            gen.Emit(OpCodes.Ret);

            gen.MarkLabel(label2);
            gen.Emit(OpCodes.Ldloca_S, 1);
            gen.Emit(OpCodes.Call, hasValueMethod);
            gen.Emit(OpCodes.Brtrue_S, label3);

            gen.Emit(OpCodes.Ldc_I4_1);
            gen.Emit(OpCodes.Ret);

            gen.MarkLabel(label3);
            gen.Emit(OpCodes.Ldloca_S, 0);
            gen.Emit(OpCodes.Call, getValueMethod);
            gen.Emit(OpCodes.Stloc_2);
            gen.Emit(OpCodes.Ldloca_S, 1);
            gen.Emit(OpCodes.Call, getValueMethod);
            gen.Emit(OpCodes.Stloc_3);
            gen.Emit(OpCodes.Ldloca_S, 2);
            gen.Emit(OpCodes.Ldloc_3);
            gen.Emit(OpCodes.Call, compareMethod);
            gen.Emit(OpCodes.Ret);
        }
        else if (propertyType.IsValueType)
        {
            gen.DeclareLocal(propertyType);
            gen.DeclareLocal(propertyType);
            gen.DeclareLocal(type);

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Unbox_Any, type);
            gen.Emit(OpCodes.Stloc_2);
            gen.Emit(OpCodes.Ldloca_S, 2);
            gen.Emit(OpCodes.Call, getPropertyValueMethod);
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Unbox_Any, type);
            gen.Emit(OpCodes.Stloc_2);
            gen.Emit(OpCodes.Ldloca_S, 2);
            gen.Emit(OpCodes.Call, getPropertyValueMethod);
            gen.Emit(OpCodes.Stloc_1);
            gen.Emit(OpCodes.Ldloca_S, 0);
            gen.Emit(OpCodes.Ldloc_1);
            gen.Emit(OpCodes.Call, compareMethod);
            gen.Emit(OpCodes.Ret);
        }
        else
        {
            Label label1 = gen.DefineLabel();
            Label label2 = gen.DefineLabel();

            gen.DeclareLocal(propertyType);
            gen.DeclareLocal(propertyType);
            gen.DeclareLocal(type);

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Unbox_Any, type);
            gen.Emit(OpCodes.Stloc_2);
            gen.Emit(OpCodes.Ldloca_S, 2);
            gen.Emit(OpCodes.Call, getPropertyValueMethod);
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Unbox_Any, type);
            gen.Emit(OpCodes.Stloc_2);
            gen.Emit(OpCodes.Ldloca_S, 2);
            gen.Emit(OpCodes.Call, getPropertyValueMethod);
            gen.Emit(OpCodes.Stloc_1);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Brtrue_S, label2);

            gen.Emit(OpCodes.Ldloc_1);
            gen.Emit(OpCodes.Brfalse_S, label1);

            gen.Emit(OpCodes.Ldc_I4_M1);
            gen.Emit(OpCodes.Ret);

            gen.MarkLabel(label1);
            gen.Emit(OpCodes.Ldc_I4_0);
            gen.Emit(OpCodes.Ret);

            gen.MarkLabel(label2);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Ldloc_1);
            gen.Emit(OpCodes.Callvirt, compareMethod);
            gen.Emit(OpCodes.Ret);
        }

        return method.CreateDelegate<PropertyComparison<object>>();
    }
}
