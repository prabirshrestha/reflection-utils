﻿//-----------------------------------------------------------------------
// <copyright file="ReflectionUtils.cs" company="The Outercurve Foundation">
//    Copyright (c) 2011, The Outercurve Foundation. 
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// <author>Prabir Shrestha (prabir.me)</author>
// <website>https://github.com/facebook-csharp-sdk/reflection-utils</website>
//-----------------------------------------------------------------------

// uncomment the following line to make ReflectionUtils public
//#define REFLECTION_UTILS_PUBLIC

// uncomment the following line for .NET 2.0, .NET 3.0, WP7.0 and XBox360 where Lambda.Compile() is not supported. SL4+, WP7.1+ and .NET 3.5+ supports Lambda.Compile();
//#define REFLECTION_UTILS_NO_LINQ_EXPRESSION

// note: by default GetNewInstanceViaReflectionEmit will fail in medium trust, for optimal solution use GetNewInstance which will fallback to GetNewInstanceViaReflection if running under medium trust.
// if Lambda.Compile is available it will use Lambda.Compile() instead of reflection.
// uncomment the following line for .NET 2.0 and .NET 3.0 only. For .NET 3.5+ use Lambda.Compile()
//#define REFLECTION_UTILS_REFLECTION_EMIT

#if SILVERLIGHT || WINDOWS_PHONE || NETFX_CORE
#undef REFLECTION_UTILS_REFLECTION_EMIT
#endif

#if NETFX_CORE
#define REFLECTION_UTILS_TYPEINFO
#endif

