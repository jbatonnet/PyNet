using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Policy;

//using RGiesecke.DllExport;

using Python;

using static Python.Python;

public static class PyNetModule
{
    public static PythonModule PythonModule { get; private set; }
    public static PythonClass ClrClass { get; private set; }
    public static PythonObject ClrObject { get; private set; }

    //[DllExport(CallingConvention = CallingConvention.Cdecl)]
    public static void initclr()
    {
        Initialize("clr");
    }
    //[DllExport(CallingConvention = CallingConvention.Cdecl)]
    public static void initPyNet()
    {
        Initialize("PyNet");
    }

    public static void Initialize(string name)
    {
        Py_NoSiteFlag = true;
        Py_Initialize();

        PythonModule = new PythonModule(name);

        PythonModule.AddFunction(nameof(AddReference), a => AddReference(null, a));
        PythonModule.AddFunction(nameof(Break), a => Break(null, null));

        PythonModule.SetAttribute(nameof(System), new ClrNamespace(nameof(System)));




        ClrClass = new PythonClass("clr");

        ClrClass.AddMethod(nameof(__getattr__), __getattr__);
        ClrClass.AddMethod(nameof(__str__), __str__);

        ClrClass.AddMethod(nameof(AddReference), AddReference);
        ClrClass.AddMethod(nameof(Break), Break);
        //ClrClass.AddMethod("Method", Method);
        //ClrClass.AddProperty("Property", GetProperty, SetProperty);
        
        ClrObject = ClrClass.Create();

        PythonModule.SetAttribute("clr", ClrObject);
    }

    public static PythonObject AddReference(PythonObject self, PythonTuple args)
    {
        string name = (args[0] as PythonString).ToString();
        Assembly assembly = LoadAssembly(name);

        return ObjectManager.ToPython(assembly);
    }
    public static PythonObject Break(PythonObject self, PythonTuple args)
    {
        if (!Debugger.IsAttached)
            Debugger.Launch();
        else
            Debugger.Break();

        return null;
    }

    public static PythonObject __getattr__(PythonObject self, PythonTuple args)
    {
        string name = (args[0] as PythonString).ToString();
        Type type = AppDomain.CurrentDomain.GetAssemblies()
                                           .Select(a => a.GetType(name))
                                           .FirstOrDefault(t => t != null);

        if (type == null)
            return new ClrNamespace(name);
        else
            return TypeManager.ToPython(type);
    }
    public static PythonObject __str__(PythonObject self, PythonTuple args)
    {
        return "PyNet :)";
    }

    private static Assembly LoadAssembly(string name, string hintPath = null)
    {
        string assemblyPath;

        // If assembly was already loaded, just return it
        Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name);
        if (assembly != null)
            return assembly;

        // If the path is rooted, just load the file
        if (Path.IsPathRooted(name))
        {
            assembly = CheckAndLoad(name);
            if (assembly != null)
                return assembly;
        }

        // If a hint path is given, try to find the assembly
        if (hintPath != null)
        {
            assemblyPath = Path.Combine(hintPath, name);
            if (!assemblyPath.ToLower().EndsWith(".dll"))
                assemblyPath += ".dll";

            if (File.Exists(assemblyPath))
                return CheckAndLoad(assemblyPath);
        }

        // If the name is not a file, query the GAC
        bool hasExtension = name.ToLower().EndsWith(".dll");

        assemblyPath = name;
        if (!hasExtension)
            assemblyPath += ".dll";
        assemblyPath = Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), assemblyPath);

        if (File.Exists(assemblyPath))
            return CheckAndLoad(assemblyPath);

        // Then check in python path
        PythonList pythonPath = (PythonList)PySys_GetObject("path");
        for (int i = 0; i < pythonPath.Size; i++)
        {
            string fullPath = (pythonPath[i] as PythonString).ToString();

            if (!Path.IsPathRooted(fullPath))
                fullPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), fullPath);

            string filePath = Path.Combine(fullPath, name);
            if (File.Exists(filePath))
                return CheckAndLoad(filePath);

            if (!hasExtension)
            {
                filePath = Path.Combine(fullPath, name + ".dll");
                if (File.Exists(filePath))
                    return CheckAndLoad(filePath);
            }
        }

        return null;
    }
    private static Assembly CheckAndLoad(string fullName)
    {
        var zone = Zone.CreateFromUrl("file:///" + fullName);
        if (zone != null && zone.SecurityZone != SecurityZone.MyComputer)
            return null;

        return Assembly.LoadFile(fullName);
    }
}