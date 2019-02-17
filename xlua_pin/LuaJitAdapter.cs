using System;
using System.Runtime.InteropServices;


public class LuaJitType
{
    public const uint LJ_TISNUM = (~13u); // this is not correct on LJ_64 && !LJ_GC64, 64bit arch without GC64
    public const uint LJ_TNUMX = (~13u);

#if UNITY_IPHONE || UNITY_TVOS
    public const bool LJ_DUALNUM = true;
    public static bool GC64 = true;
#elif UNITY_ANDROID
    public static bool LJ_DUALNUM = true;
    public const bool GC64 = false;
#else
    public static bool LJ_DUALNUM = false;
    public static bool GC64 = false; // TODO NOT correct
#endif

    static LuaJitType()
    {
#if UNITY_ANDROID
        LJ_DUALNUM = LuaJitAdapter.gse_LJ_DUALNUM() > 0 ? true : false;
#elif UNITY_IPHONE || UNITY_TVOS
        GC64 = LuaJitAdapter.gse_LJ_GC64() > 0 ? true : false;
#else
        LJ_DUALNUM = LuaJitAdapter.gse_LJ_DUALNUM() > 0 ? true : false;
        GC64 = LuaJitAdapter.gse_LJ_GC64() > 0 ? true : false;
#endif
    }

    public static bool IsIntType(ref LuaJitTValue tv)
    {
#if UNITY_IPHONE || UNITY_TVOS
        if(GC64)
            return (((UInt32)(tv.it64 >> 47)) == LJ_TISNUM);
        else
            return (tv.it == LJ_TISNUM);
#elif UNITY_ANDROID
        return (LJ_DUALNUM && tv.it == LJ_TISNUM);
#else
        return false;
#endif
        
    }


    public static double GetDouble(ref LuaJitTValue tv)
    {
#if UNITY_IPHONE || UNITY_TVOS || UNITY_ANDROID
        if (IsIntType(ref tv))
            return tv.i;
        else
            return tv.n;
#else
        return tv.n;
#endif

    }

    public static int GetInt(ref LuaJitTValue tv)
    {
#if UNITY_IPHONE || UNITY_TVOS || UNITY_ANDROID
        if (IsIntType(ref tv))
            return tv.i;
        else
            return (int)tv.n;        
#else
        return (int)tv.n;
#endif
    }
}

public unsafe class LuaSafePinTB
{
    protected int _max = 0;
    protected LuaJitTValue* _testarr = null;
    public LuaSafePinTB(int max, int pinTBIdx)
    {
        _testarr =(LuaJitTValue*) LuaJitAdapter.gse_c_get_pinned_array(pinTBIdx);
        _max = max;
    }

    public void Resize(int max)
    {
        _max = max;
    }

    public double GetDouble(int index)
    {
        if (index > 0 && index <= _max)
        {
            return LuaJitType.GetDouble(ref _testarr[index]);
        }
#if UNITY_EDITOR
        UnityEngine.Debug.LogError("get index error");
#endif
        return 0;
    }


    public void SetDouble(int index, double val)
    {
        if (index > 0 && index <= _max)
        {
            _testarr[index].n = val;
            return;
        }
#if UNITY_EDITOR
        UnityEngine.Debug.LogError("get index error");
#endif
    }

    public int GetInt(int index)
    {
        if (index > 0 && index <= _max)
        {
            return LuaJitType.GetInt(ref _testarr[index]);
        }
#if UNITY_EDITOR
        UnityEngine.Debug.LogError("get index error");
#endif
        return 0;
    }

    public void SetInt(int index, int val)
    {
        if (index > 0 && index <= _max)
        {
            _testarr[index].n = val;
            return;
        }
#if UNITY_EDITOR
        UnityEngine.Debug.LogError("get index error");
#endif
    }
}

/// <summary>
/// see TValue in lj_obj.h
/// we make sure to handle both GC_64 and 32, also be careful on i/it for endian issue
/// LJ_ARCH_ENDIAN = LUAJIT_LE in x86/x64/arm/arm64
/// </summary>
[StructLayout(LayoutKind.Explicit, Size=8)]
public struct LuaJitTValue
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

    // internal object tag for GC64
    [FieldOffset(0)]
    public Int64 it64;

    // internal object tag
    [FieldOffset(4)]
    public UInt32 it;
}


/// <summary>
/// see Node in lj_obj.h
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 24)]
public struct LuaJitNode
{
    [FieldOffset(0)]
    public LuaJitTValue val;

    [FieldOffset(8)]
    public LuaJitTValue key;

    [FieldOffset(16)]
    public UInt64 next64;

    [FieldOffset(16)]
    public UInt32 next32;

    [FieldOffset(20)]
    public UInt32 freetop;
}


// get field from 
public class LuaJitAdapter
{
#if UNITY_IPHONE || UNITY_TVOS
#if UNITY_EDITOR
        const string DLLNAME = "ulua";
#else
        const string DLLNAME = "__Internal";
#endif
#else
    const string DLLNAME = "ulua";
#endif

    [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr gse_c_get_pinned_gctab(int pinID);

    [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr gse_c_get_pinned_array(int pinID);

    [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr gse_c_get_pinned_nodes(int pinID);

    [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gse_c_get_node_idx(int pinID, string key);

    [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gse_LJ_DUALNUM();

    [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gse_LJ_GC64();

    [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern string gse_LJ_ARCH_NAME();

}