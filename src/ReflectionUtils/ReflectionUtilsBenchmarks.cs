﻿//-----------------------------------------------------------------------
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
// <website>https://github.com/facebook-csharp-sdk/reflection-utils</website>
//-----------------------------------------------------------------------


namespace ReflectionUtils
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Diagnostics;

    public class ReflectionUtilsBenchmarks
    {
        public const int loops = 1000000;

        public static PropertyInfo SamplePropertyInfo = ReflectionUtils.GetProperties(typeof(SimpleClass)).Single(p => p.Name == "Prop");
        public static FieldInfo SampleFieldInfo = ReflectionUtils.GetFields(typeof(SimpleClass)).Single(p => p.Name == "Field");

        private static Action<string> _writer;

        public static void Run(Action<string> writer)
        {
            _writer = writer;
            GetConstructorByReflection();
            GetConstructorByReflectionEmit();
            GetConstructorByCompiledLambda();

            GetPropertyByReflection();
            GetPropertyByReflectionEmit();
            GetPropertyByCompiledLambda();

            SetPropertyByReflection();
            SetPropertyByReflectionEmit();
            SetPropertyByCompiledLambda();

            GetFieldByReflection();
            GetFieldByReflectionEmit();
            GetFieldByCompiledLambda();

            SetFieldByReflection();
            SetFieldByReflectionEmit();
            SetFieldByCompiledLambda();
        }


        private static void GetConstructorByReflection()
        {
            var cache = ReflectionUtils.CreateConstructorCacheForReflection();

            using (new Profiler("ctor method invoke", _writer))
            {
                var obj = ReflectionUtils.GetConstructor(cache, typeof(SimpleClass), ReflectionUtils.EmptyTypes)();
            }

            using (new Profiler(_writer))
            {
                var obj = ReflectionUtils.GetConstructor(cache, typeof(SimpleClass), ReflectionUtils.EmptyTypes)();
            }

            using (new Profiler(_writer))
            {
                for (int i = 0; i < loops; i++)
                {
                    var obj = ReflectionUtils.GetConstructor(cache, typeof(SimpleClass), ReflectionUtils.EmptyTypes)();
                }
            }
        }

        private static void GetConstructorByReflectionEmit()
        {
#if REFLECTION_UTILS_REFLECTION_EMIT
            var cache = ReflectionUtils.CreateConstructorCacheForReflectionEmit();

            using (new Profiler("ctor reflection.emit", _writer))
            {
                var obj = ReflectionUtils.GetConstructor(cache, typeof(SimpleClass), ReflectionUtils.EmptyTypes)();
            }

            using (new Profiler(_writer))
            {
                var obj = ReflectionUtils.GetConstructor(cache, typeof(SimpleClass), ReflectionUtils.EmptyTypes)();
            }

            using (new Profiler(_writer))
            {
                for (int i = 0; i < loops; i++)
                {
                    var obj = ReflectionUtils.GetConstructor(cache, typeof(SimpleClass), ReflectionUtils.EmptyTypes)();
                }
            }
#endif
        }

        private static void GetConstructorByCompiledLambda()
        {
            var cache = ReflectionUtils.CreateConstructorCacheForCompiledLambda();

            using (new Profiler("ctor compiled lambda", _writer))
            {
                var obj = ReflectionUtils.GetConstructor(cache, typeof(SimpleClass), ReflectionUtils.EmptyTypes)();
            }

            using (new Profiler(_writer))
            {
                var obj = ReflectionUtils.GetConstructor(cache, typeof(SimpleClass), ReflectionUtils.EmptyTypes)();
            }

            using (new Profiler(_writer))
            {
                for (int i = 0; i < loops; i++)
                {
                    var obj = ReflectionUtils.GetConstructor(cache, typeof(SimpleClass), ReflectionUtils.EmptyTypes)();
                }
            }
        }

        private static void GetPropertyByReflection()
        {
            var cache = ReflectionUtils.CreateGetMethodForMemberInfoCacheForReflection();

            var obj = new SimpleClass();
            using (new Profiler("prop.get method invoke", _writer))
            {
                var getter = ReflectionUtils.GetGetMethod(cache, SamplePropertyInfo);
                var value = getter(obj);
            }

            using (new Profiler(_writer))
            {
                var getter = ReflectionUtils.GetGetMethod(cache, SamplePropertyInfo);
                var value = getter(obj);
            }

            using (new Profiler(_writer))
            {
                for (int i = 0; i < loops; i++)
                {
                    var getter = ReflectionUtils.GetGetMethod(cache, SamplePropertyInfo);
                    var value = getter(obj);
                }
            }
        }

        private static void GetPropertyByReflectionEmit()
        {
#if REFLECTION_UTILS_REFLECTION_EMIT
            var cache = ReflectionUtils.CreateGetMethodForMemberInfoCacheForReflectionEmit();

            var obj = new SimpleClass();
            using (new Profiler("prop.get reflection emit", _writer))
            {
                var getter = ReflectionUtils.GetGetMethod(cache, SamplePropertyInfo);
                var value = getter(obj);
            }

            using (new Profiler(_writer))
            {
                var getter = ReflectionUtils.GetGetMethod(cache, SamplePropertyInfo);
                var value = getter(obj);
            }

            using (new Profiler(_writer))
            {
                for (int i = 0; i < loops; i++)
                {
                    var getter = ReflectionUtils.GetGetMethod(cache, SamplePropertyInfo);
                    var value = getter(obj);
                }
            }
#endif
        }

        private static void GetPropertyByCompiledLambda()
        {
            var cache = ReflectionUtils.CreateGetMethodForMemberInfoCacheForCompiledLambda();

            var obj = new SimpleClass();
            using (new Profiler("prop.get compiled lambda", _writer))
            {
                var getter = ReflectionUtils.GetGetMethod(cache, SamplePropertyInfo);
                var value = getter(obj);
            }

            using (new Profiler(_writer))
            {
                var getter = ReflectionUtils.GetGetMethod(cache, SamplePropertyInfo);
                var value = getter(obj);
            }

            using (new Profiler(_writer))
            {
                for (int i = 0; i < loops; i++)
                {
                    var getter = ReflectionUtils.GetGetMethod(cache, SamplePropertyInfo);
                    var value = getter(obj);
                }
            }
        }

        private static void SetPropertyByReflection()
        {
            var cache = ReflectionUtils.CreateSetMethodForMemberInfoCacheForReflection();

            var obj = new SimpleClass();
            using (new Profiler("prop.set method invoke", _writer))
            {
                var setter = ReflectionUtils.GetSetMethod(cache, SamplePropertyInfo);
                setter(obj, "val");
            }

            using (new Profiler(_writer))
            {
                var setter = ReflectionUtils.GetSetMethod(cache, SamplePropertyInfo);
                setter(obj, "val");
            }

            using (new Profiler(_writer))
            {
                for (int i = 0; i < loops; i++)
                {
                    var setter = ReflectionUtils.GetSetMethod(cache, SamplePropertyInfo);
                    setter(obj, "val");
                }
            }
        }

        private static void SetPropertyByReflectionEmit()
        {
#if REFLECTION_UTILS_REFLECTION_EMIT
            var cache = ReflectionUtils.CreateSetMethodForMemberInfoCacheForReflectionEmit();

            var obj = new SimpleClass();
            using (new Profiler("prop.set reflection emit", _writer))
            {
                var setter = ReflectionUtils.GetSetMethod(cache, SamplePropertyInfo);
                setter(obj, "val");
            }

            using (new Profiler(_writer))
            {
                var setter = ReflectionUtils.GetSetMethod(cache, SamplePropertyInfo);
                setter(obj, "val");
            }

            using (new Profiler(_writer))
            {
                for (int i = 0; i < loops; i++)
                {
                    var setter = ReflectionUtils.GetSetMethod(cache, SamplePropertyInfo);
                    setter(obj, "val");
                }
            }
#endif
        }

        private static void SetPropertyByCompiledLambda()
        {
            var cache = ReflectionUtils.CreateSetMethodForMemberInfoCacheForCompiledLambda();

            var obj = new SimpleClass();
            using (new Profiler("prop.set compiled lambda", _writer))
            {
                var setter = ReflectionUtils.GetSetMethod(cache, SamplePropertyInfo);
                setter(obj, "val");
            }

            using (new Profiler(_writer))
            {
                var setter = ReflectionUtils.GetSetMethod(cache, SamplePropertyInfo);
                setter(obj, "val");
            }

            using (new Profiler(_writer))
            {
                for (int i = 0; i < loops; i++)
                {
                    var setter = ReflectionUtils.GetSetMethod(cache, SamplePropertyInfo);
                    setter(obj, "val");
                }
            }
        }

        private static void GetFieldByReflection()
        {
            var cache = ReflectionUtils.CreateGetMethodForMemberInfoCacheForReflection();

            var obj = new SimpleClass();
            using (new Profiler("field.get method invoke", _writer))
            {
                var getter = ReflectionUtils.GetGetMethod(cache, SampleFieldInfo);
                var value = getter(obj);
            }

            using (new Profiler(_writer))
            {
                var getter = ReflectionUtils.GetGetMethod(cache, SampleFieldInfo);
                var value = getter(obj);
            }

            using (new Profiler(_writer))
            {
                for (int i = 0; i < loops; i++)
                {
                    var getter = ReflectionUtils.GetGetMethod(cache, SampleFieldInfo);
                    var value = getter(obj);
                }
            }
        }

        private static void GetFieldByReflectionEmit()
        {
#if REFLECTION_UTILS_REFLECTION_EMIT
            var cache = ReflectionUtils.CreateGetMethodForMemberInfoCacheForReflectionEmit();

            var obj = new SimpleClass();
            using (new Profiler("field.get reflection emit", _writer))
            {
                var getter = ReflectionUtils.GetGetMethod(cache, SampleFieldInfo);
                var value = getter(obj);
            }

            using (new Profiler(_writer))
            {
                var getter = ReflectionUtils.GetGetMethod(cache, SampleFieldInfo);
                var value = getter(obj);
            }

            using (new Profiler(_writer))
            {
                for (int i = 0; i < loops; i++)
                {
                    var getter = ReflectionUtils.GetGetMethod(cache, SampleFieldInfo);
                    var value = getter(obj);
                }
            }
#endif
        }

        private static void GetFieldByCompiledLambda()
        {
            var cache = ReflectionUtils.CreateGetMethodForMemberInfoCacheForCompiledLambda();

            var obj = new SimpleClass();
            using (new Profiler("field.get compiled lambda", _writer))
            {
                var getter = ReflectionUtils.GetGetMethod(cache, SampleFieldInfo);
                var value = getter(obj);
            }

            using (new Profiler(_writer))
            {
                var getter = ReflectionUtils.GetGetMethod(cache, SampleFieldInfo);
                var value = getter(obj);
            }

            using (new Profiler(_writer))
            {
                for (int i = 0; i < loops; i++)
                {
                    var getter = ReflectionUtils.GetGetMethod(cache, SampleFieldInfo);
                    var value = getter(obj);
                }
            }
        }

        private static void SetFieldByReflection()
        {
            var cache = ReflectionUtils.CreateSetMethodForMemberInfoCacheForReflection();

            var obj = new SimpleClass();
            using (new Profiler("field.set method invoke", _writer))
            {
                var setter = ReflectionUtils.GetSetMethod(cache, SampleFieldInfo);
                setter(obj, "val");
            }

            using (new Profiler(_writer))
            {
                var setter = ReflectionUtils.GetSetMethod(cache, SampleFieldInfo);
                setter(obj, "val");
            }

            using (new Profiler(_writer))
            {
                for (int i = 0; i < loops; i++)
                {
                    var setter = ReflectionUtils.GetSetMethod(cache, SampleFieldInfo);
                    setter(obj, "val");
                }
            }
        }

        private static void SetFieldByReflectionEmit()
        {
#if REFLECTION_UTILS_REFLECTION_EMIT
            var cache = ReflectionUtils.CreateSetMethodForMemberInfoCacheForReflectionEmit();

            var obj = new SimpleClass();
            using (new Profiler("field.set reflection emit", _writer))
            {
                var setter = ReflectionUtils.GetSetMethod(cache, SampleFieldInfo);
                setter(obj, "val");
            }

            using (new Profiler(_writer))
            {
                var setter = ReflectionUtils.GetSetMethod(cache, SampleFieldInfo);
                setter(obj, "val");
            }

            using (new Profiler(_writer))
            {
                for (int i = 0; i < loops; i++)
                {
                    var setter = ReflectionUtils.GetSetMethod(cache, SampleFieldInfo);
                    setter(obj, "val");
                }
            }
#endif
        }

        private static void SetFieldByCompiledLambda()
        {
            var cache = ReflectionUtils.CreateSetMethodForMemberInfoCacheForCompiledLambda();

            var obj = new SimpleClass();
            using (new Profiler("field.set compiled lambda", _writer))
            {
                var setter = ReflectionUtils.GetSetMethod(cache, SampleFieldInfo);
                setter(obj, "val");
            }

            using (new Profiler(_writer))
            {
                var setter = ReflectionUtils.GetSetMethod(cache, SampleFieldInfo);
                setter(obj, "val");
            }

            using (new Profiler(_writer))
            {
                for (int i = 0; i < loops; i++)
                {
                    var setter = ReflectionUtils.GetSetMethod(cache, SampleFieldInfo);
                    setter(obj, "val");
                }
            }
        }
    }

    public class Profiler : IDisposable
    {
        private readonly Action<string> _writer;
        private readonly Stopwatch _stopwatch;

        public Profiler(string message, Action<string> writer)
        {
            _writer = writer;
            writer(message);
            _stopwatch = Stopwatch.StartNew();
        }

        public Profiler(Action<string> writer)
        {
            _writer = writer;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _writer(_stopwatch.Elapsed.ToString());
        }
    }

    public class SimpleClass
    {
        // SimpleClass
        public SimpleClass()
        {
        }

        private SimpleClass(Type t)
        {
        }

        // StringField
        public string stringField;

        public string stringProperty { get; set; }

        public string Prop { get; set; }

        public string Field;

        // CreateInstance
        internal static SimpleClass CreateInstance()
        {
            return new SimpleClass();
        }
    }
}