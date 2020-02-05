using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using static Python.Python;

namespace Python
{
    /// <summary>
    /// Wraps a Python object and provides methodes to manipulate it
    /// </summary>
    public class PythonObject :
#if DYNAMIC
        DynamicObject,
#endif
        IEquatable<PythonObject>, IEnumerable<KeyValuePair<string, PythonObject>>
    {
        /// <summary>
        /// The pointer of this Python object
        /// </summary>
        public IntPtr Pointer { get; protected set; }
        /// <summary>
        /// The Python Type of this object
        /// </summary>
        public PythonType Type
        {
            get
            {
                using (PythonException.Checker)
                    return new PythonType(PyObject_Type(Pointer));
            }
        }

        protected PythonObject() { }
        /// <summary>
        /// Wraps the specified Python object pointer
        /// </summary>
        /// <param name="pointer"></param>
        public PythonObject(IntPtr pointer)
        {
            Pointer = pointer;
        }

        /// <summary>
        /// Tries to convert the specified managed object in a Python object
        /// </summary>
        /// <param name="value">The managed object to convert</param>
        /// <returns>The wrapped Python object</returns>
        public static PythonObject From(object value)
        {
            if (value == null)
                return Py_None;

            Type type = value.GetType();

            if (type.IsEnum)
                type = type.GetEnumUnderlyingType();

            if (type == typeof(char)) return new PythonString(new string((char)value, 1));
            if (type == typeof(string)) return new PythonString((string)value);

            if (type == typeof(bool)) return new PythonBoolean((bool)value);
            if (type == typeof(sbyte)) return new PythonNumber((sbyte)value);
            if (type == typeof(byte)) return new PythonNumber((byte)value);
            if (type == typeof(short)) return new PythonNumber((short)value);
            if (type == typeof(ushort)) return new PythonNumber((ushort)value);
            if (type == typeof(int)) return new PythonNumber((int)value);
            if (type == typeof(uint)) return new PythonNumber((uint)value);
            if (type == typeof(long)) return new PythonNumber((long)value);
            if (type == typeof(ulong)) return new PythonNumber((long)(ulong)value);

            if (type == typeof(float)) return PyFloat_FromDouble((float)value);
            if (type == typeof(double)) return PyFloat_FromDouble((double)value);

            if (type.IsValueType)
            {
                // TODO: Generate Python type with slots
                // TODO: Send raw structure to Python
            }

            return null;
        }
        public static object Convert(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero || pointer == Py_None)
                return null;

            IntPtr type = PyObject_Type(pointer);

            if (type == PythonString.Type) return Marshal.PtrToStringAnsi(PyString_AsString(pointer));
            if (type == PythonBoolean.Type) return PyInt_AsLong(pointer) != 0;
            if (type == PythonNumber.IntType) return PyInt_AsLong(pointer);
            if (type == PythonNumber.LongType) return PyLong_AsLongLong(pointer);

            //if (PySequence_Check(pointer))
            //    return Enumerate(pointer);

            return (PythonObject)pointer;
        }
        public static T Convert<T>(IntPtr pointer)
        {
            return (T)Convert(pointer, typeof(T));
        }
        public static object Convert(IntPtr pointer, Type type)
        {
            object result;

            if (!TryConvert(pointer, type, out result))
            {
                if (type.IsValueType)
                    return Activator.CreateInstance(type);
                else
                    return null;
            }

