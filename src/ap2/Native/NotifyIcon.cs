using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ap2.Native;

#pragma warning disable CA1815 // Override equals and operator equals on value types

[Flags]
internal enum NotifyIconFlags
{
    /// <summary>
    /// The uCallbackMessage member is valid.
    /// </summary>
    Message = 0x00000001,
    /// <summary>
    /// The hIcon member is valid.
    /// </summary>
    Icon = 0x00000002,
    /// <summary>
    /// The szTip member is valid.
    /// </summary>
    Tip = 0x00000004,
    /// <summary>
    /// The dwState and dwStateMask members are valid.
    /// </summary>
    State = 0x00000008,
    /// <summary>
    /// Display a balloon notification. The szInfo, szInfoTitle, dwInfoFlags, and uTimeout members are valid.
    /// Note that uTimeout is valid only in Windows 2000 and Windows XP.
    /// To display the balloon notification, specify <see cref="NotifyIconFlags.Info"/> and provide text in szInfo.
    /// To remove a balloon notification, specify <see cref="NotifyIconFlags.Info"/> and provide an empty string through szInfo.
    /// To add a notification area icon without displaying a notification, do not set the <see cref="NotifyIconFlags.Info"/> flag.
    /// </summary>
    Info = 0x00000010,
    /// <summary>
    /// Windows 7 and later: The guidItem is valid.
    /// Windows Vista and earlier: Reserved.
    /// </summary>
    Guid = 0x00000020,
    /// <summary>
    /// Windows Vista and later. If the balloon notification cannot be displayed immediately, discard it.
    /// Use this flag for notifications that represent real-time information which would be meaningless or misleading if displayed
    /// at a later time. For example, a message that states "Your telephone is ringing." <see cref="NotifyIconFlags.RealTime"/>
    /// is meaningful only when combined with the <see cref="NotifyIconFlags.Info"/> flag.
    /// </summary>
    RealTime = 0x00000040,
    /// <summary>
    /// Windows Vista and later. Use the standard tooltip. Normally, when uVersion is set to NOTIFYICON_VERSION_4,
    /// the standard tooltip is suppressed and can be replaced by the application-drawn, pop-up UI. If the application wants
    /// to show the standard tooltip with NOTIFYICON_VERSION_4, it can specify <see cref="NotifyIconFlags.ShowTip"/>
    /// to indicate the standard tooltip should still be shown.
    /// </summary>
    ShowTip = 0x00000080
}

[Flags]
internal enum NofityIconStates
{
    /// <summary>
    /// The icon is hidden.
    /// </summary>
    Hidden = 0x00000001,
    /// <summary>
    /// The icon resource is shared between multiple icons.
    /// </summary>
    SharedIcon = 0x00000002,
}

[Flags]
internal enum NotifyIconInfoFlags
{
    /// <summary>
    /// No icon.
    /// </summary>
    IconNone = 0x00000000,
    /// <summary>
    /// An information icon.
    /// </summary>
    IconInfo = 0x00000001,
    /// <summary>
    /// A warning icon.
    /// </summary>
    IconWarning = 0x00000002,
    /// <summary>
    /// An error icon.
    /// </summary>
    IconError = 0x00000003,
    /// <summary>
    /// Windows XP SP2 and later.
    /// Windows XP: Use the icon identified in hIcon as the notification balloon's title icon.
    /// Windows Vista and later: Use the icon identified in hBalloonIcon as the notification balloon's title icon.
    /// </summary>
    IconUser = 0x00000004,
    /// <summary>
    /// Windows XP and later. Do not play the associated sound. Applies only to notifications.
    /// </summary>
    NoSound = 0x00000010,
    /// <summary>
    /// Windows Vista and later. The large version of the icon should be used as the notification icon. This corresponds to the icon with dimensions SM_CXICON x SM_CYICON. If this flag is not set, the icon with dimensions XM_CXSMICON x SM_CYSMICON is used.
    /// This flag can be used with all stock icons.
    /// Applications that use older customized icons (NIIF_USER with hIcon) must provide a new SM_CXICON x SM_CYICON version in the tray icon (hIcon). These icons are scaled down when they are displayed in the System Tray or System Control Area (SCA).
    /// New customized icons (NIIF_USER with hBalloonIcon) must supply an SM_CXICON x SM_CYICON version in the supplied icon (hBalloonIcon).
    /// </summary>
    LargeIcon = 0x00000020,
    /// <summary>
    /// Windows 7 and later. Do not display the balloon notification if the current user is in "quiet time", which is the first hour after a new user logs into his or her account for the first time. During this time, most notifications should not be sent or shown.
    /// This lets a user become accustomed to a new computer system without those distractions. Quiet time also occurs for each user after an operating system upgrade or clean installation. A notification sent with this flag during quiet time is not queued; it is simply dismissed unshown.
    /// The application can resend the notification later if it is still valid at that time.
    /// Because an application cannot predict when it might encounter quiet time, we recommended that this flag always be set on all appropriate notifications by any application that means to honor quiet time.
    /// During quiet time, certain notifications should still be sent because they are expected by the user as feedback in response to a user action, for instance when he or she plugs in a USB device or prints a document.
    /// If the current user is not in quiet time, this flag has no effect.
    /// </summary>
    RespectQuietTime = 0x00000080,
    /// <summary>
    /// Windows XP and later. Reserved.
    /// </summary>
    IconMask = 0x0000000F,
}

