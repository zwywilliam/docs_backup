using System;
using System.Runtime.InteropServices;
using XLua;
using XLua.LuaDLL;
using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;


/*

see luaconf.h.in for luaInteger and lua_Number 
     
*/

//typedef union Value {
//  GCObject *gc;    /* collectable objects */
//void* p;         /* light userdata */
//int b;           /* booleans */
//lua_CFunction f; /* light C functions */
//lua_Integer i;   /* integer numbers */
//lua_Number n;    /* float numbers */
//} Value;


//#define TValuefields	Value value_; int tt_


//typedef struct lua_TValue
//{
//    TValuefields;
//}
//TValue;

//LUA_TNUMBER
//LUA_TNUMINT


// TValue from lua source lobject.h


// 32 bit version
[StructLayout(LayoutKind.Explicit, Size = 8)]
public struct LuaTValue32
{
    // GCObject*
    [FieldOffset(0)]
    public IntPtr gc;

    // bool
    [FieldOffset(0)]
    public int b;

    //lua_CFunction
    [FieldOffset(0)]
    public IntPtr f;

    // number
    [FieldOffset(0)]
    public float n;

    // integer value
    [FieldOffset(0)]
    public int i;

    // int tt_
    [FieldOffset(4)]
    public int tt_;
}


// 64 bit version
[StructLayout(LayoutKind.Explicit, Size = 12)]
public struct LuaTValue64
{
    // uint64
    [FieldOffset(0)]
    public UInt64 u64;

    // number
    [FieldOffset(0)]
    public double n;

    // integer value
    [FieldOffset(0)]
    public long i;

    // int tt_
    [FieldOffset(8)]
    public int tt_;
}


//#define CommonHeader	GCObject *next; lu_byte tt; lu_byte marked

//typedef struct Table
//{
//    CommonHeader;
//  lu_byte flags;  /* 1<<p means tagmethod(p) is not present */
//    lu_byte lsizenode;  /* log2 of size of 'node' array */
//    unsigned int sizearray;  /* size of 'array' array */
//    TValue* array;  /* array part */
//    Node* node;
//    Node* lastfree;  /* any free position is before this position */
//    struct Table *metatable;
//  GCObject* gclist;
//}
//Table;

[StructLayout(LayoutKind.Sequential)]
public struct LuaTableRawDef
{
    public IntPtr next;

    // lu_byte tt; lu_byte marked; lu_byte flags; lu_byte lsizenode;
    public uint bytes;

    // unsigned int sizearray
    public uint sizearray;

    // TValue* array
    public IntPtr array;

    // Node* node
    public IntPtr node;

    // Node* lastfree
    public IntPtr lastfree;

    // Table* metatable
    public IntPtr metatable;

    // GCObejct* gclist
    public IntPtr gclist;
}



public static class LuaEnvValues
{
    public static bool Is64Bit = true;

    public const int LUA_TNUMBER = 3;
    public const int LUA_TTABLE = 5;

    public const int LUA_TNUMFLT = (LUA_TNUMBER | (0 << 4));
    public const int LUA_TNUMINT = (LUA_TNUMBER | (1 << 4));
}

// check index and lua table is lived
public class LuaTableSafeAccess
{
    WeakReference<LuaTablePin> target;

    public LuaTableSafeAccess(LuaTablePin TablePin)
    {
        target = new WeakReference<LuaTablePin>(TablePin);
    }

    public LuaTablePin GetRaw()
    {
        LuaTablePin targetObj = null;
        target.TryGetTarget(out targetObj);
        return targetObj;
    }

    public double GetDouble(int index)
    {
        LuaTablePin targetObj;
        target.TryGetTarget(out targetObj);
        if(targetObj != null)
        {
            return targetObj.GetDouble(index);
        }
        return 0;
    }

    public int GetInt(int index)
    {
        LuaTablePin targetObj;
        target.TryGetTarget(out targetObj);
        if (targetObj != null)
        {
            return targetObj.GetInt(index);
        }
        return 0;
    }

    public void SetInt(int index, int v)
    {
        LuaTablePin targetObj;
        target.TryGetTarget(out targetObj);
        if (targetObj != null)
        {
            targetObj.SetInt(index, v);
        }
    }
}

[LuaCallCSharp]
public unsafe class LuaTablePin
{
    LuaTableRawDef* TableRawPtr;

    [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
    public static int PinFunction(IntPtr L)
    {
        ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
        IntPtr TablePtr = Lua.lua_topointer(L, 1);
        LuaTablePin gen_to_be_invoked = (LuaTablePin)translator.FastGetCSObj(L, 2);
        if (TablePtr != IntPtr.Zero && Lua.lua_istable(L, 1))
        {
            gen_to_be_invoked.TableRawPtr = (LuaTableRawDef*)TablePtr;
        }
        return 0;
    }

    public static void RegisterPinFunc(System.IntPtr L)
    {
        string name = "lua_safe_pin_bind";
        Lua.lua_pushstdcallcfunction(L, PinFunction);
        if (0 != Lua.xlua_setglobal(L, name))
        {
            throw new Exception("call xlua_setglobal fail!");
        }
    }

    public int GetInt(int index)
    {
        if(LuaEnvValues.Is64Bit)
        {
            LuaTValue64* tv = (LuaTValue64*)(TableRawPtr->array) + index;
            if (tv->tt_ == LuaEnvValues.LUA_TNUMINT)
            {
                return (int)tv->i;
            }
            else
            {
                return (int)tv->n;
            }
        }
        else
        {
            LuaTValue32* tv = (LuaTValue32*)(TableRawPtr->array) + index;
            if (tv->tt_ == LuaEnvValues.LUA_TNUMINT)
            {
                return (int)tv->i;
            }
            else
            {
                return (int)tv->n;
            }
        }
    }

    public void SetInt(int index, int Value)
    {
        if (LuaEnvValues.Is64Bit)
        {
            ((LuaTValue64*)(TableRawPtr->array))[index].i = Value;
        }
        else
        {
            ((LuaTValue32*)(TableRawPtr->array))[index].i = Value;
        }
    }

    public double GetDouble(int index)
    {
        if (LuaEnvValues.Is64Bit)
        {
            return ((LuaTValue64*)(TableRawPtr->array))[index].n;
        }
        else
        {
            return ((LuaTValue32*)(TableRawPtr->array))[index].n;
        }
    }

    public void SetDouble(int index, double Value)
    {
        if (LuaEnvValues.Is64Bit)
        {
            ((LuaTValue64*)(TableRawPtr->array))[index].n = Value;
        }
        else
        {
            ((LuaTValue32*)(TableRawPtr->array))[index].n = (float)Value;
        }
    }
}