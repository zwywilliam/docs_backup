using System;
using System.Runtime.InteropServices;
using XLua;
using XLua.LuaDLL;


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


// TValue from lua source lobject.h


// 32 bit version
[StructLayout(LayoutKind.Explicit, Size = 8)]
public struct LuaTValue32
{
    // uint64
    [FieldOffset(0)]
    public UInt64 u64;

    // number
    [FieldOffset(0)]
    public float n;

    // integer value
    [FieldOffset(0)]
    public int i;
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
    public int i;
}


[StructLayout(LayoutKind.Sequential)]
public struct LuaTableRawDef
{
    public IntPtr next;

    public uint bytes;

    public uint sizearray;

    public IntPtr array;

    public IntPtr node;

    public IntPtr lastfree;

    public IntPtr metatable;

    public IntPtr gclist;
}



public static class LuaEnvValues
{
    public static bool Is64Bit;
}


public unsafe class LuaTablePin
{
    LuaTValue32* RawPtr32;
    LuaTValue64* RawPtr64;

    public void PinByIndex(IntPtr L, int index)
    {
        IntPtr TablePtr = Lua.lua_topointer(L, index);
        if(TablePtr != IntPtr.Zero && Lua.lua_istable(L, index))
        {
            LuaTableRawDef* TablePtrRaw = (LuaTableRawDef*)TablePtr;
            if (LuaEnvValues.Is64Bit)
            {
                RawPtr64 = (LuaTValue64*)TablePtrRaw->array;
            }
            else
            {
                RawPtr32 = (LuaTValue32*)TablePtrRaw->array;
            }
        }
    }

    public int GetInt(int index)
    {
        if(LuaEnvValues.Is64Bit)
        {
            return RawPtr64[index].i;
        }
        else
        {
            return RawPtr32[index].i;
        }
    }

    public void SetInt(int index, int Value)
    {
        if (LuaEnvValues.Is64Bit)
        {
            RawPtr64[index].i = Value;
        }
        else
        {
            RawPtr32[index].i = Value;
        }
    }

    public double GetDouble(int index)
    {
        if (LuaEnvValues.Is64Bit)
        {
            return RawPtr64[index].n;
        }
        else
        {
            return RawPtr32[index].n;
        }
    }

    public void SetDouble(int index, double Value)
    {
        if (LuaEnvValues.Is64Bit)
        {
            RawPtr64[index].n = Value;
        }
        else
        {
            RawPtr32[index].n = (float)Value;
        }
    }
}