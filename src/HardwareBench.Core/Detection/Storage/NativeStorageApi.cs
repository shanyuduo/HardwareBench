using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace HardwareBench.Core.Detection.Storage;

[SupportedOSPlatform("windows")]
public sealed class NativeStorageApi
{
    private const uint PropertyStandardDefine = 0;
    private const uint StorageDeviceProperty = 0;
    private const uint StorageDeviceSeekPenalty = 7;
    private const uint StorageDeviceTemperatureProperty = 51;
    private const uint IoctlStorageQueryProperty = 0x002D1400;

    public IEnumerable<(string Path, byte[] Descriptor, bool SeekPenalty, int? TempC)> EnumeratePhysicalDrives()
    {
        for (int i = 0; i < 16; i++)
        {
            string path = $@"\\.\PhysicalDrive{i}";
            SafeFileHandle? handle = TryOpen(path);
            if (handle is null || handle.IsInvalid) { handle?.Dispose(); continue; }
            using (handle)
            {
                byte[]? desc = QueryDescriptor(handle, StorageDeviceProperty, 4096);
                if (desc is null) continue;
                bool seek = QuerySeekPenalty(handle);
                int? temp = QueryTemperature(handle);
                yield return (path, desc, seek, temp);
            }
        }
    }

    private static SafeFileHandle? TryOpen(string path)
    {
        try
        {
            var h = NativeMethods.CreateFile(path, NativeMethods.GenericRead,
                FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
            return h.IsInvalid ? null : h;
        }
        catch (Win32Exception) { return null; }
    }

    private static byte[]? QueryDescriptor(SafeHandle handle, uint propertyId, int size)
    {
        var query = new byte[8 + sizeof(uint) * 3];
        query[0] = (byte)propertyId; query[1] = (byte)(propertyId >> 8);
        query[2] = (byte)(propertyId >> 16); query[3] = (byte)(propertyId >> 24);
        query[4] = (byte)PropertyStandardDefine; query[5] = (byte)(PropertyStandardDefine >> 8);
        query[6] = (byte)(PropertyStandardDefine >> 16); query[7] = (byte)(PropertyStandardDefine >> 24);
        return DeviceIoControlRead(handle, IoctlStorageQueryProperty, query, size);
    }

    private static bool QuerySeekPenalty(SafeHandle handle)
    {
        var outBuf = QueryDescriptor(handle, StorageDeviceSeekPenalty, 64);
        return outBuf is { Length: >= 12 } && outBuf[8] != 0;
    }

    private static int? QueryTemperature(SafeHandle handle)
    {
        var outBuf = QueryDescriptor(handle, StorageDeviceTemperatureProperty, 1024);
        if (outBuf is null || outBuf.Length < 16) return null;
        short raw = (short)(outBuf[14] | (outBuf[15] << 8));
        return raw == short.MinValue ? null : raw;
    }

    private static byte[]? DeviceIoControlRead(SafeHandle handle, uint ioctl, byte[] input, int outSize)
    {
        var output = new byte[outSize];
        if (!NativeMethods.DeviceIoControl(handle, ioctl, input, (uint)input.Length,
                output, (uint)outSize, out uint returned, IntPtr.Zero))
            return null;
        Array.Resize(ref output, (int)returned);
        return output;
    }

    private static class NativeMethods
    {
        public const uint GenericRead = 0x80000000;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess,
            FileShare dwShareMode, IntPtr lpSecurityAttributes, FileMode dwCreationDisposition,
            uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeviceIoControl(SafeHandle hDevice, uint dwIoControlCode,
            byte[] lpInBuffer, uint nInBufferSize, byte[] lpOutBuffer, uint nOutBufferSize,
            out uint lpBytesReturned, IntPtr lpOverlapped);
    }
}