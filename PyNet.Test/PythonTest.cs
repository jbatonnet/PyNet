using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PyNet.Test
{
    using Python;
    using Python = Python.Python;

    [TestClass]
    public class PythonTest
    {
        protected static string code, script;

        static PythonTest()
        {
            Python.Py_NoSiteFlag = true;
            Python.Py_Initialize();

            PyNetModule.initclr();
        }

        public static PythonObject Evaluate(string code, params object[] args)
        {
            PythonObject compilation;
            using (PythonException.Checker)
                compilation = Python.Py_CompileString(code, $"{nameof(PythonTest)}.{nameof(Evaluate)}", Python.Py_eval_input);

            PythonModule module = new PythonModule("__main__");
            PythonDictionary globals = module.Dictionary;
            PythonDictionary locals = new PythonDictionary();

            //locals.Add("clr", PyNetModule.ClrObject);
            locals.Add("args", new PythonTuple(args.Select(a => (PythonObject)ObjectManager.ToPython(a))));

            PythonObject result;
            using (PythonException.Checker)
                result = Python.PyEval_EvalCode(compilation, globals, locals);
            
            return result;
        }
        public static PythonModule Build(string code, params object[] args)
        {
            PythonObject compilation;
            using (PythonException.Checker)
                compilation = Python.Py_CompileString(code, $"{nameof(PythonTest)}.{nameof(Evaluate)}", Python.Py_file_input);

            PythonModule module = new PythonModule("__main__");
            PythonDictionary globals = module.Dictionary;
            PythonDictionary locals = new PythonDictionary();

            locals.Add("clr", PyNetModule.ClrObject); // FIXME
            locals.Add("args", new PythonTuple(args.Select(a => (PythonObject)ObjectManager.ToPython(a))));

            using (PythonException.Checker)
                Python.PyImport_ExecCodeModule(module.Name, compilation);

            return module;
        }
        public static object EvaluateAndConvert(string code, params object[] args)
        {
            PythonObject pythonResult = Evaluate(code, args);
            object clrResult = ObjectManager.FromPython(pythonResult);

            return clrResult;
        }
        public static T EvaluateAndConvert<T>(string code, params object[] args)
        {
            return (T)EvaluateAndConvert(code, args);
        }
    }
}