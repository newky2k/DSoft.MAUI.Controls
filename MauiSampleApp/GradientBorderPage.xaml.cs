namespace MauiSampleApp;

public partial class GradientBorderPage : ContentPage
{
    public GradientBorderPage()
    {
        InitializeComponent();
    }

    private async void OnCloseClicked(object sender, EventArgs e)
        => await Navigation.PopModalAsync();

    private void OnSunsetClicked(object sender, EventArgs e)
        => SetGradient("#FF7E5F", "#FEB47B");

    private void OnOceanClicked(object sender, EventArgs e)
        => SetGradient("#00C6FB", "#005BEA");

    private void OnForestClicked(object sender, EventArgs e)
        => SetGradient("#11998E", "#38EF7D");

    private void OnPurpleClicked(object sender, EventArgs e)
        => SetGradient("#8E2DE2", "#4A00E0");

    private void SetGradient(string fromHex, string toHex)
    {
        PreviewBorder.FromColor = Color.FromArgb(fromHex);
        PreviewBorder.ToColor = Color.FromArgb(toHex);
    }
}
