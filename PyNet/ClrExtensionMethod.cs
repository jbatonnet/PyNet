using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using RGiesecke.DllExport;
using System.Runtime.InteropServices;

using Python;
using Python.Net;
using static Python.Python;

namespace Python.Net
{
    public class ClrExtensionMethod : PythonObject
    {
        public static PythonClass Type { get; private set; }

        private static Dictionary<string, ClrNamespace> namespaces = new Dictionary<string, ClrNamespace>();
        private static Dictionary<IntPtr, ClrNamespace> instances = new Dictionary<IntPtr, ClrNamespace>();

        public ClrNamespace Parent { get; private set; }
        public string Name { get; private set; }

        static ClrExtensionMethod()
        {
            Type = new PythonClass("extension");
            Type.AddMethod("__call__", __call__);
        }
        public ClrExtensionMethod(string name)
        {
            Pointer = Type.Create();
            //instances.Add(Pointer, this);

            //Parent = parent;
            Name = name;
        }

        private static PythonObject __call__(PythonObject self, PythonTuple args)
        {
            ClrNamespace me = instances[self];

            string name = (args[0] as PythonString).ToString();
            string fullName = me.ToIdentifier() + "." + name;

            Type type = AppDomain.CurrentDomain.GetAssemblies()
                                               .Select(a => a.GetType(fullName))
                                               .FirstOrDefault(t => t != null);

            if (type == null)
                return new ClrNamespace(name, me);
            else
                return TypeManager.ToPython(type);
        }

        public string ToIdentifier()
        {
            if (Parent == null)
                return Name;
            else
                return Parent.ToIdentifier() + "." + Name;
        }
        public override string ToString()
        {
            return "extension " + ToIdentifier();
        }
    }
}