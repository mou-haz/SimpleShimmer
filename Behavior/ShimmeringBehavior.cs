using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Xaml.Interactivity;
using Windows.UI;

namespace SimpleShimmer;

public sealed class ShimmeringBehavior : Behavior<FrameworkElement>
{
    private ShimmeringHelper _shimmeringHelper;

    protected override void OnAttached()
    {
        base.OnAttached();
        _shimmeringHelper = new(AssociatedObject)
        {
            IsActive = IsActive,
            Color = Color,
            Duration = Duration
        };
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        _shimmeringHelper = null;
    }

    #region IsActive
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
           nameof(IsActive),
           typeof(bool),
           typeof(ShimmeringBehavior),
           new PropertyMetadata(false, OnIsActiveChanged));

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ShimmeringBehavior behavior || behavior._shimmeringHelper is null || e.NewValue is not bool _isActive)
        {
            return;
        }

        behavior._shimmeringHelper.IsActive = _isActive;
    }
    #endregion

    #region Color
    public Color? Color
    {
        get => (Color?)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
           nameof(Color),
          typeof(Color?),
          typeof(ShimmeringBehavior),
          new PropertyMetadata(null, OnColorChanged));

    private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ShimmeringBehavior behavior || behavior._shimmeringHelper is null)
        {
            return;
        }

        behavior._shimmeringHelper.Color = e.NewValue as Color?;
    }
    #endregion

    #region Brush
    public Brush? Brush
    {
        get => (Brush?)GetValue(BrushProperty);
        set => SetValue(BrushProperty, value);
    }

    public static readonly DependencyProperty BrushProperty = DependencyProperty.Register(
           nameof(Brush),
          typeof(Brush),
          typeof(ShimmeringBehavior),
          new PropertyMetadata(null, OnBrushChanged));

    private static void OnBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ShimmeringBehavior behavior || behavior._shimmeringHelper is null)
        {
            return;
        }

        behavior._shimmeringHelper.Brush = e.NewValue as Brush;
    }
    #endregion

    #region Duration
    public TimeSpan Duration
    {
        get => (TimeSpan)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }
    public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
           nameof(Duration),
          typeof(TimeSpan),
          typeof(ShimmeringBehavior),
          new PropertyMetadata(TimeSpan.FromSeconds(1), OnDurationChanged));

    private static void OnDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ShimmeringBehavior behavior || behavior._shimmeringHelper is null || e.NewValue is not TimeSpan newDuration)
        {
            return;
        }

        behavior._shimmeringHelper.Duration = newDuration;
    }
    #endregion
}