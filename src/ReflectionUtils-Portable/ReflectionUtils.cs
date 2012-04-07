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
 static class ReflectionUtils
    {
        public static readonly Type[] EmptyTypes = new Type[] { };
        private static readonly object[] EmptyObjects = new object[] { };

#if REFLECTION_UTILS_REFLECTION_EMIT

        private static readonly bool UseReflectionEmit;
        static ReflectionUtils()
        {
            try
            {
                // try creating a new object by Reflection.Emit first to see if we have enough permission.
                object dummyObj = GetConstructorByReflectionEmit(typeof(DummyClassForReflectionEmitTest), EmptyTypes)();
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

        public static ConstructorDelegate GetConstructorByCompiledLambda(ConstructorInfo constructorInfo)
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

        public static ConstructorDelegate GetConstructorByReflectionEmit(Type type, params Type[] argsType)
        {
            ConstructorDelegate ctor = null;
            DynamicMethod dynamicMethod = new DynamicMethod("ctro_DynamicMethod" + type.FullName, typeof(object), TypeofObjectArray, typeof(ReflectionUtils));
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

        class DummyClassForReflectionEmitTest { }

#endif

        public static GetDelegate GetGetMethodByReflection(PropertyInfo propertyInfo)
        {
#if REFLECTION_UTILS_TYPEINFO
            MethodInfo methodInfo = propertyInfo.GetMethod;
#else
            MethodInfo methodInfo = propertyInfo.GetGetMethod(true);
#endif
            return delegate(object source) { return methodInfo.Invoke(source, EmptyObjects); };
        }

        public static SetDelegate GetSetMethodByReflection(PropertyInfo propertyInfo)
        {
#if REFLECTION_UTILS_TYPEINFO
            MethodInfo methodInfo = propertyInfo.SetMethod;
#else
            MethodInfo methodInfo = propertyInfo.GetSetMethod(true);
#endif
            return delegate(object source, object value) { methodInfo.Invoke(source, new object[] { value }); };
        }

    }

}