            return result;
        }
        public static bool TryConvert(IntPtr pointer, Type type, out object result)
        {
            IntPtr pythonType = PyObject_Type(pointer);

            if (pythonType == PythonString.Type)
            {
                if (type == typeof(char) && PyString_Size(pointer) == 1)
                {
                    result = Marshal.PtrToStringAnsi(PyString_AsString(pointer))[0];
                    return true;
                }
                if (type == typeof(string))
                {
                    result = Marshal.PtrToStringAnsi(PyString_AsString(pointer));
                    return true;
                }
            }

            if (pythonType == PythonBoolean.Type)
            {
                bool value = PyInt_AsLong(pointer) != 0;

                if (type == typeof(bool)) { result = value; return true; }
                if (type == typeof(sbyte)) { result = (sbyte)(value ? 1 : 0); return true; }
                if (type == typeof(byte)) { result = (byte)(value ? 1 : 0); return true; }
                if (type == typeof(short)) { result = (short)(value ? 1 : 0); return true; }
                if (type == typeof(ushort)) { result = (ushort)(value ? 1 : 0); return true; }
                if (type == typeof(int)) { result = (int)(value ? 1 : 0); return true; }
                if (type == typeof(uint)) { result = (uint)(value ? 1 : 0); return true; }
                if (type == typeof(long)) { result = (long)(value ? 1 : 0); return true; }
                if (type == typeof(ulong)) { result = (ulong)(value ? 1 : 0); return true; }
                if (type == typeof(float)) { result = (float)(value ? 1 : 0); return true; }
                if (type == typeof(double)) { result = (double)(value ? 1 : 0); return true; }
            }

            if (pythonType == PythonNumber.IntType || pythonType == PythonNumber.LongType)
            {
                long signed = PyLong_AsLongLong(pointer);

                if (type == typeof(bool)) { result = signed != 0; return true; }
                if (type == typeof(sbyte)) { result = (sbyte)signed; return true; }
                if (type == typeof(byte)) { result = (byte)signed; return true; }
                if (type == typeof(short)) { result = (short)signed; return true; }
                if (type == typeof(ushort)) { result = (ushort)signed; return true; }
                if (type == typeof(int)) { result = (int)signed; return true; }
                if (type == typeof(uint)) { result = (uint)signed; return true; }
                if (type == typeof(long)) { result = (long)signed; return true; }
                if (type == typeof(float)) { result = (float)signed; return true; }
                if (type == typeof(double)) { result = (double)signed; return true; }

                ulong unsigned = PyLong_AsUnsignedLongLong(pointer);

                if (type == typeof(ulong)) { result = unsigned; return true; }
            }

            if (pythonType == PythonNumber.FloatType)
            {
                double value = PyFloat_AsDouble(pointer);

                if (type == typeof(bool)) { result = value != 0; return true; }
                if (type == typeof(sbyte)) { result = (sbyte)value; return true; }
                if (type == typeof(byte)) { result = (byte)value; return true; }
                if (type == typeof(short)) { result = (short)value; return true; }
                if (type == typeof(ushort)) { result = (ushort)value; return true; }
                if (type == typeof(int)) { result = (int)value; return true; }
                if (type == typeof(uint)) { result = (uint)value; return true; }
                if (type == typeof(long)) { result = (long)value; return true; }
                if (type == typeof(float)) { result = (float)value; return true; }
                if (type == typeof(double)) { result = value; return true; }
                if (type == typeof(ulong)) { result = value; return true; }
            }

            if (pythonType != PythonString.Type && PySequence_Check(pointer))
            {
                if (type.IsArray)
                {
                    Type elementType = type.GetElementType();

                    result = typeof(Enumerable)
                                .GetMethod(nameof(Enumerable.ToArray))
                                .MakeGenericMethod(elementType)
                                .Invoke(null, new object[] { typeof(Enumerable)
                                                                 .GetMethod(nameof(Enumerable.Cast), new[] { typeof(IEnumerable) })
                                                                 .MakeGenericMethod(elementType)
                                                                 .Invoke(null, new object[] { Enumerate(pointer, elementType) }) });
                    return true;
                }

                if (type.IsGenericType)
                {
                    Type genericTypeDefinition = type.GetGenericTypeDefinition();
                    Type genericType = type.GetGenericArguments()?.FirstOrDefault();

                    if (genericTypeDefinition == typeof(IEnumerable<>))
                    {
                        result = typeof(Enumerable)
                                    .GetMethod(nameof(Enumerable.Cast), new[] { typeof(IEnumerable) })
                                    .MakeGenericMethod(genericType)
                                    .Invoke(null, new object[] { Enumerate(pointer, genericType) });
                        return true;
                    }
                    if (genericTypeDefinition == typeof(List<>))
                    {
                        result = typeof(Enumerable)
                                    .GetMethod(nameof(Enumerable.ToList))
                                    .MakeGenericMethod(genericType)
                                    .Invoke(null, new object[] { typeof(Enumerable)
                                                                     .GetMethod(nameof(Enumerable.Cast), new[] { typeof(IEnumerable) })
                                                                     .MakeGenericMethod(genericType)
                                                                     .Invoke(null, new object[] { Enumerate(pointer, genericType) }) });
                        return true;
                    }
                }

                if (type == typeof(IEnumerable))
                {
                    result = Enumerate(pointer);
                    return true;
                }

                if (type == typeof(Array))
                {
                    result = Enumerate(pointer).OfType<object>().ToArray();
                    return true;
                }
            }

            if (type.IsValueType)
                result = Activator.CreateInstance(type);
            else
                result = null;

            return false;
        }
        private static IEnumerable Enumerate(IntPtr pointer, Type type = null)
        {
            int size = PySequence_Size(pointer);

            for (int i = 0; i < size; i++)
            {
                IntPtr item = PySequence_GetItem(pointer, i);

                if (type == null)
                    yield return Convert(item);
                else
                    yield return Convert(item, type);
            }
        }

#if DYNAMIC
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            using (PythonException.Checker)
            {
                PythonObject dir = PyObject_Dir(Pointer);
                int size = PySequence_Size(dir);

                for (int i = 0; i < size; i++)
                {
                    IntPtr key = PySequence_GetItem(dir, i);
                    string name = Marshal.PtrToStringAnsi(PyString_AsString(key));

                    yield return name;
                }
            }
        }
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            using (PythonException.Checker)
                result = (PythonObject)PyObject_GetAttrString(Pointer, binder.Name);

            return true;
        }
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            using (PythonException.Checker)
                PyObject_SetAttrString(Pointer, binder.Name, From(value));

            return true;
        }
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (indexes.Length == 1 && indexes[0] is int)
            {
                using (PythonException.Checker)
                    result = (PythonObject)PySequence_GetItem(Pointer, (int)indexes[0]);

                return true;
            }
            else if (indexes.Length == 2 && indexes[0] is int && indexes[1] is int)
            {
                using (PythonException.Checker)
                    result = (PythonObject)PySequence_GetSlice(Pointer, (int)indexes[0], (int)indexes[1]);

                return true;
            }

            result = null;
            return false;
        }
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            using (PythonException.Checker)
            {
                PythonFunction function = (PythonFunction)PyObject_GetAttrString(Pointer, binder.Name);
                if (function != null)
                {
                    result = function.Invoke(args.Select(a => From(a)).ToArray());
                    return true;
                }
            }

            result = null;
            return false;
        }
