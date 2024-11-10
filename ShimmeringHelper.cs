using System;
using System.Diagnostics;
using System.Numerics;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace SimpleShimmer;

public sealed class ShimmeringHelper
{
    private readonly FrameworkElement _associatedObject;

    private CompositionMaskBrush _maskBrush;

    private SpriteVisual _maskVisual;

    private ScalarKeyFrameAnimation _animation;

    private LinearEasingFunction _animationEasing;

    private Compositor compositor;

    public ShimmeringHelper(FrameworkElement associatedObject)
    {
        _associatedObject = associatedObject;
        _colorSourceProperty = GetColorSourceProperty();

        if (_associatedObject.IsLoaded)
        {
            OnLoaded(null, null);
        }

        _associatedObject.Loaded += OnLoaded;
        _associatedObject.SizeChanged += OnSizeChanged;
        _associatedObject.Unloaded += OnUnLoaded;
    }

    ~ShimmeringHelper()
    {
        _associatedObject.Loaded -= OnLoaded;
        _associatedObject.SizeChanged -= OnSizeChanged;
        _associatedObject.Unloaded -= OnUnLoaded;
        OnUnLoaded(null, null);
    }

    private bool _resourcesInitialized;
    private void InitializeResources()
    {
        if (_resourcesInitialized)
        {
            return;
        }

        compositor = ElementCompositionPreview.GetElementVisual(_associatedObject).Compositor;

        UpdateMaskBrush(compositor);
        CheckMaskBrushSource();
        UpdateMaskVisual(compositor);
        CreateAnimation(compositor);

        UpdateAnimation();

        _resourcesInitialized = true;
    }

    private void CheckMaskBrushSource()
    {
        if (compositor is null)
        {
            return;
        }

        var brush = Brush;
        if (brush is not null)
        {
            UpdateMaskBrushSource(GetCompositionBrush(compositor, brush));
            RemoveColorListeners();
            return;
        }

        var color = Color;
        if (color is not null)
        {
            UpdateMaskBrushSource(GetBrushForColor(compositor, color.Value));
            RemoveColorListeners();
            return;
        }

        AddColorListeners();
        UpdateMaskBrushSource(GetBrushForColor(compositor, CalculateAlternativeColor(_associatedObject)));
        return;
    }

    private void CreateAnimation(Compositor _compositor)
    {
        _animation = _compositor.CreateScalarKeyFrameAnimation();
        _animationEasing = _compositor.CreateLinearEasingFunction();

        _animation.Duration = Duration;
        _animation.IterationBehavior = AnimationIterationBehavior.Forever;
    }

    private void UpdateMaskVisual(Compositor _compositor)
    {
        _maskVisual = _compositor.CreateSpriteVisual();
        _maskVisual.Brush = _maskBrush;

        if (_associatedObject is not Shape and not TextBlock)
        {
            HandleCornerRadiusClip(_associatedObject, _compositor, _maskVisual);
        }
    }

    private void UpdateMaskBrush(Compositor _compositor)
    {
        _maskBrush = _compositor.CreateMaskBrush();

        if (_associatedObject is Shape shape)
        {
            _maskBrush.Mask = shape.GetAlphaMask();
        }

        else if (_associatedObject is TextBlock block)
        {
            _maskBrush.Mask = block.GetAlphaMask();
        }

        else if (_associatedObject is Panel panel)
        {
            _maskBrush.Mask = CreateCustomMaskBrushForPanel(_compositor, panel);
        }
    }

    private static CompositionBrush CreateCustomMaskBrushForPanel(Compositor compositor, Panel panel)
    {
        //TODO: this doesn't take into account any runtime changes in panel children
        var containerVisual = compositor.CreateContainerVisual();

        foreach (var child in panel.Children)
        {
            var childVisual = ElementCompositionPreview.GetElementVisual(child);

            // don't use child visual itself, makes the child transparnent
            var surfaceVisual = compositor.CreateRedirectVisual(childVisual);
            surfaceVisual.Offset = child.ActualOffset;

            containerVisual.Children.InsertAtTop(surfaceVisual);
        }

        var visualSurface = compositor.CreateVisualSurface();
        visualSurface.SourceVisual = containerVisual;
        visualSurface.SourceSize = new Vector2((float)panel.RenderSize.Width, (float)panel.RenderSize.Height);

        var surfaceBrush = compositor.CreateSurfaceBrush(visualSurface);
        return surfaceBrush;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        InitializeResources();
    }

    private void OnUnLoaded(object sender, RoutedEventArgs e)
    {
        DisposeResources();
    }

