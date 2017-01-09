using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PyNet.Test
{
    [TestClass]
    public class StructTest : PythonTest
    {
        [TestCategory("Struct")]
        [TestMethod]
        public void Struct()
        {
            SimpleStruct simpleStruct = new SimpleStruct(1234, 5678.9, "Hello World !");

            string result = EvaluateAndConvert<string>("str(args[0])", simpleStruct);
        }
    }
}