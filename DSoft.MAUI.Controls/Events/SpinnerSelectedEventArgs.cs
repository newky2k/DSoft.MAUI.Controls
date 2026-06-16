namespace DSoft.Maui.Controls.Events;

public class SpinnerSelectedEventArgs(int selectedIndex, object? selectedItem, int previousIndex, object? previousItem)
    : EventArgs
{
    public int SelectedIndex { get; } = selectedIndex;
    public object? SelectedItem { get; } = selectedItem;
    public int PreviousIndex { get; } = previousIndex;
    public object? PreviousItem { get; } = previousItem;
}
