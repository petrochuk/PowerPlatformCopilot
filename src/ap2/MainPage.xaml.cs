using Microsoft.Maui.Controls;

namespace ap2;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
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

    private void Prompt_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!e.NewTextValue.EndsWith('\r') &&
            !e.NewTextValue.EndsWith('\n') &&
            !e.NewTextValue.EndsWith(Environment.NewLine))
            return;

        var lastRowDefinition = _grid.RowDefinitions.Last();
        _grid.RowDefinitions.Add(new RowDefinition { Height = _prompt.Height });
        _prompt.SetValue(Grid.RowProperty, _grid.RowDefinitions.Count-1);

        var histEntry = new Editor()
        {
            Text = _prompt.Text.Trim( new char[] { ' ', '\n', '\r' }),
            IsTextPredictionEnabled = false,
            IsSpellCheckEnabled = true,
            IsReadOnly = true,
            VerticalTextAlignment = TextAlignment.Center,
            AutoSize = EditorAutoSizeOption.TextChanges,
        };
        histEntry.SetValue(Grid.RowProperty, _grid.RowDefinitions.Count-2);
        _grid.Children.Add(histEntry);
        var promptMeasure = histEntry.Measure(_prompt.Width, double.MaxValue);
        lastRowDefinition.Height = promptMeasure.Request.Height;
        var heightGrowth = lastRowDefinition.Height.Value + _grid.RowSpacing;

#if WINDOWS
           IntPtr windowHandle  = WinRT.Interop.WindowNative.GetWindowHandle(Window.Handler.PlatformView);
           Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
           Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

           Windows.Graphics.RectInt32 rect = new();
           rect.X = appWindow.Position.X;
           rect.Y = (int)(appWindow.Position.Y - heightGrowth);
           rect.Width = appWindow.Size.Width;
           rect.Height = (int)(appWindow.Size.Height + heightGrowth);

           appWindow.MoveAndResize( rect );
#endif

        _prompt.Text = string.Empty;
        _prompt.Focus();
    }
}
