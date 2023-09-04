using System.Runtime.InteropServices;

namespace ap2.Native;

public class NativeWindow : IDisposable
{
    /// <summary>
    /// The following are the extended window styles
    /// </summary>
    [Flags]
    public enum ExtendedWindowStyles : uint
    {
        /// <summary>
        /// The window has a double border; the window can, optionally, be created with a title bar by specifying
        /// the WS_CAPTION style in the dwStyle parameter.
        /// </summary>
        WS_EX_DLGMODALFRAME = 0X0001,

        /// <summary>
        /// The child window created with this style does not send the WM_PARENTNOTIFY message to its parent window
        /// when it is created or destroyed.
        /// </summary>
        WS_EX_NOPARENTNOTIFY = 0X0004,

        /// <summary>
        /// The window should be placed above all non-topmost windows and should stay above all non-topmost windows
        /// and should stay above them, even when the window is deactivated.
        /// </summary>
        WS_EX_TOPMOST = 0X0008,

        /// <summary>
        /// The window accepts drag-drop files.
        /// </summary>
        WS_EX_ACCEPTFILES = 0x0010,

        /// <summary>
        /// The window should not be painted until siblings beneath the window (that were created by the same thread)
        /// have been painted.
        /// </summary>
        WS_EX_TRANSPARENT = 0x0020,

        /// <summary>
        /// The window is a MDI child window.
        /// </summary>
        WS_EX_MDICHILD = 0x0040,

        /// <summary>
        /// The window is intended to be used as a floating toolbar. A tool window has a title bar that is shorter
        /// than a normal title bar, and the window title is drawn using a smaller font. A tool window does not
        /// appear in the taskbar or in the dialog that appears when the user presses ALT+TAB.
        /// </summary>
        WS_EX_TOOLWINDOW = 0x0080,

        /// <summary>
        /// The window has a border with a raised edge.
        /// </summary>
        WS_EX_WINDOWEDGE = 0x0100,

        /// <summary>
        /// The window has a border with a sunken edge.
        /// </summary>
        WS_EX_CLIENTEDGE = 0x0200,

        /// <summary>
        /// The title bar of the window includes a question mark.
        /// </summary>
        WS_EX_CONTEXTHELP = 0x0400,

        /// <summary>
        /// The window has generic "right-aligned" properties. This depends on the window class. This style has
        /// an effect only if the shell language supports reading-order alignment, otherwise is ignored.
        /// </summary>
        WS_EX_RIGHT = 0x1000,

        /// <summary>
        /// The window has generic left-aligned properties. This is the default.
        /// </summary>
        WS_EX_LEFT = 0x0,

        /// <summary>
        /// If the shell language supports reading-order alignment, the window text is displayed using right-to-left
        /// reading-order properties. For other languages, the styles is ignored.
        /// </summary>
        WS_EX_RTLREADING = 0x2000,

        /// <summary>
        /// The window text is displayed using left-to-right reading-order properties. This is the default.
        /// </summary>
        WS_EX_LTRREADING = 0x0,

        /// <summary>
        /// If the shell language supports reading order alignment, the vertical scroll bar (if present) is to
        /// the left of the client area. For other languages, the style is ignored.
        /// </summary>
        WS_EX_LEFTSCROLLBAR = 0x4000,

        /// <summary>
        /// The vertical scroll bar (if present) is to the right of the client area. This is the default.
        /// </summary>
        WS_EX_RIGHTSCROLLBAR = 0x0,

        /// <summary>
        /// The window itself contains child windows that should take part in dialog box, navigation. If this
        /// style is specified, the dialog manager recurses into children of this window when performing
        /// navigation operations such as handling tha TAB key, an arrow key, or a keyboard mnemonic.
        /// </summary>
        WS_EX_CONTROLPARENT = 0x10000,

        /// <summary>
        /// The window has a three-dimensional border style intended to be used for items that do not accept
        /// user input.
        /// </summary>
        WS_EX_STATICEDGE = 0x20000,

        /// <summary>
        /// Forces a top-level window onto the taskbar when the window is visible.
        /// </summary>
        WS_EX_APPWINDOW = 0x40000,

        /// <summary>
        /// The window is an overlapped window.
        /// </summary>
        WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE,

        /// <summary>
        /// The window is palette window, which is a modeless dialog box that presents an array of commands.
        /// </summary>
        WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST,

        /// <summary>
        /// The window is a layered window. This style cannot be used if the window has a class style of either
        /// CS_OWNDC or CS_CLASSDC. Only for top level window before Windows 8, and child windows from Windows 8.
        /// </summary>
        WS_EX_LAYERED = 0x80000,

        /// <summary>
        /// The window does not pass its window layout to its child windows.
        /// </summary>
        WS_EX_NOINHERITLAYOUT = 0x100000,

        /// <summary>
        /// If the shell language supports reading order alignment, the horizontal origin of the window is on the
        /// right edge. Increasing horizontal values advance to the left.
        /// </summary>
        WS_EX_LAYOUTRTL = 0x400000,

        /// <summary>
        /// Paints all descendants of a window in bottom-to-top painting order using double-buffering.
        /// Bottom-to-top painting order allows a descendent window to have translucency (alpha) and
        /// transparency (color-key) effects, but only if the descendent window also has the WS_EX_TRANSPARENT
        /// bit set. Double-buffering allows the window and its descendents to be painted without flicker.
        /// </summary>
        WS_EX_COMPOSITED = 0x2000000,

        /// <summary>
        /// A top-level window created with this style does not become the foreground window when the user
        /// clicks it. The system does not bring this window to the foreground when the user minimizes or closes
        /// the foreground window.
        /// </summary>
        WS_EX_NOACTIVATE = 0x8000000,
    }

    public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct WNDCLASS
    {
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszClassName;
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern UInt16 RegisterClassW([In] ref WNDCLASS lpWndClass);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr CreateWindowExW(
       UInt32 dwExStyle,
       [MarshalAs(UnmanagedType.LPWStr)]
       string lpClassName,
       [MarshalAs(UnmanagedType.LPWStr)]
       string lpWindowName,
       UInt32 dwStyle,
       Int32 x, Int32 y,
       Int32 nWidth, Int32 nHeight,
       IntPtr hWndParent,
       IntPtr hMenu,
       IntPtr hInstance,
       IntPtr lpParam
    );

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool DestroyWindow(IntPtr hWnd);

    private const int ERROR_CLASS_ALREADY_EXISTS = 1410;

    private bool _disposed;

    public IntPtr Handle { get; private set; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
            }

            // Dispose unmanaged resources
            if (Handle != IntPtr.Zero)
            {
                DestroyWindow(Handle);
                Handle = IntPtr.Zero;
            }
        }
    }

    public NativeWindow(string className, WndProc customWndProc = null)
    {
        if (string.IsNullOrWhiteSpace(className))
            throw new ArgumentNullException(nameof(className));

        _customWndProc = customWndProc != null ? customWndProc : CustomWndProc;

        // Create WNDCLASS
        var wndclass = new WNDCLASS();
        wndclass.lpszClassName = className;
        wndclass.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_customWndProc);

        UInt16 classAtom = RegisterClassW(ref wndclass);

        int lastError = Marshal.GetLastWin32Error();
        if (classAtom == 0 && lastError != ERROR_CLASS_ALREADY_EXISTS)
        {
            throw new Exception("Could not register window class");
        }

        // Create window
        Handle = CreateWindowExW(
            0,
            className,
            String.Empty,
            0, 0, 0, 0, 0,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero
        );
    }

    private static IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        return NativeMethods.DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private WndProc _customWndProc;
}