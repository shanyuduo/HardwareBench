using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace HardwareBench.Core.Benchmarks;

public record ProcessSpec(string ExePath, IReadOnlyList<string> Args, int TimeoutMs, string WorkingDirectory);

public record ProcessResult(int ExitCode, string StdOut, string StdErr, bool TimedOut);

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken ct);
}

[SupportedOSPlatform("windows")]
public sealed class BoundedProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken ct)
    {
        using var proc = new Process();
        proc.StartInfo = new ProcessStartInfo
        {
            FileName = spec.ExePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = spec.WorkingDirectory,
        };
        foreach (var arg in spec.Args)
            proc.StartInfo.ArgumentList.Add(arg);

        using var job = new JobObjectScope();
        proc.Start();
        try
        {
            job.AssignProcess(proc.Handle);
        }
        catch
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
            throw;
        }

        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();

        var exitTask = proc.WaitForExitAsync(ct);
        var timeoutTask = Task.Delay(spec.TimeoutMs, CancellationToken.None);

        var completed = await Task.WhenAny(exitTask, timeoutTask);
        var timedOut = completed == timeoutTask;

        if (timedOut)
        {
            ct.ThrowIfCancellationRequested();
            try { proc.Kill(entireProcessTree: true); } catch { }
            await proc.WaitForExitAsync(CancellationToken.None);
        }
        else
        {
            await exitTask;
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return new ProcessResult(proc.ExitCode, stdout, stderr, timedOut);
    }
}

[SupportedOSPlatform("windows")]
internal sealed class JobObjectScope : IDisposable
{
    private const uint JobObjectExtendedLimitInformation = 9;
    private const uint JobObjectLimitKillOnJobClose = 0x2000;

    private readonly IntPtr _job;

    internal JobObjectScope()
    {
        _job = NativeMethods.CreateJobObjectW(IntPtr.Zero, null);
        if (_job == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                LimitFlags = JobObjectLimitKillOnJobClose,
            },
        };

        var size = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
        var ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(info, ptr, false);
            if (!NativeMethods.SetInformationJobObject(_job, JobObjectExtendedLimitInformation, ptr, (uint)size))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    internal void AssignProcess(IntPtr processHandle)
    {
        if (!NativeMethods.AssignProcessToJobObject(_job, processHandle))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    public void Dispose()
    {
        if (_job != IntPtr.Zero)
            NativeMethods.CloseHandle(_job);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public IntPtr MinimumWorkingSetSize;
        public IntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public long Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public IntPtr ProcessMemoryLimit;
        public IntPtr JobMemoryLimit;
        public IntPtr PeakProcessMemoryUsed;
        public IntPtr PeakJobMemoryUsed;
    }

    private static partial class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateJobObjectW(IntPtr lpJobAttributes, string? lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetInformationJobObject(IntPtr hJob, uint JobObjectInfoClass,
            IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);
    }
}