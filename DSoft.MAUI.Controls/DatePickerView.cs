using DSoft.Maui.Controls.Core.Enums;
using DSoft.Maui.Controls.Events;

namespace DSoft.Maui.Controls;

/// <summary>
/// An iOS-style calendar date/time picker with optional range selection.
/// Supports Date, Time, and DateTime modes.
/// </summary>
public class DatePickerView : ContentView
{
    #region Fields

    private Grid? _calendarGrid;
    private Grid? _dayNamesRow;
    private readonly List<DayCell> _dayCells = new();
    private readonly List<Label> _dayNameLabels = new();
    private Label? _monthYearLabel;
    private Grid? _monthYearPickerGrid;
    private Border? _monthYearPickerCard;
    private SpinnerPickerView? _monthSpinner;
    private SpinnerPickerView? _yearSpinner;
    private Grid? _headerGrid;
    private Button? _prevMonthButton;
    private Button? _nextMonthButton;
    private Button? _todayButton;
    private Grid? _calendarSection;
    private Grid? _timeSection;
    private SpinnerPickerView? _hourPicker;
    private SpinnerPickerView? _minutePicker;
    private SpinnerPickerView? _amPmPicker;
    private DateTime _displayedMonth;
    private bool _showingMonthYearPicker;
    private bool _suppressTimeCallbacks;
    private bool _suppressMonthYearCallbacks;

    private static readonly Color DefaultTodayColor = Color.FromArgb("#007AFF");
    private static readonly Color DefaultSelectedColor = Color.FromArgb("#007AFF");
    private static readonly Color DefaultRangeColor = Color.FromArgb("#CCE4FF");
    private static readonly Color DefaultHeaderColor = Colors.Transparent;
    private static readonly Color DefaultDayNameColor = Colors.Gray;
    private static readonly Color DefaultDayColor = Colors.Black;
    private static readonly Color DefaultDisabledColor = Colors.LightGray;
    private static readonly Color DefaultOtherMonthColor = Colors.LightGray;

    // A month grid is always rendered with a full 6 weeks (padded with adjacent-month
    // overflow days) so the calendar area has a constant height. That lets the
    // month/year picker occupy the same fixed-size slot without ever resizing
    // the section — swapping between calendar and picker no longer reflows the
    // rest of the control.
    private const int CalendarVisibleRows = 6;
    private const double CalendarRowHeight = 44;
    private const double CalendarAreaHeight = CalendarRowHeight * CalendarVisibleRows;

    // Nav button HeightRequest (44) + header Padding top/bottom (8+4). Fixed at
    // the RowDefinition level so hiding the prev/next/today buttons — via opacity,
    // not IsVisible, see SetNavButtonsReserved — can never change this row's height.
    private const double HeaderAreaHeight = 44 + 8 + 4;

    // Day-name label HeightRequest (28) + row Padding bottom (4). Also fixed —
    // this row is hidden outright (IsVisible=false) while the month/year card is
    // open, and an Auto row collapses to 0 when its only child goes invisible.
    private const double DayNamesAreaHeight = 28 + 4;

    private List<string>? _cachedMonthNames;
    private List<int>? _cachedYears;
    private int _cachedYearsMin = int.MinValue;
    private int _cachedYearsMax = int.MinValue;

    #endregion

    #region Day Cell

    /// <summary>
    /// A single day cell in the calendar grid. The 42 cells are created once and then
    /// re-stamped in place (see <see cref="ApplyDayCellState"/>), so neither paging
    /// between months nor changing an appearance property allocates any views.
    /// </summary>
    private sealed class DayCell
    {
        public DayCell(Grid container, Border shape, Label label)
        {
            Container = container;
            Shape = shape;
            Label = label;
        }

        public Grid Container { get; }
        public Border Shape { get; }
        public Label Label { get; }

        public DateTime Date { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool IsDisabled { get; set; }
    }

    #endregion

    #region Bindable Properties

    public static readonly BindableProperty ModeProperty = BindableProperty.Create(
        nameof(Mode), typeof(DatePickerMode), typeof(DatePickerView),
        DatePickerMode.Date, propertyChanged: OnModeChanged);

