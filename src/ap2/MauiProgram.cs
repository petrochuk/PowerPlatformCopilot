using ap2.Native;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ap2;

public static class MauiProgram
{
#if DISABLE_XAML_GENERATED_MAIN
    [DllImport("Microsoft.ui.xaml.dll")]
    private static extern void XamlCheckProcessRequirements();

    static readonly Guid _iconId = new Guid("1eb514a6-f95e-45fd-83b7-007af0791d6d");
    const int DefaultWidth = 300;
    const int DefaultHeight = 150;

    [STAThread]
    public static void Main(string[] args)
    {
        XamlCheckProcessRequirements();
        global::WinRT.ComWrappersSupport.InitializeComWrappers();

        var nativeWindow = new NativeWindow(Assembly.GetExecutingAssembly().GetName().Name, NotificationWndProc);
        ap2.Native.Shell.AddNotifyIcon("ProDev Copilot", _iconId, nativeWindow.Handle);

        NativeMethods.MSG msg;
        while (NativeMethods.GetMessage(out msg, nativeWindow.Handle, 0, 0) > 0)
        {
            NativeMethods.TranslateMessage(ref msg);
            NativeMethods.DispatchMessage(ref msg);
        }
    }

    static WinUI.App _app;
    static Windows.Graphics.RectInt32 _openPosition;

    private static IntPtr NotificationWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case NotifyIcon.WM_NOTIFYICON:
                var iconMessage = (WindowsMessage)(lParam & 0xFFFF);
                switch (iconMessage)
                {
                    case WindowsMessage.WM_LBUTTONUP:
                        if (_app == null)
                        {
                            _openPosition = new Windows.Graphics.RectInt32(
                                (short)wParam - DefaultWidth, (short)((uint)wParam >> 16) - DefaultHeight, DefaultWidth, DefaultHeight);
                            var monitor = NativeMethods.MonitorFromPoint(new NativeMethods.POINT() { X = _openPosition.X, Y = _openPosition.Y }, NativeMethods.MonitorFromPointFlags.MONITOR_DEFAULTTONEAREST);
                            var monitorInfo = new NativeMethods.MONITORINFOEX();
                            NativeMethods.GetMonitorInfo(new HandleRef(null, monitor), monitorInfo);
                            var horizontalAdjustment = monitorInfo.rcWork.Width - (_openPosition.X + _openPosition.Width);
                            if (horizontalAdjustment > 0)
                                _openPosition.X += horizontalAdjustment;
                            var verticalAdjustment = monitorInfo.rcWork.Height - (_openPosition.Y + _openPosition.Height);
                            if (verticalAdjustment < 0)
                                _openPosition.Y += verticalAdjustment;

                            global::Microsoft.UI.Xaml.Application.Start((p) => {
                                var context = new global::Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                                SynchronizationContext.SetSynchronizationContext(context);
                                _app = new WinUI.App();
                            });
                        }
                        else 
                        {
                            var window = _app.Application.Windows.First().Handler.PlatformView as global::Microsoft.Maui.MauiWinUIWindow;
                            if (window != null)
                            {
                                window.Activate();
                            }
                            //_app.MainWindow.Activate();
                        }
                        break;
                    case WindowsMessage.WM_CONTEXTMENU:
                        break;
                    default:
                        //Debug.WriteLine($"NotificationWndProc: {iconMessage}");
                        break;
                }
                break;
                default:
                    //Debug.WriteLine($"NotificationWndProc: {msg}");
                    break;
        }

        return NativeMethods.DefWindowProcW(hWnd, msg, wParam, lParam);
    }
#endif

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if WINDOWS
        builder.ConfigureLifecycleEvents(events =>
        {
            // Make sure to add "using Microsoft.Maui.LifecycleEvents;" in the top of the file 
            events.AddWindows(windowsLifecycleBuilder =>
            {
                windowsLifecycleBuilder.OnWindowCreated(window =>
                {
                    //window.ExtendsContentIntoTitleBar = false;
                    var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);

                    var extendedStyle = Native.NativeMethods.GetWindowLong(handle, Native.NativeMethods.GWL_STYLE);
                    extendedStyle |= (int)Native.NativeWindow.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
                    Native.NativeMethods.SetWindowLong(handle, Native.NativeMethods.GWL_STYLE, extendedStyle);

                    var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                    var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);
                    switch (appWindow.Presenter)
                    {
                        case Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter:
                            //overlappedPresenter.SetBorderAndTitleBar(false, false);
                            appWindow.MoveAndResize(_openPosition);
                            break;
                    }
                    appWindow.Closing += (window, args) =>
                    {
                        window.Hide();
                        args.Cancel = true;
                    };
                });
            });
        });
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
