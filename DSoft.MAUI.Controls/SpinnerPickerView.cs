using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using DSoft.Maui.Controls.Events;

namespace DSoft.Maui.Controls;

/// <summary>
/// A drum-roll / wheel-style picker. Scroll vertically to select a value.
/// The selected item is always centred; surrounding items fade and scale down.
/// Uses only MAUI primitives — no native elements.
/// </summary>
public class SpinnerPickerView : ContentView
{
    #region Fields

    private readonly VerticalStackLayout _itemsLayout;
    private readonly RowDefinition _centerRowDef;
    private readonly BoxView _topLine;
    private readonly BoxView _bottomLine;
    private readonly List<View> _itemViews = new();
    private int _originalCount;
    private int _loopCopies;
    private int _loopOffset;
    private double _startTranslation;
    private bool _suppressCallbacks;

    #endregion

    #region ItemsSource

    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IList), typeof(SpinnerPickerView),
        null, propertyChanged: OnItemsSourceChanged);

    public IList? ItemsSource
    {
        get => (IList?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    #endregion

    #region SelectedIndex

    public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(
        nameof(SelectedIndex), typeof(int), typeof(SpinnerPickerView),
        0, BindingMode.TwoWay, propertyChanged: OnSelectedIndexChanged);

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    #endregion

    #region SelectedItem

    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(
        nameof(SelectedItem), typeof(object), typeof(SpinnerPickerView),
        null, BindingMode.TwoWay, propertyChanged: OnSelectedItemChanged);

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    #endregion

    #region ItemHeight

    public static readonly BindableProperty ItemHeightProperty = BindableProperty.Create(
        nameof(ItemHeight), typeof(double), typeof(SpinnerPickerView),
        44.0, propertyChanged: OnLayoutChanged);

    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    #endregion

    #region VisibleItemCount

    public static readonly BindableProperty VisibleItemCountProperty = BindableProperty.Create(
        nameof(VisibleItemCount), typeof(int), typeof(SpinnerPickerView),
        5, propertyChanged: OnLayoutChanged);

    /// <summary>
    /// Number of rows to show. Use an odd number for a clear centre selection.
    /// </summary>
    public int VisibleItemCount
    {
        get => (int)GetValue(VisibleItemCountProperty);
        set => SetValue(VisibleItemCountProperty, value);
    }

    #endregion

    #region TextColor

    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
        nameof(TextColor), typeof(Color), typeof(SpinnerPickerView),
        Colors.Gray, propertyChanged: OnStyleChanged);

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    #endregion

    #region SelectedTextColor

    public static readonly BindableProperty SelectedTextColorProperty = BindableProperty.Create(
        nameof(SelectedTextColor), typeof(Color), typeof(SpinnerPickerView),
        Colors.Black, propertyChanged: OnStyleChanged);

    public Color SelectedTextColor
    {
        get => (Color)GetValue(SelectedTextColorProperty);
        set => SetValue(SelectedTextColorProperty, value);
    }

    #endregion

    #region FontSize

    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize), typeof(double), typeof(SpinnerPickerView),
        16.0, propertyChanged: OnStyleChanged);

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    #endregion

    #region SelectorColor

    public static readonly BindableProperty SelectorColorProperty = BindableProperty.Create(
        nameof(SelectorColor), typeof(Color), typeof(SpinnerPickerView),
        Colors.LightGray, propertyChanged: OnSelectorColorChanged);

    public Color SelectorColor
    {
        get => (Color)GetValue(SelectorColorProperty);
        set => SetValue(SelectorColorProperty, value);
    }

    #endregion

    #region IsLooping

    public static readonly BindableProperty IsLoopingProperty = BindableProperty.Create(
        nameof(IsLooping), typeof(bool), typeof(SpinnerPickerView),
        false, propertyChanged: OnBuildRequired);

    /// <summary>
    /// When true the list wraps — the user can scroll past the last item and arrive
    /// at the first, and vice versa.
    /// </summary>
    public bool IsLooping
    {
        get => (bool)GetValue(IsLoopingProperty);
        set => SetValue(IsLoopingProperty, value);
    }

    #endregion

    #region DisplayMemberPath

    public static readonly BindableProperty DisplayMemberPathProperty = BindableProperty.Create(
        nameof(DisplayMemberPath), typeof(string), typeof(SpinnerPickerView),
        null, propertyChanged: OnBuildRequired);

    /// <summary>
    /// Name of the property on each item to use as the display text.
    /// When null, <c>ToString()</c> is called on each item.
    /// Has no effect when <see cref="ItemTemplate"/> is set.
    /// </summary>
    public string? DisplayMemberPath
    {
        get => (string?)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    #endregion

    #region ItemTemplate

    public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(
        nameof(ItemTemplate), typeof(DataTemplate), typeof(SpinnerPickerView),
        null, propertyChanged: OnBuildRequired);

    /// <summary>
    /// Optional <see cref="DataTemplate"/> used to create each row.
    /// The <c>BindingContext</c> of the root view is set to the item.
    /// Opacity and Scale transforms are still applied by the picker;
    /// text colour / font styling is left to the template.
    /// </summary>
    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    #endregion

    #region Events

    public event EventHandler<SpinnerSelectedEventArgs>? SelectionChanged;

    #endregion

    #region Constructor

    public SpinnerPickerView()
    {
        _itemsLayout = new VerticalStackLayout
        {
            Spacing = 0,
            VerticalOptions = LayoutOptions.Start,
        };

        _centerRowDef = new RowDefinition { Height = new GridLength(ItemHeight, GridUnitType.Absolute) };

        _topLine = new BoxView
        {
            Color = SelectorColor,
            HeightRequest = 1,
            VerticalOptions = LayoutOptions.End,
            InputTransparent = true,
        };

        _bottomLine = new BoxView
        {
            Color = SelectorColor,
            HeightRequest = 1,
            VerticalOptions = LayoutOptions.Start,
            InputTransparent = true,
        };

        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Star },
                _centerRowDef,
                new RowDefinition { Height = GridLength.Star },
            }
        };

        Grid.SetRow(_itemsLayout, 0);
        Grid.SetRowSpan(_itemsLayout, 3);
        grid.Add(_itemsLayout);

        Grid.SetRow(_topLine, 0);
        grid.Add(_topLine);

        Grid.SetRow(_bottomLine, 2);
        grid.Add(_bottomLine);

        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnPanUpdated;
        grid.GestureRecognizers.Add(pan);

        grid.IsClippedToBounds = true;
        Content = grid;
        HeightRequest = ItemHeight * VisibleItemCount;
    }

    #endregion

    #region Methods

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _startTranslation = _itemsLayout.TranslationY;
                this.AbortAnimation("Snap");
                break;

            case GestureStatus.Running:
                _itemsLayout.TranslationY = ClampTranslation(_startTranslation + e.TotalY);
                RefreshItemTransforms();
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                SnapToNearestItem();
                break;
        }
    }

    private double ClampTranslation(double y)
    {
        if (_itemViews.Count == 0) return y;
        // Looping: no hard clamp — the repeated copies act as the buffer.
        if (IsLooping) return y;
        return Math.Clamp(y, CenterOffsetForIndex(_itemViews.Count - 1), CenterOffsetForIndex(0));
    }

    // TranslationY that places item[index] in the vertical centre of the control.
    // Derivation: item centre = (index + 0.5) * ItemHeight + TranslationY == ControlHeight / 2
    private double CenterOffsetForIndex(int index)
        => ItemHeight * ((VisibleItemCount - 1) / 2.0 - index);

    private async void SnapToNearestItem()
    {
        if (_itemViews.Count == 0) return;

        var rawIndex = (VisibleItemCount - 1) / 2.0 - _itemsLayout.TranslationY / ItemHeight;
        var internalIndex = Math.Clamp((int)Math.Round(rawIndex), 0, _itemViews.Count - 1);

        // Resolve the actual source index, accounting for looping copies.
        var actualIndex = IsLooping && _originalCount > 0
            ? ((internalIndex % _originalCount) + _originalCount) % _originalCount
            : internalIndex;

        await AnimateToIndex(internalIndex);

        // After the snap animation, silently reposition to the middle copy so the
        // user always has room to scroll in either direction.
        if (IsLooping && _originalCount > 0)
        {
            _itemsLayout.TranslationY = CenterOffsetForIndex(_loopOffset + actualIndex);
            RefreshItemTransforms();
        }

        if (actualIndex == SelectedIndex && Equals(ItemsSource?[actualIndex], SelectedItem))
            return;

        var previousIndex = SelectedIndex;
        var previousItem = SelectedItem;

        _suppressCallbacks = true;
        SelectedIndex = actualIndex;
        SelectedItem = ItemsSource?[actualIndex];
        _suppressCallbacks = false;

        SelectionChanged?.Invoke(this, new SpinnerSelectedEventArgs(actualIndex, SelectedItem, previousIndex, previousItem));
    }

    private Task AnimateToIndex(int internalIndex)
    {
        var startY = _itemsLayout.TranslationY;
        var targetY = CenterOffsetForIndex(internalIndex);

        this.AbortAnimation("Snap");

        var tcs = new TaskCompletionSource<bool>();

        new Animation(v =>
        {
            _itemsLayout.TranslationY = v;
            RefreshItemTransforms();
        }, startY, targetY, Easing.SpringOut)
        .Commit(this, "Snap", 16, 300, finished: (_, cancelled) =>
        {
            if (!cancelled)
            {
                _itemsLayout.TranslationY = targetY;
                RefreshItemTransforms();
            }
            tcs.TrySetResult(true);
        });

        return tcs.Task;
    }

    private void RefreshItemTransforms()
    {
        if (_itemViews.Count == 0) return;

        var controlCenterY = ItemHeight * VisibleItemCount / 2.0;

        for (var i = 0; i < _itemViews.Count; i++)
        {
            var view = _itemViews[i];
            var itemCenterY = (i + 0.5) * ItemHeight + _itemsLayout.TranslationY;
            var distance = Math.Abs(itemCenterY - controlCenterY) / ItemHeight;

            view.Opacity = Math.Max(0.2, 1.0 - distance * 0.5);
            view.Scale = Math.Max(0.7, 1.0 - distance * 0.15);

            // Only apply text styling to default Label rows; custom templates own their own styling.
            if (view is Label label)
            {
                if (distance < 0.5)
                {
                    label.TextColor = SelectedTextColor;
                    label.FontAttributes = FontAttributes.Bold;
                }
                else
                {
                    label.TextColor = TextColor;
                    label.FontAttributes = FontAttributes.None;
                }
            }
        }
    }

    private string GetDisplayText(object? item)
    {
        if (item == null) return string.Empty;
        if (DisplayMemberPath == null) return item.ToString() ?? string.Empty;

        var prop = item.GetType().GetProperty(DisplayMemberPath, BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(item)?.ToString() ?? item.ToString() ?? string.Empty;
    }

    private View CreateItemView(object? item)
    {
        if (ItemTemplate != null)
        {
            var view = (View)ItemTemplate.CreateContent();
            view.BindingContext = item;
            view.HeightRequest = ItemHeight;
            return view;
        }

        return new Label
        {
            Text = GetDisplayText(item),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            HeightRequest = ItemHeight,
            FontSize = FontSize,
            TextColor = TextColor,
        };
    }

    private void BuildItems()
    {
        _itemViews.Clear();
        _itemsLayout.Children.Clear();

        if (ItemsSource == null) return;

        _originalCount = ItemsSource.Count;
        if (_originalCount == 0) return;

        // Number of copies: enough to have ~50 source items on each side when looping.
        // Always odd so the middle copy index is exact. Minimum 3.
        _loopCopies = IsLooping
            ? Math.Max(3, (100 / _originalCount + 1) | 1)
            : 1;
        _loopOffset = _originalCount * (_loopCopies / 2);

        for (var copy = 0; copy < _loopCopies; copy++)
        {
            foreach (var item in ItemsSource)
            {
                var view = CreateItemView(item);
                _itemViews.Add(view);
                _itemsLayout.Children.Add(view);
            }
        }

        var safeIndex = Math.Clamp(SelectedIndex, 0, _originalCount - 1);
        var startInternalIndex = _loopOffset + safeIndex;
        _itemsLayout.TranslationY = CenterOffsetForIndex(startInternalIndex);
        RefreshItemTransforms();
    }

    private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (SpinnerPickerView)bindable;

        if (oldValue is INotifyCollectionChanged oldCollection)
            oldCollection.CollectionChanged -= control.OnCollectionChanged;

        if (newValue is INotifyCollectionChanged newCollection)
            newCollection.CollectionChanged += control.OnCollectionChanged;

        control.BuildItems();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => MainThread.BeginInvokeOnMainThread(BuildItems);

    private static void OnSelectedIndexChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (SpinnerPickerView)bindable;
        if (control._suppressCallbacks || control._itemViews.Count == 0) return;

        var index = Math.Clamp((int)newValue, 0, control._originalCount - 1);

        control._suppressCallbacks = true;
        control.SelectedItem = control.ItemsSource?[index];
        control._suppressCallbacks = false;

        // When looping, normalise to middle copy before animating so there's always room to scroll.
        if (control.IsLooping && control._originalCount > 0)
        {
            var currentRaw = (control.VisibleItemCount - 1) / 2.0 - control._itemsLayout.TranslationY / control.ItemHeight;
            var currentActual = ((int)Math.Round(currentRaw) % control._originalCount + control._originalCount) % control._originalCount;
            control._itemsLayout.TranslationY = control.CenterOffsetForIndex(control._loopOffset + currentActual);
            _ = control.AnimateToIndex(control._loopOffset + index);
        }
        else
        {
            _ = control.AnimateToIndex(index);
        }
    }

    private static void OnSelectedItemChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (SpinnerPickerView)bindable;
        if (control._suppressCallbacks || control.ItemsSource == null) return;

        for (var i = 0; i < control.ItemsSource.Count; i++)
        {
            if (!Equals(control.ItemsSource[i], newValue)) continue;

            control._suppressCallbacks = true;
            control.SelectedIndex = i;
            control._suppressCallbacks = false;

            var internalIndex = control.IsLooping ? control._loopOffset + i : i;
            _ = control.AnimateToIndex(internalIndex);
            return;
        }
    }

    private static void OnLayoutChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (SpinnerPickerView)bindable;
        control._centerRowDef.Height = new GridLength(control.ItemHeight, GridUnitType.Absolute);
        control.HeightRequest = control.ItemHeight * control.VisibleItemCount;
        control.BuildItems();
    }

    private static void OnStyleChanged(BindableObject bindable, object oldValue, object newValue)
        => ((SpinnerPickerView)bindable).RefreshItemTransforms();

    private static void OnSelectorColorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (SpinnerPickerView)bindable;
        var color = (Color)newValue;
        control._topLine.Color = color;
        control._bottomLine.Color = color;
    }

    private static void OnBuildRequired(BindableObject bindable, object oldValue, object newValue)
        => ((SpinnerPickerView)bindable).BuildItems();

    #endregion
}
