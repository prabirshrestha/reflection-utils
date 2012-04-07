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

namespace ReflectionUtils
{
    class Program
    {
        public const int loops = 1000000;

        static void Main(string[] args)
        {
            GetConstructorByReflection();
            GetConstructorByReflectionEmit();
            GetConstructorByCompiledLambda();

            GetPropertyByReflection();
        }

        private static void GetConstructorByReflection()
        {
            var cache = ReflectionUtils.CreateConstructorCacheForReflection();

            using (new Profiler("ctor method invoke", WriteLine))
            {
                var obj = ReflectionUtils.GetConstructor(cache, typeof(SimpleClass), ReflectionUtils.EmptyTypes)();
            }

            using (new Profiler(WriteLine))
            {
                var obj = ReflectionUtils.GetConstructor(cache, typeof(SimpleClass), ReflectionUtils.EmptyTypes)();
            }

            using (new Profiler(WriteLine))
            {
                for (int i = 0; i < loops; i++)
                {
                    var obj = ReflectionUtils.GetConstructor(cache, typeof(SimpleClass), ReflectionUtils.EmptyTypes)();
                }
            }
        }

        private static void GetConstructorByReflectionEmit()
        {
            var cache = ReflectionUtils.CreateConstructorCacheForReflectionEmit();

            using (new Profiler("ctor reflection.emit", WriteLine))
            {
                var obj = ReflectionUtils.GetConstructor(cache, typeof(SimpleClass), ReflectionUtils.EmptyTypes)();
            }

            using (new Profiler(WriteLine))
            {
                var obj = ReflectionUtils.GetConstructor(cache, typeof(SimpleClass), ReflectionUtils.EmptyTypes)();
            }

            using (new Profiler(WriteLine))
            {
                for (int i = 0; i < loops; i++)
                {
                    var obj = ReflectionUtils.GetConstructor(cache, typeof(SimpleClass), ReflectionUtils.EmptyTypes)();
                }
            }
        }

        private static void GetConstructorByCompiledLambda()
        {
            var cache = ReflectionUtils.CreateConstructorCacheForCompiledLambda();

            using (new Profiler("ctor compiled lambda", WriteLine))
            {
                var obj = ReflectionUtils.GetConstructor(cache, typeof(SimpleClass), ReflectionUtils.EmptyTypes)();
            }

            using (new Profiler(WriteLine))
            {
                var obj = ReflectionUtils.GetConstructor(cache, typeof(SimpleClass), ReflectionUtils.EmptyTypes)();
            }

            using (new Profiler(WriteLine))
            {
                for (int i = 0; i < loops; i++)
                {
                    var obj = ReflectionUtils.GetConstructor(cache, typeof(SimpleClass), ReflectionUtils.EmptyTypes)();
                }
            }
        }

        private static void GetPropertyByReflection()
        {
            var cache = ReflectionUtils.CreateConstructorCacheForReflection();

            using (new Profiler("prop.get method invoke", WriteLine))
            {
                var obj = ReflectionUtils.GetConstructor(cache, typeof(SimpleClass), ReflectionUtils.EmptyTypes)();
            }

            using (new Profiler(WriteLine))
            {
                var obj = ReflectionUtils.GetConstructor(cache, typeof(SimpleClass), ReflectionUtils.EmptyTypes)();
            }

            using (new Profiler(WriteLine))
            {
                for (int i = 0; i < loops; i++)
                {
                    var obj = ReflectionUtils.GetConstructor(cache, typeof(SimpleClass), ReflectionUtils.EmptyTypes)();
                }
            }
        }

        public static void WriteLine(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}