    private void DisposeResources()
    {
        if (!_resourcesInitialized)
        {
            return;
        }

        StopAnimation();

        ElementCompositionPreview.SetElementChildVisual(_associatedObject, null);

        _maskVisual.Dispose();
        _maskVisual = null;

        _maskBrush.Source?.Dispose();
        _maskBrush.Dispose();
        _maskBrush = null;

        _animation.Dispose();
        _animation = null;

        _resourcesInitialized = false;
    }

    private void OnSizeChanged(object sender, RoutedEventArgs e)
    {
        if (CheckResourcesInitialization())
        {
            UpdateAnimation();
        }
    }

    private bool CheckResourcesInitialization()
    {
        if (_resourcesInitialized)
        {
            return true;
        }

        if (_associatedObject.IsLoaded)
        {
            InitializeResources();
            return true;
        }

        return false;
    }

    private void UpdateAnimation()
    {
        var element = _associatedObject;

        StopAnimation();

        var width = GetElementWidth(element);
        var startOffset = -width / 2;
        var endOffset = width * 2;

        if (element is Panel panel)
        {
            _maskBrush.Mask = CreateCustomMaskBrushForPanel(compositor, panel);
        }

        else if (element is not Shape and not TextBlock)
        {
            HandleCornerRadiusClip(element, compositor, _maskVisual);
        }

        _maskVisual.Size = new Vector2(width, GetElementHeight(element));
        //confirm animation beginning, if removed will need to reset gradien offset in StartAnimation()
        _animation.InsertKeyFrame(0f, startOffset, _animationEasing);

        //u can just make it width, width * 2 (the difference between animation and actual _maskVisual Width) gives a pause between iterations
        _animation.InsertKeyFrame(1f, endOffset, _animationEasing);

        if (IsActive)
        {
            StartAnimation();
        }
    }

    private static float GetElementWidth(FrameworkElement element)
    {
        var width = element.RenderSize.Width;
        if (double.IsNaN(width) || double.IsInfinity(width))
        {
            width = element.ActualWidth;
        }
        return (float)width;
    }

    private static float GetElementHeight(FrameworkElement element)
    {
        var Height = element.RenderSize.Height;
        if (double.IsNaN(Height) || double.IsInfinity(Height))
        {
            Height = element.ActualHeight;
        }
        return (float)Height;
    }

    private void StartAnimation()
    {
        ElementCompositionPreview.SetElementChildVisual(_associatedObject, _maskVisual);
        _maskBrush.Source?.StartAnimation("Offset.X", _animation);
    }

    private void StopAnimation()
    {
        _maskBrush.Source?.StopAnimation("Offset.X");
        ElementCompositionPreview.SetElementChildVisual(_associatedObject, null);
    }

    private static void HandleCornerRadiusClip(FrameworkElement element, Compositor _compositor, SpriteVisual _mask)
    {
        CornerRadius? cornerRadius = element switch
        {
            var e when e is Control control => control.CornerRadius,
            var e when e is Grid panel => panel.CornerRadius,
            _ => null
        };

        if (cornerRadius is null)
        {
            return;
        }

        var roundedRectGeometry = GetCompositionPath(element, _compositor, cornerRadius.Value);
        // Set the clip on the mask visual
        _mask.Clip = _compositor.CreateGeometricClip(roundedRectGeometry);
    }

    private void UpdateMaskBrushSource(CompositionBrush source)
    {
        _maskBrush.Source = source;
    }

    static Color CalculateAlternativeColor(FrameworkElement element)
    {
        var _color = element switch
        {
            var e when e is Control control && control.Background is SolidColorBrush brush => brush.Color,
            var e when e is Shape shape && shape.Fill is SolidColorBrush brush => brush.Color,
            var e when e is TextBlock block && block.Foreground is SolidColorBrush brush => brush.Color,
            _ => Colors.DarkGray
        };

        return GetBrighterColor(_color, 1.3f);
    }

    private CompositionLinearGradientBrush GetBrushForColor(Compositor _compositor, Color color)
    {
        var _transparent = Windows.UI.Color.FromArgb(0, (byte)color.R, (byte)color.G, (byte)color.B);

        var brush = _compositor.CreateLinearGradientBrush();
        brush.ColorStops.Add(compositor.CreateColorGradientStop(0f, _transparent));
        brush.ColorStops.Add(compositor.CreateColorGradientStop(0.25f, color));
        brush.ColorStops.Add(compositor.CreateColorGradientStop(0.35f, color));
        brush.ColorStops.Add(compositor.CreateColorGradientStop(0.5f, _transparent));

        return brush;
    }

