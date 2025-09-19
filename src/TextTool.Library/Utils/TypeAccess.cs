using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TextTool.Library.Utils;

public delegate ref TField RefField<TInstance, TField>(TInstance obj = default);
public delegate ref TField RefBindingField<TInstance, TField>();
public delegate ref int RefEnumField<TInstance>(TInstance obj = default);
public delegate ref int RefBindingEnumField<TInstance>();
public delegate object UntypedConstructor(params object[] args);

public static class TypeAccess
{
    // ---------------------------------
    // Type Search Helper
    // ---------------------------------

    public static Type? GetType(string fullName, bool throwOnError = false)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        Type? type;
        for (int i = 0; i < assemblies.Length; i++)
        {
            type = assemblies[i].GetType(fullName, throwOnError);
            if (type != null)
            {
                return type;
            }
        }

        for (int i = 0; i < assemblies.Length; i++)
        {
            Type[] types = assemblies[i].GetTypes();
            for (int j = 0; j < types.Length; j++)
            {
                type = types[j];
                if (type.FullName == fullName)
                {
                    return type;
                }
            }
        }

        if (throwOnError)
        {
            throw new FileNotFoundException($"Cannot find the type '{fullName}'");
        }

        return null;
    }

    // ---------------------------------
    // Public Property Helpers
    // ---------------------------------

    public static PropertyInfo? GetPublicProperty(Type type, string propertyName)
    {
        foreach (PropertyInfo info in GetPublicProperties(type))
        {
            if (info.Name == propertyName)
            {
                return info;
            }
        }
        return null;
    }

    public static PropertyInfo[] GetPublicProperties(Type type)
    {
        if (!_PublicPropertyTable.TryGetValue(type, out PropertyInfo[]? properties))
        {
            properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            _PublicPropertyTable[type] = properties;
        }
        return properties;
    }

    // ---------------------------------
    // Typed Field Delegates
    // ---------------------------------

    public static Func<TInstance, TField> CreateFieldGetter<TInstance, TField>(string fieldName)
    {
        FieldInfo? info = typeof(TInstance).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        if (info == null)
        {
            throw new ArgumentException($"Cannot find field '{fieldName}' in type '{typeof(TInstance)}'");
        }

        return CreateFieldGetter<TInstance, TField>(info);
    }

    public static Func<TInstance, TField> CreateFieldGetter<TInstance, TField>(FieldInfo info)
    {
        Type targetType = typeof(TInstance);
        Type fieldType = typeof(TField);

        DynamicMethod method = new(
            $"_{targetType.FullName}_get_{info.Name}_{fieldType.FullName}_", fieldType, [targetType], targetType, true);

        ILGenerator gen = method.GetILGenerator();
        if (info.IsStatic)
        {
            gen.Emit(OpCodes.Ldsfld, info);
            gen.Emit(OpCodes.Ret);
        }
        else
        {
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, info);
            gen.Emit(OpCodes.Ret);
        }

        return method.CreateDelegate<Func<TInstance, TField>>();
    }

    public static RefField<TInstance, TField> CreateRefFieldGetter<TInstance, TField>(string fieldName)
    {
        FieldInfo? info = typeof(TInstance).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        if (info == null)
        {
            throw new ArgumentException($"Cannot find field '{fieldName}' in type '{typeof(TInstance)}'");
        }

        return CreateRefFieldGetter<TInstance, TField>(info);
    }

    public static RefField<TInstance, TField> CreateRefFieldGetter<TInstance, TField>(FieldInfo info)
    {
        if (info.IsInitOnly)
        {
            throw new ArgumentException($"A readonly field '{info.Name}' cannot be returned by a ref type");
        }

        Type type = typeof(TInstance);
        Type fieldType = typeof(TField);

        DynamicMethod method = new(
            $"_{type.FullName}_ref_{fieldType.FullName}_{info.Name}_", fieldType.MakeByRefType(), [type], type, true);

        ILGenerator gen = method.GetILGenerator();
        if (info.IsStatic)
        {
            gen.Emit(OpCodes.Ldsflda, info);
            gen.Emit(OpCodes.Ret);
        }
        else
        {
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldflda, info);
            gen.Emit(OpCodes.Ret);
        }

        return method.CreateDelegate<RefField<TInstance, TField>>();
    }

    public static RefBindingField<TInstance, TField> CreateRefBindingFieldGetter<TInstance, TField>(string fieldName, TInstance target)
    {
        FieldInfo? info = typeof(TInstance).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        if (info == null)
        {
            throw new ArgumentException($"Cannot find field '{fieldName}' in type '{typeof(TInstance)}'");
        }

        return CreateRefBindingFieldGetter<TInstance, TField>(info, target);
    }

    public static RefBindingField<TInstance, TField> CreateRefBindingFieldGetter<TInstance, TField>(FieldInfo info, TInstance target)
    {
        if (info.IsInitOnly)
        {
            throw new ArgumentException($"A readonly field '{info.Name}' cannot be returned by a ref type");
        }

        Type type = typeof(TInstance);
        Type fieldType = typeof(TField);

        DynamicMethod method = new(
            $"_{type.FullName}_ref_{fieldType.FullName}_{info.Name}_", fieldType.MakeByRefType(), [type], type, true);

        ILGenerator gen = method.GetILGenerator();
        gen.Emit(OpCodes.Ldarg_0);
        gen.Emit(OpCodes.Ldflda, info);
        gen.Emit(OpCodes.Ret);

        return method.CreateDelegate<RefBindingField<TInstance, TField>>(target);
    }

    // ---------------------------------
    // Typed Enum Field Delegates
    //
    // Note: does not support eum types with an underlying type of Int64 / UInt64
    // ---------------------------------

    public static RefEnumField<TInstance> CreateRefEnumFieldGetter<TInstance>(string enumFieldName)
    {
        FieldInfo? field = typeof(TInstance).GetField(
            enumFieldName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        if (field == null)
        {
            throw new ArgumentException($"Cannot find field '{enumFieldName}' in type '{typeof(TInstance)}'");
        }

        return CreateRefEnumFieldGetter<TInstance>(field);
    }

    public static RefEnumField<TInstance> CreateRefEnumFieldGetter<TInstance>(FieldInfo info)
    {
        if (info.IsInitOnly)
        {
            throw new ArgumentException($"A readonly field '{info.Name}' cannot be returned by a ref type");
        }

        Type targetType = typeof(TInstance);
        Type enumType = info.FieldType;

        if (!enumType.IsEnum)
        {
            throw new ArgumentOutOfRangeException(info.Name, "Field is not Enum");
        }

        MethodInfo enumAsRefInt = UnsafeAsRef.MakeGenericMethod([enumType, typeof(int)]);
        DynamicMethod method = new($"_{targetType.FullName}_ref_{RefIntType.FullName}_{info.Name}_", RefIntType, [targetType], targetType, true);
        ILGenerator gen = method.GetILGenerator();
        if (info.IsStatic)
        {
            gen.Emit(OpCodes.Ldsflda, info);
            gen.Emit(OpCodes.Call, enumAsRefInt);
            gen.Emit(OpCodes.Ret);
        }
        else
        {
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldflda, info);
            gen.Emit(OpCodes.Call, enumAsRefInt);
            gen.Emit(OpCodes.Ret);
        }

        return method.CreateDelegate<RefEnumField<TInstance>>();
    }

    public static RefBindingEnumField<TInstance> CreateRefBindingEnumFieldGetter<TInstance>(string enumFieldName, TInstance target)
    {
        FieldInfo? field = typeof(TInstance).GetField(
            enumFieldName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);

        if (field == null)
        {
            throw new ArgumentException($"Cannot find field '{enumFieldName}' in type '{typeof(TInstance)}'");
        }

        return CreateRefBindingEnumFieldGetter(field, target);
    }

    public static RefBindingEnumField<TInstance> CreateRefBindingEnumFieldGetter<TInstance>(FieldInfo info, TInstance target)
    {
        if (info.IsInitOnly)
        {
            throw new ArgumentException($"A readonly field '{info.Name}' cannot be returned by a ref type");
        }

        Type targetType = typeof(TInstance);
        Type enumType = info.FieldType;

        if (!enumType.IsEnum)
        {
            throw new ArgumentOutOfRangeException(info.Name, "Field is not Enum");
        }

        MethodInfo enumAsRefInt = UnsafeAsRef.MakeGenericMethod([enumType, typeof(int)]);
        DynamicMethod method = new($"_{targetType.FullName}_ref_{RefIntType.FullName}_{info.Name}_", RefIntType, [targetType], targetType, true);
        ILGenerator gen = method.GetILGenerator();
        gen.Emit(OpCodes.Ldarg_0);
        gen.Emit(OpCodes.Ldflda, info);
        gen.Emit(OpCodes.Call, enumAsRefInt);
        gen.Emit(OpCodes.Ret);

        return method.CreateDelegate<RefBindingEnumField<TInstance>>(target);
    }

    // ---------------------------------
    // Typed Property Delegates
    // ---------------------------------

    public static Func<TTarget, TReturn> CreateTypedPropertyGetter<TTarget, TReturn>(PropertyInfo propertyInfo)
    {
        return (Func<TTarget, TReturn>)Delegate.CreateDelegate(typeof(Func<TTarget, TReturn>), propertyInfo.GetGetMethod(true));
    }

    public static Action<TTarget, TProperty> CreateTypedPropertySetter<TTarget, TProperty>(PropertyInfo propertyInfo)
    {
        return (Action<TTarget, TProperty>)Delegate.CreateDelegate(typeof(Action<TTarget, TProperty>), propertyInfo.GetSetMethod(true));
    }

    // ---------------------------------
    // Untyped Property Delegates
    // ---------------------------------

    public static Func<object, TReturn> CreateUntypedPropertyGetter<TReturn>(Type type, PropertyInfo propertyInfo)
    {
        if (CreateUntypedPropertyGetterByDynamicMethod<TReturn>(type, propertyInfo) is Func<object, TReturn> getter)
        {
            return getter;
        }

        Type returnType = typeof(TReturn);
        ParameterExpression target = Expression.Parameter(typeof(object));
        UnaryExpression castedTarget = Expression.Convert(target, type);
        MethodCallExpression property = Expression.Call(castedTarget, propertyInfo.GetGetMethod());
        UnaryExpression castedProperty = Expression.Convert(property, returnType);
        return Expression.Lambda<Func<object, TReturn>>(castedProperty, target).Compile();
    }

    private static Func<object, TReturn>? CreateUntypedPropertyGetterByDynamicMethod<TReturn>(Type type, PropertyInfo propertyInfo)
    {
        Type propertyType = propertyInfo.PropertyType;
        Type returnType = typeof(TReturn);
        bool boxing;

        if (propertyType == returnType)
        {
            boxing = false;
        }
        else if (propertyType.IsValueType && returnType == typeof(object))
        {
            boxing = true;
        }
        else
        {
            return null;
        }

        MethodInfo getMethod = propertyInfo.GetGetMethod(true);
        DynamicMethod method = new($"_{type.FullName}_{returnType.FullName}_{getMethod.Name}_", returnType, [typeof(object)], type, true);
        ILGenerator gen = method.GetILGenerator();
        if (type.IsValueType)
        {
            gen.DeclareLocal(type);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Unbox_Any, type);
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Ldloca_S, 0);
            gen.Emit(OpCodes.Call, getMethod);
        }
        else
        {
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Castclass, type);
            gen.Emit(OpCodes.Callvirt, getMethod);
        }
        if (boxing)
        {
            gen.Emit(OpCodes.Box, propertyType);
        }
        gen.Emit(OpCodes.Ret);

        return method.CreateDelegate<Func<object, TReturn>>();
    }

    // ---------------------------------
    // Typed Enum Delegate Helpers
    // ---------------------------------

    // (target) => (integer)target.EnumProperty
    public static Func<object, TInteger> CreateEnumAsIntegerGetter<TInteger>(Type type, PropertyInfo enumProperty)
    {
        Type underlyingType = typeof(TInteger);
        MethodInfo enumGetMethod = enumProperty.GetGetMethod(true);
        DynamicMethod method = new($"_{type.FullName}_{underlyingType.FullName}_{enumGetMethod.Name}_", underlyingType, [typeof(object)], type, true);
        ILGenerator gen = method.GetILGenerator();
        if (type.IsValueType)
        {
            gen.DeclareLocal(type);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Unbox_Any, type);
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Ldloca_S, 0);
            gen.Emit(OpCodes.Call, enumGetMethod);
        }
        else
        {
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Castclass, type);
            gen.Emit(OpCodes.Callvirt, enumGetMethod);
        }
        gen.Emit(OpCodes.Ret);

        return method.CreateDelegate<Func<object, TInteger>>();
    }

    // (target) => (integer?)target.EnumProperty?
    public static Func<object, TNullableInteger> CreateNullableEnumAsIntegerGetter<TNullableInteger>(Type type, PropertyInfo propertyInfo)
    {
        Type propertyType = propertyInfo.PropertyType;
        Type returnType = typeof(TNullableInteger);
        if (!_NullableIntegerTable.TryGetValue(returnType, out ConstructorInfo? returnTypeCtor))
        {
            throw new ArgumentException($"'{returnType}' is not a valid nullable integer type for enum");
        }

        MethodInfo enumGetMethod = propertyInfo.GetGetMethod(true);
        MethodInfo hasValueMethod = propertyType.GetProperty("HasValue", BindingFlags.Instance | BindingFlags.Public).GetGetMethod(true);
        MethodInfo getValueMethod = propertyType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public).GetGetMethod(true);
        DynamicMethod method = new($"_EnumGetter_{type.FullName}_{returnType.FullName}_{enumGetMethod.Name}_", returnType, [typeof(object)], type, true);
        ILGenerator gen = method.GetILGenerator();
        Label hasValueCase = gen.DefineLabel();
        if (type.IsValueType)
        {
            gen.DeclareLocal(type);
            gen.DeclareLocal(propertyType);
            gen.DeclareLocal(returnType);

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Unbox_Any, type);
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Ldloca_S, 0);
            gen.Emit(OpCodes.Call, enumGetMethod);
            gen.Emit(OpCodes.Stloc_1);
            gen.Emit(OpCodes.Ldloca_S, 1);
            gen.Emit(OpCodes.Call, hasValueMethod);
            gen.Emit(OpCodes.Brtrue_S, hasValueCase);

            // HasValue == false
            gen.Emit(OpCodes.Ldloca_S, 2);
            gen.Emit(OpCodes.Initobj, returnType);
            gen.Emit(OpCodes.Ldloc_2);
            gen.Emit(OpCodes.Ret);

            // HasValue == true
            gen.MarkLabel(hasValueCase);
            gen.Emit(OpCodes.Ldloca_S, 1);
        }
        else
        {
            gen.DeclareLocal(propertyType);
            gen.DeclareLocal(returnType);

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Castclass, type);
            gen.Emit(OpCodes.Callvirt, enumGetMethod);
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Ldloca_S, 0);
            gen.Emit(OpCodes.Call, hasValueMethod);
            gen.Emit(OpCodes.Brtrue_S, hasValueCase);

            // HasValue == false
            gen.Emit(OpCodes.Ldloca_S, 1);
            gen.Emit(OpCodes.Initobj, returnType);
            gen.Emit(OpCodes.Ldloc_1);
            gen.Emit(OpCodes.Ret);

            // HasValue == true
            gen.MarkLabel(hasValueCase);
            gen.Emit(OpCodes.Ldloca_S, 0);
        }
        gen.Emit(OpCodes.Call, getValueMethod);
        gen.Emit(OpCodes.Newobj, returnTypeCtor);
        gen.Emit(OpCodes.Ret);

        return method.CreateDelegate<Func<object, TNullableInteger>>();
    }

    

    // ---------------------------------
    // Untyped Constructor Delegates
    // ---------------------------------

    public static object CreateInstance(ConstructorInfo info, params object[] args)
    {
        Type type = info.ReflectedType;
        if (!_UntypedConstructorDelegateTable.TryGetValue(type.FullName, out Delegate? constructor))
        {
            constructor = CreateConstructorDelegate(info);
            _UntypedConstructorDelegateTable[type.FullName] = constructor;
        }
        return ((UntypedConstructor)constructor)(args);
    }

    public static UntypedConstructor CreateConstuctorDelegate(Type type, params Type[] parameterTypes)
    {
        ConstructorInfo? info = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, parameterTypes);

        if (info == null)
        {
            throw new ArgumentException($"Cannot find type constructor matching the parameter types {parameterTypes}");
        }

        return BuildDefaultConstructor(type, info, parameterTypes);
    }

    public static UntypedConstructor CreateConstructorDelegate(ConstructorInfo info)
    {
        return BuildDefaultConstructor(info.ReflectedType, info, info.GetParameters().Select(p => p.ParameterType).ToArray());
    }

    private static UntypedConstructor BuildDefaultConstructor(Type type, ConstructorInfo info, Type[] paramaterTypes)
    {
        Type[] paramTypes = [typeof(object[])];
        DynamicMethod method = new($"_{type.FullName}_ctor_{info.Name}_{paramTypes}_", typeof(object), paramTypes, type, true);
        ILGenerator gen = method.GetILGenerator();

        for (int i = 0; i < paramaterTypes.Length; i++)
        {
            gen.Emit(OpCodes.Ldarg_0);
            switch (i)
            {
                case 0: gen.Emit(OpCodes.Ldc_I4_0); break;
                case 1: gen.Emit(OpCodes.Ldc_I4_1); break;
                case 2: gen.Emit(OpCodes.Ldc_I4_2); break;
                case 3: gen.Emit(OpCodes.Ldc_I4_3); break;
                case 4: gen.Emit(OpCodes.Ldc_I4_4); break;
                case 5: gen.Emit(OpCodes.Ldc_I4_5); break;
                case 6: gen.Emit(OpCodes.Ldc_I4_6); break;
                case 7: gen.Emit(OpCodes.Ldc_I4_7); break;
                case 8: gen.Emit(OpCodes.Ldc_I4_8); break;
                default: gen.Emit(OpCodes.Ldc_I4, i); break;
            }
            gen.Emit(OpCodes.Ldelem_Ref);
            Type paramType = paramaterTypes[i];
            gen.Emit(paramType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, paramType);
        }
        gen.Emit(OpCodes.Newobj, info);
        gen.Emit(OpCodes.Ret);

        return method.CreateDelegate<UntypedConstructor>();
    }

    // ---------------------------------
    // Typed Constructor Delegates
    // ---------------------------------

    public static TConstuctor CreateConstuctorDelegate<TConstuctor>(Type type) where TConstuctor : Delegate
    {
        MethodInfo invoke = typeof(TConstuctor).GetMethod("Invoke");
        Type returnType = invoke.ReturnType;
        if (!returnType.IsAssignableFrom(type))
        {
            throw new ArgumentException($"Return type [{returnType}] of {typeof(TConstuctor)} is not compatible to {type}");
        }

        Type[] paramTypes = invoke.GetParameters().Select(p => p.ParameterType).ToArray();
        if (paramTypes.Length > 256)
        {
            throw new ArgumentException($"{typeof(TConstuctor)} must not have more than 256 parameters");
        }

        ConstructorInfo? ctor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, paramTypes);

        if (ctor == null)
        {
            throw new ArgumentException($"Cannot find type constructor matching the signature of {typeof(TConstuctor)}");
        }

        DynamicMethod method = new($"_{type.FullName}_ctor_{ctor.Name}_{paramTypes}_", returnType, paramTypes, type, true);
        ILGenerator gen = method.GetILGenerator();
        for (int i = 0; i < paramTypes.Length; i++)
        {
            switch (i)
            {
                case 0: gen.Emit(OpCodes.Ldarg_0); break;
                case 1: gen.Emit(OpCodes.Ldarg_1); break;
                case 2: gen.Emit(OpCodes.Ldarg_2); break;
                case 3: gen.Emit(OpCodes.Ldarg_3); break;
                default: gen.Emit(OpCodes.Ldarg_S, i); break;
            }
        }
        gen.Emit(OpCodes.Newobj, ctor);
        gen.Emit(OpCodes.Ret);

        return method.CreateDelegate<TConstuctor>();
    }

    public static TMethod CreateMethodDelegate<TMethod>(Type type, object? target, string methodName, params Type[] parameterTypes) where TMethod : Delegate
    {
        if (type.GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            parameterTypes)
            is not MethodInfo methodInfo)
        {
            throw new ArgumentException($"[{methodName}] of [{type}] not found");
        }

        return CreateMethodDelegate<TMethod>(methodInfo, target);
    }

    // ---------------------------------
    // Typed Method Delegates
    // ---------------------------------

    // Creating a wrapper delegate <TMethod> to call a specified target method of <MethodInfo>,
    //
    // The wrapper delegate <TMethod> will cast all parameters to the types of parameters of target method as following:
    //
    // TReturn WrapperMethod(TParam0 param0, TParam1 param1, TParam2 param2...)
    // {
    //    return TargetMethod((TParamA)param0, (TParamB)param1, (TParamC)param2...);
    // }
    //
    // The TargetMethod corresponds to <MethodInfo>.
    // 'out' modifier and generic parameters are not supported in <TMethod>.
    // The return type <TReturn> must be assignable from the return type of <MethodInfo>.
    //
    // The optional target object is used as the caller of the wrapper delegate.
    // If <MethodInfo> represents a static method, the target object is not needed.
    // If <MethodInfo> represents a non-static method and the target object is not null, the wrapper delegate will be only bound to the target object.
    // If <MethodInfo> represents a non-static method and the target object is null, <TMethod> must specify the target object as the first parameter.
    public static TMethod CreateMethodDelegate<TMethod>(MethodInfo methodInfo, object? target) where TMethod : Delegate
    {
        MethodInfo delegeteInfo = typeof(TMethod).GetMethod("Invoke")!;
        
        if (delegeteInfo.IsGenericMethod || methodInfo.IsGenericMethod)
        {
            throw new ArgumentException($"Generic method is not supported");
        }

        if (methodInfo.DeclaringType is not Type methodDeclaringType)
        {
            throw new ArgumentException($"Global module method is not supported");
        }

        Type delegateReturnType = delegeteInfo.ReturnType;
        Type methodReturnType = methodInfo.ReturnType;

        if (!delegateReturnType.IsAssignableFrom(methodReturnType))
        {
            throw new ArgumentException($"Method return type [{methodReturnType}] must be derived from the delegate return type [{delegateReturnType}]");
        }

        ParameterInfo[] delegateParameters = delegeteInfo.GetParameters();
        ParameterInfo[] methodParameters = methodInfo.GetParameters();

        Type? targetType;
        bool isStaticMethod = false;
        bool isOpenInstanceMethod = false;
        bool isClosedInstanceMethod = false;
        
        if (methodInfo.IsStatic)
        {
            targetType = null;
            isStaticMethod = true;

            if (delegateParameters.Length != methodParameters.Length)
            {
                throw new ArgumentException($"[{delegeteInfo}] and {methodInfo} must have same numbers of parameters");
            }
            // TReturn StaticMethod(params) => DynamicMethod(params) + OPCodes.Call
            // CreateDelegate<TMethod>()
        }
        else
        {
            targetType = methodDeclaringType;

            if (target == null)
            {
                isOpenInstanceMethod = true;
                if (delegateParameters.Length < 1)
                {
                    throw new ArgumentException($"Instance type [{methodInfo.DeclaringType}] of method must be the first parameter of delegate");
                }

                if (!delegateParameters[0].ParameterType.IsAssignableFrom(targetType))
                {
                    throw new ArgumentException($"Instance type [{delegateParameters[0].ParameterType}] is not compatible with [{targetType}] of method");
                }

                if (delegateParameters.Length - 1 != methodParameters.Length)
                {
                    throw new ArgumentException($"[{delegeteInfo}] and {methodInfo} must have same numbers of parameters");
                }

                // TReturn InstanceMethod(TInstance, params) => DynamicMethod(TInstance, params) + OPCodes.Callvirt
                // CreateDelegate<TMethod>()
            }
            else
            {
                isClosedInstanceMethod = true;
                if (delegateParameters.Length != methodParameters.Length)
                {
                    throw new ArgumentException($"[{delegeteInfo}] and {methodInfo} must have same numbers of parameters");
                }
                // TReturn InstanceMethod(params) => DynamicMethod(methodInfo.DeclaredType, params) + OPCodes.Callvirt
                // CreateDelegate<TMethod>(target)
            }
        }

        int start;
        int offset;
        int paramsLength;
        Type[] delegateParamTypes;
        Type[] methodParamTypes;

        if (isStaticMethod)
        {
            start = 0;
            offset = 0;
            paramsLength = methodParameters.Length;
            delegateParamTypes = new Type[paramsLength];
            methodParamTypes = new Type[paramsLength];
        }
        else
        {
            start = 1;
            offset = isOpenInstanceMethod ? 1 : 0;
            paramsLength = methodParameters.Length + 1;
            delegateParamTypes = new Type[paramsLength];
            delegateParamTypes[0] = delegateParameters[0].ParameterType;
            methodParamTypes = new Type[paramsLength];
            methodParamTypes[0] = targetType!;
        }

        for (int i = start; i < paramsLength; i++)
        {
            Type delegateParamType = delegateParameters[i - start + offset].ParameterType;
            Type methodParamType = methodParameters[i - start].ParameterType;
            if (delegateParamType.IsAssignableFrom(methodParamType))
            {
                delegateParamTypes[i] = delegateParamType;
                methodParamTypes[i] = methodParamType;
            }
            else
            {
                throw new ArgumentException($"Method parameter [{methodParamType}] must be derived from the delegate parameter [{delegateParamType}]");
            }
        }

        Type declaringType = methodInfo.DeclaringType;
        DynamicMethod method = new($"_{declaringType.FullName}_{methodReturnType.FullName}_{methodInfo.Name}_", delegeteInfo.ReturnType, delegateParamTypes, methodInfo.DeclaringType, true);
        ILGenerator gen = method.GetILGenerator();

        for (int i = 0; i < paramsLength; i++)
        {
            switch (i)
            {
                case 0: gen.Emit(OpCodes.Ldarg_0); break;
                case 1: gen.Emit(OpCodes.Ldarg_1); break;
                case 2: gen.Emit(OpCodes.Ldarg_2); break;
                case 3: gen.Emit(OpCodes.Ldarg_3); break;
                default: gen.Emit(OpCodes.Ldarg_S, i); break;
            }

            Type methodParamType = methodParamTypes[i];
            if (methodParamType != delegateParamTypes[i])
            {
                gen.Emit(methodParamType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, methodParamType);
            }
        }
        gen.Emit(isStaticMethod ? OpCodes.Call : OpCodes.Callvirt, methodInfo);

        if (delegateReturnType != methodReturnType)
        {
            gen.Emit(methodReturnType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, methodReturnType);
        }
        gen.Emit(OpCodes.Ret);

        return isClosedInstanceMethod ? method.CreateDelegate<TMethod>(target) : method.CreateDelegate<TMethod>();
    }

    // ---------------------------------
    // Generic Type Argument Helper
    // ---------------------------------

    public static IEnumerable<Type[]> EnumerateTypeArgumentsOfGenericInterface(Type currentType, Type interfaceType)
    {
        if (!interfaceType.IsInterface)
        {
            throw new ArgumentOutOfRangeException(nameof(interfaceType), "not an interface type");
        }

        if (!interfaceType.IsGenericType)
        {
            // return IEnumerable<Type[]>.Empty
            yield break;
        }

        if (!interfaceType.IsGenericTypeDefinition)
        {
            interfaceType = interfaceType.GetGenericTypeDefinition();
        }
        
        foreach (Type it in currentType.GetInterfaces())
        {
            if (it.IsGenericType && it.GetGenericTypeDefinition() == interfaceType)
            {
                // current interface type is a generic type so that the type arguments must be a non-empty Type[]
                yield return it.GetGenericArguments();
            }
        }
    }

    // ---------------------------------
    // IComparable/IComparable<T> Helper
    // ---------------------------------

    public static bool IsIComparable(Type type, out bool isIComparableT)
    {
        Type[] interfaces = type.GetInterfaces();
        
        foreach (Type it in interfaces)
        {
            if (it.IsGenericType && it.GetGenericTypeDefinition() == typeof(IComparable<>) && it.GetGenericArguments()[0] == type)
            {
                isIComparableT = true;
                return true;
            }
        }

        foreach (Type it in interfaces)
        {
            if (it == typeof(IComparable))
            {
                isIComparableT = false;
                return true;
            }
        }

        isIComparableT = false;
        return false;
    }

    // ---------------------------------
    // IComparisonOperators Helper
    // ---------------------------------

    public static bool IsIComparisonOperators(Type type)
    {
        if (!_IsIComparisonOperatorsTypeTable.TryGetValue(type, out bool support))
        {
            support = false;
            foreach (Type interfaceType in type.GetInterfaces())
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IComparisonOperators<,,>))
                {
                    Type[] args = interfaceType.GetGenericArguments();
                    if (args[0] == type && args[1] == type && args[2] == typeof(bool))
                    {
                        support = true;
                    }
                }
            }
            _IsIComparisonOperatorsTypeTable[type] = support;
        }

        return support;
    }

    public static bool HasIComparisonOperators(Type type)
    {
        if (IsIComparisonOperators(type))
        {
            return true;
        }
        
        if (!_HasIComparisonOperatorsTypeTable.TryGetValue(type, out bool support))
        {
            Type[] param = [type, type];
            BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            support = false;

            if (type.GetMethod("op_GreaterThan", flags, param) is MethodInfo greaterThanMethod
                && greaterThanMethod.IsSpecialName && greaterThanMethod.ReturnType != typeof(bool))
            {
                support = true;
            }
            _HasIComparisonOperatorsTypeTable[type] = support;
        }

        return support;
    }

    

    // ---------------------------------
    // NonPublic Member Helpers
    // ---------------------------------

    public static MethodInfo? GetNonPublicMethod(Type type, string methodName, params Type[] parameterTypes)
    {
        return type.GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic,
            Type.DefaultBinder,
            parameterTypes ?? [],
            null
        );
    }

    public static PropertyInfo? GetNonPublicProperty(Type type, string propertyName, Type? propertyType)
    {
        return type.GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.NonPublic,
            Type.DefaultBinder,
            propertyType,
            [],
            null
        );
    }

    // ref int
    private static readonly Type RefIntType = typeof(int).MakeByRefType();

    // Unsafe.As<TFrom, TTo>(TFrom)
    private static readonly MethodInfo UnsafeAsRef = typeof(Unsafe).GetMethod(
        "As",
        2,
        BindingFlags.Static | BindingFlags.Public,
        [Type.MakeGenericMethodParameter(0).MakeByRefType()])!;

    private static readonly ConcurrentDictionary<string, Delegate> _UntypedConstructorDelegateTable = [];
    private static readonly Dictionary<Type, PropertyInfo[]> _PublicPropertyTable = [];
    private static readonly Dictionary<Type, ConstructorInfo> _NullableIntegerTable = new()
    {
        { typeof(sbyte?),  typeof(sbyte?).GetConstructor([typeof(sbyte)])   },
        { typeof(byte?),   typeof(byte?).GetConstructor([typeof(byte)])     },
        { typeof(short?),  typeof(short?).GetConstructor([typeof(short)])   },
        { typeof(ushort?), typeof(ushort?).GetConstructor([typeof(ushort)]) },
        { typeof(int?),    typeof(int?).GetConstructor([typeof(int)])       },
        { typeof(uint?),   typeof(uint?).GetConstructor([typeof(uint)])     },
        { typeof(long?),   typeof(long?).GetConstructor([typeof(long)])     },
        { typeof(ulong?),  typeof(ulong?).GetConstructor([typeof(ulong)])   },
    };

    private static readonly Dictionary<Type, bool> _HasIComparisonOperatorsTypeTable = new()
    {
        { typeof(DateTime), true }
    };

    private static readonly Dictionary<Type, bool> _IsIComparisonOperatorsTypeTable = new()
    {
        { typeof(char), true },
        { typeof(byte), true },
        { typeof(sbyte), true },
        { typeof(short), true },
        { typeof(ushort), true },
        { typeof(int), true },
        { typeof(uint), true },
        { typeof(long), true },
        { typeof(ulong), true },
        { typeof(float), true },
        { typeof(double), true },
        { typeof(decimal), true },
        { typeof(Int128), true },
        { typeof(UInt128), true },
        { typeof(BigInteger), true },
        { typeof(Half), true },
        { typeof(NFloat), true },
        { typeof(IntPtr), true },
        { typeof(UIntPtr), true },
    };
}