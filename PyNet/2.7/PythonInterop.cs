using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using static Python.Python;

namespace Python
{
    public static class TypeManager
    {
#if DEBUG
        public static bool DumpConvertedType { get; set; } = false;
#endif

        public static Type FromPython(IntPtr pointer, Type baseType = null)
        {
            if (pointer == IntPtr.Zero)
                throw new ArgumentNullException("The specified pointer is null");
            if (ObjectManager.PythonToClr.ContainsKey(pointer))
                return (Type)ObjectManager.PythonToClr[pointer];

            // Get Python type name
            PythonObject pythonObject = pointer;
            string typeName = null;

            if (pythonObject is PythonType)
                typeName = (pythonObject as PythonType).Name;
            else if (pythonObject is PythonClass)
                typeName = (pythonObject as PythonClass).Name;
            else if (pythonObject is PythonModule)
                typeName = (pythonObject as PythonModule).Name;
            else
                throw new ArgumentNullException("The specified pointer cannot be converted to .NET type");

            // Convert base type
            if (baseType == null)
            {
                if (pythonObject is PythonClass)
                {
                    PythonTuple bases = (pythonObject as PythonClass).Bases;
                    int basesCount = bases.Size;

                    if (basesCount > 1)
                        throw new NotSupportedException("Cannot convert python type with multiple base classes");
                    else if (basesCount == 1)
                        baseType = FromPython(bases[0]);
                }
            }

            // Setup builders
            AssemblyName assemblyName = new AssemblyName(typeName + "_Assembly");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder module = assemblyBuilder.DefineDynamicModule(typeName + "_Module");

            // Find class specs
            if (baseType == null)
                baseType = typeof(object);

            // Proxy methods
            MethodInfo constructorProxy = typeof(FromPythonHelper).GetMethod(nameof(FromPythonHelper.ConstructorProxy));
            MethodInfo methodProxy = typeof(FromPythonHelper).GetMethod(nameof(FromPythonHelper.MethodProxy));

            // Build type
            TypeBuilder typeBuilder = module.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class, baseType);
            FieldInfo pointerField = typeBuilder.DefineField("pointer", typeof(IntPtr), FieldAttributes.Private);
            List<ConstructorBuilder> constructorBuilders = new List<ConstructorBuilder>();

            foreach (var member in pythonObject)
            {
                PythonType memberType = member.Value.Type;

                switch (memberType.Name)
                {
                    // Properties
                    case "property":
                    {
                        PythonObject pythonGetMethod = member.Value.GetAttribute("fget");
                        PythonObject pythonSetMethod = member.Value.GetAttribute("fset");

                        PropertyInfo clrProperty = baseType.GetProperty(member.Key);
                        MethodInfo clrGetMethod = clrProperty?.GetGetMethod(true);
                        MethodInfo clrSetMethod = clrProperty?.GetSetMethod(true);

                        MethodBuilder getMethodBuilder = null, setMethodBuilder = null;
                        Type propertyType = clrProperty?.PropertyType ?? typeof(object);

                        if (pythonGetMethod != Py_None)
                            getMethodBuilder = FromPythonHelper.AddMethodProxy(typeBuilder, "get_" + member.Key, pythonGetMethod.Pointer, pointerField, clrGetMethod, propertyType, Type.EmptyTypes, true);
                        if (pythonSetMethod != Py_None)
                            setMethodBuilder = FromPythonHelper.AddMethodProxy(typeBuilder, "set_" + member.Key, pythonSetMethod.Pointer, pointerField, clrSetMethod, typeof(void), new Type[] { propertyType }, true);

                        if (clrProperty == null)
                        {
                            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(member.Key, PropertyAttributes.None, typeof(object), Type.EmptyTypes);

                            if (getMethodBuilder != null)
                                propertyBuilder.SetGetMethod(getMethodBuilder);
                            if (setMethodBuilder != null)
                                propertyBuilder.SetSetMethod(setMethodBuilder);
                        }

                        break;
                    }

                    // Methods
                    case "instancemethod":
                    {
                        if (member.Key == typeName) // Constructor
                        {

                        }
                        else switch (member.Key)
                        {
                            // Object
                            case "__str__": break;
                            case "__hash__": break;

                            // IDisposable
                            case "__enter__": break;
                            case "__exit__": break;

                            // Methods
                            default:
                            {
                                MethodInfo method = baseType.GetMethod(member.Key);
                                if (method?.IsFinal == true)
                                    continue;

                                if (method == null)
                                    FromPythonHelper.AddGenericMethodProxy(typeBuilder, member.Key, member.Value.Pointer, pointerField);
                                else
                                    FromPythonHelper.AddMethodProxy(typeBuilder, member.Key, member.Value.Pointer, pointerField, method, method.ReturnType, method.GetParameters().Select(p => p.ParameterType).ToArray());

                                break;
                            }
                        }

                        break;
                    }
                }
            }

