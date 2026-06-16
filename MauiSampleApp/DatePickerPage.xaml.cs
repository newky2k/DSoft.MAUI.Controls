using DSoft.Maui.Controls.Core.Enums;
using DSoft.Maui.Controls.Events;
using DSoft.Maui.Controls;

namespace MauiSampleApp;

public partial class DatePickerPage : ContentPage
{
    public DatePickerPage()
    {
        InitializeComponent();
    }

    private async void OnCloseClicked(object sender, EventArgs e)
        => await Navigation.PopModalAsync();

    private void OnModeChanged(object sender, SegmentSelectedEventArgs e)
    {
        Picker.Mode = e.SelectedIndex switch
        {
            1 => DatePickerMode.Time,
            2 => DatePickerMode.DateTime,
            _ => DatePickerMode.Date,
        };
    }

    private void OnRangeToggled(object sender, ToggledEventArgs e)
    {
        Picker.IsRangeSelectionEnabled = e.Value;
        Picker.SelectedStartDate = null;
        Picker.SelectedEndDate = null;
        ResultLabel.Text = "Selected: —";
    }

    private void OnHourFormatToggled(object sender, ToggledEventArgs e)
        => Picker.Use24HourFormat = e.Value;

    private void OnDateSelected(object sender, DateSelectedEventArgs e)
        => ResultLabel.Text = $"Selected: {e.SelectedDate:d MMMM yyyy}";

    private void OnDateRangeSelected(object sender, DateRangeSelectedEventArgs e)
        => ResultLabel.Text = $"Range: {e.StartDate:d MMM} – {e.EndDate:d MMM yyyy}";

    private void OnTimeChanged(object sender, DateSelectedEventArgs e)
        => ResultLabel.Text = $"Time: {e.SelectedDate:h:mm tt}";
}
