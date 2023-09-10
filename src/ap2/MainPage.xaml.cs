using AP2.DataverseAzureAI;
using Microsoft.Maui.Controls.PlatformConfiguration;
using System.Drawing;
using ap2.Native;
using System.Text;

namespace ap2;

public partial class MainPage : ContentPage
{
    DataverseAIClient _dataverseAIClient;
    public MainPage()
    {
        InitializeComponent();

        _dataverseAIClient = MauiProgram.Host.Services.GetRequiredService<DataverseAIClient>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
    }

    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        _prompt.Focus();
    }

    private void Prompt_Completed(object sender, EventArgs e)
    {

    }

    private async void Prompt_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!e.NewTextValue.EndsWith('\r') &&
            !e.NewTextValue.EndsWith('\n') &&
            !e.NewTextValue.EndsWith(Environment.NewLine))
            return;

        var promptText = _prompt.Text.Trim(new char[] { ' ', '\n', '\r' });
        if (string.IsNullOrWhiteSpace(promptText))
            return;

        var response = _dataverseAIClient.GetChatCompletionAsync(promptText).ConfigureAwait(true);

        var lastRowDefinition = _grid.RowDefinitions.Last();
        _grid.RowDefinitions.Add(new RowDefinition { Height = _prompt.Height });
        _prompt.SetValue(Grid.RowProperty, _grid.RowDefinitions.Count-1);

        var histEntry = new Editor()
        {
            Text = promptText,
            IsTextPredictionEnabled = false,
            IsSpellCheckEnabled = true,
            IsReadOnly = true,
            VerticalTextAlignment = TextAlignment.Center,
            AutoSize = EditorAutoSizeOption.TextChanges,
        };
        if (App.Current.Resources.TryGetValue("PowerAppsDark", out var colorvalue))
            histEntry.BackgroundColor = (Microsoft.Maui.Graphics.Color)colorvalue;

        histEntry.SetValue(Grid.RowProperty, _grid.RowDefinitions.Count-2);
        _grid.Children.Add(histEntry);
        var promptMeasure = histEntry.Measure(_prompt.Width, double.MaxValue);
        lastRowDefinition.Height = promptMeasure.Request.Height;
        var heightGrowth = MauiProgram.ScaleY(lastRowDefinition.Height.Value + _grid.RowSpacing);

        Rectangle rect = Rectangle.Empty;
#if WINDOWS
        IntPtr windowHandle  = WinRT.Interop.WindowNative.GetWindowHandle(Window.Handler.PlatformView);
        var monitor = NativeMethods.MonitorFromWindow(windowHandle, NativeMethods.MonitorFromPointFlags.MONITOR_DEFAULTTONEAREST);
        var monitorInfo = new NativeMethods.MONITORINFOEX();
        NativeMethods.GetMonitorInfo(monitor, monitorInfo);

        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
        Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        if (appWindow.Position.Y < heightGrowth)
            heightGrowth = appWindow.Position.Y;

        rect = new (
            appWindow.Position.X,
            (int)(appWindow.Position.Y - heightGrowth),
            appWindow.Size.Width,
            (int)(appWindow.Size.Height + heightGrowth));

        appWindow.MoveAndResize(rect.ToRectInt32());
#endif

        _prompt.Text = string.Empty;
        _prompt.Focus();

        try
        {
            await AddResponse(await response, rect).ConfigureAwait(true);
        }
        catch (Azure.RequestFailedException ex)
        {
            var errors = ex.Message.Split("Status:");
            await AddResponse(errors[0], rect).ConfigureAwait(true);
        }
    }

    private async Task AddResponse(string response, Rectangle rect)
    {
        var lastRowDefinition = _grid.RowDefinitions.Last();
        _grid.RowDefinitions.Add(new RowDefinition { Height = _prompt.Height });
        _prompt.SetValue(Grid.RowProperty, _grid.RowDefinitions.Count - 1);

        var histEditor = new Editor()
        {
            
            Text = response,
            IsTextPredictionEnabled = false,
            IsSpellCheckEnabled = true,
            IsReadOnly = true,
            VerticalTextAlignment = TextAlignment.Center,
            AutoSize = EditorAutoSizeOption.TextChanges,
        };
        var histWebView = new WebView
        {
            Source = ResponseToHtml(response)
        };

        histWebView.SetValue(Grid.RowProperty, _grid.RowDefinitions.Count - 2);
        // Add editor first just to measure it
        _grid.Children.Add(histEditor);
        var promptMeasure = histEditor.Measure(_prompt.Width, double.MaxValue);
        _grid.Children.Remove(histEditor);
        _grid.Children.Add(histWebView);
        lastRowDefinition.Height = promptMeasure.Request.Height;
        var heightGrowth = MauiProgram.ScaleY(lastRowDefinition.Height.Value + _grid.RowSpacing);
        if (rect.Y < heightGrowth)
            heightGrowth = rect.Y;

        rect.Y -= (int)heightGrowth;
        rect.Height += (int)heightGrowth;
#if WINDOWS
        IntPtr windowHandle  = WinRT.Interop.WindowNative.GetWindowHandle(Window.Handler.PlatformView);
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
        Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

        appWindow.MoveAndResize(rect.ToRectInt32());
#endif
        _prompt.Focus();

        (_scrollView as IView).InvalidateArrange();
        await Task.Delay(100).ConfigureAwait(true);
        await _scrollView.ScrollToAsync(_prompt, ScrollToPosition.End, true).ConfigureAwait(true);
    }

    private HtmlWebViewSource ResponseToHtml(string response)
    {
        var v = Application.Current.RequestedTheme; 

        var htmlBuilder = new StringBuilder();
        htmlBuilder.Append("<html>");
        htmlBuilder.Append("<head>");
        htmlBuilder.Append("<style>body { font-family: 'Segoe UI', 'Helvetica Neue', sans-serif; font-size: 14px; }</style>");
        htmlBuilder.Append("</head>");
        htmlBuilder.Append("<body bgcolor='");
        htmlBuilder.Append(BackgroundColor.ToHex());
        htmlBuilder.Append("'");
        htmlBuilder.Append(" text='");
        if (Application.Current.RequestedTheme == AppTheme.Dark)
        {
            if (App.Current.Resources.TryGetValue("White", out var colorValue))
                htmlBuilder.Append(((Microsoft.Maui.Graphics.Color)colorValue).ToHex());
            else
                htmlBuilder.Append("#ffffff");
        }
        else
        {
            if (App.Current.Resources.TryGetValue("Gray900", out var colorValue))
                htmlBuilder.Append(((Microsoft.Maui.Graphics.Color)colorValue).ToHex());
            else
                htmlBuilder.Append("#000000");
        }
        htmlBuilder.Append("'>");
        htmlBuilder.Append(response.Replace("\n", "<br>"));
        htmlBuilder.Append("</body>");
        htmlBuilder.Append("</html>");

        var html = new HtmlWebViewSource();
        html.Html = htmlBuilder.ToString();
        return html;
    }
}
