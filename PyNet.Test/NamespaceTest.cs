using Microsoft.VisualStudio.TestTools.UnitTesting;

using Python;

namespace PyNet.Test
{
    [TestClass]
    public class NamespaceTest : PythonTest
    {
        [TestCategory("Namespace")]
        [TestMethod]
        public void KnownNamespace()
        {
            ClrNamespace systemNamespace = EvaluateAndConvert("clr.System") as ClrNamespace;

            Assert.IsNotNull(systemNamespace);
            Assert.AreEqual(systemNamespace.Name, "System");
            Assert.AreEqual(systemNamespace.Parent, null);

            ClrNamespace systemReflectionNamespace = EvaluateAndConvert("clr.System.Reflection") as ClrNamespace;

            Assert.IsNotNull(systemReflectionNamespace);
            Assert.AreEqual(systemReflectionNamespace.Name, "Reflection");
            Assert.IsNotNull(systemReflectionNamespace.Parent);
            Assert.AreEqual(systemReflectionNamespace.Parent.Name, "System");
        }

        [TestCategory("Namespace")]
        [TestMethod]
        public void UnknownNamespace()
        {
            ClrNamespace dedeNamespace = EvaluateAndConvert("clr.Dede") as ClrNamespace;

            Assert.IsNotNull(dedeNamespace);
            Assert.AreEqual(dedeNamespace.Name, "Dede");
            Assert.AreEqual(dedeNamespace.Parent, null);
        }

        [TestCategory("Namespace")]
        [TestMethod]
        public void NamespaceProperties()
        {
            ClrNamespace dedeNamespace = new ClrNamespace("Dede");
            string dedeNamespaceToString = EvaluateAndConvert("str(clr.Dede)") as string;

            Assert.AreEqual(dedeNamespaceToString, dedeNamespace.ToString());
        }
    }
}
