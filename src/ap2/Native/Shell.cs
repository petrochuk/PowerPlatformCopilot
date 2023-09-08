using System.Data;
using System.Runtime.InteropServices;

namespace ap2.Native;

#pragma warning disable CA1045 // Do not pass types by reference
#pragma warning disable CA1060 // Move pinvokes to native methods class
#pragma warning disable CA1401 // P/Invokes should not be visible

public static class Shell
{
    /// <summary>
    /// WinUser.h
    /// </summary>
    public const int IDI_APPLICATION = 32512;

    /// <summary>
    /// https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyicona
    /// Sends a message to the taskbar's status area.
    /// </summary>
    /// <param name="dwMessage">action to be taken by this function</param>
    /// <param name="notifyIconData"></param>
    /// <returns></returns>
    [DllImport("shell32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern bool Shell_NotifyIcon(NotifyIconMessages dwMessage, ref NotifyIconData notifyIconData);

    [DllImport("Shcore.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern int GetDpiForMonitor(IntPtr hMonitor, MONITOR_DPI_TYPE dpiType, out uint dpiX, out uint dpiY);

    public enum MONITOR_DPI_TYPE
    {
        EFFECTIVE_DPI = 0,
        ANGULAR_DPI = 1,
        RAW_DPI = 2,
        DEFAULT
    };

    public static NotifyIcon AddNotifyIcon(string toolTip, Guid iconId, IntPtr? hWnd = null)
    {
        if (hWnd == null)
            hWnd = NativeMethods.GetProcessMainWindowHandle();

        IntPtr hInstance = NativeMethods.GetModuleHandle();
        IntPtr hIcon = NativeMethods.LoadIcon(hInstance, new IntPtr(IDI_APPLICATION));
        var data = new NotifyIconData()
        {
            hWnd = hWnd.Value,
            uFlags = (int)(NotifyIconFlags.Guid | NotifyIconFlags.Tip | NotifyIconFlags.Icon | NotifyIconFlags.Message),
            guidItem = iconId == Guid.Empty ? Guid.NewGuid() : iconId,
            szTip = toolTip,
            hIcon = hIcon,
            uCallbackMessage = NotifyIcon.WM_NOTIFYICON
        };

        Shell_NotifyIcon(NotifyIconMessages.Add, ref data);

        Shell_NotifyIcon(NotifyIconMessages.SetVersion, ref data);

        return new NotifyIcon(data);
    }
}
