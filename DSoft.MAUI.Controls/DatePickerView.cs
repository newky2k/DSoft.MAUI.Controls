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
    private Label? _monthYearLabel;
    private Grid? _yearPickerGrid;
    private Grid? _calendarSection;
    private Grid? _timeSection;
    private SpinnerPickerView? _hourPicker;
    private SpinnerPickerView? _minutePicker;
    private SpinnerPickerView? _amPmPicker;
    private DateTime _displayedMonth;
    private bool _showingYearPicker;
    private int _yearPageStart;
    private bool _suppressTimeCallbacks;

    private static readonly Color DefaultTodayColor = Color.FromArgb("#007AFF");
    private static readonly Color DefaultSelectedColor = Color.FromArgb("#007AFF");
    private static readonly Color DefaultRangeColor = Color.FromArgb("#CCE4FF");
    private static readonly Color DefaultHeaderColor = Colors.Transparent;
    private static readonly Color DefaultDayNameColor = Colors.Gray;
    private static readonly Color DefaultDayColor = Colors.Black;
    private static readonly Color DefaultDisabledColor = Colors.LightGray;
    private static readonly Color DefaultOtherMonthColor = Colors.LightGray;

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
        null, propertyChanged: OnCalendarRebuildRequired);

    public DateTime? MinimumDate
    {
        get => (DateTime?)GetValue(MinimumDateProperty);
        set => SetValue(MinimumDateProperty, value);
    }

    public static readonly BindableProperty MaximumDateProperty = BindableProperty.Create(
        nameof(MaximumDate), typeof(DateTime?), typeof(DatePickerView),
        null, propertyChanged: OnCalendarRebuildRequired);

    public DateTime? MaximumDate
    {
        get => (DateTime?)GetValue(MaximumDateProperty);
        set => SetValue(MaximumDateProperty, value);
    }

    public static readonly BindableProperty IsRangeSelectionEnabledProperty = BindableProperty.Create(
        nameof(IsRangeSelectionEnabled), typeof(bool), typeof(DatePickerView),
        false, propertyChanged: OnCalendarRebuildRequired);

    public bool IsRangeSelectionEnabled
    {
        get => (bool)GetValue(IsRangeSelectionEnabledProperty);
        set => SetValue(IsRangeSelectionEnabledProperty, value);
    }

    public static readonly BindableProperty SelectedStartDateProperty = BindableProperty.Create(
        nameof(SelectedStartDate), typeof(DateTime?), typeof(DatePickerView),
        null, BindingMode.TwoWay, propertyChanged: OnCalendarRebuildRequired);

    public DateTime? SelectedStartDate
    {
        get => (DateTime?)GetValue(SelectedStartDateProperty);
        set => SetValue(SelectedStartDateProperty, value);
    }

    public static readonly BindableProperty SelectedEndDateProperty = BindableProperty.Create(
        nameof(SelectedEndDate), typeof(DateTime?), typeof(DatePickerView),
        null, BindingMode.TwoWay, propertyChanged: OnCalendarRebuildRequired);

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

    // --- Appearance ---

    public static readonly BindableProperty TodayHighlightColorProperty = BindableProperty.Create(
        nameof(TodayHighlightColor), typeof(Color), typeof(DatePickerView),
        DefaultTodayColor, propertyChanged: OnCalendarRebuildRequired);

    public Color TodayHighlightColor
    {
        get => (Color)GetValue(TodayHighlightColorProperty);
        set => SetValue(TodayHighlightColorProperty, value);
    }

    public static readonly BindableProperty SelectionColorProperty = BindableProperty.Create(
        nameof(SelectionColor), typeof(Color), typeof(DatePickerView),
        DefaultSelectedColor, propertyChanged: OnCalendarRebuildRequired);

    public Color SelectionColor
    {
        get => (Color)GetValue(SelectionColorProperty);
        set => SetValue(SelectionColorProperty, value);
    }

    public static readonly BindableProperty RangeHighlightColorProperty = BindableProperty.Create(
        nameof(RangeHighlightColor), typeof(Color), typeof(DatePickerView),
        DefaultRangeColor, propertyChanged: OnCalendarRebuildRequired);

    public Color RangeHighlightColor
    {
        get => (Color)GetValue(RangeHighlightColorProperty);
        set => SetValue(RangeHighlightColorProperty, value);
    }

    public static readonly BindableProperty HeaderBackgroundColorProperty = BindableProperty.Create(
        nameof(HeaderBackgroundColor), typeof(Color), typeof(DatePickerView),
        DefaultHeaderColor, propertyChanged: OnCalendarRebuildRequired);

    public Color HeaderBackgroundColor
    {
        get => (Color)GetValue(HeaderBackgroundColorProperty);
        set => SetValue(HeaderBackgroundColorProperty, value);
    }

    public static readonly BindableProperty DayNameColorProperty = BindableProperty.Create(
        nameof(DayNameColor), typeof(Color), typeof(DatePickerView),
        DefaultDayNameColor, propertyChanged: OnCalendarRebuildRequired);

    public Color DayNameColor
    {
        get => (Color)GetValue(DayNameColorProperty);
        set => SetValue(DayNameColorProperty, value);
    }

    public static readonly BindableProperty DayColorProperty = BindableProperty.Create(
        nameof(DayColor), typeof(Color), typeof(DatePickerView),
        DefaultDayColor, propertyChanged: OnCalendarRebuildRequired);

    public Color DayColor
    {
        get => (Color)GetValue(DayColorProperty);
        set => SetValue(DayColorProperty, value);
    }

    public static readonly BindableProperty DisabledDayColorProperty = BindableProperty.Create(
        nameof(DisabledDayColor), typeof(Color), typeof(DatePickerView),
        DefaultDisabledColor, propertyChanged: OnCalendarRebuildRequired);

    public Color DisabledDayColor
    {
        get => (Color)GetValue(DisabledDayColorProperty);
        set => SetValue(DisabledDayColorProperty, value);
    }

    public static readonly BindableProperty OtherMonthDayColorProperty = BindableProperty.Create(
        nameof(OtherMonthDayColor), typeof(Color), typeof(DatePickerView),
        DefaultOtherMonthColor, propertyChanged: OnCalendarRebuildRequired);

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
                new RowDefinition { Height = GridLength.Auto }, // header
                new RowDefinition { Height = GridLength.Auto }, // day names
                new RowDefinition { Height = GridLength.Auto }, // calendar or year grid
            }
        };

        // Header row
        var header = BuildCalendarHeader();
        Grid.SetRow(header, 0);
        section.Add(header);

        // Day names row
        var dayNames = BuildDayNamesRow();
        Grid.SetRow(dayNames, 1);
        section.Add(dayNames);

        // Calendar grid placeholder
        _calendarGrid = new Grid();
        _yearPickerGrid = new Grid();
        _yearPickerGrid.IsVisible = false;

        var calendarContainer = new Grid();
        calendarContainer.Add(_calendarGrid);
        calendarContainer.Add(_yearPickerGrid);
        Grid.SetRow(calendarContainer, 2);
        section.Add(calendarContainer);

        PopulateCalendarGrid();
        return section;
    }

    private Grid BuildCalendarHeader()
    {
        var header = new Grid
        {
            BackgroundColor = HeaderBackgroundColor,
            Padding = new Thickness(8, 8, 8, 4),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto }, // prev
                new ColumnDefinition { Width = GridLength.Star }, // month/year label
                new ColumnDefinition { Width = GridLength.Auto }, // next
            }
        };

        var prevButton = new Button
        {
            Text = "‹",
            FontSize = 22,
            BackgroundColor = Colors.Transparent,
            TextColor = SelectionColor,
            WidthRequest = 44,
            HeightRequest = 44,
            Padding = 0,
        };
        prevButton.Clicked += OnPrevMonthClicked;

        var nextButton = new Button
        {
            Text = "›",
            FontSize = 22,
            BackgroundColor = Colors.Transparent,
            TextColor = SelectionColor,
            WidthRequest = 44,
            HeightRequest = 44,
            Padding = 0,
        };
        nextButton.Clicked += OnNextMonthClicked;

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

        Grid.SetColumn(prevButton, 0);
        Grid.SetColumn(_monthYearLabel, 1);
        Grid.SetColumn(nextButton, 2);

        header.Add(prevButton);
        header.Add(_monthYearLabel);
        header.Add(nextButton);

        return header;
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
        }

        return grid;
    }

    private void PopulateCalendarGrid()
    {
        if (_calendarGrid == null) return;

        _calendarGrid.Children.Clear();
        _calendarGrid.RowDefinitions.Clear();
        _calendarGrid.ColumnDefinitions.Clear();

        for (var i = 0; i < 7; i++)
            _calendarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

        var firstDay = _displayedMonth;
        var daysInMonth = DateTime.DaysInMonth(firstDay.Year, firstDay.Month);
        var startDayOfWeek = (int)firstDay.DayOfWeek; // 0=Sun
        var totalCells = startDayOfWeek + daysInMonth;
        var rowCount = (int)Math.Ceiling(totalCells / 7.0);

        for (var r = 0; r < rowCount; r++)
            _calendarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) });

        _calendarGrid.Padding = new Thickness(4, 0, 4, 4);

        for (var cell = 0; cell < rowCount * 7; cell++)
        {
            var row = cell / 7;
            var col = cell % 7;
            var dayNumber = cell - startDayOfWeek + 1;

            var date = dayNumber >= 1 && dayNumber <= daysInMonth
                ? new DateTime(firstDay.Year, firstDay.Month, dayNumber)
                : (DateTime?)null;

            // Show overflow days from adjacent months
            if (date == null)
            {
                if (cell < startDayOfWeek)
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
            }

            var dayView = CreateDayCell(date.Value, dayNumber >= 1 && dayNumber <= daysInMonth);
            Grid.SetRow(dayView, row);
            Grid.SetColumn(dayView, col);
            _calendarGrid.Add(dayView);
        }
    }

    private View CreateDayCell(DateTime date, bool isCurrentMonth)
    {
        var today = DateTime.Today;
        var isToday = date.Date == today;
        var isSelected = !IsRangeSelectionEnabled && date.Date == SelectedDate.Date;
        var isDisabled = (MinimumDate.HasValue && date.Date < MinimumDate.Value.Date)
                      || (MaximumDate.HasValue && date.Date > MaximumDate.Value.Date);

        var isRangeStart = IsRangeSelectionEnabled && SelectedStartDate.HasValue && date.Date == SelectedStartDate.Value.Date;
        var isRangeEnd = IsRangeSelectionEnabled && SelectedEndDate.HasValue && date.Date == SelectedEndDate.Value.Date;
        var isInRange = IsRangeSelectionEnabled
            && SelectedStartDate.HasValue && SelectedEndDate.HasValue
            && date.Date > SelectedStartDate.Value.Date
            && date.Date < SelectedEndDate.Value.Date;

        Color bgColor;
        Color textColor;

        if (isRangeStart || isRangeEnd)
        {
            bgColor = SelectionColor;
            textColor = Colors.White;
        }
        else if (isSelected)
        {
            bgColor = SelectionColor;
            textColor = Colors.White;
        }
        else if (isInRange)
        {
            bgColor = RangeHighlightColor;
            textColor = DayColor;
        }
        else if (isToday)
        {
            bgColor = Colors.Transparent;
            textColor = TodayHighlightColor;
        }
        else if (!isCurrentMonth)
        {
            bgColor = Colors.Transparent;
            textColor = OtherMonthDayColor;
        }
        else if (isDisabled)
        {
            bgColor = Colors.Transparent;
            textColor = DisabledDayColor;
        }
        else
        {
            bgColor = Colors.Transparent;
            textColor = DayColor;
        }

        var label = new Label
        {
            Text = date.Day.ToString(),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            TextColor = textColor,
            FontSize = 15,
            FontAttributes = isToday && !isSelected && !isRangeStart && !isRangeEnd ? FontAttributes.Bold : FontAttributes.None,
        };

        View cellContent;

        if (isSelected || isRangeStart || isRangeEnd)
        {
            cellContent = new Border
            {
                BackgroundColor = bgColor,
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.Ellipse(),
                WidthRequest = 36,
                HeightRequest = 36,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Content = label,
            };
        }
        else if (isToday)
        {
            // Ring around today
            cellContent = new Border
            {
                BackgroundColor = Colors.Transparent,
                Stroke = TodayHighlightColor,
                StrokeThickness = 2,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.Ellipse(),
                WidthRequest = 36,
                HeightRequest = 36,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Content = label,
            };
        }
        else
        {
            var innerGrid = new Grid
            {
                BackgroundColor = isInRange ? RangeHighlightColor : Colors.Transparent,
            };
            innerGrid.Add(label);
            cellContent = innerGrid;
        }

        var container = new Grid
        {
            HeightRequest = 44,
            BackgroundColor = isInRange ? RangeHighlightColor : Colors.Transparent,
        };
        container.Add(cellContent);

        if (!isDisabled)
        {
            var tap = new TapGestureRecognizer();
            var capturedDate = date;
            tap.Tapped += (s, e) => OnDayTapped(capturedDate);
            container.GestureRecognizers.Add(tap);
        }

        return container;
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

    private void PopulateYearPicker()
    {
        if (_yearPickerGrid == null) return;

        _yearPickerGrid.Children.Clear();
        _yearPickerGrid.RowDefinitions.Clear();
        _yearPickerGrid.ColumnDefinitions.Clear();

        const int cols = 4;
        const int yearsPerPage = 20;
        _yearPageStart = _displayedMonth.Year - (_displayedMonth.Year % yearsPerPage);

        for (var c = 0; c < cols; c++)
            _yearPickerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

        var rows = (int)Math.Ceiling(yearsPerPage / (double)cols);
        for (var r = 0; r < rows + 1; r++) // +1 for nav row
            _yearPickerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) });

        // Navigation row
        var navGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
            }
        };

        var prevYears = new Button
        {
            Text = "‹",
            FontSize = 22,
            BackgroundColor = Colors.Transparent,
            TextColor = SelectionColor,
            WidthRequest = 44,
            HeightRequest = 44,
            Padding = 0,
        };
        prevYears.Clicked += (s, e) =>
        {
            _yearPageStart -= yearsPerPage;
            PopulateYearPicker();
        };

        var nextYears = new Button
        {
            Text = "›",
            FontSize = 22,
            BackgroundColor = Colors.Transparent,
            TextColor = SelectionColor,
            WidthRequest = 44,
            HeightRequest = 44,
            Padding = 0,
        };
        nextYears.Clicked += (s, e) =>
        {
            _yearPageStart += yearsPerPage;
            PopulateYearPicker();
        };

        var rangeLabel = new Label
        {
            Text = $"{_yearPageStart} – {_yearPageStart + yearsPerPage - 1}",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            TextColor = DayColor,
        };

        Grid.SetColumn(prevYears, 0);
        Grid.SetColumn(rangeLabel, 1);
        Grid.SetColumn(nextYears, 2);
        navGrid.Add(prevYears);
        navGrid.Add(rangeLabel);
        navGrid.Add(nextYears);

        Grid.SetColumnSpan(navGrid, cols);
        Grid.SetRow(navGrid, 0);
        _yearPickerGrid.Add(navGrid);

        for (var i = 0; i < yearsPerPage; i++)
        {
            var year = _yearPageStart + i;
            var row = i / cols + 1;
            var col = i % cols;

            var isCurrentYear = year == _displayedMonth.Year;
            var isDisabled = (MinimumDate.HasValue && year < MinimumDate.Value.Year)
                          || (MaximumDate.HasValue && year > MaximumDate.Value.Year);

            var yearBtn = new Border
            {
                BackgroundColor = isCurrentYear ? SelectionColor : Colors.Transparent,
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                Margin = new Thickness(2),
                Content = new Label
                {
                    Text = year.ToString(),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    TextColor = isCurrentYear ? Colors.White : (isDisabled ? DisabledDayColor : DayColor),
                    FontSize = 15,
                    FontAttributes = isCurrentYear ? FontAttributes.Bold : FontAttributes.None,
                }
            };

            if (!isDisabled)
            {
                var capturedYear = year;
                var tap = new TapGestureRecognizer();
                tap.Tapped += (s, e) => OnYearSelected(capturedYear);
                yearBtn.GestureRecognizers.Add(tap);
            }

            Grid.SetRow(yearBtn, row);
            Grid.SetColumn(yearBtn, col);
            _yearPickerGrid.Add(yearBtn);
        }
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
        if (_showingYearPicker)
        {
            _yearPageStart -= 20;
            PopulateYearPicker();
            return;
        }

        var newMonth = _displayedMonth.AddMonths(-1);
        if (MinimumDate.HasValue && newMonth < new DateTime(MinimumDate.Value.Year, MinimumDate.Value.Month, 1))
            return;
        _displayedMonth = newMonth;
        UpdateMonthYearLabel();
        PopulateCalendarGrid();
    }

    private void OnNextMonthClicked(object? sender, EventArgs e)
    {
        if (_showingYearPicker)
        {
            _yearPageStart += 20;
            PopulateYearPicker();
            return;
        }

        var newMonth = _displayedMonth.AddMonths(1);
        if (MaximumDate.HasValue && newMonth > new DateTime(MaximumDate.Value.Year, MaximumDate.Value.Month, 1))
            return;
        _displayedMonth = newMonth;
        UpdateMonthYearLabel();
        PopulateCalendarGrid();
    }

    private void OnMonthYearLabelTapped(object? sender, TappedEventArgs e)
    {
        _showingYearPicker = !_showingYearPicker;

        if (_calendarGrid != null) _calendarGrid.IsVisible = !_showingYearPicker;
        if (_yearPickerGrid != null) _yearPickerGrid.IsVisible = _showingYearPicker;

        if (_showingYearPicker)
        {
            _yearPageStart = _displayedMonth.Year - (_displayedMonth.Year % 20);
            PopulateYearPicker();
        }

        UpdateMonthYearLabel();
    }

    private void OnYearSelected(int year)
    {
        _displayedMonth = new DateTime(year, _displayedMonth.Month, 1);
        _showingYearPicker = false;

        if (_calendarGrid != null) _calendarGrid.IsVisible = true;
        if (_yearPickerGrid != null) _yearPickerGrid.IsVisible = false;

        UpdateMonthYearLabel();
        PopulateCalendarGrid();
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
            PopulateCalendarGrid();
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

        PopulateCalendarGrid();
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
        _monthYearLabel.Text = _showingYearPicker
            ? $"{_yearPageStart} – {_yearPageStart + 19} ▲"
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

        // Navigate calendar to the new date's month
        control._displayedMonth = new DateTime(date.Year, date.Month, 1);
        control.UpdateMonthYearLabel();
        control.PopulateCalendarGrid();
        control.PopulateTimePickers();
    }

    private static void OnCalendarRebuildRequired(BindableObject bindable, object oldValue, object newValue)
        => ((DatePickerView)bindable).PopulateCalendarGrid();

    private static void OnTimeRebuildRequired(BindableObject bindable, object oldValue, object newValue)
        => ((DatePickerView)bindable).PopulateTimePickers();

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
    }

    #endregion
}
