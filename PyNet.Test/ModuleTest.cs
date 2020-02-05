using Microsoft.VisualStudio.TestTools.UnitTesting;

using Python;

namespace PyNet.Test
{
    [TestClass]
    public class ModuleTest : PythonTest
    {
        [TestMethod]
        public void PyNetModule()
        {
            string pythonModuleCode = @"import clr
clr.System.Math.Round(2.5)";

            PythonModule pythonModule = Build(pythonModuleCode);
        }
    }
}
