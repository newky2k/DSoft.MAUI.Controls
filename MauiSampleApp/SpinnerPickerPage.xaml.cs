using DSoft.Maui.Controls.Events;

namespace MauiSampleApp;

public record ColorSwatch(string Name, Color Swatch);

public partial class SpinnerPickerPage : ContentPage
{
    public SpinnerPickerPage()
    {
        InitializeComponent();
        PopulatePickers();
    }

    private void PopulatePickers()
    {
        // Date picker
        var months = new[]
        {
            "Jan", "Feb", "Mar", "Apr", "May", "Jun",
            "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
        };
        var days = Enumerable.Range(1, 31).Select(d => d.ToString()).ToList();
        var years = Enumerable.Range(2020, 11).Select(y => y.ToString()).ToList();

        MonthPicker.ItemsSource = months;
        DayPicker.ItemsSource = days;
        YearPicker.ItemsSource = years;

        var today = DateTime.Today;
        MonthPicker.SelectedIndex = today.Month - 1;
        DayPicker.SelectedIndex = today.Day - 1;
        YearPicker.SelectedIndex = Math.Clamp(today.Year - 2020, 0, 10);

        UpdateDateLabel();

        // DisplayMemberPath — list of objects; picker reads the Name property
        var countries = new[]
        {
            new { Name = "Australia" }, new { Name = "Brazil" },
            new { Name = "Canada" },    new { Name = "Denmark" },
            new { Name = "Egypt" },     new { Name = "France" },
            new { Name = "Germany" },   new { Name = "Hungary" },
            new { Name = "India" },     new { Name = "Japan" },
            new { Name = "Kenya" },     new { Name = "Luxembourg" },
            new { Name = "Mexico" },    new { Name = "Norway" },
            new { Name = "Portugal" },  new { Name = "Spain" },
            new { Name = "Turkey" },    new { Name = "Vietnam" },
        };
        CountryPicker.ItemsSource = countries;
        CountryLabel.Text = $"Selected: {countries[0].Name}";

        // ItemTemplate — rich rows with a colour swatch
        var swatches = new[]
        {
            new ColorSwatch("Crimson",       Color.FromArgb("#DC143C")),
            new ColorSwatch("DodgerBlue",    Color.FromArgb("#1E90FF")),
            new ColorSwatch("ForestGreen",   Color.FromArgb("#228B22")),
            new ColorSwatch("Gold",          Color.FromArgb("#FFD700")),
            new ColorSwatch("MediumOrchid",  Color.FromArgb("#BA55D3")),
            new ColorSwatch("OrangeRed",     Color.FromArgb("#FF4500")),
            new ColorSwatch("SteelBlue",     Color.FromArgb("#4682B4")),
            new ColorSwatch("Teal",          Color.FromArgb("#008080")),
        };
        ColorPicker.ItemsSource = swatches;
        ColorLabel.Text = $"Selected: {swatches[0].Name}";
    }

    private void OnSelectionChanged(object? sender, SpinnerSelectedEventArgs e)
        => UpdateDateLabel();

    private void UpdateDateLabel()
    {
        var month = MonthPicker.SelectedItem as string ?? "—";
        var day = DayPicker.SelectedItem as string ?? "—";
        var year = YearPicker.SelectedItem as string ?? "—";
        SelectionLabel.Text = $"Selected: {month} {day}, {year}";
    }

    private void OnCountrySelectionChanged(object? sender, SpinnerSelectedEventArgs e)
    {
        dynamic? item = e.SelectedItem;
        CountryLabel.Text = $"Selected: {item?.Name ?? "—"}";
    }

    private void OnColorSelectionChanged(object? sender, SpinnerSelectedEventArgs e)
    {
        if (e.SelectedItem is ColorSwatch swatch)
            ColorLabel.Text = $"Selected: {swatch.Name}";
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
        => await Navigation.PopModalAsync();
}