/// <summary>
/// https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyicona
/// </summary>
public enum NotifyIconMessages
{
    /// <summary>
    /// Adds an icon to the status area. The icon is given an identifier in the NOTIFYICONDATA structure pointed to by lpdata—either
    /// through its uID or guidItem member.
    /// This identifier is used in subsequent calls to <see cref="Shell_NotifyIcon"/> to perform later actions on the icon.
    /// </summary>
    Add = 0x00000000,
    /// <summary>
    /// Modifies an icon in the status area. NOTIFYICONDATA structure pointed to by lpdata uses the ID originally assigned to the icon
    /// when it was added to the notification area (<see cref="Add"/>) to identify the icon to be modified.
    /// </summary>
    Modify = 0x00000001,
    /// <summary>
    /// Deletes an icon from the status area. NOTIFYICONDATA structure pointed to by lpdata uses the ID originally assigned to the icon
    /// when it was added to the notification area (<see cref="Add"/>) to identify the icon to be deleted.
    /// </summary>
    Delete = 0x00000002,
    /// <summary>
    /// Shell32.dll version 5.0 and later only. Returns focus to the taskbar notification area. Notification area icons
    /// should use this message when they have completed their UI operation. For example, if the icon displays a shortcut menu,
    /// but the user presses ESC to cancel it, use <see cref="SetFocus"/> to return focus to the notification area.
    /// </summary>
    SetFocus = 0x00000003,
    /// <summary>
    /// Shell32.dll version 5.0 and later only. Instructs the notification area to behave according to the version number specified
    /// in the uVersion member of the structure pointed to by lpdata. The version number specifies which members are recognized.
    /// <see cref="SetVersion"/> must be called every time a notification area icon is added (<see cref="Add"/>).
    /// It does not need to be called with <see cref="Modify"/>. The version setting is not persisted once a user logs off.
    /// </summary>
    SetVersion = 0x00000004,
}

public enum NotifyIconNotification : uint
{
    NIN_SELECT = 0x400,
    NIN_KEYSELECT = 0x401,
    NIN_BALLOONSHOW = 0x402,
    NIN_BALLOONHIDE = 0x403,
    NIN_BALLOONTIMEOUT = 0x404,
    NIN_BALLOONUSERCLICK = 0x405,
    NIN_POPUPOPEN = 0x406,
    NIN_POPUPCLOSE = 0x407,
}

/// <summary>
/// https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-notifyicondataa
/// </summary>
[SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
[StructLayout(LayoutKind.Sequential)]
public struct NotifyIconData
{
    public int cbSize = Marshal.SizeOf(typeof(NotifyIconData));
    public IntPtr hWnd = IntPtr.Zero;
    public int uID = 0;
    public int uFlags = 0;
    public uint uCallbackMessage = 0;
    public IntPtr hIcon = IntPtr.Zero;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string szTip = string.Empty;
    public int dwState = 0;
    public int dwStateMask = 0;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string szInfo = string.Empty;
    public uint uTimeoutOrVersion = 4; // NOTIFYICON_VERSION_4;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string szInfoTitle = string.Empty;
    public int dwInfoFlags = 0;
    public Guid guidItem = Guid.Empty;
    public IntPtr hBalloonIcon = IntPtr.Zero;

    public NotifyIconData()
    {
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct NOTIFYICONIDENTIFIER
{
    public uint cbSize;
    public IntPtr hWnd;
    public uint uID;
    public Guid guidItem;
}

public class NotifyIcon
{
    public const uint WM_NOTIFYICON = 0xBFFF + 1;

    public NotifyIcon(NotifyIconData data)
    {
        Id = data.guidItem;
    }

    public Guid Id { get; }
}
