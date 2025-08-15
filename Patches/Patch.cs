using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;

namespace WeaponPaints;

// Thanks cssharp-fixes
public static class Patch
{
    private static IntPtr GetAddress(string modulePath, string signature)
    {
        // Returns address if found, otherwise a C++ nullptr which is a IntPtr.Zero in C#
        var address = NativeAPI.FindSignature(modulePath, signature);
        
        return address;
    }

    public static void PerformPatch(string signature, string patch)
    {
        IntPtr address = GetAddress(Addresses.ServerPath, signature);
        if(address == IntPtr.Zero)
        {
            return;
        }
        
        WriteBytesToAddress(address, HexToByte(patch));
    }

    private static void WriteBytesToAddress(IntPtr address, List<byte> bytes)
    {
        int patchSize = bytes.Count;
        if(patchSize == 0) throw new ArgumentException("Patch bytes list cannot be empty.");
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            MemoryLinux.PatchBytesAtAddress(address, bytes.ToArray(), patchSize);
        }
        else
        {
            MemoryWindows.PatchBytesAtAddress(address, bytes.ToArray(), patchSize);
        }
    }

    private static List<byte> HexToByte(string src)
    {
        if (string.IsNullOrEmpty(src))
        {
            return new List<byte>();
        }

        byte HexCharToByte(char c)
        {
            if (c is >= '0' and <= '9') return (byte)(c - '0');
            if (c is >= 'A' and <= 'F') return (byte)(c - 'A' + 10);
            if (c is >= 'a' and <= 'f') return (byte)(c - 'a' + 10);
            return 0xFF; // Invalid hex character
        }

        List<byte> result = new List<byte>();
        bool isCodeStyle = src[0] == '\\';
        string pattern = isCodeStyle ? "\\x" : " ";
        string wildcard = isCodeStyle ? "2A" : "?";
        int pos = 0;

        while (pos < src.Length)
        {
            int found = src.IndexOf(pattern, pos);
            if (found == -1)
            {
                found = src.Length;
            }

            string str = src.Substring(pos, found - pos);
            pos = found + pattern.Length;

            if (string.IsNullOrEmpty(str)) continue;

            string byteStr = str;

            if (byteStr.Substring(0, wildcard.Length) == wildcard)
            {
                result.Add(0xFF); // Representing wildcard as 0xFF
                continue;
            }

            if (byteStr.Length < 2)
            {
                return new List<byte>(); // Invalid byte length
            }

            byte high = HexCharToByte(byteStr[0]);
            byte low = HexCharToByte(byteStr[1]);

            if (high == 0xFF || low == 0xFF)
            {
                return new List<byte>(); // Invalid hex character
            }

            result.Add((byte)((high << 4) | low));
        }

        return result;
    }
}