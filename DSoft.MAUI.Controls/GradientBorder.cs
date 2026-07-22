namespace DSoft.Maui.Controls;

/// <summary>
/// A <see cref="Border"/> with a linear gradient background running from
/// <see cref="FromColor"/> to <see cref="ToColor"/>.
/// </summary>
public class GradientBorder : Border
{
    #region Bindable Properties

    public static readonly BindableProperty FromColorProperty = BindableProperty.Create(
        nameof(FromColor), typeof(Color), typeof(GradientBorder),
        Colors.Transparent, propertyChanged: OnGradientChanged);

    public Color FromColor
    {
        get => (Color)GetValue(FromColorProperty);
        set => SetValue(FromColorProperty, value);
    }

    public static readonly BindableProperty ToColorProperty = BindableProperty.Create(
        nameof(ToColor), typeof(Color), typeof(GradientBorder),
        Colors.Transparent, propertyChanged: OnGradientChanged);

    public Color ToColor
    {
        get => (Color)GetValue(ToColorProperty);
        set => SetValue(ToColorProperty, value);
    }

    #endregion

    #region Constructor

    public GradientBorder()
    {
        UpdateGradient();
    }

    #endregion

    #region Methods

    private static void OnGradientChanged(BindableObject bindable, object oldValue, object newValue)
        => ((GradientBorder)bindable).UpdateGradient();

    private void UpdateGradient()
    {
        Background = new LinearGradientBrush
        {
            StartPoint = new Point(1, 0),
            EndPoint = new Point(0, 1),
            GradientStops =
            {
                new GradientStop { Color = FromColor, Offset = 0 },
                new GradientStop { Color = ToColor, Offset = 1 },
            }
        };
    }

    #endregion
}