// ReSharper disable CheckNamespace
namespace ReflectionUtils
// ReSharper restore CheckNamespace
{
    using System;
    using System.Collections.Generic;
#if !REFLECTION_UTILS_NO_LINQ_EXPRESSION
    using System.Linq.Expressions;
#endif
    using System.Reflection;
#if REFLECTION_UTILS_REFLECTION_EMIT
    using System.Reflection.Emit;
#endif

#if REFLECTION_UTILS_PUBLIC
     public
#else
    internal
#endif
 delegate object ConstructorDelegate(params object[] args);

#if REFLECTION_UTILS_PUBLIC
     public
#else
    internal
#endif
 delegate object GetDelegate(object source);

#if REFLECTION_UTILS_PUBLIC
     public
#else
    internal
#endif
 delegate void SetDelegate(object source, object value);

#if REFLECTION_UTILS_PUBLIC
     public
#else
    internal
#endif
 delegate object MethodDelegate(object source, params object[] args);

#if REFLECTION_UTILS_PUBLIC
     public
#else
    internal
#endif
 delegate TValue ThreadSafeDictionaryValueFactory<TKey, TValue>(TKey key);

#if REFLECTION_UTILS_PUBLIC
     public
#else
    internal
#endif
 delegate GetDelegate GetDelegateFactory(MemberInfo memberInfo);

#if REFLECTION_UTILS_PUBLIC
     public
#else
    internal
#endif
 delegate SetDelegate SetDelegateFactory(MemberInfo memberInfo);

#if REFLECTION_UTILS_PUBLIC
     public
#else
    internal
#endif
 static class ReflectionUtils
    {
        public static readonly Type[] EmptyTypes = new Type[] { };
        private static readonly object[] EmptyObjects = new object[] { };

#if REFLECTION_UTILS_REFLECTION_EMIT

        public static readonly bool CanSafelyUseUseReflectionEmit;

        static ReflectionUtils()
        {
            try
            {
                // try creating a new object by Reflection.Emit first to see if we have enough permission.
                // coz Refleciton.Emit fails in medium trust.
                object dummyObj = GetConstructorByReflectionEmit(typeof(DummyClassForReflectionEmitTest), EmptyTypes)();
                if (dummyObj != null)
                    CanSafelyUseUseReflectionEmit = true;
            }
            catch
            {
            }
        }
#endif

        public static IEnumerable<ConstructorInfo> GetConstructors(Type type)
        {
#if REFLECTION_UTILS_TYPEINFO
            return type.GetTypeInfo().DeclaredConstructors;
#else
            return type.GetConstructors();
#endif
        }

        public static ConstructorInfo GetConstructorInfo(Type type, params Type[] argsType)
        {
            IEnumerable<ConstructorInfo> constructorInfos = GetConstructors(type);
            int i;
            bool matches;
            foreach (ConstructorInfo constructorInfo in constructorInfos)
            {
                ParameterInfo[] parameters = constructorInfo.GetParameters();
                if (argsType.Length != parameters.Length)
                    continue;

                i = 0;
                matches = true;
                foreach (ParameterInfo parameterInfo in constructorInfo.GetParameters())
                {
                    if (parameterInfo.ParameterType != argsType[i])
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                    return constructorInfo;
            }

            return null;
        }

        public static ThreadSafeDictionary<ConstructorKey, ConstructorDelegate> CreateConstructorCacheForReflection()
        {
            return new ThreadSafeDictionary<ConstructorKey, ConstructorDelegate>(delegate(ConstructorKey key) { return GetConstructorByReflection(key.Type, key.ArgsType); });
        }

        public static ConstructorDelegate GetConstructor(ThreadSafeDictionary<ConstructorKey, ConstructorDelegate> cache, Type type, params Type[] argsType)
        {
            return cache.Get(new ConstructorKey(type, argsType));
        }

        public static ConstructorDelegate GetConstructorByReflection(ConstructorInfo constructorInfo)
        {
            return delegate(object[] args) { return constructorInfo.Invoke(args); };
        }

        public static ConstructorDelegate GetConstructorByReflection(Type type, params Type[] argsType)
        {
            ConstructorInfo constructorInfo = GetConstructorInfo(type, argsType);
            return constructorInfo == null ? null : GetConstructorByReflection(constructorInfo);
        }

#if !REFLECTION_UTILS_NO_LINQ_EXPRESSION

        public static ThreadSafeDictionary<ConstructorKey, ConstructorDelegate> CreateConstructorCacheForCompiledLambda()
        {
            return new ThreadSafeDictionary<ConstructorKey, ConstructorDelegate>(delegate(ConstructorKey key) { return GetConstructorByCompiledLambda(key.Type, key.ArgsType); });
        }

        public static ConstructorDelegate GetConstructorByCompiledLambda(ConstructorInfo constructorInfo)
        {
            ParameterInfo[] paramsInfo = constructorInfo.GetParameters();
            ParameterExpression param = Expression.Parameter(typeof(object[]), "args");
            Expression[] argsExp = new Expression[paramsInfo.Length];
            for (int i = 0; i < paramsInfo.Length; i++)
            {
                Expression index = Expression.Constant(i);
                Type paramType = paramsInfo[i].ParameterType;
                Expression paramAccessorExp = Expression.ArrayIndex(param, index);
                Expression paramCastExp = Expression.Convert(paramAccessorExp, paramType);
                argsExp[i] = paramCastExp;
            }
            NewExpression newExp = Expression.New(constructorInfo, argsExp);
            Expression<Func<object[], object>> lambda = Expression.Lambda<Func<object[], object>>(newExp, param);
            Func<object[], object> compiledLambda = lambda.Compile();
            return delegate(object[] args) { return compiledLambda(args); };
        }

        public static ConstructorDelegate GetConstructorByCompiledLambda(Type type, params Type[] argsType)
        {
            ConstructorInfo constructorInfo = GetConstructorInfo(type, argsType);
            return constructorInfo == null ? null : GetConstructorByCompiledLambda(constructorInfo);
        }
#endif

#if REFLECTION_UTILS_REFLECTION_EMIT

        private static readonly Type[] TypeofObjectArray = new Type[] { typeof(object) };

        public static ThreadSafeDictionary<ConstructorKey, ConstructorDelegate> CreateConstructorCacheForReflectionEmit()
        {
            return CreateConstructorCacheForReflectionEmit(false);
        }

        public static ThreadSafeDictionary<ConstructorKey, ConstructorDelegate> CreateConstructorCacheForReflectionEmit(bool forceReflectionEmit)
        {
            if (!forceReflectionEmit)
            {
                if (!CanSafelyUseUseReflectionEmit)
                    return CreateConstructorCacheForReflection();
            }

            return new ThreadSafeDictionary<ConstructorKey, ConstructorDelegate>(delegate(ConstructorKey key) { return GetConstructorByReflectionEmit(key.Type, key.ArgsType); });
        }

        public static ConstructorDelegate GetConstructorByReflectionEmit(Type type, params Type[] argsType)
        {
            ConstructorDelegate ctor = null;
            DynamicMethod dynamicMethod = new DynamicMethod("ctor_DynamicMethod" + type.FullName, typeof(object), TypeofObjectArray, typeof(ReflectionUtils));
            ILGenerator generator = dynamicMethod.GetILGenerator();

            bool canCreate = true;
            bool hasRefParams = false;
            foreach (Type paramTypes in argsType)
            {
                if (paramTypes.IsByRef)
                    hasRefParams = true;
            }

            if (type.IsValueType && argsType == EmptyTypes)
            {
                generator.DeclareLocal(type);
                generator.Emit(OpCodes.Localloc, 0);
                generator.Emit(OpCodes.Initobj, type);
                generator.Emit(OpCodes.Ldloc_0);
            }
            else if (type.IsArray)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldc_I4_0);
                generator.Emit(OpCodes.Ldelem_Ref);
                generator.Emit(OpCodes.Unbox_Any, typeof(int));
                generator.Emit(OpCodes.Newarr, type.GetElementType());
            }
            else
            {
                ConstructorInfo constructorInfo = GetConstructorInfo(type, argsType);
                if (constructorInfo == null)
                {
                    canCreate = false;
                }
                else
                {
                    byte startUsableLocalIndex = 0;

                    if (hasRefParams)
                    {
                        startUsableLocalIndex = CreateLocalsForByRefParams(generator, 0, constructorInfo, argsType);
                        generator.DeclareLocal(type);
                    }

                    PushParamsOrLocalsToStack(generator, 0, argsType);
                    generator.Emit(OpCodes.Newobj, constructorInfo);

                    if (hasRefParams)
                    {
                        if (startUsableLocalIndex >= byte.MinValue && startUsableLocalIndex <= byte.MaxValue)
                        {
                            if (startUsableLocalIndex == 0)
                                generator.Emit(OpCodes.Stloc_0);
                            else if (startUsableLocalIndex == 1)
                                generator.Emit(OpCodes.Stloc_1);
                            else if (startUsableLocalIndex == 2)
                                generator.Emit(OpCodes.Stloc_2);
                            else if (startUsableLocalIndex == 3)
                                generator.Emit(OpCodes.Stloc_3);
                            else
                                generator.Emit(OpCodes.Stloc_S, startUsableLocalIndex);
                        }

                        byte currentByRefParams = 0;
                        for (int i = 0; i < argsType.Length; i++)
                        {
                            Type argType = argsType[i];
                            if (argType.IsByRef)
                            {
                                generator.Emit(OpCodes.Ldarg_0);
                                generator.Emit(OpCodes.Ldc_I4, i);
                                generator.Emit(OpCodes.Ldloc, currentByRefParams++);
                                if (argType.IsValueType)
                                    generator.Emit(OpCodes.Box, argType);
                                generator.Emit(OpCodes.Stelem_Ref);
                            }
                        }

                        generator.Emit(OpCodes.Ldloc, startUsableLocalIndex);
                    }
                }
            }

            if (canCreate)
            {
                if (type.IsValueType)
                    generator.Emit(OpCodes.Box, type);
                generator.Emit(OpCodes.Ret);
                ctor = (ConstructorDelegate)dynamicMethod.CreateDelegate(typeof(ConstructorDelegate));
            }

            return ctor;
        }

        private static byte CreateLocalsForByRefParams(ILGenerator generator, byte paramArrayIndex, ConstructorInfo constructorInfo, Type[] argsType)
        {
            byte numberOfByRefParams = 0;
            ParameterInfo[] parameters = constructorInfo.GetParameters();
            for (int i = 0; i < argsType.Length; i++)
            {
                Type paramType = argsType[i];
                if (paramType.IsByRef)
                {
                    Type type = paramType.GetElementType();
                    generator.DeclareLocal(type);
                    if (!parameters[i].IsOut)
                    {
                        generator.Emit(OpCodes.Ldarg, paramArrayIndex);
                        generator.Emit(OpCodes.Ldc_I4, i);
                        generator.Emit(OpCodes.Ldelem_Ref);
                        if (paramType != typeof(object))
                            generator.Emit(paramType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, paramType);

                        if (numberOfByRefParams >= byte.MinValue && numberOfByRefParams <= byte.MaxValue)
                        {
                            if (numberOfByRefParams == 0)
                                generator.Emit(OpCodes.Stloc_0);
                            else if (numberOfByRefParams == 1)
                                generator.Emit(OpCodes.Stloc_1);
                            else if (numberOfByRefParams == 2)
                                generator.Emit(OpCodes.Stloc_2);
                            else if (numberOfByRefParams == 3)
                                generator.Emit(OpCodes.Stloc_3);
                            else
                                generator.Emit(OpCodes.Stloc_S, paramArrayIndex);
                        }
                        else
                        {
                            generator.Emit(OpCodes.Stloc, numberOfByRefParams);
                        }
                    }
                    numberOfByRefParams++;
                }
            }
            return numberOfByRefParams;
        }

        private static void PushParamsOrLocalsToStack(ILGenerator generator, int paramArrayIndex, Type[] argsType)
        {
            byte currentByRefParam = 0;
            for (int i = 0; i < argsType.Length; i++)
            {
                Type paramType = argsType[i];
                if (paramType.IsByRef)
                {
                    generator.Emit(OpCodes.Ldloca_S, currentByRefParam++);
                }
                else
                {
                    if (paramArrayIndex == 0)
                        generator.Emit(OpCodes.Ldarg_0);
                    else if (paramArrayIndex == 1)
                        generator.Emit(OpCodes.Ldarg_1);
                    else if (paramArrayIndex == 2)
                        generator.Emit(OpCodes.Ldarg_2);
                    else if (paramArrayIndex == 3)
                        generator.Emit(OpCodes.Ldarg_3);
                    else
                    {
                        if (paramArrayIndex <= byte.MaxValue)
                            generator.Emit(OpCodes.Ldarg_S, (byte)paramArrayIndex);
                        else if (paramArrayIndex <= short.MaxValue)
                            generator.Emit(OpCodes.Ldarg, (short)paramArrayIndex);
                        else
                            throw new ArgumentOutOfRangeException("paramArrayIndex");
                    }
                    generator.Emit(OpCodes.Ldc_I4, i);
                    generator.Emit(OpCodes.Ldelem_Ref);
                    if (paramType != typeof(object))
                    {
                        generator.Emit(paramType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, paramType);
                    }
                }
            }
        }

        private class DummyClassForReflectionEmitTest
        {
        }

#endif
        public static bool IsValueType(Type type)
        {
#if NETFX_CORE
            return type.GetTypeInfo().IsValueType;
#else
            return type.IsValueType;
#endif
        }

        public static IEnumerable<PropertyInfo> GetProperties(Type type)
        {
#if REFLECTION_UTILS_TYPEINFO
            return type.GetTypeInfo().DeclaredProperties;
#else
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
#endif
        }

        public static IEnumerable<FieldInfo> GetFields(Type type)
        {
#if REFLECTION_UTILS_TYPEINFO
            return type.GetTypeInfo().DeclaredFields;
#else
            return type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
#endif
        }

        public static MethodInfo GetGetMethodInfo(PropertyInfo propertyInfo)
        {
#if REFLECTION_UTILS_TYPEINFO
            return propertyInfo.GetMethod;
#else
            return propertyInfo.GetGetMethod(true);
#endif
        }

        public static MethodInfo GetSetMethodInfo(PropertyInfo propertyInfo)
        {
#if NETFX_CORE
            return propertyInfo.SetMethod;
#else
            return propertyInfo.GetSetMethod(true);
#endif
        }

        public static ThreadSafeDictionary<MemberInfoKey, GetDelegate> CreateGetMethodForMemberInfoCacheForReflection()
        {
            return new ThreadSafeDictionary<MemberInfoKey, GetDelegate>(
                delegate(MemberInfoKey key)
                {
                    return key.IsProperty ? GetGetMethodByReflection(key.MemberInfo as PropertyInfo): GetGetMethodByReflection(key.MemberInfo as FieldInfo);
                });
        }

        public static GetDelegate GetGetMethod(ThreadSafeDictionary<MemberInfoKey, GetDelegate> cache, MemberInfo propertyInfo)
        {
            return cache.Get(new MemberInfoKey(propertyInfo));
        }

        public static GetDelegate GetGetMethodByReflection(PropertyInfo propertyInfo)
        {
            MethodInfo methodInfo = GetGetMethodInfo(propertyInfo);
            return delegate(object source) { return methodInfo.Invoke(source, EmptyObjects); };
        }

        public static GetDelegate GetGetMethodByReflection(FieldInfo fieldInfo)
        {
            return delegate(object source) { return fieldInfo.GetValue(source); };
        }

#if !REFLECTION_UTILS_NO_LINQ_EXPRESSION
        public static ThreadSafeDictionary<MemberInfoKey, GetDelegate> CreateGetMethodForMemberInfoCacheForCompiledLambda()
        {
            return new ThreadSafeDictionary<MemberInfoKey, GetDelegate>(
                delegate(MemberInfoKey key)
                {
                    return key.IsProperty ? GetGetMethodByCompiledLambda(key.MemberInfo as PropertyInfo) : GetGetMethodByCompiledLambda(key.MemberInfo as FieldInfo);
                });
        }

        public static GetDelegate GetGetMethodByCompiledLambda(PropertyInfo propertyInfo)
        {
            MethodInfo getMethodInfo = GetGetMethodInfo(propertyInfo);
            var instance = Expression.Parameter(typeof(object), "instance");
            UnaryExpression instanceCast = (!IsValueType(propertyInfo.DeclaringType)) ? Expression.TypeAs(instance, propertyInfo.DeclaringType) : Expression.Convert(instance, propertyInfo.DeclaringType);
            var compiled = Expression.Lambda<Func<object, object>>(Expression.TypeAs(Expression.Call(instanceCast, getMethodInfo), typeof(object)), instance).Compile();
            return delegate(object source) { return compiled(source); };
        }

        public static GetDelegate GetGetMethodByCompiledLambda(FieldInfo fieldInfo)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var member = Expression.Field(Expression.Convert(instance, fieldInfo.DeclaringType), fieldInfo);
            var compiled = Expression.Lambda<GetDelegate>(Expression.Convert(member, typeof(object)), instance).Compile();
            return delegate(object source) { return compiled(source); };
        }

        public static ThreadSafeDictionary<MemberInfoKey, SetDelegate> CreateSetMethodForMemberInfoCacheForCompiledLambda()
        {
            return new ThreadSafeDictionary<MemberInfoKey, SetDelegate>(
                delegate(MemberInfoKey key)
                {
                    return key.IsProperty ? GetSetMethodByCompiledLambda(key.MemberInfo as PropertyInfo) : GetSetMethodByCompiledLambda(key.MemberInfo as FieldInfo);
                });
        }

        public static SetDelegate GetSetMethodByCompiledLambda(PropertyInfo propertyInfo)
        {
            MethodInfo setMethodInfo = GetSetMethodInfo(propertyInfo);
            var instance = Expression.Parameter(typeof(object), "instance");
            var value = Expression.Parameter(typeof(object), "value");
            UnaryExpression instanceCast = (!IsValueType(propertyInfo.DeclaringType)) ? Expression.TypeAs(instance, propertyInfo.DeclaringType) : Expression.Convert(instance, propertyInfo.DeclaringType);
            UnaryExpression valueCast = (!IsValueType(propertyInfo.PropertyType)) ? Expression.TypeAs(value, propertyInfo.PropertyType) : Expression.Convert(value, propertyInfo.PropertyType);
            var compiled = Expression.Lambda<Action<object, object>>(Expression.Call(instanceCast, setMethodInfo, valueCast), new ParameterExpression[] { instance, value }).Compile();
            return delegate(object source, object val) { compiled(source, val); };
        }

        public static SetDelegate GetSetMethodByCompiledLambda(FieldInfo fieldInfo)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var value = Expression.Parameter(typeof(object), "value");
            var compiled = Expression.Lambda<Action<object, object>>(
                Assign(Expression.Field(Expression.Convert(instance, fieldInfo.DeclaringType),fieldInfo),Expression.Convert(value, fieldInfo.FieldType)),instance,value).Compile();
            return delegate(object source, object val) { compiled(source, val); };
        }

        public static BinaryExpression Assign(Expression left, Expression right)
        {
#if NETFX_CORE
            return Expression.Assign(left, right);
#else
            var assign = typeof(Assigner<>).MakeGenericType(left.Type).GetMethod("Assign");
            var assignExpr = Expression.Add(left, right, assign);
            return assignExpr;
#endif
        }

        private static class Assigner<T>
        {
            public static T Assign(ref T left, T right)
            {
                return (left = right);
            }
        }