    private CompositionBrush GetCompositionBrush(Compositor _compositor, Brush source)
    {
        if (source is SolidColorBrush solidBrush)
        {
            return _compositor.CreateColorBrush(solidBrush.Color);
        }

        if (source is LinearGradientBrush linearBrush)
        {
            var newBrush = _compositor.CreateLinearGradientBrush();

            newBrush.MappingMode = linearBrush.MappingMode switch
            {
                BrushMappingMode.RelativeToBoundingBox => CompositionMappingMode.Relative,
                BrushMappingMode.Absolute => CompositionMappingMode.Absolute,
                _ => newBrush.MappingMode,
            };

            newBrush.ExtendMode = linearBrush.SpreadMethod switch
            {
                GradientSpreadMethod.Reflect => CompositionGradientExtendMode.Mirror,
                GradientSpreadMethod.Repeat => CompositionGradientExtendMode.Wrap,
                //GradientSpreadMethod.Pad => CompositionGradientExtendMode.Mirror,
                _ => newBrush.ExtendMode,
            };

            foreach (var stop in linearBrush.GradientStops)
            {
                newBrush.ColorStops.Add(_compositor.CreateColorGradientStop((float)stop.Offset, stop.Color));
            }

            return newBrush;
        }

        if (source is RadialGradientBrush radialBrush)
        {
            var newBrush = _compositor.CreateRadialGradientBrush();

            newBrush.MappingMode = radialBrush.MappingMode switch
            {
                BrushMappingMode.RelativeToBoundingBox => CompositionMappingMode.Relative,
                BrushMappingMode.Absolute => CompositionMappingMode.Absolute,
                _ => newBrush.MappingMode,
            };

            newBrush.EllipseRadius = new Vector2((float)radialBrush.RadiusX, (float)radialBrush.RadiusY);
            newBrush.EllipseCenter = new Vector2((float)radialBrush.Center.X, (float)radialBrush.Center.Y);
            newBrush.InterpolationSpace = radialBrush.InterpolationSpace;

            newBrush.ExtendMode = radialBrush.SpreadMethod switch
            {
                GradientSpreadMethod.Reflect => CompositionGradientExtendMode.Mirror,
                GradientSpreadMethod.Repeat => CompositionGradientExtendMode.Wrap,
                //GradientSpreadMethod.Pad => CompositionGradientExtendMode.Mirror,
                _ => newBrush.ExtendMode,
            };

            newBrush.GradientOriginOffset = new Vector2((float)radialBrush.GradientOrigin.X, (float)radialBrush.GradientOrigin.Y);

            foreach (var stop in radialBrush.GradientStops)
            {
                newBrush.ColorStops.Add(_compositor.CreateColorGradientStop((float)stop.Offset, stop.Color));
            }

            return newBrush;
        }

        Debug.WriteLine($"[ShimmeringHelper] non handled Brush Type: {source.GetType()}");
        return null;
    }

    private static Color GetBrighterColor(Color origin, float change)
    {
        var originColor = System.Drawing.Color.FromArgb(origin.A, origin.R, origin.G, origin.B);

        var lum = originColor.GetBrightness() * change;
        var hue = originColor.GetHue();
        var sat = originColor.GetSaturation();
        return GetColorFromHSL(lum, hue, sat, origin.A);
    }
    
    private static Color GetColorFromHSL(float lum, float hue, float sat, byte alpha)
    {
        var a = sat * Math.Min(lum, 1 - lum);
        return Windows.UI.Color.FromArgb(alpha, nFunk(0, hue, lum , a), nFunk(8, hue, lum , a), nFunk(4, hue, lum , a));

        static byte nFunk(double n, double hue, double lum, double a)
        {
            var k = (n + hue / 30) % 12;
            var factor = Math.Clamp(Math.Min(k - 3, 9 - k), -1, 1);
            return (byte)Math.Round((lum - a * factor) * 255);
        }
    }
    
    private static Color GetColorFromHSL2(float lum, float hue, float sat)
    {
        if (lum == 0)
        {
            return Windows.UI.Color.FromArgb(0xFF, 0, 0, 0);
        }

        if (sat == 0)
        {
            var _lum = (byte)(255 * lum);
            return Windows.UI.Color.FromArgb(0xFF, _lum, _lum, _lum);
        }

        var temp = lum < 0.5
            ? lum * (1.0 + sat)
            : (lum + sat - (lum * sat));

        var temp1 = 2.0 * lum - temp;

        var r = GetColorComponent(temp, temp1, hue + 1.0 / 3.0);
        var g = GetColorComponent(temp, temp1, hue);
        var b = GetColorComponent(temp, temp1, hue - 1.0 / 3.0);
        return Windows.UI.Color.FromArgb(0xFF, r, g, b);
    }