#endif

        /// <summary>
        /// The the specified Python attribute of this object
        /// </summary>
        /// <param name="name">The name of the attribute to get</param>
        /// <returns>The attribute</returns>
        public PythonObject GetAttribute(string name)
        {
            using (PythonException.Checker)
                return PyObject_GetAttrString(Pointer, name);
        }
        public PythonObject SetAttribute(string name, PythonObject value)
        {
            using (PythonException.Checker)
                return PyObject_SetAttrString(Pointer, name, value);
        }

        IEnumerable<KeyValuePair<string, PythonObject>> GetEnumerable()
        {
            using (PythonException.Checker)
            {
                PythonObject dir = PyObject_Dir(Pointer);
                int size = PySequence_Size(dir);

                for (int i = 0; i < size; i++)
                {
                    IntPtr key = PySequence_GetItem(dir, i);
                    IntPtr value = PyObject_GetAttr(Pointer, key);

                    string name = Marshal.PtrToStringAnsi(PyString_AsString(key));
                    yield return new KeyValuePair<string, PythonObject>(name, value);
                }
            }
        }
        public IEnumerator<KeyValuePair<string, PythonObject>> GetEnumerator()
        {
            return GetEnumerable().GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            using (PythonException.Checker)
            {
                IntPtr pythonString = PyObject_Str(Pointer);
                if (pythonString == IntPtr.Zero)
                    return null;

                IntPtr cString = PyString_AsString(pythonString);
                if (cString == IntPtr.Zero)
                    return null;
             
                return Marshal.PtrToStringAnsi(cString);
            }
        }
        public bool Equals(PythonObject other)
        {
            return Pointer == other.Pointer;
        }

        public static implicit operator PythonObject(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero)
                return Py_None; // new PythonObject(pointer);

            Py_IncRef(pointer);
            IntPtr type = PyObject_Type(pointer);

            if (type == PythonBoolean.Type) return new PythonBoolean(pointer);
            if (type == PythonString.Type) return new PythonString(pointer);
            if (type == PythonNumber.IntType) return new PythonNumber(pointer);
            if (type == PythonNumber.LongType) return new PythonNumber(pointer);
            if (type == PythonList.Type) return new PythonList(pointer);
            if (type == PythonTuple.Type) return new PythonTuple(pointer);
            if (type == PythonDictionary.Type) return new PythonDictionary(pointer);
            if (type == PythonFunction.Type) return new PythonFunction(pointer);
            if (type == PythonMethod.Type) return new PythonMethod(pointer);
            if (type == PythonType.Type) return new PythonType(pointer);
            if (type == PythonClass.Type) return new PythonClass(pointer);
            if (type == PythonModule.Type) return new PythonModule(pointer);

            return new PythonObject(pointer);
        }
        public static implicit operator IntPtr(PythonObject obj)
        {
            return obj?.Pointer ?? Py_None;
        }

        public static implicit operator PythonObject(string value)
        {
            return (PythonString)value;
        }
        public static implicit operator PythonObject(bool value)
        {
            return (PythonBoolean)value;
        }
        public static implicit operator PythonObject(int value)
        {
            return (PythonNumber)value;
        }
        public static implicit operator PythonObject(long value)
        {
            return (PythonNumber)value;
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr OneArgPythonFunction(IntPtr a);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr TwoArgsPythonFunction(IntPtr a, IntPtr b);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr ThreeArgsPythonFunction(IntPtr a, IntPtr b, IntPtr c);

    public delegate PythonObject OneArgPythonObjectFunction(PythonTuple args);
    public delegate PythonObject TwoArgsPythonObjectFunction(PythonObject self, PythonTuple args);
    public delegate PythonObject ThreeArgsPythonObjectFunction(PythonObject self, PythonTuple args, PythonDictionary kw);

    public enum PythonFunctionType : int
    {
        VarArgs =  0x01,
        Keywords = 0x02,
        NoArgs =   0x04,
        OneArg =   0x08,
        Class =    0x10,
        Static =   0x20,
    }
    public abstract class PythonSequence : PythonObject, IEnumerable<PythonObject>
    {
        public virtual int Size
        {
            get
            {
                return PySequence_Size(this);
            }
        }

        public virtual PythonObject this[int index]
        {
            get
            {
                return PySequence_GetItem(this, index);
            }
            set
            {
                PySequence_SetItem(this, index, value);
            }
        }
        public virtual PythonObject this[int from, int to]
        {
            get
            {
                return PySequence_GetSlice(this, from, to);
            }
            set
            {
                PySequence_SetSlice(this, from, to, value);
            }
        }

        protected PythonSequence() { }
        internal PythonSequence(IntPtr pointer) : base(pointer) { }

        public IEnumerable<PythonObject> GetEnumerable()
        {
            int size = Size;
            for (int i = 0; i < size; i++)
            {
                IntPtr value = this[i];
                yield return value;
            }
        }
        IEnumerator<PythonObject> IEnumerable<PythonObject>.GetEnumerator()
        {
            return GetEnumerable().GetEnumerator();
        }
    }

    public class PythonBoolean : PythonObject
    {
        public new static PythonType Type
        {
            get
            {
                return new PythonType(PyBool_Type);
            }
        }

        public bool Value
        {
            get
            {
                return PyLong_AsLongLong(Pointer) != 0;
            }
        }

        internal PythonBoolean(IntPtr pointer) : base(pointer) { }
        public PythonBoolean(bool value)
        {
            Pointer = PyBool_FromLong(value ? 1 : 0);
        }

        public static explicit operator PythonBoolean(bool value)
        {
            return new PythonBoolean(value);
        }
        public static explicit operator bool(PythonBoolean value)
        {
            return value.Value;
        }
    }
    public class PythonNumber : PythonObject
    {
        public static PythonType IntType
        {
            get
            {
                return new PythonType(PyInt_Type);
            }
        }
        public static PythonType LongType
        {
            get
            {
                return new PythonType(PyLong_Type);
            }
        }
        public static PythonType FloatType
        {
            get
            {
                return new PythonType(PyFloat_Type);
            }
        }

        internal PythonNumber(IntPtr pointer) : base(pointer) { }
        public PythonNumber(bool value)
        {
            Pointer = PyBool_FromLong(value ? 1 : 0);
        }
        public PythonNumber(int value)
        {
            Pointer = PyInt_FromLong(value);
        }
        public PythonNumber(long value)
        {
            Pointer = PyLong_FromLongLong(value);
        }
        public PythonNumber(ulong value)
        {
            Pointer = PyLong_FromUnsignedLongLong(value);
        }
        public PythonNumber(float value)
        {
            Pointer = PyFloat_FromDouble(value);
        }
        public PythonNumber(double value)
        {
            Pointer = PyFloat_FromDouble(value);
        }

        public static explicit operator PythonNumber(bool value)
        {
            return new PythonNumber(value);
        }
        public static explicit operator PythonNumber(int value)
        {
            return new PythonNumber(value);
        }
        public static explicit operator PythonNumber(long value)
        {
            return new PythonNumber(value);
        }
        public static explicit operator long(PythonNumber value)
        {
            return PyLong_AsLongLong(value.Pointer);
        }
    }
    public class PythonString : PythonSequence
    {
        public new static PythonType Type
        {
            get
            {
                return new PythonType(PyString_Type);
            }
        }

        public string Value
        {
            get
            {
                return Marshal.PtrToStringAnsi(PyString_AsString(Pointer));
            }
        }

        internal PythonString(IntPtr pointer) : base(pointer) { }
        public PythonString(string value)
        {
            Pointer = PyString_FromString(value);
        }

        public static explicit operator PythonString(string value)
        {
            return new PythonString(value);
        }
        public static explicit operator string(PythonString value)
        {
            return value.Value;
        }
    }
    public class PythonList : PythonSequence
    {
        public new static PythonType Type
        {
            get
            {
                return new PythonType(PyList_Type);
            }
        }

        public override int Size
        {
            get
            {
                return PyList_Size(this);
            }
        }

        public override PythonObject this[int index]
        {
            get
            {
                return PyList_GetItem(this, index);
            }
            set
            {
                PyList_SetItem(this, index, value);
            }
        }
        public override PythonObject this[int from, int to]
        {
            get
            {
                return PyList_GetSlice(this, from, to);
            }
            set
            {
                PyList_SetSlice(this, from, to, value);
            }
        }

        internal PythonList(IntPtr pointer) : base(pointer) { }
        public PythonList(int size)
        {
            Pointer = PyList_New(size);
        }

        public void Append(PythonObject item)
        {
            using (PythonException.Checker)
                PyList_Append(Pointer, item);
        }
        public void Insert(int index, PythonObject item)
        {
            using (PythonException.Checker)
                PyList_Insert(Pointer, index, item);
        }
        public void Sort()
        {
            using (PythonException.Checker)
                PyList_Sort(Pointer);
        }
    }
    public class PythonTuple : PythonSequence
    {
        public new static PythonType Type
        {
            get
            {
                return new PythonType(PyTuple_Type);
            }
        }

        public override int Size
        {
            get
            {
                using (PythonException.Checker)
                    return PyTuple_Size(Pointer);
            }
        }

        public override PythonObject this[int index]
        {
            get
            {
                using (PythonException.Checker)
                    return PyTuple_GetItem(Pointer, index);
            }
            set
            {
                using (PythonException.Checker)
                    PyTuple_SetItem(Pointer, index, value);
            }
        }
        public override PythonObject this[int from, int to]
        {
            get
            {
                using (PythonException.Checker)
                    return PyTuple_GetSlice(Pointer, from, to);
            }
        }

        internal PythonTuple(IntPtr pointer) : base(pointer) { }
        public PythonTuple(int size)
        {
            Pointer = PyTuple_New(size);
        }
        public PythonTuple(IEnumerable<PythonObject> objects) : this(objects.ToArray()) { }
        public PythonTuple(params PythonObject[] objects)
        {
            Pointer = PyTuple_New(objects.Length);

            for (int i = 0; i < objects.Length; i++)
                this[i] = objects[i];
        }
    }
    public class PythonDictionary : PythonSequence, IDictionary<PythonObject, PythonObject>
    {
        public new static PythonType Type
        {
            get
            {
                return new PythonType(PyDict_Type);
            }
        }

        public override int Size
        {
            get
            {
                using (PythonException.Checker)
                    return PyDict_Size(Pointer);
            }
        }

        public ICollection<PythonObject> Keys
        {
            get
            {
                using (PythonException.Checker)
                {
                    int size = PyDict_Size(Pointer);
                    PythonObject keys = PyDict_Keys(Pointer);

                    List<PythonObject> result = new List<PythonObject>(size);
                    for (int i = 0; i < size; i++)
                        result.Add(PySequence_GetItem(keys, i));

                    return result; // TODO: Make keys editable
                }
            }
        }
        public ICollection<PythonObject> Values
        {
            get
            {
                using (PythonException.Checker)
                {
                    int size = PyDict_Size(Pointer);
                    PythonObject values = PyDict_Values(Pointer);

                    List<PythonObject> result = new List<PythonObject>(size);
                    for (int i = 0; i < size; i++)
                        result.Add(PySequence_GetItem(values, i));

                    return result; // TODO: Make values editable
                }
            }
        }
        public int Count
        {
            get
            {
                using (PythonException.Checker)
                    return PyDict_Size(Pointer);
            }
        }
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public new PythonObject this[int key]
        {
            get
            {
                using (PythonException.Checker)
                    return PyDict_GetItem(Pointer, (PythonNumber)key);
            }
            set
            {
                using (PythonException.Checker)
                    PyDict_SetItem(Pointer, (PythonNumber)key, value);
            }
        }
        public PythonObject this[string key]
        {
            get
            {
                using (PythonException.Checker)
                    return PyDict_GetItem(Pointer, (PythonString)key);
            }
            set
            {
                using (PythonException.Checker)
                    PyDict_SetItem(Pointer, (PythonString)key, value);
            }
        }
        public PythonObject this[PythonObject key]
        {
            get
            {
                using (PythonException.Checker)
                    return PyDict_GetItem(Pointer, key);
            }
            set
            {
                using (PythonException.Checker)
                    PyDict_SetItem(Pointer, key, value);
            }
        }

        internal PythonDictionary(IntPtr pointer) : base(pointer) { }
        public PythonDictionary()
        {
            Pointer = PyDict_New();
        }

        public bool ContainsKey(PythonObject key)
        {
            using (PythonException.Checker)
                return PyDict_GetItem(Pointer, key) != Py_None;
        }
        public void Add(PythonObject key, PythonObject value)
        {
            using (PythonException.Checker)
                PyDict_SetItem(Pointer, key, value);
        }
        public bool Remove(PythonObject key)
        {
            throw new NotImplementedException();
        }
        public bool TryGetValue(PythonObject key, out PythonObject value)
        {
            if (ContainsKey(key))
            {
                value = this[key];
                return true;
            }
            else
            {
                value = Py_None;
                return false;
            }
        }
        public void Add(KeyValuePair<PythonObject, PythonObject> item)
        {
            using (PythonException.Checker)
                PyDict_SetItem(Pointer, item.Key, item.Value);
        }
        public void Clear()
        {
            using (PythonException.Checker)
                PyDict_Clear(Pointer);
        }
        public bool Contains(KeyValuePair<PythonObject, PythonObject> item)
        {
            throw new NotImplementedException();
        }
        public void CopyTo(KeyValuePair<PythonObject, PythonObject>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }
        public bool Remove(KeyValuePair<PythonObject, PythonObject> item)
        {
            throw new NotImplementedException();
        }
        IEnumerator<KeyValuePair<PythonObject, PythonObject>> IEnumerable<KeyValuePair<PythonObject, PythonObject>>.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
    public class PythonFunction : PythonObject
    {
        public new static PythonType Type
        {
            get
            {
                return new PythonType(PyFunction_Type);
            }
        }
        
        private GCHandle functionHandle;

        internal PythonFunction(IntPtr pointer) : base(pointer) { }
        public PythonFunction(string name, OneArgPythonFunction function, PythonFunctionType type, string documentation = null)
        {
            using (PythonException.Checker)
            {
                functionHandle = GCHandle.Alloc(function);

                PyMethodDef methodDef = new PyMethodDef()
                {
                    ml_name = name,
                    ml_meth = Marshal.GetFunctionPointerForDelegate(function),
                    ml_flags = (int)type,
                    ml_doc = documentation
                };

                IntPtr methodPointer = PyMem_Malloc(Marshal.SizeOf(typeof(PyMethodDef)));
                Marshal.StructureToPtr(methodDef, methodPointer, false);
                Pointer = PyCFunction_New(methodPointer, IntPtr.Zero);
            }
        }
        public PythonFunction(string name, OneArgPythonFunction function, string documentation = null) : this(name, function, PythonFunctionType.VarArgs, documentation) { }
        public PythonFunction(string name, TwoArgsPythonFunction function, PythonFunctionType type, string documentation = null)
        {
            using (PythonException.Checker)
            {
                functionHandle = GCHandle.Alloc(function);

                PyMethodDef methodDef = new PyMethodDef()
                {
                    ml_name = name,
                    ml_meth = Marshal.GetFunctionPointerForDelegate(function),
                    ml_flags = (int)type,
                    ml_doc = documentation
                };

                IntPtr methodPointer = PyMem_Malloc(Marshal.SizeOf(typeof(PyMethodDef)));
                Marshal.StructureToPtr(methodDef, methodPointer, false);
                Pointer = PyCFunction_New(methodPointer, IntPtr.Zero);
            }
        }
        public PythonFunction(string name, TwoArgsPythonFunction function, string documentation = null) : this(name, function, PythonFunctionType.VarArgs, documentation) { }
        public PythonFunction(string name, ThreeArgsPythonFunction function, PythonFunctionType type, string documentation = null)
        {
            using (PythonException.Checker)
            {
                functionHandle = GCHandle.Alloc(function);

                PyMethodDef methodDef = new PyMethodDef()
                {
                    ml_name = name,
                    ml_meth = Marshal.GetFunctionPointerForDelegate(function),
                    ml_flags = (int)(type | PythonFunctionType.Keywords),
                    ml_doc = documentation
                };

                IntPtr methodPointer = PyMem_Malloc(Marshal.SizeOf(typeof(PyMethodDef)));
                Marshal.StructureToPtr(methodDef, methodPointer, false);
                Pointer = PyCFunction_New(methodPointer, IntPtr.Zero);
            }
        }
        public PythonFunction(string name, ThreeArgsPythonFunction function, string documentation = null) : this(name, function, PythonFunctionType.VarArgs, documentation) { }

        public PythonObject Invoke(params PythonObject[] parameters)
        {
            int parameterLength = parameters?.Length ?? 0;
            PythonTuple args = new PythonTuple(parameterLength);

            for (int i = 0; i < parameterLength; i++)
                args[i] = parameters[i];

            using (PythonException.Checker)
                return PyObject_CallObject(Pointer, args);
        }
    }
    public class PythonMethod : PythonObject
    {
        public new static PythonType Type
        {
            get
            {
                return new PythonType(PyMethod_Type);
            }
        }
        public static PythonType StaticType
        {
            get
            {
                return new PythonType(PyStaticMethod_Type);
            }
        }

        internal PythonMethod(IntPtr pointer) : base(pointer) { }
        public PythonMethod(PythonType type, PythonFunction function)
        {
            Pointer = PyMethod_New(function, IntPtr.Zero, type);
        }
        public PythonMethod(PythonClass type, PythonFunction function)
        {
            Pointer = PyMethod_New(function, IntPtr.Zero, type);
        }

        public PythonObject Invoke(PythonObject instance, params PythonObject[] parameters)
        {
            int parameterLength = parameters?.Length ?? 0;
            PythonTuple args = new PythonTuple(parameterLength + 1);

            args[0] = instance;
            for (int i = 0; i < parameterLength; i++)
                args[i + 1] = parameters[i];

            using (PythonException.Checker)
                return PyObject_CallObject(Pointer, args);
        }
    }
    public class PythonType : PythonObject
    {
        public new static PythonType Type
        {
            get
            {
                return new PythonType(PyType_Type);
            }
        }

        public string Name
        {
            get
            {
                using (PythonException.Checker)
                    return (string)(PythonString)PyObject_GetAttrString(Pointer, "__name__");
            }
        }
        public PythonDictionary Dictionary
        {
            get
            {
                using (PythonException.Checker)
                    return (PythonDictionary)PyObject_GetAttrString(Pointer, "__dict__");
            }
        }

        internal PythonType(IntPtr pointer) : base(pointer) { }
        public PythonType(string name, params PythonType[] bases)
        {
            using (PythonException.Checker)
            {
                PythonString className = new PythonString(name);

                PythonTuple classBases = new PythonTuple(bases.Length);
                for (int i = 0; i < bases.Length; i++)
                    classBases[i] = bases[i];

                PythonTuple classArgs = new PythonTuple(3);
                classArgs[0] = className;
                classArgs[1] = classBases;
                classArgs[2] = new PythonDictionary();

                Pointer = PyObject_CallObject(PyType_Type, classArgs);
            }
        }

        public void AddMethod(string name, TwoArgsPythonObjectFunction function, string documentation = null)
        {
            using (PythonException.Checker)
            {
                PythonFunction pythonFunction = new PythonFunction(name, (a, b) => MethodProxy(a, b, function), PythonFunctionType.VarArgs, documentation);
                PythonMethod pythonMethod = new PythonMethod(this, pythonFunction);

                PyObject_SetAttrString(this, name, pythonMethod);
            }
        }
        public void AddProperty(string name, TwoArgsPythonObjectFunction getter, TwoArgsPythonObjectFunction setter = null)
        {
            PythonMethod getMethod = new PythonMethod(this, new PythonFunction("get_" + name, (a, b) => MethodProxy(a, b, getter)));
            PythonMethod setMethod = null;

            if (setter != null)
                setMethod = new PythonMethod(this, new PythonFunction("set_" + name, (a, b) => MethodProxy(a, b, setter)));

            PythonTuple propertyArgs = new PythonTuple(setMethod == null ? 1 : 2);
            propertyArgs[0] = getMethod;
            if (setMethod != null)
                propertyArgs[1] = setMethod;

            using (PythonException.Checker)
            {
                PythonObject propertyPython = PyObject_CallObject(PyProperty_Type, propertyArgs);
                PyObject_SetAttrString(Pointer, name, propertyPython);
            }
        }

        public PythonObject Create()
        {
            using (PythonException.Checker)
                return PyObject_CallObject(this, IntPtr.Zero);
        }

        private static IntPtr MethodProxy(IntPtr a, IntPtr b, TwoArgsPythonObjectFunction function)
        {
            IntPtr self, args;

            using (PythonException.Checker)
                self = PyTuple_GetItem(b, 0);
            using (PythonException.Checker)
                args = PyTuple_GetSlice(b, 1, PyTuple_Size(b));

            return function(self, new PythonTuple(args));
        }
    }
    public class PythonClass : PythonObject
    {
        public new static PythonType Type
        {
            get
            {
                return new PythonType(PyClass_Type);
            }
        }

        public string Name
        {
            get
            {
                using (PythonException.Checker)
                    return (string)(PythonString)PyObject_GetAttrString(Pointer, "__name__");
            }
        }
        public PythonDictionary Dictionary
        {
            get
            {
                using (PythonException.Checker)
                    return (PythonDictionary)PyObject_GetAttrString(Pointer, "__dict__");
            }
        }
        public PythonTuple Bases
        {
            get
            {
                using (PythonException.Checker)
                    return (PythonTuple)PyObject_GetAttrString(Pointer, "__bases__");
            }
        }

        internal PythonClass(IntPtr pointer) : base(pointer) { }
        public PythonClass(string name)
        {
            Pointer = PyClass_New(IntPtr.Zero, new PythonDictionary(), PyString_FromString(name));
        }
        public PythonClass(string name, params PythonObject[] bases)
        {
            Pointer = PyClass_New(new PythonTuple(bases), new PythonDictionary(), PyString_FromString(name));
        }

        public void AddMethod(string name, TwoArgsPythonObjectFunction function, string documentation = null)
        {
            using (PythonException.Checker)
            {
                PythonFunction pythonFunction = new PythonFunction(name, (a, b) => MethodProxy(a, b, function), PythonFunctionType.VarArgs, documentation);
                PythonMethod pythonMethod = new PythonMethod(this, pythonFunction);

                PyObject_SetAttrString(this, name, pythonMethod);
            }
        }
        public void AddMethod(string name, ThreeArgsPythonObjectFunction function, string documentation = null)
        {
            using (PythonException.Checker)
            {
                PythonFunction pythonFunction = new PythonFunction(name, (a, b, c) => MethodProxy(a, b, c, function), PythonFunctionType.VarArgs, documentation);
                PythonMethod pythonMethod = new PythonMethod(this, pythonFunction);

                PyObject_SetAttrString(this, name, pythonMethod);
            }
        }
        public void AddStaticMethod(string name, OneArgPythonObjectFunction function, string documentation = null)
        {
            using (PythonException.Checker)
            {
                PythonFunction pythonFunction = new PythonFunction(name, (a, b) => function((PythonTuple)b), PythonFunctionType.VarArgs, documentation);
                Dictionary[name] = pythonFunction;

                /*PythonFunction pythonFunction = new PythonFunction(name, a => StaticMethodProxy(a, function), PythonFunctionType.Static, documentation);
                PythonMethod pythonMethod = new PythonMethod(this, pythonFunction);

                PythonTuple staticMethodArgs = new PythonTuple(1);
                staticMethodArgs[0] = pythonMethod;

                PythonObject pythonStaticMethod = PyObject_CallObject(PythonMethod.StaticType, staticMethodArgs);

                PyObject_SetAttrString(this, name, pythonStaticMethod);*/
            }
        }
        public void AddProperty(string name, TwoArgsPythonObjectFunction getter, TwoArgsPythonObjectFunction setter = null)
        {
            PythonMethod getMethod = new PythonMethod(this, new PythonFunction("get_" + name, (a, b) => MethodProxy(a, b, getter)));
            PythonMethod setMethod = null;

            if (setter != null)
                setMethod = new PythonMethod(this, new PythonFunction("set_" + name, (a, b) => MethodProxy(a, b, setter)));

            PythonTuple propertyArgs = new PythonTuple(setMethod == null ? 1 : 2);
            propertyArgs[0] = getMethod;
            if (setMethod != null)
                propertyArgs[1] = setMethod;

            using (PythonException.Checker)
            {
                PythonObject propertyPython = PyObject_CallObject(PyProperty_Type, propertyArgs);
                PyObject_SetAttrString(this, name, propertyPython);
            }
        }
        public void AddStaticProperty(string name, OneArgPythonObjectFunction getter, OneArgPythonObjectFunction setter = null)
        {
            PythonMethod getMethod = new PythonMethod(this, new PythonFunction("get_" + name, a => StaticMethodProxy(a, getter)));

            PythonType classMethodType = PythonModule.Builtin.GetAttribute("classmethod") as PythonType;

            PythonClass staticPropertyClass = new PythonClass("staticproperty", PyProperty_Type);
            staticPropertyClass.AddMethod("__get__", (a, b) => getter(new PythonTuple()));
            /*{
                var fget = a.GetAttribute("fget");
                var tmp = (PythonObject)PyObject_CallObject(classMethodType, new PythonTuple(fget));
                var get = tmp.GetAttribute("__get__");

                tmp = PyObject_CallObject(get, new PythonTuple(Py_None, b[1]));
                var result = PyObject_CallObject(tmp, Py_None);
                return result;
            });*/

            PythonObject propertyPython = PyObject_CallObject(staticPropertyClass, new PythonTuple((PythonObject)getMethod));
            PyObject_SetAttrString(this, name, propertyPython);
        }

        public PythonObject Create()
        {
            using (PythonException.Checker)
                return PyObject_CallObject(this, IntPtr.Zero);
        }
        public PythonObject CreateEmpty()
        {
            // TODO: Need to create a python object without creating a .NET one

            using (PythonException.Checker)
                return PyObject_Call(this, IntPtr.Zero, new PythonDictionary());
        }

        private static IntPtr MethodProxy(IntPtr a, IntPtr b, TwoArgsPythonObjectFunction function)
        {
            IntPtr self, args;

            using (PythonException.Checker)
                self = PyTuple_GetItem(b, 0);
            using (PythonException.Checker)
                args = PyTuple_GetSlice(b, 1, PyTuple_Size(b));

            return function(self, new PythonTuple(args));
        }
        private static IntPtr MethodProxy(IntPtr a, IntPtr b, IntPtr c, ThreeArgsPythonObjectFunction function)
        {
            IntPtr self, args;

            using (PythonException.Checker)
                self = PyTuple_GetItem(b, 0);
            using (PythonException.Checker)
                args = PyTuple_GetSlice(b, 1, PyTuple_Size(b));

            return function(self, new PythonTuple(args), new PythonDictionary(c));
        }
        private static IntPtr StaticMethodProxy(IntPtr a, OneArgPythonObjectFunction function)
        {
            return function((PythonTuple)a);
        }
    }
    public class PythonModule : PythonObject
    {
        public static PythonModule Builtin
        {
            get
            {
                return Import("__builtin__");
            }
        }
        public static PythonModule Sys
        {
            get
            {
                return Import("sys");
            }
        }
        public static PythonModule Imp
        {
            get
            {
                return Import("imp");
            }
        }

        public new static PythonType Type
        {
            get
            {
                return new PythonType(PyModule_Type);
            }
        }

        public string Name
        {
            get
            {
                using (PythonException.Checker)
                    return (string)(PythonString)PyObject_GetAttrString(Pointer, "__name__");
            }
        }
        public string Filename
        {
            get
            {
                using (PythonException.Checker)
                    return (string)(PythonString)PyObject_GetAttrString(Pointer, "__file__");
            }
        }
        public PythonDictionary Dictionary
        {
            get
            {
                using (PythonException.Checker)
                    return (PythonDictionary)PyModule_GetDict(Pointer);
            }
        }

        internal PythonModule(IntPtr pointer) : base(pointer) { }
        public PythonModule(string name)
        {
            Pointer = PyImport_AddModule(name);
        }

        public static PythonModule FromFile(string path)
        {
            IntPtr result = IntPtr.Zero;

            using (PythonException.Checker)
            {
                FileInfo fileInfo = new FileInfo(path);
                if (!fileInfo.Exists)
                    throw new FileNotFoundException("Could not find file " + path, path);

                PythonObject loadMethod = Imp.GetAttribute("load_source");

                PythonTuple args = new PythonTuple(2);
                args[0] = (PythonString)Path.GetFileNameWithoutExtension(fileInfo.Name);
                args[1] = (PythonString)fileInfo.FullName;

                result = PyObject_CallObject(loadMethod, args);
            }

            return (PythonModule)result;
        }
        public static PythonModule Import(string name)
        {
            using (PythonException.Checker)
                return (PythonObject)PyImport_ImportModule(name) as PythonModule;
        }

        public void AddFunction(string name, OneArgPythonObjectFunction function, string documentation = null)
        {
            using (PythonException.Checker)
            {
                PythonFunction pythonFunction = new PythonFunction(name, (a, b) => function((PythonTuple)b), PythonFunctionType.VarArgs, documentation);
                Dictionary[name] = pythonFunction;
            }
        }
    }

    public class PythonException : Exception
    {
        public class PythonStackFrame : StackFrame
        {
            public string Function { get; }
            public string File { get; }
            public int Line { get; }

            public PythonStackFrame(string function, string file, int line)
            {
                Function = function;
                File = file;
                Line = line;
            }

            public override string ToString()
            {
                return base.ToString();
            }
            public override string GetFileName()
            {
                return File;
            }
            public override int GetFileLineNumber()
            {
                return Line;
            }
        }

        private class PythonExceptionChecker : IDisposable
        {
            [DebuggerHidden]
            public void Dispose()
            {
                IntPtr occured = PyErr_Occurred();
                if (occured == IntPtr.Zero)
                    return;

                IntPtr type = IntPtr.Zero, value = IntPtr.Zero, traceback = IntPtr.Zero;
                PyErr_Fetch(ref type, ref value, ref traceback);

                throw new PythonException(type, value, traceback);
            }
        }
        public static IDisposable Checker { get; } = new PythonExceptionChecker();

        public override string StackTrace
        {
            get
            {
                return string.Join(Environment.NewLine, frames.Reverse<StackFrame>()
                                                              .Select(f =>
                                                              {
                                                                  StringBuilder result = new StringBuilder();

                                                                  result.Append("   à ");
                                                                  result.Append((f as PythonStackFrame)?.Function ?? f.GetMethod().ToString());

                                                                  if (!string.IsNullOrEmpty(f.GetFileName()))
                                                                      result.Append($" dans {f.GetFileName()}:ligne {f.GetFileLineNumber()}");

                                                                  return result;
                                                              }));
            }
        }
        private List<StackFrame> frames = new List<StackFrame>();

        internal PythonException(IntPtr type, IntPtr value, IntPtr traceback) : base(GetExceptionMessage(type, value))
        {
            // Dump .NET callstack
            StackTrace trace = new StackTrace(2, true);
            frames.AddRange(trace.GetFrames().Reverse());

            // Dump Python callstack
            while (traceback != IntPtr.Zero)
            {
                if (PyObject_HasAttrString(traceback, "tb_frame") == 0)
                    break;

                IntPtr framePointer = PyObject_GetAttrString(traceback, "tb_frame");
                if (framePointer == IntPtr.Zero)
                    break;

                IntPtr linePointer = PyObject_GetAttrString(framePointer, "f_lineno");
                IntPtr codePointer = PyObject_GetAttrString(framePointer, "f_code");
                IntPtr filePointer = PyObject_GetAttrString(codePointer, "co_filename");
                IntPtr functionPointer = PyObject_GetAttrString(codePointer, "co_name");

                int line = PyLong_AsLong(linePointer);
                string file = Marshal.PtrToStringAnsi(PyString_AsString(filePointer));
                string function = Marshal.PtrToStringAnsi(PyString_AsString(functionPointer));

                frames.Add(new PythonStackFrame(function, file, line));
                traceback = PyObject_GetAttrString(traceback, "tb_next");
            }

            IntPtr typeName = PyObject_GetAttrString(type, "__name__");

            string exceptionType = Marshal.PtrToStringAnsi(PyString_AsString(typeName));
            string exceptionMessage = Marshal.PtrToStringAnsi(PyString_AsString(value));
        }

        private static string GetExceptionMessage(IntPtr type, IntPtr value)
        {
            if (value == IntPtr.Zero)
                return "";

            IntPtr objectStr = PyObject_Str(value);
            if (objectStr == IntPtr.Zero)
                return "";

            IntPtr cStr = PyString_AsString(objectStr);
            if (cStr == IntPtr.Zero)
                return "";

            string message = Marshal.PtrToStringAnsi(cStr);

#if DEBUG
            Console.Error.WriteLine("PythonException: " + message);
#endif

            return message;
        }
    }
}