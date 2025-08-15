using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WeaponPaints;

public static class MemoryWindows
{
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);
    
    public static void PatchBytesAtAddress(IntPtr pPatchAddress, byte[] pPatch, int iPatchSize)
    {
        if(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        
        IntPtr bytesWritten;
        WriteProcessMemory(Process.GetCurrentProcess().Handle, pPatchAddress, pPatch, (uint)iPatchSize, out bytesWritten);
    }
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(int processAccess, bool bInheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);
    
    public static byte[]? ReadMemory(IntPtr address, int size)
    {
        if(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return null;
        
        byte[] buffer = new byte[size];
        int bytesRead;
        ReadProcessMemory(Process.GetCurrentProcess().Handle, address, buffer, size, out bytesRead);
        return buffer;
    }
}