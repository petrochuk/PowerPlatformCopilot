using ap2.Native;
using AP2.DataverseAzureAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.LifecycleEvents;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.Hosting;

namespace ap2;

public static class MauiProgram
{
    public static double DpiX { get; private set; } = 96.0;
    public static double DpiY { get; private set; } = 96.0;

    public static double ScaleX(double value) => value * DpiX / 96.0;

    public static double ScaleY(double value) => value * DpiY / 96.0;

    private static IntPtr _mainWindowProc;

#if DISABLE_XAML_GENERATED_MAIN
    [DllImport("Microsoft.ui.xaml.dll")]
    private static extern void XamlCheckProcessRequirements();

    static readonly Guid _iconId = new Guid("1eb514a6-f95e-45fd-83b7-007af0791d6d");
    const double DefaultWidth = 300;
    const double DefaultHeight = 150;

    [STAThread]
    public static void Main(string[] args)
    {
        XamlCheckProcessRequirements();
        global::WinRT.ComWrappersSupport.InitializeComWrappers();

        InitializeApp(args);

        using var client = Host.Services.GetRequiredService<DataverseAIClient>();
        client.Run();

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

    private static IntPtr MainWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        Debug.WriteLine($"NotificationWndProc: {(WindowsMessage)msg}");

        return NativeMethods.CallWindowProc(_mainWindowProc, hWnd, msg, wParam, lParam);
    }

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
                            var monitor = NativeMethods.MonitorFromPoint(new NativeMethods.POINT() 
                                { X = _openPosition.X, Y = _openPosition.Y }, NativeMethods.MonitorFromPointFlags.MONITOR_DEFAULTTONEAREST);
                            ap2.Native.Shell.GetDpiForMonitor(monitor, ap2.Native.Shell.MONITOR_DPI_TYPE.EFFECTIVE_DPI, out uint dpiX, out uint dpiY);
                            DpiX = dpiX;
                            DpiY = dpiY;
                            _openPosition = new Windows.Graphics.RectInt32(
                                (int)((short)wParam - ScaleX(DefaultWidth)), (int)((short)((uint)wParam >> 16) - ScaleY(DefaultHeight)), 
                                (int)ScaleX(DefaultWidth), (int)ScaleY(DefaultHeight));
                            
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
#if WINDOWS
                                IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                                Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
                                Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
                                (appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter).IsAlwaysOnTop = true;
                                (appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter).IsAlwaysOnTop = false;
#endif
                                window.Activate();
                            }
                        }
                        break;
                    case WindowsMessage.WM_CONTEXTMENU:
                        break;
                    case WindowsMessage.WM_DPICHANGED:
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
        var configuration = LoadConfiguration();
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
        builder.Services.AddSingleton<DataverseAIClient>();

#if WINDOWS

        builder.ConfigureLifecycleEvents(events =>
        {
            // Make sure to add "using Microsoft.Maui.LifecycleEvents;" in the top of the file 
            events.AddWindows(windowsLifecycleBuilder =>
            {
                windowsLifecycleBuilder.OnWindowCreated(window =>
                {
                    window.ExtendsContentIntoTitleBar = false;
                    var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);

                    var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                    var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);
                    switch (appWindow.Presenter)
                    {
                        case Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter:
                            appWindow.MoveAndResize(_openPosition);
                            appWindow.TitleBar.IconShowOptions = Microsoft.UI.Windowing.IconShowOptions.ShowIconAndSystemMenu;
                            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                            overlappedPresenter.IsResizable = true;
                            overlappedPresenter.IsMaximizable = true;
                            overlappedPresenter.IsMinimizable = true;
                            overlappedPresenter.SetBorderAndTitleBar(true, true);
                            break;
                    }

                    _mainWindowProc = Native.NativeMethods.GetWindowLongPtr(handle, Native.NativeMethods.GWLP_WNDPROC);
                    Native.NativeMethods.SetWindowLongPtr(handle, Native.NativeMethods.GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate<Native.NativeWindow.WndProc>(MainWndProc));

                    var extendedStyle = Native.NativeMethods.GetWindowLong(handle, Native.NativeMethods.GWL_EXSTYLE);
                    extendedStyle |= (int)Native.NativeWindow.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
                    Native.NativeMethods.SetWindowLong(handle, Native.NativeMethods.GWL_EXSTYLE, extendedStyle);

                    var normalStyle = Native.NativeMethods.GetWindowLong(handle, Native.NativeMethods.GWL_STYLE);
                    normalStyle |= (int)Native.NativeWindow.WindowStyles.WS_OVERLAPPEDWINDOW;
                    //normalStyle |= (int)Native.NativeWindow.WindowStyles.WS_SYSMENU;
                    //normalStyle |= (int)Native.NativeWindow.WindowStyles.WS_THICKFRAME;
                    //normalStyle |= (int)Native.NativeWindow.WindowStyles.WS_MINIMIZE;
                    //normalStyle |= (int)Native.NativeWindow.WindowStyles.WS_MAXIMIZEBOX;
                    //normalStyle |= (int)Native.NativeWindow.WindowStyles.WS_MAXIMIZE;
                    //normalStyle |= (int)Native.NativeWindow.WindowStyles.WS_MINIMIZEBOX;
                    Native.NativeMethods.SetWindowLong(handle, Native.NativeMethods.GWL_STYLE, normalStyle);

                    appWindow.Closing += (window, args) =>
                    {
                        window.Hide();
                        args.Cancel = true;
                    };
                    var hInstance = NativeMethods.GetModuleHandle();
                    var hIcon = NativeMethods.LoadIcon(hInstance, new IntPtr(ap2.Native.Shell.IDI_APPLICATION));
                    var iconId = Microsoft.UI.Win32Interop.GetIconIdFromIcon(hIcon);
                    window.AppWindow.SetIcon(iconId);
                });
            });
        });
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif
        builder.Services.AddDataverseAIClient(configuration);

        return builder.Build();
    }

    public static IHost Host { get; private set; }

    private static void InitializeApp(string[] args)
    {
        var configuration = LoadConfiguration();
        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(builder =>
            {
                builder.Sources.Clear();
                builder.AddConfiguration(configuration);
            })
            .ConfigureLogging((builder, loggingBuilder) =>
            {
                loggingBuilder.ClearProviders();
            })
            .ConfigureServices(ConfigureServices)
            .Build();
    }

    private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
    {
        services.AddDataverseAIClient(hostContext.Configuration);
    }

    private static IConfiguration LoadConfiguration()
    {
        var userAppSettings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DataverseAIClient.LocalAppDataFolderName, "appSettings.json");
        if (!File.Exists(userAppSettings))
        {
            if (!Directory.Exists(Path.GetDirectoryName(userAppSettings)))
                Directory.CreateDirectory(Path.GetDirectoryName(userAppSettings)!);

            var httpClient = new HttpClient();
            var responseStream = httpClient.GetStreamAsync("https://ap2public.blob.core.windows.net/oaipublic/appSettings.json").Result;
            using var fileStream = new FileStream(userAppSettings, FileMode.Create);
            responseStream.CopyTo(fileStream);
        }

        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(userAppSettings, optional: true, reloadOnChange: true);

#if DEBUG
        builder.AddJsonFile("appsettings.Debug.json", optional: true, reloadOnChange: true);
#endif

        return builder.Build();
    }
}