    public DatePickerMode Mode
    {
        get => (DatePickerMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public static readonly BindableProperty SelectedDateProperty = BindableProperty.Create(
        nameof(SelectedDate), typeof(DateTime), typeof(DatePickerView),
        DateTime.Today, BindingMode.TwoWay, propertyChanged: OnSelectedDateChanged);

    public DateTime SelectedDate
    {
        get => (DateTime)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public static readonly BindableProperty MinimumDateProperty = BindableProperty.Create(
        nameof(MinimumDate), typeof(DateTime?), typeof(DatePickerView),
        null, propertyChanged: OnCalendarRepaintRequired);

    public DateTime? MinimumDate
    {
        get => (DateTime?)GetValue(MinimumDateProperty);
        set => SetValue(MinimumDateProperty, value);
    }

    public static readonly BindableProperty MaximumDateProperty = BindableProperty.Create(
        nameof(MaximumDate), typeof(DateTime?), typeof(DatePickerView),
        null, propertyChanged: OnCalendarRepaintRequired);

    public DateTime? MaximumDate
    {
        get => (DateTime?)GetValue(MaximumDateProperty);
        set => SetValue(MaximumDateProperty, value);
    }

    public static readonly BindableProperty IsRangeSelectionEnabledProperty = BindableProperty.Create(
        nameof(IsRangeSelectionEnabled), typeof(bool), typeof(DatePickerView),
        false, propertyChanged: OnCalendarRepaintRequired);

    public bool IsRangeSelectionEnabled
    {
        get => (bool)GetValue(IsRangeSelectionEnabledProperty);
        set => SetValue(IsRangeSelectionEnabledProperty, value);
    }

    public static readonly BindableProperty SelectedStartDateProperty = BindableProperty.Create(
        nameof(SelectedStartDate), typeof(DateTime?), typeof(DatePickerView),
        null, BindingMode.TwoWay, propertyChanged: OnCalendarRepaintRequired);

    public DateTime? SelectedStartDate
    {
        get => (DateTime?)GetValue(SelectedStartDateProperty);
        set => SetValue(SelectedStartDateProperty, value);
    }

    public static readonly BindableProperty SelectedEndDateProperty = BindableProperty.Create(
        nameof(SelectedEndDate), typeof(DateTime?), typeof(DatePickerView),
        null, BindingMode.TwoWay, propertyChanged: OnCalendarRepaintRequired);

    public DateTime? SelectedEndDate
    {
        get => (DateTime?)GetValue(SelectedEndDateProperty);
        set => SetValue(SelectedEndDateProperty, value);
    }

    public static readonly BindableProperty Use24HourFormatProperty = BindableProperty.Create(
        nameof(Use24HourFormat), typeof(bool), typeof(DatePickerView),
        false, propertyChanged: OnTimeRebuildRequired);

    public bool Use24HourFormat
    {
        get => (bool)GetValue(Use24HourFormatProperty);
        set => SetValue(Use24HourFormatProperty, value);
    }

    public static readonly BindableProperty ShowTodayButtonProperty = BindableProperty.Create(
        nameof(ShowTodayButton), typeof(bool), typeof(DatePickerView),
        false, propertyChanged: OnShowTodayButtonChanged);

    /// <summary>
    /// When true, shows a "Today" button in the top-left of the header that
    /// jumps back to today's date. The month prev/next buttons move together
    /// on the right to make room.
    /// </summary>
    public bool ShowTodayButton
    {
        get => (bool)GetValue(ShowTodayButtonProperty);
        set => SetValue(ShowTodayButtonProperty, value);
    }

    // --- Appearance ---

    public static readonly BindableProperty TodayHighlightColorProperty = BindableProperty.Create(
        nameof(TodayHighlightColor), typeof(Color), typeof(DatePickerView),
        DefaultTodayColor, propertyChanged: OnCalendarRepaintRequired);

    public Color TodayHighlightColor
    {
        get => (Color)GetValue(TodayHighlightColorProperty);
        set => SetValue(TodayHighlightColorProperty, value);
    }

    public static readonly BindableProperty SelectionColorProperty = BindableProperty.Create(
        nameof(SelectionColor), typeof(Color), typeof(DatePickerView),
        DefaultSelectedColor, propertyChanged: OnCalendarRepaintRequired);

    public Color SelectionColor
    {
        get => (Color)GetValue(SelectionColorProperty);
        set => SetValue(SelectionColorProperty, value);
    }

    public static readonly BindableProperty RangeHighlightColorProperty = BindableProperty.Create(
        nameof(RangeHighlightColor), typeof(Color), typeof(DatePickerView),
        DefaultRangeColor, propertyChanged: OnCalendarRepaintRequired);

    public Color RangeHighlightColor
    {
        get => (Color)GetValue(RangeHighlightColorProperty);
        set => SetValue(RangeHighlightColorProperty, value);
    }

    public static readonly BindableProperty HeaderBackgroundColorProperty = BindableProperty.Create(
        nameof(HeaderBackgroundColor), typeof(Color), typeof(DatePickerView),
        DefaultHeaderColor, propertyChanged: OnCalendarRepaintRequired);

    public Color HeaderBackgroundColor
    {
        get => (Color)GetValue(HeaderBackgroundColorProperty);
        set => SetValue(HeaderBackgroundColorProperty, value);
    }

    public static readonly BindableProperty DayNameColorProperty = BindableProperty.Create(
        nameof(DayNameColor), typeof(Color), typeof(DatePickerView),
        DefaultDayNameColor, propertyChanged: OnCalendarRepaintRequired);

    public Color DayNameColor
    {
        get => (Color)GetValue(DayNameColorProperty);
        set => SetValue(DayNameColorProperty, value);
    }

    public static readonly BindableProperty DayColorProperty = BindableProperty.Create(
        nameof(DayColor), typeof(Color), typeof(DatePickerView),
        DefaultDayColor, propertyChanged: OnCalendarRepaintRequired);

    public Color DayColor
    {
        get => (Color)GetValue(DayColorProperty);
        set => SetValue(DayColorProperty, value);
    }

    public static readonly BindableProperty DisabledDayColorProperty = BindableProperty.Create(
        nameof(DisabledDayColor), typeof(Color), typeof(DatePickerView),
        DefaultDisabledColor, propertyChanged: OnCalendarRepaintRequired);

    public Color DisabledDayColor
    {
        get => (Color)GetValue(DisabledDayColorProperty);
        set => SetValue(DisabledDayColorProperty, value);
    }

    public static readonly BindableProperty OtherMonthDayColorProperty = BindableProperty.Create(
        nameof(OtherMonthDayColor), typeof(Color), typeof(DatePickerView),
        DefaultOtherMonthColor, propertyChanged: OnCalendarRepaintRequired);

    public Color OtherMonthDayColor
    {
        get => (Color)GetValue(OtherMonthDayColorProperty);
        set => SetValue(OtherMonthDayColorProperty, value);
    }

    public static readonly BindableProperty SpinnerTextColorProperty = BindableProperty.Create(
        nameof(SpinnerTextColor), typeof(Color), typeof(DatePickerView),
        Colors.Gray, propertyChanged: OnTimeStyleChanged);

    public Color SpinnerTextColor
    {
        get => (Color)GetValue(SpinnerTextColorProperty);
        set => SetValue(SpinnerTextColorProperty, value);
    }

    public static readonly BindableProperty SpinnerSelectedTextColorProperty = BindableProperty.Create(
        nameof(SpinnerSelectedTextColor), typeof(Color), typeof(DatePickerView),
        Colors.Black, propertyChanged: OnTimeStyleChanged);

    public Color SpinnerSelectedTextColor
    {
        get => (Color)GetValue(SpinnerSelectedTextColorProperty);
        set => SetValue(SpinnerSelectedTextColorProperty, value);
    }

    public static readonly BindableProperty SpinnerSelectorColorProperty = BindableProperty.Create(
        nameof(SpinnerSelectorColor), typeof(Color), typeof(DatePickerView),
        Colors.LightGray, propertyChanged: OnTimeStyleChanged);

    public Color SpinnerSelectorColor
    {
        get => (Color)GetValue(SpinnerSelectorColorProperty);
        set => SetValue(SpinnerSelectorColorProperty, value);
    }

    #endregion

    #region Events

    public event EventHandler<DateSelectedEventArgs>? DateSelected;
    public event EventHandler<DateRangeSelectedEventArgs>? DateRangeSelected;
    public event EventHandler<DateSelectedEventArgs>? TimeChanged;

    #endregion

    #region Constructor

    public DatePickerView()
    {
        _displayedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        BuildLayout();
    }

    #endregion

    #region Layout Building

    private void BuildLayout()
    {
        var root = new VerticalStackLayout { Spacing = 0 };

        _calendarSection = BuildCalendarSection();
        _timeSection = BuildTimeSection();

        root.Add(_calendarSection);
        root.Add(_timeSection);

        ApplyModeVisibility();
        Content = root;
    }

    private Grid BuildCalendarSection()
    {
        var section = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = new GridLength(HeaderAreaHeight) }, // header
                new RowDefinition { Height = new GridLength(DayNamesAreaHeight) }, // day names
                new RowDefinition { Height = new GridLength(CalendarAreaHeight) }, // calendar / month-year card
            }
        };

        // Header row
        var header = BuildCalendarHeader();
        Grid.SetRow(header, 0);
        section.Add(header);

        // Day names row
        _dayNamesRow = BuildDayNamesRow();
        Grid.SetRow(_dayNamesRow, 1);
        section.Add(_dayNamesRow);

        // Fixed height so toggling between the calendar grid and the month/year
        // picker never changes this section's measured size (see CalendarAreaHeight).
        var calendarContainer = new Grid { HeightRequest = CalendarAreaHeight };

        _calendarGrid = new Grid();
        calendarContainer.Add(_calendarGrid);

        _monthYearPickerGrid = BuildMonthYearPickerGrid();
        _monthYearPickerCard = new Border
        {
            Content = _monthYearPickerGrid,
            BackgroundColor = Colors.White,
            Stroke = Colors.LightGray,
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.25f, Radius = 16, Offset = new Point(0, 6) },
            IsVisible = false,
        };
        calendarContainer.Add(_monthYearPickerCard);

