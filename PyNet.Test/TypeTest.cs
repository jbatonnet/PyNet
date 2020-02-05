using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Python;

namespace PyNet.Test
{
    [TestClass]
    public class TypeTest : PythonTest
    {
        [TestCategory("Struct")]
        [TestMethod]
        public void Type()
        {
            string pythonModuleCode = @"class TestType:

    def get_OldProperty(self):
        return 0
    def set_OldProperty(self, value):
        pass

    @property
    def NewProperty(self):
        return 'test'
    @NewProperty.setter
    def NewProperty(self, value):
        pass";

            PythonModule pythonModule = Build(pythonModuleCode);
            PythonClass pythonClass = pythonModule.Dictionary.Values.OfType<PythonClass>().First();

            Type clrType = TypeManager.FromPython(pythonClass);

            PropertyInfo oldPropertyInfo = clrType.GetProperty("OldProperty");
            //Assert.IsNotNull(oldPropertyInfo);

            PropertyInfo newPropertyInfo = clrType.GetProperty("NewProperty");
            Assert.IsNotNull(newPropertyInfo);
            Assert.AreEqual(newPropertyInfo.PropertyType, typeof(object));
        }
    }
}