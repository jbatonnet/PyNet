using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PyNet.Sample
{
    using Python;

    class Program
    {
        static void Main(string[] args)
        {
            Python.Py_NoSiteFlag = true;
            Python.Py_Initialize();

            PythonModule testModule = PythonModule.FromFile(@"..\..\..\PyNet.Sample\Test.py");
            dynamic testDynamic = testModule;

            {
                Console.WriteLine("a: " + testModule.GetAttribute("a"));
                Console.WriteLine("b: " + testModule.GetAttribute("b"));
                Console.WriteLine();
            }

            {
                Console.WriteLine("a: " + testDynamic.a);
                Console.WriteLine("b: " + testDynamic.b);

                testDynamic.c = (int)testDynamic.a + (int)testDynamic.b;
                testDynamic.show("c:");
            }

            Console.ReadKey(true);
        }
    }
}