using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Python
{
    public static class Python
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string filename);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("libdl.so")]
        private static extern IntPtr dlopen(string filename, int flags);
        [DllImport("libdl.so")]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);
        [DllImport("libdl.so")]
        private static extern string dlerror();

        private const string library = "python27.dll";

        private static IntPtr module = IntPtr.Zero;
        private static Dictionary<string, IntPtr> addresses = new Dictionary<string, IntPtr>();

        private static IntPtr GetAddress(string symbol)
        {
            IntPtr address;
            if (addresses.TryGetValue(symbol, out address))
                return address;

            if (module == IntPtr.Zero)
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    module = LoadLibrary("python27.dll");
                    if (module == IntPtr.Zero)
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                else if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    module = dlopen("libpython2.7.so", 2); // RTLD_NOW
                    if (module == IntPtr.Zero)
                        throw new Exception(dlerror());
                }
                else
                    throw new PlatformNotSupportedException("Unable to load Python library on this platform");
            }

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                address = GetProcAddress(module, symbol);
                if (address == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            else if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                address = dlsym(module, symbol);
                if (address == IntPtr.Zero)
                    throw new Exception(dlerror());
            }

            addresses.Add(symbol, address);
            return address;
        }

        public static bool Py_NoSiteFlag
        {
            get
            {
                return Marshal.ReadInt32(GetAddress("Py_NoSiteFlag")) != 0;
            }
            set
            {
                Marshal.WriteInt32(GetAddress("Py_NoSiteFlag"), value ? 1 : 0);
            }
        }

        public static IntPtr Py_None
        {
            get
            {
                return Py_BuildValue("");
            }
        }

        public static IntPtr PyBool_Type
        {
            get
            {
                return GetAddress("PyBool_Type");
            }
        }
        public static IntPtr PyInt_Type
        {
            get
            {
                return GetAddress("PyInt_Type");
            }
        }
        public static IntPtr PyLong_Type
        {
            get
            {
                return GetAddress("PyLong_Type");
            }
        }
        public static IntPtr PyFloat_Type
        {
            get
            {
                return GetAddress("PyFloat_Type");
            }
        }
        public static IntPtr PySlice_Type
        {
            get
            {
                return GetAddress("PySlice_Type");
            }
        }
        public static IntPtr PyList_Type
        {
            get
            {
                return GetAddress("PyList_Type");
            }
        }
        public static IntPtr PyTuple_Type
        {
            get
            {
                return GetAddress("PyTuple_Type");
            }
        }
        public static IntPtr PyDict_Type
        {
            get
            {
                return GetAddress("PyDict_Type");
            }
        }
        public static IntPtr PyFunction_Type
        {
            get
            {
                return GetAddress("PyFunction_Type");
            }
        }
        public static IntPtr PyMethod_Type
        {
            get
            {
                return GetAddress("PyMethod_Type");
            }
        }
        public static IntPtr PyType_Type
        {
            get
            {
                return GetAddress("PyType_Type");
            }
        }
        public static IntPtr PyClass_Type
        {
            get
            {
                return GetAddress("PyClass_Type");
            }
        }
        public static IntPtr PyInstance_Type
        {
            get
            {
                return GetAddress("PyInstance_Type");
            }
        }
        public static IntPtr PyModule_Type
        {
            get
            {
                return GetAddress("PyModule_Type");
            }
        }
        public static IntPtr PyString_Type
        {
            get
            {
                return GetAddress("PyString_Type");
            }
        }
        public static IntPtr PyProperty_Type
        {
            get
            {
                return GetAddress("PyProperty_Type");
            }
        }
        public static IntPtr PyRange_Type
        {
            get
            {
                return GetAddress("PyRange_Type");
            }
        }
        public static IntPtr PyReversed_Type
        {
            get
            {
                return GetAddress("PyReversed_Type");
            }
        }
        public static IntPtr PySet_Type
        {
            get
            {
                return GetAddress("PySet_Type");
            }
        }
        public static IntPtr PyStaticMethod_Type
        {
            get
            {
                return GetAddress("PyStaticMethod_Type");
            }
        }
        public static IntPtr PyFrame_Type
        {
            get
            {
                return GetAddress("PyFrame_Type");
            }
        }

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Py_IncRef(IntPtr pointer);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Py_DecRef(IntPtr pointer);

        #region General

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Py_Initialize();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Py_IsInitialized();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Py_Finalize();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Py_NewInterpreter();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Py_EndInterpreter(IntPtr threadState);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Py_Main(int argc, string[] argv);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern string Py_GetProgramName();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Py_SetProgramName(string name);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern string Py_GetPythonHome();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Py_SetPythonHome(string home);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern string Py_GetVersion();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern string Py_GetPlatform();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern string Py_GetCopyright();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern string Py_GetCompiler();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern string Py_GetBuildInfo();

        #endregion
        #region Threads

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyThreadState_New(IntPtr istate);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyThreadState_Get();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyThread_get_key_value(IntPtr key);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyThread_get_thread_ident();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyThread_set_key_value(IntPtr key, IntPtr value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyThreadState_Swap(IntPtr key);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyGILState_Ensure();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyGILState_Release(IntPtr gs);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyGILState_GetThisThreadState();

        #endregion
        #region Runtime

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyEval_InitThreads();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyEval_AcquireLock();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyEval_ReleaseLock();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyEval_AcquireThread(IntPtr tstate);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyEval_ReleaseThread(IntPtr tstate);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyEval_SaveThread();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyEval_RestoreThread(IntPtr tstate);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyEval_GetBuiltins();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyEval_GetGlobals();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyEval_GetLocals();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyEval_EvalCode(IntPtr co, IntPtr globals, IntPtr locals);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyRun_SimpleString(string code);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyRun_String(string code, int start, IntPtr globals, IntPtr locals);

        #endregion
        #region Objects

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyObject_HasAttrString(IntPtr pointer, string name);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyObject_GetAttrString(IntPtr pointer, string name);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyObject_SetAttrString(IntPtr pointer, string name, IntPtr value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyObject_HasAttr(IntPtr pointer, IntPtr name);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyObject_GetAttr(IntPtr pointer, IntPtr name);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyObject_SetAttr(IntPtr pointer, IntPtr name, IntPtr value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyObject_GetItem(IntPtr pointer, IntPtr key);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyObject_SetItem(IntPtr pointer, IntPtr key, IntPtr value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyObject_DelItem(IntPtr pointer, IntPtr key);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyObject_GetIter(IntPtr op);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyObject_Call(IntPtr pointer, IntPtr args, IntPtr kw);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyObject_CallObject(IntPtr pointer, IntPtr args);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyObject_Compare(IntPtr value1, IntPtr value2);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyObject_IsInstance(IntPtr ob, IntPtr type);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyObject_IsSubclass(IntPtr ob, IntPtr type);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyCallable_Check(IntPtr pointer);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyObject_IsTrue(IntPtr pointer);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyObject_Size(IntPtr pointer);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyObject_Hash(IntPtr op);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyObject_Repr(IntPtr pointer);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyObject_Str(IntPtr pointer);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyObject_Type(IntPtr pointer);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyObject_Unicode(IntPtr pointer);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyObject_Dir(IntPtr pointer);

        #endregion
        #region Numbers

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyBool_FromLong(int value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyNumber_Int(IntPtr ob);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyNumber_Long(IntPtr ob);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyNumber_Float(IntPtr ob);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool PyNumber_Check(IntPtr ob);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyInt_FromLong(int value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyInt_AsLong(IntPtr value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyInt_FromString(string value, IntPtr end, int radix);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyInt_GetMax();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyLong_FromLong(long value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyLong_FromUnsignedLong(uint value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyLong_FromDouble(double value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyLong_FromLongLong(long value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyLong_FromUnsignedLongLong(ulong value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyLong_FromString(string value, IntPtr end, int radix);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyLong_AsLong(IntPtr value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint PyLong_AsUnsignedLong(IntPtr value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern long PyLong_AsLongLong(IntPtr value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong PyLong_AsUnsignedLongLong(IntPtr value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyFloat_FromDouble(double value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyFloat_FromString(IntPtr value, IntPtr junk);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern double PyFloat_AsDouble(IntPtr ob);

        #endregion
        #region Sequences

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool PySequence_Check(IntPtr pointer);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PySequence_GetItem(IntPtr pointer, int index);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PySequence_SetItem(IntPtr pointer, int index, IntPtr value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PySequence_DelItem(IntPtr pointer, int index);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PySequence_GetSlice(IntPtr pointer, int i1, int i2);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PySequence_SetSlice(IntPtr pointer, int i1, int i2, IntPtr v);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PySequence_DelSlice(IntPtr pointer, int i1, int i2);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PySequence_Size(IntPtr pointer);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PySequence_Contains(IntPtr pointer, IntPtr item);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PySequence_Concat(IntPtr pointer, IntPtr other);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PySequence_Repeat(IntPtr pointer, int count);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PySequence_Index(IntPtr pointer, IntPtr item);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PySequence_Count(IntPtr pointer, IntPtr value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PySequence_Tuple(IntPtr pointer);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PySequence_List(IntPtr pointer);

        #endregion
        #region Strings

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyString_FromString(string value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyString_FromStringAndSize(string value, int size);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyString_AsStringAndSize(IntPtr pointer, StringBuilder buffer, ref int length);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyString_AsString(IntPtr op);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyString_Size(IntPtr pointer);

        #endregion
        #region Dictionaries

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyDict_New();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyDictProxy_New(IntPtr dict);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyDict_GetItem(IntPtr pointer, IntPtr key);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyDict_GetItemString(IntPtr pointer, string key);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyDict_SetItem(IntPtr pointer, IntPtr key, IntPtr value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyDict_SetItemString(IntPtr pointer, string key, IntPtr value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyDict_DelItem(IntPtr pointer, IntPtr key);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyMapping_HasKey(IntPtr pointer, IntPtr key);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyDict_Keys(IntPtr pointer);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyDict_Values(IntPtr pointer);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyDict_Items(IntPtr pointer);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyDict_Copy(IntPtr pointer);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyDict_Update(IntPtr pointer, IntPtr other);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyDict_Clear(IntPtr pointer);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyDict_Size(IntPtr pointer);

        #endregion
        #region Lists

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyList_New(int size);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyList_AsTuple(IntPtr pointer);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyList_GetItem(IntPtr pointer, int index);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyList_SetItem(IntPtr pointer, int index, IntPtr value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyList_Insert(IntPtr pointer, int index, IntPtr value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyList_Append(IntPtr pointer, IntPtr value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyList_Reverse(IntPtr pointer);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyList_Sort(IntPtr pointer);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyList_GetSlice(IntPtr pointer, int start, int end);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyList_SetSlice(IntPtr pointer, int start, int end, IntPtr value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyList_Size(IntPtr pointer);

        #endregion
        #region Tuples

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyTuple_New(int size);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyTuple_GetItem(IntPtr pointer, int index);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyTuple_SetItem(IntPtr pointer, int index, IntPtr value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyTuple_GetSlice(IntPtr pointer, int start, int end);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyTuple_Size(IntPtr pointer);

        #endregion
        #region Iterators

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool PyIter_Check(IntPtr pointer);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyIter_Next(IntPtr pointer);

        #endregion
        #region Modules

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Py_InitModule(string name, IntPtr methods);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyModule_Check(IntPtr pointer);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern string PyModule_GetName(IntPtr module);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyModule_GetDict(IntPtr module);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern string PyModule_GetFilename(IntPtr module);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyImport_Import(IntPtr name);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyImport_ImportModule(string name);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyImport_ReloadModule(IntPtr module);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyImport_AddModule(string name);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyImport_GetModuleDict();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PySys_SetArgv(int argc, IntPtr argv);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PySys_GetObject(string name);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PySys_SetObject(string name, IntPtr ob);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyModule_AddIntConstant(IntPtr module, string name, long value);

        #endregion
        #region Types

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool PyType_IsSubtype(IntPtr t1, IntPtr t2);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyType_GenericNew(IntPtr type, IntPtr args, IntPtr kw);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyType_GenericAlloc(IntPtr type, int n);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyType_Ready(IntPtr type);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr _PyType_Lookup(IntPtr type, IntPtr name);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyObject_GenericGetAttr(IntPtr obj, IntPtr name);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyObject_GenericSetAttr(IntPtr obj, IntPtr name, IntPtr value);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr _PyObject_GetDictPtr(IntPtr obj);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyObject_GC_New(IntPtr tp);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyObject_GC_Del(IntPtr tp);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyObject_GC_Track(IntPtr tp);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyObject_GC_UnTrack(IntPtr tp);

        #endregion
        #region Memory

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyMem_Malloc(int size);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyMem_Realloc(IntPtr ptr, int size);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyMem_Free(IntPtr ptr);

        #endregion
        #region Exceptions

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyErr_SetString(IntPtr ob, string message);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyErr_SetObject(IntPtr ob, IntPtr message);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyErr_SetFromErrno(IntPtr ob);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyErr_SetNone(IntPtr ob);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyErr_ExceptionMatches(IntPtr exception);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyErr_GivenExceptionMatches(IntPtr ob, IntPtr val);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyErr_NormalizeException(IntPtr ob, IntPtr val, IntPtr tb);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyErr_Occurred();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyErr_Fetch(ref IntPtr ob, ref IntPtr val, ref IntPtr tb);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyErr_Restore(IntPtr ob, IntPtr val, IntPtr tb);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyErr_Clear();

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyErr_Print();

        #endregion
        #region Methods

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyMethod_Self(IntPtr ob);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyMethod_Function(IntPtr ob);

        #endregion

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Py_BuildValue(string format);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Py_CompileString(string code, string file, IntPtr tok);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyImport_ExecCodeModule(string name, IntPtr code);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyCFunction_New(IntPtr ml, IntPtr self);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyCFunction_New(ref PyMethodDef ml, IntPtr self);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyCFunction_NewEx(IntPtr ml, IntPtr self, IntPtr mod);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyCFunction_Call(IntPtr func, IntPtr args, IntPtr kw);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyClass_New(IntPtr bases, IntPtr dict, IntPtr name);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyInstance_New(IntPtr cls, IntPtr args, IntPtr kw);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyInstance_NewRaw(IntPtr cls, IntPtr dict);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyMethod_New(IntPtr func, IntPtr self, IntPtr cls);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyCode_NewEmpty(string filename, string funcname, int firstlineno);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyFrame_New(IntPtr tstate, IntPtr code, IntPtr globals, IntPtr unknown);

        [DllImport(library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void _Py_Dealloc(IntPtr pointer);

        public static readonly IntPtr Py_single_input = new IntPtr(256);
        public static readonly IntPtr Py_file_input = new IntPtr(257);
        public static readonly IntPtr Py_eval_input = new IntPtr(258);



        public const int METH_VARARGS = 0x0001;
        public const int METH_KEYWORDS = 0x0002;
        public const int METH_NOARGS = 0x0004;
        public const int METH_O = 0x0008;
        public const int METH_CLASS = 0x0010;
        public const int METH_STATIC = 0x0020;



        [StructLayout(LayoutKind.Sequential)]
        public struct PyMethodDef
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string ml_name;

            //[MarshalAs(UnmanagedType.FunctionPtr)]
            public IntPtr ml_meth;

            public int ml_flags;

            [MarshalAs(UnmanagedType.LPStr)]
            public string ml_doc;
        }
    }
}