        Grid.SetRow(calendarContainer, 2);
        section.Add(calendarContainer);

        PopulateCalendarGrid();
        return section;
    }

    private Grid BuildCalendarHeader()
    {
        _headerGrid = new Grid
        {
            BackgroundColor = HeaderBackgroundColor,
            Padding = new Thickness(8, 8, 8, 4),
        };

        _prevMonthButton = CreateNavButton("‹");
        _prevMonthButton.Clicked += OnPrevMonthClicked;

        _nextMonthButton = CreateNavButton("›");
        _nextMonthButton.Clicked += OnNextMonthClicked;

        _todayButton = new Button
        {
            Text = "Today",
            FontSize = 14,
            BackgroundColor = Colors.Transparent,
            TextColor = SelectionColor,
            Padding = new Thickness(4, 0),
        };
        _todayButton.Clicked += OnTodayClicked;

        _monthYearLabel = new Label
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            FontSize = 17,
            FontAttributes = FontAttributes.Bold,
            TextColor = DayColor,
        };
        UpdateMonthYearLabel();

        var labelTap = new TapGestureRecognizer();
        labelTap.Tapped += OnMonthYearLabelTapped;
        _monthYearLabel.GestureRecognizers.Add(labelTap);

        LayoutCalendarHeader();

        return _headerGrid;
    }

    private Button CreateNavButton(string text) => new Button
    {
        Text = text,
        FontSize = 22,
        BackgroundColor = Colors.Transparent,
        TextColor = SelectionColor,
        WidthRequest = 44,
        HeightRequest = 44,
        Padding = 0,
    };

    private void LayoutCalendarHeader()
    {
        if (_headerGrid == null || _prevMonthButton == null || _nextMonthButton == null
            || _todayButton == null || _monthYearLabel == null) return;

        _headerGrid.Children.Clear();
        _headerGrid.ColumnDefinitions.Clear();

        if (ShowTodayButton)
        {
            // Today | label | ‹ | ›
            _headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            _headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(_todayButton, 0);
            Grid.SetColumn(_monthYearLabel, 1);
            Grid.SetColumn(_prevMonthButton, 2);
            Grid.SetColumn(_nextMonthButton, 3);

            _headerGrid.Add(_todayButton);
            _headerGrid.Add(_monthYearLabel);
            _headerGrid.Add(_prevMonthButton);
            _headerGrid.Add(_nextMonthButton);
        }
        else
        {
            // ‹ | label | ›
            _headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            _headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(_prevMonthButton, 0);
            Grid.SetColumn(_monthYearLabel, 1);
            Grid.SetColumn(_nextMonthButton, 2);

            _headerGrid.Add(_prevMonthButton);
            _headerGrid.Add(_monthYearLabel);
            _headerGrid.Add(_nextMonthButton);
        }
    }

    private Grid BuildDayNamesRow()
    {
        var colDefs = new ColumnDefinitionCollection();
        for (var i = 0; i < 7; i++)
            colDefs.Add(new ColumnDefinition { Width = GridLength.Star });

        var grid = new Grid
        {
            ColumnDefinitions = colDefs,
            Padding = new Thickness(4, 0, 4, 4),
        };

        var dayNames = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        _dayNameLabels.Clear();
        for (var i = 0; i < 7; i++)
        {
            var lbl = new Label
            {
                Text = dayNames[i],
                TextColor = DayNameColor,
                FontSize = 12,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                HeightRequest = 28,
            };
            Grid.SetColumn(lbl, i);
            grid.Add(lbl);
            _dayNameLabels.Add(lbl);
        }

        return grid;
    }

    /// <summary>
    /// Creates the 6x7 grid skeleton and its 42 day cells. Runs once per control;
    /// every later populate or repaint re-stamps the cells created here.
    /// </summary>
    private void EnsureCalendarSkeleton()
    {
        if (_calendarGrid == null || _dayCells.Count > 0) return;

        for (var i = 0; i < 7; i++)
            _calendarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

        // Always a full 6 weeks (padded with adjacent-month overflow days) so the
        // grid's height is constant across months — see CalendarAreaHeight.
        for (var r = 0; r < CalendarVisibleRows; r++)
            _calendarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(CalendarRowHeight) });

        _calendarGrid.Padding = new Thickness(4, 0, 4, 0);

        for (var cell = 0; cell < CalendarVisibleRows * 7; cell++)
        {
            var dayCell = CreateDayCell();
            Grid.SetRow(dayCell.Container, cell / 7);
            Grid.SetColumn(dayCell.Container, cell % 7);
            _calendarGrid.Add(dayCell.Container);
            _dayCells.Add(dayCell);
        }
    }

    /// <summary>
    /// Assigns the dates of the displayed month — padded with adjacent-month overflow
    /// days — to the existing cells and repaints them.
    /// </summary>
    private void PopulateCalendarGrid()
    {
        if (_calendarGrid == null) return;

        EnsureCalendarSkeleton();

        var firstDay = _displayedMonth;
        var daysInMonth = DateTime.DaysInMonth(firstDay.Year, firstDay.Month);
        var startDayOfWeek = (int)firstDay.DayOfWeek; // 0=Sun

        for (var cell = 0; cell < _dayCells.Count; cell++)
        {
            var dayNumber = cell - startDayOfWeek + 1;
            var isCurrentMonth = dayNumber >= 1 && dayNumber <= daysInMonth;

            DateTime date;
            if (isCurrentMonth)
            {
                date = new DateTime(firstDay.Year, firstDay.Month, dayNumber);
            }
            else if (cell < startDayOfWeek)
            {
                var prevMonth = firstDay.AddMonths(-1);
                var prevDays = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
                date = new DateTime(prevMonth.Year, prevMonth.Month, prevDays - (startDayOfWeek - cell - 1));
            }
            else
            {
                var nextMonth = firstDay.AddMonths(1);
                date = new DateTime(nextMonth.Year, nextMonth.Month, dayNumber - daysInMonth);
            }

            var dayCell = _dayCells[cell];
            dayCell.Date = date;
            dayCell.IsCurrentMonth = isCurrentMonth;
            ApplyDayCellState(dayCell);
        }
    }

    /// <summary>
    /// Repaints every cell — plus the header and day-name row — without touching the
    /// dates the cells hold.
    /// </summary>
    private void RepaintCalendar()
    {
        if (_headerGrid != null)
            _headerGrid.BackgroundColor = HeaderBackgroundColor;

        foreach (var lbl in _dayNameLabels)
            lbl.TextColor = DayNameColor;

        foreach (var cell in _dayCells)
            ApplyDayCellState(cell);
    }

    /// <summary>
    /// Repaints a single day cell in place, e.g. after a selection change that
    /// doesn't require repainting the whole visible month.
    /// </summary>
    private void RefreshDayCell(DateTime date)
    {
        date = date.Date;

        foreach (var cell in _dayCells)
        {
            if (cell.Date.Date != date) continue;

            ApplyDayCellState(cell);
            return;
        }
    }

    /// <summary>
    /// Builds an empty cell. Every cell has the same shape — a container grid holding a
    /// centred <see cref="Border"/> around the day label — so any state can be expressed
    /// by changing colours alone. The plain (unselected, not-today) look is a transparent
    /// border with zero stroke thickness.
    /// </summary>
    private DayCell CreateDayCell()
    {
        var label = new Label
        {
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            FontSize = 15,
        };

        var shape = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.Ellipse(),
            WidthRequest = 36,
            HeightRequest = 36,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Content = label,
        };

        var container = new Grid { HeightRequest = 44 };
        container.Add(shape);

        var cell = new DayCell(container, shape, label);

        // Attached once. Disabled dates are gated at tap time rather than by adding and
        // removing the recognizer, so a min/max change never needs the cell rebuilt.
        var tap = new TapGestureRecognizer();
        tap.Tapped += (s, e) =>
        {
            if (cell.IsDisabled) return;
            OnDayTapped(cell.Date);
        };
        container.GestureRecognizers.Add(tap);

        return cell;
    }

    /// <summary>
    /// Stamps the current selection, range and enabled state — and the appearance
    /// properties — onto an existing cell. Allocates nothing.
    /// </summary>
    private void ApplyDayCellState(DayCell cell)
    {
        var date = cell.Date.Date;
        var isToday = date == DateTime.Today;
        var isSelected = !IsRangeSelectionEnabled && date == SelectedDate.Date;
        var isDisabled = (MinimumDate.HasValue && date < MinimumDate.Value.Date)
                      || (MaximumDate.HasValue && date > MaximumDate.Value.Date);

        var isRangeStart = IsRangeSelectionEnabled && SelectedStartDate.HasValue && date == SelectedStartDate.Value.Date;
        var isRangeEnd = IsRangeSelectionEnabled && SelectedEndDate.HasValue && date == SelectedEndDate.Value.Date;
        var isInRange = IsRangeSelectionEnabled
            && SelectedStartDate.HasValue && SelectedEndDate.HasValue
            && date > SelectedStartDate.Value.Date
            && date < SelectedEndDate.Value.Date;

        var isHighlighted = isSelected || isRangeStart || isRangeEnd;
        var showTodayRing = isToday && !isHighlighted;

        Color textColor;
        if (isHighlighted) textColor = Colors.White;
        else if (isInRange) textColor = DayColor;
        else if (isToday) textColor = TodayHighlightColor;
        else if (!cell.IsCurrentMonth) textColor = OtherMonthDayColor;
        else if (isDisabled) textColor = DisabledDayColor;
        else textColor = DayColor;

        cell.IsDisabled = isDisabled;

        cell.Label.Text = date.Day.ToString();
        cell.Label.TextColor = textColor;
        cell.Label.FontAttributes = showTodayRing ? FontAttributes.Bold : FontAttributes.None;

        cell.Shape.BackgroundColor = isHighlighted ? SelectionColor : Colors.Transparent;
        cell.Shape.Stroke = showTodayRing ? TodayHighlightColor : Colors.Transparent;
        cell.Shape.StrokeThickness = showTodayRing ? 2 : 0;

        cell.Container.BackgroundColor = isInRange ? RangeHighlightColor : Colors.Transparent;
    }

    private Grid BuildTimeSection()
    {
        var section = new Grid
        {
            Padding = new Thickness(16, 8, 16, 8),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }, // AM/PM (hidden in 24h)
            }
        };

        _hourPicker = new SpinnerPickerView
        {
            VisibleItemCount = 3,
            ItemHeight = 44,
            IsLooping = true,
            TextColor = SpinnerTextColor,
            SelectedTextColor = SpinnerSelectedTextColor,
            SelectorColor = SpinnerSelectorColor,
        };
        _hourPicker.SelectionChanged += OnHourChanged;

        _minutePicker = new SpinnerPickerView
        {
            VisibleItemCount = 3,
            ItemHeight = 44,
            IsLooping = true,
            TextColor = SpinnerTextColor,
            SelectedTextColor = SpinnerSelectedTextColor,
            SelectorColor = SpinnerSelectorColor,
        };
        _minutePicker.SelectionChanged += OnMinuteChanged;

        _amPmPicker = new SpinnerPickerView
        {
            VisibleItemCount = 3,
            ItemHeight = 44,
            IsLooping = false,
            TextColor = SpinnerTextColor,
            SelectedTextColor = SpinnerSelectedTextColor,
            SelectorColor = SpinnerSelectorColor,
            ItemsSource = new List<string> { "AM", "PM" },
        };
        _amPmPicker.SelectionChanged += OnAmPmChanged;

        var colon = new Label
        {
            Text = ":",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            TextColor = DayColor,
            WidthRequest = 16,
        };

        Grid.SetColumn(_hourPicker, 0);
        Grid.SetColumn(colon, 1);
        Grid.SetColumn(_minutePicker, 2);
        Grid.SetColumn(_amPmPicker, 3);

        section.Add(_hourPicker);
        section.Add(colon);
        section.Add(_minutePicker);
        section.Add(_amPmPicker);

        PopulateTimePickers();
        return section;
    }

    private void PopulateTimePickers()
    {
        if (_hourPicker == null || _minutePicker == null || _amPmPicker == null) return;

        _suppressTimeCallbacks = true;

        if (Use24HourFormat)
        {
            _hourPicker.ItemsSource = Enumerable.Range(0, 24).Select(h => h.ToString("D2")).ToList();
            _hourPicker.SelectedIndex = SelectedDate.Hour;
            _amPmPicker.IsVisible = false;
        }
        else
        {
            _hourPicker.ItemsSource = Enumerable.Range(1, 12).Select(h => h.ToString()).ToList();
            var hour12 = SelectedDate.Hour % 12;
            if (hour12 == 0) hour12 = 12;
            _hourPicker.SelectedIndex = hour12 - 1;
            _amPmPicker.IsVisible = true;
            _amPmPicker.SelectedIndex = SelectedDate.Hour >= 12 ? 1 : 0;
        }

        _minutePicker.ItemsSource = Enumerable.Range(0, 60).Select(m => m.ToString("D2")).ToList();
        _minutePicker.SelectedIndex = SelectedDate.Minute;

        _suppressTimeCallbacks = false;
    }

    private void UpdateTimeSelection()
    {
        if (_hourPicker == null || _minutePicker == null || _amPmPicker == null) return;

        _suppressTimeCallbacks = true;

        if (Use24HourFormat)
        {
            _hourPicker.SelectedIndex = SelectedDate.Hour;
        }
        else
        {
            var hour12 = SelectedDate.Hour % 12;
            if (hour12 == 0) hour12 = 12;
            _hourPicker.SelectedIndex = hour12 - 1;
            _amPmPicker.SelectedIndex = SelectedDate.Hour >= 12 ? 1 : 0;
        }

        _minutePicker.SelectedIndex = SelectedDate.Minute;

        _suppressTimeCallbacks = false;
    }

    private Grid BuildMonthYearPickerGrid()
    {
        var grid = new Grid
        {
            Padding = new Thickness(16, 0),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
            }
        };

        _monthSpinner = new SpinnerPickerView
        {
            VisibleItemCount = CalendarVisibleRows,
            ItemHeight = CalendarRowHeight,
            IsLooping = false,
            TextColor = SpinnerTextColor,
            SelectedTextColor = SpinnerSelectedTextColor,
            SelectorColor = SpinnerSelectorColor,
        };
        _monthSpinner.SelectionChanged += OnMonthSpinnerChanged;

        _yearSpinner = new SpinnerPickerView
        {
            VisibleItemCount = CalendarVisibleRows,
            ItemHeight = CalendarRowHeight,
            IsLooping = false,
            TextColor = SpinnerTextColor,
            SelectedTextColor = SpinnerSelectedTextColor,
            SelectorColor = SpinnerSelectorColor,
        };
        _yearSpinner.SelectionChanged += OnYearSpinnerChanged;

        Grid.SetColumn(_monthSpinner, 0);
        Grid.SetColumn(_yearSpinner, 1);
        grid.Add(_monthSpinner);
        grid.Add(_yearSpinner);

        return grid;
    }

    private void OpenMonthYearPicker()
    {
        if (_monthSpinner == null || _yearSpinner == null) return;

        _suppressMonthYearCallbacks = true;

        // Month names never change at runtime, and the year range is only a
        // function of Minimum/MaximumDate — cache both so reopening the picker
        // doesn't rebuild ~150 native spinner rows every time (SpinnerPickerView
        // rebuilds its item views whenever ItemsSource is reassigned).
        _cachedMonthNames ??= System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.MonthNames
            .Where(m => !string.IsNullOrEmpty(m))
            .ToList();
        if (!ReferenceEquals(_monthSpinner.ItemsSource, _cachedMonthNames))
            _monthSpinner.ItemsSource = _cachedMonthNames;
        _monthSpinner.SelectedIndex = _displayedMonth.Month - 1;

        var minYear = MinimumDate?.Year ?? Math.Min(DateTime.Today.Year - 100, _displayedMonth.Year);
        var maxYear = MaximumDate?.Year ?? Math.Max(DateTime.Today.Year + 50, _displayedMonth.Year);
        if (_cachedYears == null || _cachedYearsMin != minYear || _cachedYearsMax != maxYear)
        {
            _cachedYears = Enumerable.Range(minYear, maxYear - minYear + 1).ToList();
            _cachedYearsMin = minYear;
            _cachedYearsMax = maxYear;
            _yearSpinner.ItemsSource = _cachedYears;
        }
        _yearSpinner.SelectedIndex = _cachedYears.IndexOf(_displayedMonth.Year);

        _suppressMonthYearCallbacks = false;
    }

    private void OnMonthSpinnerChanged(object? sender, Events.SpinnerSelectedEventArgs e)
    {
        if (_suppressMonthYearCallbacks) return;
        UpdateDisplayedMonthFromSpinners();
    }

    private void OnYearSpinnerChanged(object? sender, Events.SpinnerSelectedEventArgs e)
    {
        if (_suppressMonthYearCallbacks) return;
        UpdateDisplayedMonthFromSpinners();
    }

    private void UpdateDisplayedMonthFromSpinners()
    {
        if (_monthSpinner == null || _yearSpinner == null) return;
        if (_yearSpinner.SelectedItem is not int year) return;

        var month = _monthSpinner.SelectedIndex + 1;
        _displayedMonth = new DateTime(year, month, 1);
        UpdateMonthYearLabel();
    }

    private void ApplyModeVisibility()
    {
        if (_calendarSection == null || _timeSection == null) return;

        _calendarSection.IsVisible = Mode == DatePickerMode.Date || Mode == DatePickerMode.DateTime;
        _timeSection.IsVisible = Mode == DatePickerMode.Time || Mode == DatePickerMode.DateTime;
    }

    #endregion

    #region Interaction Handlers

    private void OnPrevMonthClicked(object? sender, EventArgs e)
    {
        var newMonth = _displayedMonth.AddMonths(-1);
        if (MinimumDate.HasValue && newMonth < new DateTime(MinimumDate.Value.Year, MinimumDate.Value.Month, 1))
            return;
        _displayedMonth = newMonth;
        UpdateMonthYearLabel();
        PopulateCalendarGrid();
    }

    private void OnNextMonthClicked(object? sender, EventArgs e) 
    {
        var newMonth = _displayedMonth.AddMonths(1);
        if (MaximumDate.HasValue && newMonth > new DateTime(MaximumDate.Value.Year, MaximumDate.Value.Month, 1))
            return;
        _displayedMonth = newMonth;
        UpdateMonthYearLabel();
        PopulateCalendarGrid();
    }

    private void OnMonthYearLabelTapped(object? sender, TappedEventArgs e)
    {
        _showingMonthYearPicker = !_showingMonthYearPicker;

        if (_dayNamesRow != null) _dayNamesRow.IsVisible = !_showingMonthYearPicker;
        SetNavButtonsReserved(!_showingMonthYearPicker);

        if (_showingMonthYearPicker)
        {
            OpenMonthYearPicker();
            ShowMonthYearCard();
        }
        else
        {
            PopulateCalendarGrid();
            HideMonthYearCard();
        }

        UpdateMonthYearLabel();
    }

    /// <summary>
    /// Hides the prev/next/today buttons visually (opacity + input-transparent)
    /// rather than via IsVisible, so the header row's Auto height — driven by
    /// their 44px HeightRequest — stays constant whether they're shown or not.
    /// </summary>
    private void SetNavButtonsReserved(bool shown)
    {
        var opacity = shown ? 1d : 0d;
        if (_prevMonthButton != null) { _prevMonthButton.Opacity = opacity; _prevMonthButton.InputTransparent = !shown; }
        if (_nextMonthButton != null) { _nextMonthButton.Opacity = opacity; _nextMonthButton.InputTransparent = !shown; }
        if (_todayButton != null) { _todayButton.Opacity = opacity; _todayButton.InputTransparent = !shown; }
    }

    // The calendar grid underneath is never hidden — the card is a fully opaque
    // layer that floats over it in the same cell. Toggling _calendarGrid.IsVisible
    // was triggering a remeasure that collapsed the fixed-height container, which
    // is exactly the resize this control is trying to avoid.
    private async void ShowMonthYearCard()
    {
        if (_monthYearPickerCard == null) return;

        _monthYearPickerCard.Opacity = 0;
        _monthYearPickerCard.Scale = 0.96;
        _monthYearPickerCard.IsVisible = true;
        await Task.WhenAll(
            _monthYearPickerCard.FadeTo(1, 150, Easing.CubicOut),
            _monthYearPickerCard.ScaleTo(1, 150, Easing.CubicOut));
    }

    private void HideMonthYearCard()
    {
        if (_monthYearPickerCard == null) return;

        _monthYearPickerCard.IsVisible = false;
    }

    private void OnTodayClicked(object? sender, EventArgs e)
    {
        var today = DateTime.Today;
        if (MinimumDate.HasValue && today < MinimumDate.Value.Date) return;
        if (MaximumDate.HasValue && today > MaximumDate.Value.Date) return;

        var previous = SelectedDate;
        var newDate = new DateTime(today.Year, today.Month, today.Day,
            SelectedDate.Hour, SelectedDate.Minute, SelectedDate.Second);
        SelectedDate = newDate;

        if (_showingMonthYearPicker)
        {
            _showingMonthYearPicker = false;
            if (_dayNamesRow != null) _dayNamesRow.IsVisible = true;
            SetNavButtonsReserved(true);
            HideMonthYearCard();
            UpdateMonthYearLabel();
        }

        DateSelected?.Invoke(this, new DateSelectedEventArgs(newDate, previous));
    }

    private void OnDayTapped(DateTime date)
    {
        if (MinimumDate.HasValue && date.Date < MinimumDate.Value.Date) return;
        if (MaximumDate.HasValue && date.Date > MaximumDate.Value.Date) return;

        // Navigate to the tapped month if it was an overflow day
        if (date.Month != _displayedMonth.Month || date.Year != _displayedMonth.Year)
        {
            _displayedMonth = new DateTime(date.Year, date.Month, 1);
            UpdateMonthYearLabel();
        }

        if (IsRangeSelectionEnabled)
        {
            HandleRangeTap(date);
        }
        else
        {
            var previous = SelectedDate;
            var newDate = new DateTime(date.Year, date.Month, date.Day,
                SelectedDate.Hour, SelectedDate.Minute, SelectedDate.Second);
            SelectedDate = newDate;
            DateSelected?.Invoke(this, new DateSelectedEventArgs(newDate, previous));
        }
    }

    private void HandleRangeTap(DateTime date)
    {
        if (!SelectedStartDate.HasValue || (SelectedStartDate.HasValue && SelectedEndDate.HasValue))
        {
            // Start new range
            SelectedStartDate = date.Date;
            SelectedEndDate = null;
        }
        else
        {
            // Complete range
            if (date.Date < SelectedStartDate.Value.Date)
            {
                SelectedEndDate = SelectedStartDate;
                SelectedStartDate = date.Date;
            }
            else
            {
                SelectedEndDate = date.Date;
            }
            DateRangeSelected?.Invoke(this, new DateRangeSelectedEventArgs(SelectedStartDate, SelectedEndDate));
        }
    }

    private void OnHourChanged(object? sender, Events.SpinnerSelectedEventArgs e)
    {
        if (_suppressTimeCallbacks) return;
        UpdateSelectedDateFromTime();
    }

    private void OnMinuteChanged(object? sender, Events.SpinnerSelectedEventArgs e)
    {
        if (_suppressTimeCallbacks) return;
        UpdateSelectedDateFromTime();
    }

    private void OnAmPmChanged(object? sender, Events.SpinnerSelectedEventArgs e)
    {
        if (_suppressTimeCallbacks) return;
        UpdateSelectedDateFromTime();
    }

    private void UpdateSelectedDateFromTime()
    {
        if (_hourPicker == null || _minutePicker == null || _amPmPicker == null) return;

        int hour;
        if (Use24HourFormat)
        {
            hour = _hourPicker.SelectedIndex;
        }
        else
        {
            var selectedHour = _hourPicker.SelectedIndex + 1; // 1-12
            var isPm = _amPmPicker.SelectedIndex == 1;
            hour = selectedHour == 12 ? (isPm ? 12 : 0) : (isPm ? selectedHour + 12 : selectedHour);
        }

        var minute = _minutePicker.SelectedIndex;
        var previous = SelectedDate;
        var newDate = new DateTime(SelectedDate.Year, SelectedDate.Month, SelectedDate.Day, hour, minute, 0);

        SelectedDate = newDate;
        TimeChanged?.Invoke(this, new DateSelectedEventArgs(newDate, previous));
    }

    #endregion

    #region Helpers

    private void UpdateMonthYearLabel()
    {
        if (_monthYearLabel == null) return;
        _monthYearLabel.Text = _showingMonthYearPicker
            ? $"{_displayedMonth:MMMM yyyy} ▲"
            : $"{_displayedMonth:MMMM yyyy} ▼";
    }

    #endregion

    #region Property Changed Callbacks

    private static void OnModeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (DatePickerView)bindable;
        control.ApplyModeVisibility();
    }

    private static void OnSelectedDateChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (DatePickerView)bindable;
        var date = (DateTime)newValue;
        var oldDate = (DateTime)oldValue;

        var sameMonth = control._displayedMonth.Year == date.Year && control._displayedMonth.Month == date.Month;

        // Navigate calendar to the new date's month
        control._displayedMonth = new DateTime(date.Year, date.Month, 1);
        control.UpdateMonthYearLabel();

        if (sameMonth)
        {
            // Only the previously and newly selected cells actually look different.
            control.RefreshDayCell(oldDate.Date);
            control.RefreshDayCell(date.Date);
        }
        else
        {
            control.PopulateCalendarGrid();
        }

        control.UpdateTimeSelection();
    }

    /// <summary>
    /// The appearance and selection-state properties can only change how the existing
    /// cells are painted — never the grid's shape or the dates it shows — so they
    /// repaint in place rather than tearing down and rebuilding all 42 cells.
    /// </summary>
    private static void OnCalendarRepaintRequired(BindableObject bindable, object oldValue, object newValue)
        => ((DatePickerView)bindable).RepaintCalendar();

    private static void OnTimeRebuildRequired(BindableObject bindable, object oldValue, object newValue)
        => ((DatePickerView)bindable).PopulateTimePickers();

    private static void OnShowTodayButtonChanged(BindableObject bindable, object oldValue, object newValue)
        => ((DatePickerView)bindable).LayoutCalendarHeader();

    private static void OnTimeStyleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (DatePickerView)bindable;
        if (control._hourPicker != null)
        {
            control._hourPicker.TextColor = control.SpinnerTextColor;
            control._hourPicker.SelectedTextColor = control.SpinnerSelectedTextColor;
            control._hourPicker.SelectorColor = control.SpinnerSelectorColor;
        }
        if (control._minutePicker != null)
        {
            control._minutePicker.TextColor = control.SpinnerTextColor;
            control._minutePicker.SelectedTextColor = control.SpinnerSelectedTextColor;
            control._minutePicker.SelectorColor = control.SpinnerSelectorColor;
        }
        if (control._amPmPicker != null)
        {
            control._amPmPicker.TextColor = control.SpinnerTextColor;
            control._amPmPicker.SelectedTextColor = control.SpinnerSelectedTextColor;
            control._amPmPicker.SelectorColor = control.SpinnerSelectorColor;
        }
        if (control._monthSpinner != null)
        {
            control._monthSpinner.TextColor = control.SpinnerTextColor;
            control._monthSpinner.SelectedTextColor = control.SpinnerSelectedTextColor;
            control._monthSpinner.SelectorColor = control.SpinnerSelectorColor;
        }
        if (control._yearSpinner != null)
        {
            control._yearSpinner.TextColor = control.SpinnerTextColor;
            control._yearSpinner.SelectedTextColor = control.SpinnerSelectedTextColor;
            control._yearSpinner.SelectorColor = control.SpinnerSelectorColor;
        }
    }

    #endregion
}
