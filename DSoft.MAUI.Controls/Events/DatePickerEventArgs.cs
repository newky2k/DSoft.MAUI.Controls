namespace DSoft.Maui.Controls.Events;

public class DateSelectedEventArgs(DateTime selectedDate, DateTime? previousDate) : EventArgs
{
    public DateTime SelectedDate { get; } = selectedDate;
    public DateTime? PreviousDate { get; } = previousDate;
}

public class DateRangeSelectedEventArgs(DateTime? startDate, DateTime? endDate) : EventArgs
{
    public DateTime? StartDate { get; } = startDate;
    public DateTime? EndDate { get; } = endDate;
}