#endif

#if REFLECTION_UTILS_REFLECTION_EMIT

        public static ThreadSafeDictionary<MemberInfoKey, GetDelegate> CreateGetMethodForMemberInfoCacheForReflectionEmit()
        {
            return CreateGetMethodForMemberInfoCacheForReflectionEmit(false);
        }

        public static ThreadSafeDictionary<MemberInfoKey, GetDelegate> CreateGetMethodForMemberInfoCacheForReflectionEmit(bool forceReflectionEmit)
        {
            if (!forceReflectionEmit)
            {
                if (!CanSafelyUseUseReflectionEmit)
                    return CreateGetMethodForMemberInfoCacheForReflection();
            }

            return new ThreadSafeDictionary<MemberInfoKey, GetDelegate>(
                delegate(MemberInfoKey key)
                {
                    return key.IsProperty ? GetGetMethodByReflectionEmit(key.MemberInfo as PropertyInfo) : GetGetMethodByReflectionEmit(key.MemberInfo as FieldInfo);
                });
        }

        public static GetDelegate GetGetMethodByReflectionEmit(PropertyInfo propertyInfo)
        {
            MethodInfo getMethodInfo = GetGetMethodInfo(propertyInfo);
            Type type = propertyInfo.PropertyType;
            DynamicMethod dynamicMethod = new DynamicMethod("get_DynamicMethod" + type.FullName, propertyInfo.DeclaringType, TypeofObjectArray, propertyInfo.DeclaringType);
            ILGenerator getGenerator = dynamicMethod.GetILGenerator();
            getGenerator.Emit(OpCodes.Ldarg_0);
            getGenerator.Emit(OpCodes.Call, getMethodInfo);
            if (type.IsValueType)
                getGenerator.Emit(OpCodes.Box, type);
            getGenerator.Emit(OpCodes.Ret);
            return (GetDelegate)dynamicMethod.CreateDelegate(typeof(GetDelegate));
        }

        public static GetDelegate GetGetMethodByReflectionEmit(FieldInfo fieldInfo)
        {
            Type type = fieldInfo.FieldType;
            DynamicMethod dynamicGet = new DynamicMethod("get_DynamicMethod" + type.FullName, fieldInfo.DeclaringType, TypeofObjectArray, fieldInfo.DeclaringType);
            ILGenerator getGenerator = dynamicGet.GetILGenerator();
            getGenerator.Emit(OpCodes.Ldarg_0);
            getGenerator.Emit(OpCodes.Ldfld, fieldInfo);
            if (type.IsValueType)
                getGenerator.Emit(OpCodes.Box, type);
            getGenerator.Emit(OpCodes.Ret);
            return (GetDelegate)dynamicGet.CreateDelegate(typeof(GetDelegate));
        }

        public static ThreadSafeDictionary<MemberInfoKey, SetDelegate> CreateSetMethodForMemberInfoCacheForReflectionEmit()
        {
            return CreateSetMethodForMemberInfoCacheForReflectionEmit(false);
        }

        public static ThreadSafeDictionary<MemberInfoKey, SetDelegate> CreateSetMethodForMemberInfoCacheForReflectionEmit(bool forceReflectionEmit)
        {
            if (!forceReflectionEmit)
            {
                if (!CanSafelyUseUseReflectionEmit)
                    return CreateSetMethodForMemberInfoCacheForReflection();
            }

            return new ThreadSafeDictionary<MemberInfoKey, SetDelegate>(
                delegate(MemberInfoKey key)
                {
                    return key.IsProperty ? GetSetMethodByReflectionEmit(key.MemberInfo as PropertyInfo) : GetSetMethodByReflectionEmit(key.MemberInfo as FieldInfo);
                });
        }

        public static SetDelegate GetSetMethodByReflectionEmit(PropertyInfo propertyInfo)
        {
            MethodInfo setMethodInfo = GetSetMethodInfo(propertyInfo);
            Type type = propertyInfo.PropertyType;
            DynamicMethod dynamicMethod = CreateDynamicMethod("set_DynamicMethod_" + propertyInfo.Name, null, new Type[] { typeof(object), typeof(object) }, propertyInfo.DeclaringType);
            ILGenerator setGenerator = dynamicMethod.GetILGenerator();
            setGenerator.Emit(OpCodes.Ldarg_0);
            setGenerator.Emit(OpCodes.Ldarg_1);
            if (type.IsValueType)
                setGenerator.Emit(OpCodes.Unbox_Any, type);
            setGenerator.Emit(OpCodes.Call, setMethodInfo);
            setGenerator.Emit(OpCodes.Ret);
            return (SetDelegate)dynamicMethod.CreateDelegate(typeof(SetDelegate));
        }

        public static SetDelegate GetSetMethodByReflectionEmit(FieldInfo fieldInfo)
        {
            if (fieldInfo.IsInitOnly || fieldInfo.IsLiteral)
                return null;
            Type type = fieldInfo.FieldType;
            DynamicMethod dynamicMethod = CreateDynamicMethod("Set" + fieldInfo.Name, null, new Type[] { typeof(object), typeof(object) }, fieldInfo.DeclaringType);
            ILGenerator setGenerator = dynamicMethod.GetILGenerator();
            setGenerator.Emit(OpCodes.Ldarg_0);
            setGenerator.Emit(OpCodes.Ldarg_1);
            if (type.IsValueType)
                setGenerator.Emit(OpCodes.Unbox_Any, type);
            setGenerator.Emit(OpCodes.Stfld, fieldInfo);
            setGenerator.Emit(OpCodes.Ret);

            return (SetDelegate)dynamicMethod.CreateDelegate(typeof(SetDelegate));
        }

        private static DynamicMethod CreateDynamicMethod(string name, Type returnType, Type[] parameterTypes, Type owner)
        {
            return !owner.IsInterface ? new DynamicMethod(name, returnType, parameterTypes, owner, true) : new DynamicMethod(name, returnType, parameterTypes, (Module)null, true);
        }

