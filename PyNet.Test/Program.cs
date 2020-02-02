using System;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PyNet.Test
{
    public static class Program
    {
        static void Main(string[] args)
        {
            Type[] testTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsDefined(typeof(TestClassAttribute)))
                .ToArray();

            foreach (Type testType in testTypes)
            {
                object testObject = Activator.CreateInstance(testType);

                MethodInfo[] testMethods = testType.GetMethods()
                    .Where(m => m.IsDefined(typeof(TestMethodAttribute)))
                    .ToArray();

                foreach (MethodInfo testMethod in testMethods)
                {
                    testMethod.Invoke(testObject, new object[0]);
                }
            }
        }
    }
}