namespace ap2;

public partial class ExitPage : ContentPage
{
	public ExitPage()
	{
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        App.Current.Quit();
        MauiProgram.Quit();
        base.OnAppearing();
    }
}