    private static byte GetColorComponent(double temp, double temp1, double adjustedHue)
    {
        if (adjustedHue < 0)
        {
            adjustedHue += 1;
        }

        if (adjustedHue > 1)
        {
            adjustedHue -= 1;
        }

        var component = adjustedHue switch
        {
            < 1 / 6d => temp1 + (temp - temp1) * 6.0 * adjustedHue,
            < 0.5 => temp,
            < 2 / 3d => temp1 + (temp - temp1) * (2.0 / 3.0 - adjustedHue) * 6.0,
            _ => temp1
        };

        return (byte)(component * 255);
    }

    private static CompositionPathGeometry GetCompositionPath(FrameworkElement element, Compositor _compositor, CornerRadius cornerRadius)
    {
        var topLeftRadius = (float)cornerRadius.TopLeft;
        var topRightRadius = (float)cornerRadius.TopRight;
        var bottomRightRadius = (float)cornerRadius.BottomRight;
        var bottomLeftRadius = (float)cornerRadius.BottomLeft;

        var width = GetElementWidth(element);
        var height = GetElementHeight(element);

        var pathBuilder = new CanvasPathBuilder(null);

        pathBuilder.BeginFigure(new Vector2(topLeftRadius, 0));

        pathBuilder.AddLine(new Vector2((float)width - topRightRadius, 0));
        pathBuilder.AddArc(new Vector2((float)width, topRightRadius), topRightRadius, topRightRadius, 90, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);

        pathBuilder.AddLine(new Vector2((float)width, (float)height - bottomRightRadius));
        pathBuilder.AddArc(new Vector2((float)width - bottomRightRadius, (float)height), bottomRightRadius, bottomRightRadius, 0, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);

        pathBuilder.AddLine(new Vector2(bottomLeftRadius, (float)height));
        pathBuilder.AddArc(new Vector2(0, (float)height - bottomLeftRadius), bottomLeftRadius, bottomLeftRadius, 270, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);

        pathBuilder.AddLine(new Vector2(0, topLeftRadius));
        pathBuilder.AddArc(new Vector2(topLeftRadius, 0), topLeftRadius, topLeftRadius, 180, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);

        pathBuilder.EndFigure(CanvasFigureLoop.Closed);

        var source = CanvasGeometry.CreatePath(pathBuilder);
        var composePath = new CompositionPath(source);

        var elementMaskClipPath = _compositor.CreatePathGeometry(composePath);
        return elementMaskClipPath;
    }

    #region IsActive
    private bool _isActive = false;
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value)
            {
                return;
            }

            _isActive = value;

            if (!_resourcesInitialized)
            {
                return;
            }

            if (_isActive)
            {
                StartAnimation();
            }
            else
            {
                StopAnimation();
            }
        }
    }
    #endregion

    #region Color
    private Color? _color = null;
    /// <summary>
    /// you can use either color or brush but not both, priority for brush
    /// </summary>
    public Color? Color
    {
        get => _color;
        set
        {
            if (_color == value)
            {
                return;
            }

            _color = value;
            CheckMaskBrushSource();
        }
    }
    #endregion

    #region Brush
    private Brush? _brush = null;
    public Brush? Brush
    {
        get => _brush;
        set
        {
            if (_brush == value)
            {
                return;
            }

            _brush = value;
            CheckMaskBrushSource();
        }
    }
    #endregion

    private DependencyProperty GetColorSourceProperty()
    {
        return _associatedObject switch
        {
            var e when e is Control => Control.BackgroundProperty,
            var e when e is Shape => Shape.FillProperty,
            var e when e is TextBlock => TextBlock.ForegroundProperty,
            _ => null
        };
    }

    private long? _colorListenerToken;
    private readonly DependencyProperty? _colorSourceProperty;

    private void RemoveColorListeners()
    {
        if (_colorListenerToken is not null)
        {
            _associatedObject.UnregisterPropertyChangedCallback(_colorSourceProperty, _colorListenerToken.Value);
        }
    }

    private void AddColorListeners()
    {
        if (_colorSourceProperty is not null)
        {
            _colorListenerToken = _associatedObject.RegisterPropertyChangedCallback(_colorSourceProperty, OnColorSourceUpdated);
        }
    }

    private void OnColorSourceUpdated(DependencyObject sender, DependencyProperty dp) => CheckMaskBrushSource();

    #region Duration
    private TimeSpan _duration = TimeSpan.FromSeconds(1);
    public TimeSpan Duration
    {
        get => _duration;
        set
        {
            //timeSpan limitaion of animation Duration by winui
            if (_duration == value || value < TimeSpan.FromMilliseconds(1) || value > TimeSpan.FromDays(24))
            {
                return;
            }

            _duration = value;

            if (_animation is not null)
            {
                _animation.Duration = value;
            }
        }
    }
    #endregion
}