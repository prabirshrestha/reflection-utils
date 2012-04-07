//-----------------------------------------------------------------------
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
// <website>https://github.com/facebook-csharp-sdk/ReflectionUtils</website>
//-----------------------------------------------------------------------

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

namespace ReflectionUtilsNew
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

    public delegate object ConstructorDelegate(params object[] args);

    public static class ReflectionUtilsNew
    {
        public static readonly Type[] EmptyTypes = new Type[] { };

#if REFLECTION_UTILS_REFLECTION_EMIT

        private static readonly bool UseReflectionEmit;
        static ReflectionUtilsNew()
        {
            try
            {
                // try creating a new object by Reflection.Emit first to see if we have enough permission.
                PadLockDictionary<Type, PadLockDictionary<Type[], ConstructorDelegate>> dummyCache = new PadLockDictionary<Type, PadLockDictionary<Type[], ConstructorDelegate>>();
                object dummyObj = GetConstructorByReflectionEmit(dummyCache, typeof(DummyClassForReflectionEmitTest), EmptyTypes)();
                if (dummyObj != null)
                    UseReflectionEmit = true;
            }
            catch { }
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

        public static ConstructorInfo GetConstructorInfo(Type type, Type[] argsType)
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

        public static ConstructorDelegate GetConstructor(PadLockDictionary<Type, PadLockDictionary<Type[], ConstructorDelegate>> typeCache, Type type, params Type[] argsType)
        {
#if !REFLECTION_UTILS_NO_LINQ_EXPRESSION
            return GetConstructorByCompiledLambda(typeCache, type, argsType);
#else

#if  REFLECTION_UTILS_REFLECTION_EMIT
            if (UseReflectionEmit)
                return GetConstructorByReflectionEmit(typeCache, type, argsType);
#endif
            return GetConstructorByReflection(typeCache, type, argsType);
#endif
        }

        public static ConstructorDelegate GetConstructorByReflection(PadLockDictionary<Type, PadLockDictionary<Type[], ConstructorDelegate>> typeCache, Type type, params Type[] argsType)
        {
            PadLockDictionary<Type[], ConstructorDelegate> constructorCache;
            ConstructorDelegate ctor = null;
            if (typeCache.TryGetValue(type, out constructorCache))
            {
                if (constructorCache.TryGetValue(argsType, out ctor))
                    return ctor;
            }
            else
            {
                constructorCache = new PadLockDictionary<Type[], ConstructorDelegate>();
            }

            ConstructorInfo constructorInfo = GetConstructorInfo(type, argsType);
            if (constructorInfo != null)
                ctor = delegate(object[] args) { return constructorInfo.Invoke(args); };

            lock (typeCache.Padlock)
            {
                constructorCache.TryAdd(argsType, ctor);
                typeCache.TryAdd(type, constructorCache);
            }

            return ctor;
        }

#if !REFLECTION_UTILS_NO_LINQ_EXPRESSION
        public static ConstructorDelegate GetConstructorByCompiledLambda(PadLockDictionary<Type, PadLockDictionary<Type[], ConstructorDelegate>> typeCache, Type type, params Type[] argsType)
        {
            PadLockDictionary<Type[], ConstructorDelegate> constructorCache;
            ConstructorDelegate ctor = null;
            if (typeCache.TryGetValue(type, out constructorCache))
            {
                if (constructorCache.TryGetValue(argsType, out ctor))
                    return ctor;
            }
            else
            {
                constructorCache = new PadLockDictionary<Type[], ConstructorDelegate>();
            }

            ConstructorInfo constructorInfo = GetConstructorInfo(type, argsType);
            if (constructorInfo != null)
            {
                ParameterInfo[] paramsInfo = constructorInfo.GetParameters();
                // create a single param of type object[]
                ParameterExpression param = Expression.Parameter(typeof(object[]), "args");

                Expression[] argsExp = new Expression[paramsInfo.Length];

                // pick each arg from the params array 
                // and create a typed expression of them
                for (int i = 0; i < paramsInfo.Length; i++)
                {
                    Expression index = Expression.Constant(i);
                    Type paramType = paramsInfo[i].ParameterType;
                    Expression paramAccessorExp = Expression.ArrayIndex(param, index);
                    Expression paramCastExp = Expression.Convert(paramAccessorExp, paramType);
                    argsExp[i] = paramCastExp;
                }

                // make a NewExpression that calls the ctor with the args we just created
                NewExpression newExp = Expression.New(constructorInfo, argsExp);

                // create a lambda with the New
                // Expression as body and our param object[] as arg
                Expression<Func<object[], object>> lambda = Expression.Lambda<Func<object[], object>>(newExp, param);
                Func<object[], object> compiledLambda = lambda.Compile();

                ctor = delegate(object[] args) { return compiledLambda(args); };
            }

            lock (typeCache.Padlock)
            {
                constructorCache.TryAdd(argsType, ctor);
                typeCache.TryAdd(type, constructorCache);
            }

            return ctor;
        }
#endif

#if REFLECTION_UTILS_REFLECTION_EMIT

        private static readonly Type[] TypeofObjectArray = new Type[] { typeof(object) };
        public static ConstructorDelegate GetConstructorByReflectionEmit(PadLockDictionary<Type, PadLockDictionary<Type[], ConstructorDelegate>> typeCache, Type type, params Type[] argsType)
        {
            PadLockDictionary<Type[], ConstructorDelegate> constructorCache;
            ConstructorDelegate ctor = null;
            if (typeCache.TryGetValue(type, out constructorCache))
            {
                if (constructorCache.TryGetValue(argsType, out ctor))
                    return ctor;
            }
            else
            {
                constructorCache = new PadLockDictionary<Type[], ConstructorDelegate>();
            }

            // code from FastReflect
            DynamicMethod dynamicMethod = new DynamicMethod("ctro_DynamicMethod" + type.FullName, typeof(object), TypeofObjectArray, typeof(ReflectionUtilsNew));
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

            lock (typeCache.Padlock)
            {
                constructorCache.TryAdd(argsType, ctor);
                typeCache.TryAdd(type, constructorCache);
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

        class DummyClassForReflectionEmitTest { }

#endif

#if REFLECTION_UTILS_INTERNAL
        internal
#else
        public
#endif
 class PadLockDictionary<TKey, TValue>
        {
            public readonly object Padlock = new object();
            private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

            public bool TryGetValue(TKey key, out TValue value)
            {
                return _dictionary.TryGetValue(key, out value);
            }

            public TValue this[TKey key]
            {
                get { return _dictionary[key]; }
                set { throw new NotImplementedException(); }
            }

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            {
                return ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).GetEnumerator();
            }

            public bool TryAdd(TKey key, TValue value)
            {
                if (_dictionary.ContainsKey(key) == false)
                {
                    _dictionary.Add(key, value);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
