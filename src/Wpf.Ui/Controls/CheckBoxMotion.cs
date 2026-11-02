using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Wpf.Ui.Animations;

namespace Wpf.Ui.Controls;

public static class CheckBoxMotion
{
    private const double GlyphLength = 10.82;

    private static readonly KeySpline CheckSpline = Freeze(new KeySpline(0.55, 0, 0, 1));
    private static readonly KeySpline UncheckSpline = Freeze(new KeySpline(0.167, 0.167, 0.833, 0.833));
    private static readonly TimeSpan CheckDuration = TimeSpan.FromMilliseconds(316.7);
    private static readonly TimeSpan UncheckDuration = TimeSpan.FromMilliseconds(66.7);

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(CheckBoxMotion),
        new PropertyMetadata(false));

    static CheckBoxMotion()
    {
        EventManager.RegisterClassHandler(typeof(CheckBox), ToggleButton.CheckedEvent, new RoutedEventHandler(OnChecked));
        EventManager.RegisterClassHandler(typeof(CheckBox), ToggleButton.UncheckedEvent, new RoutedEventHandler(OnUnchecked));
        EventManager.RegisterClassHandler(typeof(CheckBox), ToggleButton.IndeterminateEvent, new RoutedEventHandler(OnIndeterminate));
    }

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnChecked(object sender, RoutedEventArgs e)
    {
        if (FindGlyph(sender) is not Path glyph)
            return;
        if (!CanAnimate((CheckBox)sender))
        {
            Reset(glyph);
            return;
        }
        Play(glyph, GlyphLength, 0, CheckDuration, CheckSpline);
    }

    private static void OnUnchecked(object sender, RoutedEventArgs e)
    {
        if (FindGlyph(sender) is not Path glyph)
            return;
        if (!CanAnimate((CheckBox)sender))
        {
            Reset(glyph);
            return;
        }
        glyph.BeginAnimation(UIElement.OpacityProperty, Hold(1, UncheckDuration));
        Play(glyph, 0, -GlyphLength, UncheckDuration, UncheckSpline);
    }

    private static void OnIndeterminate(object sender, RoutedEventArgs e)
    {
        if (FindGlyph(sender) is Path glyph)
            Reset(glyph);
    }

    private static Path? FindGlyph(object sender)
    {
        if (sender is not CheckBox box || !GetIsEnabled(box))
            return null;
        return box.Template?.FindName("CheckGlyph", box) as Path;
    }

    private static bool CanAnimate(CheckBox box) =>
        !PortableRenderer.IsActive && box.IsLoaded && box.IsVisible && SystemParameters.ClientAreaAnimation;

    private static void Reset(Path glyph)
    {
        glyph.BeginAnimation(Shape.StrokeDashOffsetProperty, null);
        glyph.BeginAnimation(UIElement.OpacityProperty, null);
        glyph.StrokeDashOffset = 0;
    }

    private static void Play(Path glyph, double from, double to, TimeSpan duration, KeySpline spline)
    {
        try
        {
            DoubleAnimationUsingKeyFrames animation = new() { FillBehavior = FillBehavior.Stop };
            animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(from, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            animation.KeyFrames.Add(new SplineDoubleKeyFrame(to, KeyTime.FromTimeSpan(duration), spline));
            glyph.BeginAnimation(Shape.StrokeDashOffsetProperty, animation);
        }
        catch (InvalidOperationException)
        {
            Reset(glyph);
        }
    }

    private static DoubleAnimationUsingKeyFrames Hold(double value, TimeSpan duration)
    {
        DoubleAnimationUsingKeyFrames animation = new() { FillBehavior = FillBehavior.Stop };
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(value, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(value, KeyTime.FromTimeSpan(duration)));
        return animation;
    }

    private static KeySpline Freeze(KeySpline spline)
    {
        spline.Freeze();
        return spline;
    }
}
