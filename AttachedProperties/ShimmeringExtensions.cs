using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Windows.UI;
using System;

namespace SimpleShimmer;

public static class ShimmerExtensions
{
    #region IsActive
    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.RegisterAttached(
            "IsActive",
            typeof(bool),
            typeof(ShimmerExtensions),
            new PropertyMetadata(false, OnIsActiveChanged));

    public static bool GetIsActive(FrameworkElement element) => (bool)element.GetValue(IsActiveProperty);

    public static void SetIsActive(FrameworkElement element, bool value) => element.SetValue(IsActiveProperty, value);

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element || e.NewValue is not bool _isActive)
        {
            return;
        }

        var helper = GetShimmeringHelper(element);
        helper.IsActive = _isActive;
    }
    #endregion

    #region Color
    public static readonly DependencyProperty ColorProperty = DependencyProperty.RegisterAttached(
            "Color",
            typeof(Color?),
            typeof(ShimmerExtensions),
            new PropertyMetadata(null, OnColorChanged));

    public static Color? GetColor(FrameworkElement element) => (Color?)element.GetValue(ColorProperty);

    public static void SetColor(FrameworkElement element, Color? value) => element.SetValue(ColorProperty, value);

    private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        var helper = GetShimmeringHelper(element);
        helper.Color = e.NewValue as Color?;
    }
    #endregion

    #region Brush
    public static readonly DependencyProperty BrushProperty = DependencyProperty.RegisterAttached(
            "Brush",
            typeof(Brush),
            typeof(ShimmerExtensions),
            new PropertyMetadata(null, OnBrushChanged));

    public static Brush? GetBrush(FrameworkElement element) => (Brush?)element.GetValue(BrushProperty);

    public static void SetBrush(FrameworkElement element, Brush? value) => element.SetValue(BrushProperty, value);

    private static void OnBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        var helper = GetShimmeringHelper(element);
        helper.Brush = e.NewValue as Brush;
    }
    #endregion

    #region Duration
    public static readonly DependencyProperty DurationProperty = DependencyProperty.RegisterAttached(
            "Duration",
            typeof(TimeSpan?),
            typeof(ShimmerExtensions),
            new PropertyMetadata(TimeSpan.FromSeconds(1), OnDurationChanged));

    public static TimeSpan? GetDuration(FrameworkElement element) => (TimeSpan?)element.GetValue(DurationProperty);

    public static void SetDuration(FrameworkElement element, TimeSpan? value) => element.SetValue(DurationProperty, value);

    private static void OnDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element || e.NewValue is not TimeSpan _newDuration)
        {
            return;
        }

        var helper = GetShimmeringHelper(element);
        helper.Duration = _newDuration;
    }
    #endregion

    #region ShimmeringHelper
    private static readonly DependencyProperty ShimmeringHelperProperty = DependencyProperty.RegisterAttached(
           "ShimmeringHelper",
           typeof(ShimmeringHelper),
           typeof(ShimmerExtensions),
           new PropertyMetadata(null));

    private static ShimmeringHelper GetShimmeringHelper(FrameworkElement element)
    {
        var _currentValue = (ShimmeringHelper)element.GetValue(ShimmeringHelperProperty);

        if (_currentValue is null)
        {
            _currentValue = new ShimmeringHelper(element);
            SetShimmeringHelper(element, _currentValue);
        }

        return _currentValue;
    }

    private static void SetShimmeringHelper(FrameworkElement element, ShimmeringHelper value)
    {
        element.SetValue(ShimmeringHelperProperty, value);
    }
    #endregion
}