using Microsoft.Maui.Controls.PlatformConfiguration;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ap2.Native;

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

    /// <summary>
    /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-monitorfrompoint
    /// </summary>
    [Flags]
    public enum MonitorFromPointFlags
    {
        /// <summary>
        /// Returns NULL.
        /// </summary>
        MONITOR_DEFAULTTONULL = 0x00000000,
        /// <summary>
        /// Returns a handle to the primary display monitor.
        /// </summary>
        MONITOR_DEFAULTTOPRIMARY = 0x00000001,
        /// <summary>
        /// Returns a handle to the display monitor that is nearest to the point.
        /// </summary>
        MONITOR_DEFAULTTONEAREST = 0x00000002
    }

    public const int GWL_STYLE = -16;
    public const int GWL_EXSTYLE = -20;

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

    [DllImport("user32.dll", EntryPoint = "GetMessageW", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
    
    [DllImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll", EntryPoint = "DispatchMessageW")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern IntPtr DispatchMessage(ref MSG lpmsg);

    [DllImport("user32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern IntPtr MonitorFromPoint(POINT pt, NativeMethods.MonitorFromPointFlags flags);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern bool GetMonitorInfo(HandleRef hmonitor, [In, Out] MONITORINFOEX info);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern IntPtr GetWindowLongPtr(IntPtr hWndParent, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern IntPtr SetWindowLongPtr(IntPtr hWndParent, int index, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern IntPtr CallWindowProc(IntPtr proc, IntPtr hwnd, uint msg, IntPtr wparam, IntPtr lparam);

    [DllImport("ntdll.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ProcessInformation processInformation, int processInformationLength, out int returnLength);

    [DllImport("kernel32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern IntPtr GetModuleHandle(string? moduleName = null);

    [DllImport("dwmapi.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    #endregion

    public const int GWLP_WNDPROC = -4;

    /// <summary>
    /// Options used by the DwmGetWindowAttribute and DwmSetWindowAttribute functions.
    /// </summary>
    [Flags]
    public enum DWMWINDOWATTRIBUTE
    {
        /// <summary>
        /// Is non-client rendering enabled/disabled
        /// </summary>
        DWMWA_NCRENDERING_ENABLED = 1,

        /// <summary>
        /// DWMNCRENDERINGPOLICY - Non-client rendering policy
        /// </summary>
        DWMWA_NCRENDERING_POLICY = 2,

        /// <summary>
        /// Potentially enable/forcibly disable transitions
        /// </summary>
        DWMWA_TRANSITIONS_FORCEDISABLED = 3,

        /// <summary>
        /// Enables content rendered in the non-client area to be visible on the frame drawn by DWM.
        /// </summary>
        DWMWA_ALLOW_NCPAINT = 4,

        /// <summary>
        /// Retrieves the bounds of the caption button area in the window-relative space.
        /// </summary>
        DWMWA_CAPTION_BUTTON_BOUNDS = 5,

        /// <summary>
        /// Is non-client content RTL mirrored
        /// </summary>
        DWMWA_NONCLIENT_RTL_LAYOUT = 6,

        /// <summary>
        /// Forces the window to display an iconic thumbnail or peek representation (a static bitmap), even if a live or snapshot representation of the window is available.
        /// </summary>
        DWMWA_FORCE_ICONIC_REPRESENTATION = 7,

        /// <summary>
        /// Designates how Flip3D will treat the window.
        /// </summary>
        DWMWA_FLIP3D_POLICY = 8,

        /// <summary>
        /// Gets the extended frame bounds rectangle in screen space
        /// </summary>
        DWMWA_EXTENDED_FRAME_BOUNDS = 9,

        /// <summary>
        /// Indicates an available bitmap when there is no better thumbnail representation.
        /// </summary>
        DWMWA_HAS_ICONIC_BITMAP = 10,

        /// <summary>
        /// Don't invoke Peek on the window.
        /// </summary>
        DWMWA_DISALLOW_PEEK = 11,

        /// <summary>
        /// LivePreview exclusion information
        /// </summary>
        DWMWA_EXCLUDED_FROM_PEEK = 12,

        /// <summary>
        /// Cloaks the window such that it is not visible to the user.
        /// </summary>
        DWMWA_CLOAK = 13,

        /// <summary>
        /// If the window is cloaked, provides one of the following values explaining why.
        /// </summary>
        DWMWA_CLOAKED = 14,

        /// <summary>
        /// Freeze the window's thumbnail image with its current visuals. Do no further live updates on the thumbnail image to match the window's contents.
        /// </summary>
        DWMWA_FREEZE_REPRESENTATION = 15,

        /// <summary>
        /// BOOL, Updates the window only when desktop composition runs for other reasons
        /// </summary>
        DWMWA_PASSIVE_UPDATE_MODE = 16,

        /// <summary>
        /// BOOL, Allows the use of host backdrop brushes for the window.
        /// </summary>
        DWMWA_USE_HOSTBACKDROPBRUSH = 17,

        /// <summary>
        /// Allows a window to either use the accent color, or dark, according to the user Color Mode preferences.
        /// </summary>
        DMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19,

        /// <summary>
        /// Allows a window to either use the accent color, or dark, according to the user Color Mode preferences.
        /// </summary>
        DWMWA_USE_IMMERSIVE_DARK_MODE = 20,

        /// <summary>
        /// Controls the policy that rounds top-level window corners.
        /// <para>Windows 11 and above.</para>
        /// </summary>
        DWMWA_WINDOW_CORNER_PREFERENCE = 33,

        /// <summary>
        /// The color of the thin border around a top-level window.
        /// </summary>
        DWMWA_BORDER_COLOR = 34,

        /// <summary>
        /// The color of the caption.
        /// <para>Windows 11 and above.</para>
        /// </summary>
        DWMWA_CAPTION_COLOR = 35,

        /// <summary>
        /// The color of the caption text.
        /// <para>Windows 11 and above.</para>
        /// </summary>
        DWMWA_TEXT_COLOR = 36,

        /// <summary>
        /// Width of the visible border around a thick frame window.
        /// <para>Windows 11 and above.</para>
        /// </summary>
        DWMWA_VISIBLE_FRAME_BORDER_THICKNESS = 37,

        /// <summary>
        /// Allows to enter a value from 0 to 4 deciding on the imposed backdrop effect.
        /// </summary>
        DWMWA_SYSTEMBACKDROP_TYPE = 38,

        /// <summary>
        /// Indicates whether the window should use the Mica effect.
        /// <para>Windows 11 and above.</para>
        /// </summary>
        DWMWA_MICA_EFFECT = 1029
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
    public class MONITORINFOEX
    {
        internal int cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
        internal RECT rcMonitor = new RECT();
        internal RECT rcWork = new RECT();
        internal int dwFlags = 0;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        internal char[] szDevice = new char[32];
    }

    public struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    public struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public RECT(Rect r)
        {
            left = (int)r.Left;
            top = (int)r.Top;
            right = (int)r.Right;
            bottom = (int)r.Bottom;
        }

        public int Width => right - left;

        public int Height => bottom - top;
    }

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

    #if WINDOWS

    public static Windows.Graphics.RectInt32 ToRectInt32(this Rectangle rect)
    {
        var rectInt32 = new Windows.Graphics.RectInt32();
        rectInt32.X = rect.X;
        rectInt32.Y = rect.Y;
        rectInt32.Width = rect.Width;
        rectInt32.Height = rect.Height;

        return rectInt32;
    }

    #endif
}