            // Build a default constructor if needed
            if (constructorBuilders.Count == 0)
            {
                ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);

                ILGenerator ilGenerator = constructorBuilder.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Dup); // instance

                if (IntPtr.Size == 4)
                    ilGenerator.Emit(OpCodes.Ldc_I4, pointer.ToInt32()); // type
                else if (IntPtr.Size == 8)
                    ilGenerator.Emit(OpCodes.Ldc_I8, pointer.ToInt64()); // type

                ilGenerator.Emit(OpCodes.Ldnull); // null
                ilGenerator.EmitCall(OpCodes.Call, constructorProxy, Type.EmptyTypes); // CallProxy
                ilGenerator.Emit(OpCodes.Stfld, pointerField);

                ilGenerator.Emit(OpCodes.Ret);
            }

            // Build type and check for abstract methods
            TypeInfo typeInfo = typeBuilder.CreateTypeInfo();

            // Register type and return it
            FromPythonHelper.BuiltTypes.Add(typeInfo);
            ObjectManager.Register(typeInfo, pointer);

            return typeInfo;
        }
        public static PythonClass ToPython(Type type)
        {
            if (ObjectManager.ClrToPython.ContainsKey(type))
                return (PythonClass)ObjectManager.ClrToPython[type];

            ClrType typeObject = new ClrType(type);
            ObjectManager.Register(type, typeObject.Pointer);

            return typeObject;
        }

        public static class FromPythonHelper
        {
            public static List<Type> BuiltTypes { get; } = new List<Type>();

            public static MethodBuilder AddMethodProxy(TypeBuilder type, string name, IntPtr pythonMethod, FieldInfo pointerField, MethodInfo baseMethod = null, Type returnType = null, Type[] parameterTypes = null, bool hidden = false)
            {
                if (returnType == null)
                    returnType = baseMethod == null ? typeof(object) : baseMethod.ReturnType;
                if (parameterTypes == null)
                    parameterTypes = baseMethod == null ? new Type[] { typeof(object[]) } : baseMethod.GetParameters().Select(p => p.ParameterType).ToArray();

                MethodAttributes methodAttributes = baseMethod?.IsVirtual == true ? (MethodAttributes.Public | MethodAttributes.Virtual) : MethodAttributes.Public;
                if (hidden)
                    methodAttributes |= MethodAttributes.HideBySig;

                MethodBuilder methodBuilder = type.DefineMethod(name, methodAttributes, returnType, parameterTypes);
                MethodInfo methodProxy = typeof(FromPythonHelper).GetMethod(nameof(MethodProxy));

                ILGenerator ilGenerator = methodBuilder.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);

                ilGenerator.Emit(OpCodes.Ldfld, pointerField); // instance

                if (IntPtr.Size == 4)
                    ilGenerator.Emit(OpCodes.Ldc_I4, pythonMethod.ToInt32()); // method
                else if (IntPtr.Size == 8)
                    ilGenerator.Emit(OpCodes.Ldc_I8, pythonMethod.ToInt64()); // method

                if (parameterTypes.Length == 0)
                    ilGenerator.Emit(OpCodes.Ldnull); // args
                else
                {
                    ilGenerator.Emit(OpCodes.Ldc_I4, parameterTypes.Length);
                    ilGenerator.Emit(OpCodes.Newarr, typeof(object));

                    for (int i = 0; i < parameterTypes.Length; i++)
                    {
                        ilGenerator.Emit(OpCodes.Dup);
                        ilGenerator.Emit(OpCodes.Ldc_I4, i);
                        ilGenerator.Emit(OpCodes.Ldarg, i + 1);

                        if (parameterTypes[i].IsValueType)
                            ilGenerator.Emit(OpCodes.Box, parameterTypes[i]);

                        ilGenerator.Emit(OpCodes.Stelem_Ref);
                    }
                }

                ilGenerator.EmitCall(OpCodes.Call, methodProxy, null); // CallProxy

                if (returnType == typeof(void))
                    ilGenerator.Emit(OpCodes.Pop);
                else if (returnType.IsValueType)
                    ilGenerator.Emit(OpCodes.Unbox_Any, returnType);

                ilGenerator.Emit(OpCodes.Ret);

                if (baseMethod?.IsVirtual == true)
                    type.DefineMethodOverride(methodBuilder, baseMethod);

                return methodBuilder;
            }
            public static MethodBuilder AddGenericMethodProxy(TypeBuilder type, string name, IntPtr pythonMethod, FieldInfo pointerField)
            {
                Type methodReturnType = typeof(object);
                Type[] methodParameterTypes = new[] { typeof(object[]) };
                MethodAttributes methodAttributes = MethodAttributes.Public;
                MethodBuilder methodBuilder = type.DefineMethod(name, methodAttributes, methodReturnType, methodParameterTypes);
                MethodInfo methodProxy = typeof(FromPythonHelper).GetMethod(nameof(MethodProxy));

                ILGenerator ilGenerator = methodBuilder.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);

                ilGenerator.Emit(OpCodes.Ldfld, pointerField); // instance

                if (IntPtr.Size == 4)
                    ilGenerator.Emit(OpCodes.Ldc_I4, pythonMethod.ToInt32()); // method
                else if (IntPtr.Size == 8)
                    ilGenerator.Emit(OpCodes.Ldc_I8, pythonMethod.ToInt64()); // method

                ilGenerator.Emit(OpCodes.Ldarg_1); // args
                ilGenerator.EmitCall(OpCodes.Call, methodProxy, null); // CallProxy
                ilGenerator.Emit(OpCodes.Ret);

                return methodBuilder;
            }

            [DebuggerHidden]
            public static IntPtr ConstructorProxy(object instance, IntPtr type, object[] args)
            {
                int length = args?.Length ?? 0;
                PythonTuple tuple = new PythonTuple(length);

                for (int i = 0; i < length; i++)
                    tuple[i] = PythonObject.From(args[i]);

                PythonObject result;
                using (PythonException.Checker)
                    result = PyObject_CallObject(type, tuple);

                ObjectManager.Register(instance, result);
                return result;
            }
            //[DebuggerHidden]
            public static object MethodProxy(IntPtr instance, IntPtr method, object[] args)
            {
                int length = args?.Length ?? 0;

                PythonTuple parameters = new PythonTuple(length + 1);
                parameters[0] = instance;

                for (int i = 0; i < length; i++)
                    parameters[i + 1] = PythonObject.From(args[i]);

                PythonObject result;
                using (PythonException.Checker)
                    result = PyObject_CallObject(method, parameters);

                return ObjectManager.FromPython(result); // TODO: Send a type hint ?
            }
        }
    }
    
    public static class ObjectManager
    {
        internal static Dictionary<object, IntPtr> ClrToPython = new Dictionary<object, IntPtr>();
        internal static Dictionary<IntPtr, object> PythonToClr = new Dictionary<IntPtr, object>();

        public static void Register(object value, IntPtr pointer)
        {
            if (PythonToClr.ContainsKey(pointer))
                return;

            PythonToClr.Add(pointer, value);
            ClrToPython.Add(value, pointer);
        }

        public static object FromPython(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero)
                return null; // FIXME: Throw exception ?
            if (pointer == Py_None)
                return null;

            if (PythonToClr.ContainsKey(pointer))
                return PythonToClr[pointer];

            object result = PythonObject.Convert(pointer);
            if (result != null)
                return result;

            // TODO: Build a .NET wrapper
            // Return the wrapper

            return null;
        }
        public static IntPtr ToPython(object value)
        {
            if (value == null)
                return Py_None;

            PythonObject result = PythonObject.From(value);
            if (result != null)
                return result;

            if (ClrToPython.ContainsKey(value))
                return (PythonObject)ClrToPython[value];

            Type type = value.GetType();
            PythonClass pythonType = TypeManager.ToPython(type);

            result = pythonType.CreateEmpty();
            Register(value, result);

            return result;
        }
    }

    public class ClrNamespace : PythonObject
    {
        public new static PythonClass Type { get; private set; }

        private static Dictionary<string, ClrNamespace> namespaces = new Dictionary<string, ClrNamespace>();
        private static Dictionary<IntPtr, ClrNamespace> instances = new Dictionary<IntPtr, ClrNamespace>();

        public ClrNamespace Parent { get; private set; }
        public string Name { get; private set; }

        static ClrNamespace()
        {
            Type = new PythonClass("namespace");
            Type.AddMethod("__getattr__", __getattr__);
            Type.AddMethod("__str__", __str__);
        }
        public ClrNamespace(string name, ClrNamespace parent = null)
        {
            Pointer = Type.Create();
            instances.Add(Pointer, this);
            ObjectManager.Register(this, Pointer);

            Parent = parent;
            Name = name;
        }

        private static PythonObject __getattr__(PythonObject self, PythonTuple args)
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
        private static PythonObject __str__(PythonObject self, PythonTuple args)
        {
            ClrNamespace me = instances[self];
            return me.ToString();
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
            return "namespace " + ToIdentifier();
        }
    }
    public class ClrType : PythonClass
    {
        public new Type Type { get; private set; }

        public ClrType(Type type) : base(type.FullName)
        {
            Type = type;

            AddMethod("__init__", __init__);
            AddMethod("__str__", __str__);
            //AddMethod("__hash__", __hash__);

            AddMethod("__getattr__", __getattr__);

            if (type.GetInterfaces().Contains(typeof(IDisposable)))
            {
                //AddMethod("__enter__", __enter__);
                //AddMethod("__exit__", __exit__);
            }

            if (type.IsSubclassOf(typeof(IEnumerable)))
            {
                //AddMethod("__iter__", __iter__);
                //AddMethod("__reversed__", __reversed__);
            }

            List<MethodInfo> propertyMethods = new List<MethodInfo>();

            // Add properties
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                MethodInfo getMethod = property.GetGetMethod(true);
                MethodInfo setMethod = property.GetSetMethod(true);

                propertyMethods.Add(getMethod);
                if (setMethod != null)
                    propertyMethods.Add(setMethod);

                AddProperty(property.Name, (a, b) => MethodProxy(getMethod, a, b), setMethod == null ? (TwoArgsPythonObjectFunction)null : (a, b) => MethodProxy(setMethod, a, b));
            }

            // Add static properties
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                MethodInfo getMethod = property.GetGetMethod(true);
                MethodInfo setMethod = property.GetSetMethod(true);

                propertyMethods.Add(getMethod);
                if (setMethod != null)
                    propertyMethods.Add(setMethod);

                AddStaticProperty(property.Name, a => MethodProxy(getMethod, null, a), setMethod == null ? (OneArgPythonObjectFunction)null : a => MethodProxy(setMethod, null, a));
            }

            // Add methods
            var methodGroups = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Except(propertyMethods)
                .Where(m => m.Name != "GetHashCode")
                .GroupBy(m => m.Name);

            foreach (var methodGroup in methodGroups)
                AddMethod(methodGroup.Key, (a, b) => MethodProxy(methodGroup.ToArray(), a, b as PythonTuple));

            var staticMethodGroups = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name != "GetType")
                .GroupBy(m => m.Name);

            foreach (var methodGroup in staticMethodGroups)
                AddStaticMethod(methodGroup.Key, a => MethodProxy(methodGroup.ToArray(), null, a as PythonTuple));

            // Add enum values
            if (type.IsEnum)
            {
                foreach (string name in type.GetEnumNames())
                    SetAttribute(name, From(Enum.Parse(type, name)));
            }
        }

        private static PythonObject MethodProxy(MethodInfo method, PythonObject self, PythonTuple args)
        {
            object clrObject = self == null ? null : ObjectManager.FromPython(self);

            object[] parameters = new object[args.Size];
            for (int i = 0; i < parameters.Length; i++)
                parameters[i] = Convert(args[i]);

            object clrResult = method.Invoke(clrObject, parameters);
            return ObjectManager.ToPython(clrResult);
        }
        private static PythonObject MethodProxy(MethodInfo[] methods, PythonObject self, PythonTuple args)
        {
            object clrObject = self == null ? null : ObjectManager.FromPython(self);

            object result;
            if (!TryCallMethod(methods, clrObject, args, out result))
                throw new ArgumentException("Could not find any overload matching the specified arguments");

            return ObjectManager.ToPython(result);
        }

        private static bool TryCallMethod(IEnumerable<MethodBase> methods, object instance, PythonTuple args, out object result)
        {
            PythonObject[] pythonParameters = (args as IEnumerable<PythonObject>).ToArray();

            foreach (MethodBase method in methods)
            {
                ParameterInfo[] parametersInfo = method.GetParameters();
                if (pythonParameters.Length > parametersInfo.Length)
                    continue;

                bool match = true;
                object[] parameters = new object[parametersInfo.Length];

                int extension = method.IsDefined(typeof(ExtensionAttribute), false) ? 1 : 0;
                if (extension > 0)
                    parameters[0] = instance;

                int clrIndex = parameters.Length - 1;
                int pythonIndex = clrIndex - extension;

                for (; pythonIndex >= 0; pythonIndex--, clrIndex--)
                {
                    if (pythonIndex >= pythonParameters.Length)
                    {
                        if (parametersInfo[clrIndex].IsOptional)
                        {
                            parameters[clrIndex] = parametersInfo[clrIndex].DefaultValue;
                            continue;
                        }
                        else
                        {
                            match = false;
                            break;
                        }
                    }

                    object parameter;
                    if (!TryConvert(pythonParameters[pythonIndex], parametersInfo[clrIndex].ParameterType, out parameter))
                    {
                        match = false;
                        break;
                    }

                    parameters[clrIndex] = parameter;
                }

                if (!match)
                    continue;

                if (method is ConstructorInfo)
                    result = (method as ConstructorInfo).Invoke(parameters);
                else
                    result = method.Invoke(instance, parameters);

                return true;
            }

            result = null;
            return false;
        }

        private PythonObject __init__(PythonObject self, PythonTuple args, PythonDictionary kw)
        {
            object clrObject;

            if (kw.Pointer != IntPtr.Zero)
                return Py_None;

            if (Type.IsAbstract)
                clrObject = new object();
            else
            {
                ConstructorInfo[] constructors = Type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

                if (!TryCallMethod(constructors, null, args, out clrObject))
                    throw new ArgumentException("Could not find any overload matching the specified arguments");
            }

            ClrObject pythonObject = new ClrObject(self, clrObject);
            ObjectManager.Register(clrObject, pythonObject.Pointer);

            return Py_None;
        }
        private static PythonObject __str__(PythonObject self, PythonTuple args)
        {
            object value = ObjectManager.FromPython(self);
            if (value == null)
                return Py_None;

            return (PythonString)value.ToString();
        }
        private static PythonObject __hash__(PythonObject self, PythonTuple args)
        {
            object value = ObjectManager.FromPython(self);
            if (value == null)
                return Py_None;

            return (PythonNumber)value.GetHashCode();
        }
        private static PythonObject __exit__(PythonObject self, PythonTuple args)
        {
            object value = ObjectManager.FromPython(self);
            if (value == null)
                return Py_None;

            (value as IDisposable).Dispose();
            return Py_None;
        }
        private static PythonObject __getattr__(PythonObject self, PythonTuple args)
        {
            object value = ObjectManager.FromPython(self);
            if (value == null)
                return Py_None;

            string name = (args[0] as PythonString)?.Value;
            if (name == null)
                throw new Exception("AttributeError");

            // TODO: Ugly hack to handle .NET PythonObject classes
            // Handle this case in TypeManager.ToPython ...
            if (value is PythonObject)
            {
                IntPtr pointer = (value as PythonObject).Pointer;

                if (PyObject_HasAttrString(pointer, name) != 0)
                    return (value as PythonObject).GetAttribute(name);
            }

            IEnumerable<MethodInfo> extensions = CollectExtensionMethods(value.GetType(), name);
            if (extensions.Any())
                return new PythonFunction(name, (a, b) => ExtensionMethodCallback(value, extensions, (PythonTuple)(PythonObject)b)); // TODO: return virtual method object
            else
                throw new Exception("AttributeError");
        }

        private static IEnumerable<MethodInfo> CollectExtensionMethods(Type type, string name)
        {
            // Start with type assembly
            Assembly typeAssembly = type.Assembly;
            IEnumerable<MethodInfo> typeAssemblyMethods = typeAssembly.GetTypes().Where(t => t.IsSealed && !t.IsGenericType && !t.IsNested)
                                                                                 .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                                                                                 .Where(m => m.IsDefined(typeof(ExtensionAttribute), false))
                                                                                 .Where(m => m.GetParameters()[0].ParameterType == type);
            foreach (MethodInfo methodInfo in typeAssemblyMethods)
                yield return methodInfo;
        }
        private static PythonObject ExtensionMethodCallback(object instance, IEnumerable<MethodInfo> methods, PythonTuple args)
        {
            object result;
            if (!TryCallMethod(methods, instance, args, out result))
                throw new ArgumentException("Could not find any overload matching the specified arguments");

            return ObjectManager.ToPython(result);
        }


        /*
            Python system methods

            __del__
            __cmp__
            __eq__
            __ne__
            __lt__
            __gt__
            __le__
            __ge__
            __pos__
            __neg__
            __abs__
            __invert__
            __round__
            __floor__
            __ceil__
            __trunc__
            __add__
            __sub__
            __mul__
            __floordiv__
            __div__
            __truediv__
            __mod__
            __divmod__
            __pow__
            __lshift__
            __rshift__
            __and__
            __or__
            __xor__
            __radd__
            __rsub__
            __rmul__
            __rfloordiv__
            __rdiv__
            __rtruediv__
            __rmod__
            __rdivmod__
            __rpow__
            __rlshift__
            __rrshift__
            __rand__
            __ror__
            __rxor__
            __iadd__
            __isub__
            __imul__
            __ifloordiv__
            __idiv__
            __itruediv__
            __imod__
            __idivmod__
            __ipow__
            __ilshift__
            __irshift__
            __iand__
            __ior__
            __ixor__
            __int__
            __long__
            __float__
            __complex__
            __oct__
            __hex__
            __index__
            __coerce__
            __repr__
            __unicode__
            __format__
            __nonzero__
            __dir__
            __sizeof__
            __getattr__
            __setattr__
            __delattr__
            __len__
            __getitem__
            __setitem__
            __delitem__
            __iter__
            __reversed__
            __contains__
            __missing__
            __instancecheck__
            __subclasscheck__
            __call__
            __enter__
            __get__
            __set__
            __delete__
            __copy__
            __deepcopy__
            __getinitargs__
            __getnewargs__
            __getstate__
            __setstate__
            __reduce__
            __reduce_ex__
        */
    }
    public class ClrObject : PythonObject
    {
        public object Object { get; private set; }

        internal ClrObject(IntPtr pointer, object value)
        {
            Pointer = pointer;
            Object = value;
        }
    }
}