#endif

        public static ThreadSafeDictionary<MemberInfoKey, SetDelegate> CreateSetMethodForMemberInfoCacheForReflection()
        {
            return new ThreadSafeDictionary<MemberInfoKey, SetDelegate>(
                 delegate(MemberInfoKey key)
                 {
                     return key.IsProperty ? GetSetMethodByReflection(key.MemberInfo as PropertyInfo) : GetSetMethodByReflection(key.MemberInfo as FieldInfo);
                 });
        }

        public static SetDelegate GetSetMethod(ThreadSafeDictionary<MemberInfoKey, SetDelegate> cache, MemberInfo propertyInfo)
        {
            return cache.Get(new MemberInfoKey(propertyInfo));
        }

        public static SetDelegate GetSetMethodByReflection(PropertyInfo propertyInfo)
        {
            MethodInfo methodInfo = GetSetMethodInfo(propertyInfo);
            return delegate(object source, object value) { methodInfo.Invoke(source, new object[] { value }); };
        }

        public static SetDelegate GetSetMethodByReflection(FieldInfo fieldInfo)
        {
            return delegate(object source, object value) { fieldInfo.SetValue(source, value); };
        }

        public sealed class ThreadSafeDictionary<TKey, TValue>
        {
            private readonly object _lock = new object();
            private readonly ThreadSafeDictionaryValueFactory<TKey, TValue> _valueFactory;
            private Dictionary<TKey, TValue> _dictionary;

            public ThreadSafeDictionary(ThreadSafeDictionaryValueFactory<TKey, TValue> valueFactory)
            {
                _valueFactory = valueFactory;
            }

            public TValue Get(TKey key)
            {
                if (_dictionary == null)
                    return AddValue(key);
                TValue value;
                if (!_dictionary.TryGetValue(key, out value))
                    return AddValue(key);
                return value;
            }

            private TValue AddValue(TKey key)
            {
                TValue value = _valueFactory(key);
                lock (_lock)
                {
                    if (_dictionary == null)
                    {
                        _dictionary = new Dictionary<TKey, TValue>();
                        _dictionary[key] = value;
                    }
                    else
                    {
                        TValue val;
                        if (_dictionary.TryGetValue(key, out val))
                            return val;
                        Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>(_dictionary);
                        dict[key] = value;
                        _dictionary = dict;
                    }
                }
                return value;
            }
        }

        public struct ConstructorKey : IEquatable<ConstructorKey>
        {
            private readonly Type _type;
            private readonly Type[] _argsType;

            public Type Type { get { return _type; } }
            public Type[] ArgsType { get { return _argsType; } }

            public ConstructorKey(Type type, params Type[] argsType)
            {
                _type = type;
                _argsType = argsType;
            }

            public override int GetHashCode()
            {
                int hash = 13;
                hash = (hash * 7) + _type.GetHashCode();
                hash = (hash * 7) + _argsType.GetHashCode();
                return hash;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is ConstructorKey))
                    return false;
                return Equals((ConstructorKey)obj);
            }

            public bool Equals(ConstructorKey obj)
            {
                return _type == obj._type && _argsType == obj._argsType;
            }
        }

        public struct MemberInfoKey : IEquatable<MemberInfoKey>
        {
            private readonly MemberInfo _memberInfo;
            private readonly Type _declaredType;
            private readonly string _name;
            private readonly Type _type;
            private readonly bool _isField;
            private readonly bool _isProperty;
            private readonly bool _canRead;
            private readonly bool _canWrite;

            public MemberInfo MemberInfo { get { return _memberInfo; } }
            public bool CanRead { get { return _canRead; } }
            public bool IsProperty { get { return _isProperty; } }
            public bool IsField { get { return _isField; } }

            public MemberInfoKey(MemberInfo memberInfo)
            {
                _memberInfo = memberInfo;
                _declaredType = memberInfo.DeclaringType;
                _name = memberInfo.Name;
                _type = null;
                _canRead = false;
                _canWrite = false;
                FieldInfo fieldInfo = memberInfo as FieldInfo;
                if (fieldInfo != null)
                {
                    _isField = true;
                    _type = fieldInfo.FieldType;
                    _canRead = true;
                    _canWrite = !fieldInfo.IsInitOnly;
                }
                else
                {
                    _isField = false;
                }

                PropertyInfo propertyInfo = memberInfo as PropertyInfo;
                if (propertyInfo != null)
                {
                    _isProperty = true;
                    _type = propertyInfo.PropertyType;
                    _canRead = propertyInfo.CanRead;
                    _canWrite = propertyInfo.CanWrite;
                }
                else
                {
                    _isProperty = false;
                }

                if (_type == null)
                    throw new NotSupportedException("MemberInfo not supported");
            }

            public override int GetHashCode()
            {
                int hash = 13;
                hash = (hash * 7) + _declaredType.GetHashCode();
                hash = (hash * 7) + _name.GetHashCode();
                hash = (hash * 7) + _type.GetHashCode();
                hash = (hash * 7) + _isField.GetHashCode();
                hash = (hash * 7) + _isProperty.GetHashCode();
                hash = (hash * 7) + _canRead.GetHashCode();
                hash = (hash * 7) + _canWrite.GetHashCode();
                return hash;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is MemberInfoKey))
                    return false;
                return Equals((MemberInfoKey)obj);
            }

            public bool Equals(MemberInfoKey obj)
            {
                return _declaredType == obj._declaredType && _name == obj._name && _type == obj._type &&
                    _isField == obj._isField && _isProperty == obj._isProperty &&
                    _canRead == obj._canRead && _canWrite == obj._canWrite;
            }
        }

    }
}
