//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="The Outercurve Foundation">
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

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace ReflectionUtils
{
    class Program
    {
        const BindingFlags BINDING_FLAGS = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static DateTime lastTestStartTime;
        private static Type type = typeof(SimpleClass);
        private static SimpleClass simpleClass = SimpleClass.CreateInstance();
        private static FieldInfo fieldInfo = type.GetField("stringField", BINDING_FLAGS);
        private static PropertyInfo propertyInfo = type.GetProperty("stringProperty", BINDING_FLAGS);
        static int loops;
        static Stopwatch stopwatch;

        static void Main(string[] args)
        {
            //Console.WriteLine("How Many Test Iterations Would You Like To Run?");
            //int loops = int.Parse(Console.ReadLine());

            loops = 100000;

            RunBenchmarks();

            return;
            Console.WriteLine(typeof(IConvertible).IsAssignableFrom(typeof(int)));
            Console.WriteLine(typeof(IConvertible).IsAssignableFrom(typeof(long)));
            Console.WriteLine(typeof(IConvertible).IsAssignableFrom(typeof(double)));
            Console.WriteLine(typeof(IConvertible).IsAssignableFrom(typeof(byte)));
            Console.WriteLine(typeof(IConvertible).IsAssignableFrom(typeof(float)));
            Console.WriteLine(typeof(IConvertible).IsAssignableFrom(typeof(short)));
            Console.WriteLine(typeof(IConvertible).IsAssignableFrom(typeof(decimal)));
            Console.WriteLine(typeof(IConvertible).IsAssignableFrom(typeof(bool)));
            Console.WriteLine(typeof(IConvertible).IsAssignableFrom(typeof(Program)));
            return;

            CreateObjectUsingReflection(loops);
            CreateObjectUsingDynamicMethodCall(loops);

            SetFieldValueUsingReflection(loops);
            SetFieldValueUsingDynamicMethodCall(loops);

            SetFieldValueUsingReflection(loops);
            SetFieldValueUsingDynamicMethodCall(loops);

            GetFieldValueUsingReflection(loops);
            GetFieldValueUsingDynamicMethodCall(loops);

            GetPropertyValueUsingReflection(loops);
            GetPropertyValueUsingDynamicMethodCall(loops);
        }

        private static void RunBenchmarks()
        {
            GetNewInstanceByReflection();
            GetNewInstanceByReflectionEmit();
            GetNewInstanceByCompiledLambda();

            GetGetMethod();
        }

        private static void GetNewInstanceByReflection()
        {
            var cache = new ReflectionUtilsNew.ReflectionUtilsNew.PadLockDictionary<Type, ReflectionUtilsNew.ReflectionUtilsNew.PadLockDictionary<Type[], ReflectionUtilsNew.ConstructorDelegate>>();
            StartTest("constructorInfo.Invoke - first");
            object first = ReflectionUtilsNew.ReflectionUtilsNew.GetConstructorByReflection(cache, type, ReflectionUtilsNew.ReflectionUtilsNew.EmptyTypes)();
            EndTest();

            StartTest("constructorInfo.Invoke - second");
            object second = ReflectionUtilsNew.ReflectionUtilsNew.GetConstructorByReflection(cache, type, ReflectionUtilsNew.ReflectionUtilsNew.EmptyTypes)();
            EndTest();

            StartTest("constructorInfo.Invoke - " + loops);
            for (int i = 2; i < loops; i++)
            {
                object multiple = ReflectionUtilsNew.ReflectionUtilsNew.GetConstructorByReflection(cache, type, ReflectionUtilsNew.ReflectionUtilsNew.EmptyTypes)();
            }
            EndTest();
        }

        private static void GetNewInstanceByReflectionEmit()
        {
            var cache = new ReflectionUtilsNew.ReflectionUtilsNew.PadLockDictionary<Type, ReflectionUtilsNew.ReflectionUtilsNew.PadLockDictionary<Type[], ReflectionUtilsNew.ConstructorDelegate>>();
            StartTest("dynamicMethod - first");
            object first = ReflectionUtilsNew.ReflectionUtilsNew.GetConstructorByReflectionEmit(cache, type, ReflectionUtilsNew.ReflectionUtilsNew.EmptyTypes)();
            EndTest();

            StartTest("dynamicMethod - second");
            object second = ReflectionUtilsNew.ReflectionUtilsNew.GetConstructorByReflectionEmit(cache, type, ReflectionUtilsNew.ReflectionUtilsNew.EmptyTypes)();
            EndTest();

            StartTest("dynamicMethod - " + loops);
            for (int i = 2; i < loops; i++)
            {
                object multiple = ReflectionUtilsNew.ReflectionUtilsNew.GetConstructorByReflectionEmit(cache, type, ReflectionUtilsNew.ReflectionUtilsNew.EmptyTypes)();
            }
            EndTest();
        }

        private static void GetNewInstanceByCompiledLambda()
        {
            var cache = new ReflectionUtilsNew.ReflectionUtilsNew.PadLockDictionary<Type, ReflectionUtilsNew.ReflectionUtilsNew.PadLockDictionary<Type[], ReflectionUtilsNew.ConstructorDelegate>>();
            StartTest("lambda.Compile - first");
            object first = ReflectionUtilsNew.ReflectionUtilsNew.GetConstructorByCompiledLambda(cache, type, ReflectionUtilsNew.ReflectionUtilsNew.EmptyTypes)();
            EndTest();

            StartTest("lambda.Compile - second");
            object second = ReflectionUtilsNew.ReflectionUtilsNew.GetConstructorByCompiledLambda(cache, type, ReflectionUtilsNew.ReflectionUtilsNew.EmptyTypes)();
            EndTest();

            StartTest("lambda.Compile - " + loops);
            for (int i = 2; i < loops; i++)
            {
                object multiple = ReflectionUtilsNew.ReflectionUtilsNew.GetConstructorByCompiledLambda(cache, type, ReflectionUtilsNew.ReflectionUtilsNew.EmptyTypes)();
            }
            EndTest();
        }

        private static void GetGetMethod()
        {
            StartTest("get.reflection - first");
            var getter = ReflectionUtilsNew.ReflectionUtilsNew.GetGetMethodByReflection(propertyInfo);
            object first = getter(simpleClass);
            EndTest();

            StartTest("get.reflection - second");
            object second = getter(simpleClass);
            EndTest();

            StartTest("get.reflection - " + loops);

            for (int i = 2; i < loops; i++)
            {
                object value = getter(simpleClass);
            }

            EndTest();
        }

        // ---------------------------

        private static void CreateObjectUsingReflection(int loops)
        {
            StartTest("Begin CreateObjectUsingReflection");

            for (int i = 0; i < loops; i++)
            {
                object result = Activator.CreateInstance(type);
            }

            EndTest("End CreateObjectUsingReflection");
        }

        // CreateObjectUsingDynamicMethodCall
        private static void CreateObjectUsingDynamicMethodCall(int loops)
        {
            StartTest("Begin CreateObjectUsingDynamicMethodCall");

            for (int i = 0; i < loops; i++)
            {
                object result = CacheResolver.GetNewInstance(type);
            }

            EndTest("End CreateObjectUsingDynamicMethodCall");
        }

        private static void SetFieldValueUsingReflection(int loops)
        {
            StartTest("Begin SetFieldValueUsingReflection");

            for (int i = 0; i < loops; i++)
            {
                fieldInfo.SetValue(simpleClass, "test");
            }

            EndTest("End SetFieldValueUsingReflection");
        }

        // SetValueUsingDynamicMethodCall
        private static void SetFieldValueUsingDynamicMethodCall(int loops)
        {
            StartTest("Begin SetFieldValueUsingDynamicMethodCall");
            SetHandler setHandler = new CacheResolver.MemberMap(fieldInfo).Setter;

            for (int i = 0; i < loops; i++)
            {
                setHandler(simpleClass, "test");
            }

            EndTest("End SetFieldValueUsingDynamicMethodCall");
        }

        private static void SetPropertyValueUsingReflection(int loops)
        {
            StartTest("Begin SetPropertyValueUsingReflection");

            for (int i = 0; i < loops; i++)
            {
                propertyInfo.SetValue(simpleClass, "test", null);
            }

            EndTest("End SetPropertyValueUsingReflection");
        }

        // SetValueUsingDynamicMethodCall
        private static void SetPropertyValueUsingDynamicMethodCall(int loops)
        {
            StartTest("Begin SetPropertyValueUsingDynamicMethodCall");
            SetHandler setHandler = new CacheResolver.MemberMap(propertyInfo).Setter;

            for (int i = 0; i < loops; i++)
            {

                setHandler(simpleClass, "test");
            }

            EndTest("End SetPropertyValueUsingDynamicMethodCall");
        }

        // GetValueUsingReflection
        private static void GetFieldValueUsingReflection(int loops)
        {
            StartTest("Begin GetFieldValueUsingReflection");

            for (int i = 0; i < loops; i++)
            {
                string value = (string)fieldInfo.GetValue(simpleClass);
            }

            EndTest("End GetFieldValueUsingReflection");
        }

        // GetValueUsingDynamicMethodCall
        private static void GetFieldValueUsingDynamicMethodCall(int loops)
        {
            StartTest("Begin GetFieldValueUsingDynamicMethodCall");
            GetHandler getHandler = new CacheResolver.MemberMap(fieldInfo).Getter;

            for (int i = 0; i < loops; i++)
            {
                string value = (string)getHandler(simpleClass);
            }

            EndTest("End GetFieldValueUsingDynamicMethodCall");
        }

        // GetValueUsingReflection
        private static void GetPropertyValueUsingReflection(int loops)
        {
            StartTest("Begin GetPropertyValueUsingReflection");

            for (int i = 0; i < loops; i++)
            {
                string value = (string)propertyInfo.GetValue(simpleClass, null);
            }

            EndTest("End GetPropertyValueUsingReflection");
        }

        // GetValueUsingDynamicMethodCall
        private static void GetPropertyValueUsingDynamicMethodCall(int loops)
        {
            StartTest("Begin GetPropertyValueUsingDynamicMethodCall");
            GetHandler getHandler = new CacheResolver.MemberMap(propertyInfo).Getter;

            for (int i = 0; i < loops; i++)
            {
                string value = (string)getHandler(simpleClass);
            }

            EndTest("End GetPropertyValueUsingDynamicMethodCall");
        }

        // StartTest
        private static void StartTest(string message)
        {
            Console.WriteLine(message);
            stopwatch = Stopwatch.StartNew();
        }

        // EndTest
        private static void EndTest()
        {
            stopwatch.Stop();
            Console.WriteLine(stopwatch.Elapsed);
        }
        private static void EndTest(string message)
        {
            Console.WriteLine(message);
            Console.WriteLine(DateTime.Now - lastTestStartTime);
            Console.WriteLine("");
        }
    }
}
