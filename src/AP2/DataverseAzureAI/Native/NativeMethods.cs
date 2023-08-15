using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AP2.DataverseAzureAI.Native;

#pragma warning disable CA1815 // Override equals and operator equals on value types
#pragma warning disable CA1401 // P/Invokes should not be visible

/// <summary>
/// PROCESS_BASIC_INFORMATION.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ProcessInformation
{
    // These members must match PROCESS_BASIC_INFORMATION
    internal IntPtr Reserved1;
    internal IntPtr PebBaseAddress;
    internal IntPtr Reserved2_0;
    internal IntPtr Reserved2_1;
    internal IntPtr UniqueProcessId;
    internal IntPtr InheritedFromUniqueProcessId;
}

public static class NativeMethods
{
    /// <summary>
    /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getancestor
    /// </summary>
    public enum GetAncestorFlag
    {
        /// <summary>
        /// Retrieves the parent window. This does not include the owner, as it does with the GetParent function.
        /// </summary>
        GetParent = 1,
        /// <summary>
        /// Retrieves the root window by walking the chain of parent windows.
        /// </summary>
        GetRoot = 2,
        /// <summary>
        /// Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent.
        /// </summary>
        GetRootOwner = 3
    }

    #region Extern

    /// <summary>
    /// Retrieves the handle to the ancestor of the specified window.
    /// </summary>
    /// <param name="hwnd">A handle to the window whose ancestor is to be retrieved.
    /// If this parameter is the desktop window, the function returns NULL. </param>
    /// <param name="flags">The ancestor to be retrieved.</param>
    /// <returns>The return value is the handle to the ancestor window.</returns>
    [DllImport("user32.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlag flags);

    [DllImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr iconName);

    [DllImport("ntdll.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ProcessInformation processInformation, int processInformationLength, out int returnLength);

    [DllImport("kernel32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern IntPtr GetModuleHandle(string? moduleName = null);

    #endregion

    /// <summary>
    /// Gets the parent process of the current process.
    /// </summary>
    /// <returns>An instance of the Process class.</returns>
    public static Process GetParentProcess()
    {
        return GetParentProcess(Process.GetCurrentProcess().Handle);
    }

    /// <summary>
    /// Gets the parent process of specified process.
    /// </summary>
    /// <param name="id">The process id.</param>
    /// <returns>An instance of the Process class.</returns>
    public static Process GetParentProcess(int id)
    {
        var process = Process.GetProcessById(id);
        return GetParentProcess(process.Handle);
    }

    /// <summary>
    /// Gets the parent process of a specified process.
    /// </summary>
    /// <param name="handle">The process handle.</param>
    /// <returns>An instance of the Process class.</returns>
    public static Process GetParentProcess(IntPtr handle)
    {
        var pbi = default(ProcessInformation);
        int status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out var _);
        if (status != 0)
            throw new Win32Exception(status);

        try
        {
            return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
        }
        catch (ArgumentException)
        {
            // not found
#pragma warning disable CS8603 // Possible null reference return.
            return null;
#pragma warning restore CS8603 // Possible null reference return.
        }
    }

    /// <summary>
    /// Searches the process tree for the first process with a window.
    /// </summary>
    /// <returns></returns>
    public static IntPtr GetProcessMainWindowHandle()
    {
        // Walk up the process tree until we find a process with a window
        var parentProcess = GetParentProcess();
        while (parentProcess != null && parentProcess.MainWindowHandle == IntPtr.Zero)
        {
            parentProcess = GetParentProcess(parentProcess.Id);
        }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        return parentProcess.MainWindowHandle;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }
}

