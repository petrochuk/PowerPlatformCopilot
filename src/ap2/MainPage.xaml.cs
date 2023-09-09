using AP2.DataverseAzureAI;
using Microsoft.Maui.Controls.PlatformConfiguration;
using System.Drawing;
using ap2.Native;

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
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
        Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

        rect = new (
            appWindow.Position.X,
            (int)(appWindow.Position.Y - heightGrowth),
            appWindow.Size.Width,
            (int)(appWindow.Size.Height + heightGrowth));

        appWindow.MoveAndResize(rect.ToRectInt32());
#endif

        _prompt.Text = string.Empty;
        _prompt.Focus();

        AddResponse(await response, rect);
    }

    private void AddResponse(string response, Rectangle rect)
    {
        var lastRowDefinition = _grid.RowDefinitions.Last();
        _grid.RowDefinitions.Add(new RowDefinition { Height = _prompt.Height });
        _prompt.SetValue(Grid.RowProperty, _grid.RowDefinitions.Count - 1);

        var histEntry = new Editor()
        {
            Text = response,
            IsTextPredictionEnabled = false,
            IsSpellCheckEnabled = true,
            IsReadOnly = true,
            VerticalTextAlignment = TextAlignment.Center,
            AutoSize = EditorAutoSizeOption.TextChanges,
        };

        histEntry.SetValue(Grid.RowProperty, _grid.RowDefinitions.Count - 2);
        _grid.Children.Add(histEntry);
        var promptMeasure = histEntry.Measure(_prompt.Width, double.MaxValue);
        lastRowDefinition.Height = promptMeasure.Request.Height;
        var heightGrowth = MauiProgram.ScaleY(lastRowDefinition.Height.Value + _grid.RowSpacing);

        rect.Y -= (int)heightGrowth;
        rect.Height += (int)heightGrowth;
#if WINDOWS
        IntPtr windowHandle  = WinRT.Interop.WindowNative.GetWindowHandle(Window.Handler.PlatformView);
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
        Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

        appWindow.MoveAndResize(rect.ToRectInt32());
#endif
    }
}
