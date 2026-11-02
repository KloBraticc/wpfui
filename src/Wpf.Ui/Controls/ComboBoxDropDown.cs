using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Wpf.Ui.Controls;

public static class ComboBoxDropDown
{
    private const string DropDownPartName = "DropDownBorder";

    private const double ClosedRatio = 0.5;

    private static readonly Duration OpenDuration = new(TimeSpan.FromMilliseconds(250));

    private static readonly KeySpline OpenSpline = CreateSpline();

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(ComboBoxDropDown),
        new PropertyMetadata(false, OnIsEnabledChanged)
    );

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    private static KeySpline CreateSpline()
    {
        KeySpline spline = new(0, 0, 0, 1);
        spline.Freeze();
        return spline;
    }

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Popup popup)
            return;
        popup.Opened -= OnPopupOpened;
        if (e.NewValue is true)
        {
            popup.Placement = PlacementMode.Custom;
            popup.CustomPopupPlacementCallback = (popupSize, targetSize, _) => PlaceFor(popup, popupSize, targetSize);
            popup.Opened += OnPopupOpened;
        }
    }

    private static ComboBox? OwnerOf(Popup popup) => popup.TemplatedParent as ComboBox;

    private static FrameworkElement? DropDownOf(ComboBox comboBox) =>
        comboBox.Template?.FindName(DropDownPartName, comboBox) as FrameworkElement;

    private static bool OpensBelow(ComboBox comboBox) =>
        ComboBoxSearch.GetSearchVisibility(comboBox) == Visibility.Visible || comboBox.SelectedIndex < 0;

    private static FrameworkElement? SelectedContainer(ComboBox comboBox) =>
        comboBox.SelectedIndex < 0 ? null : comboBox.ItemContainerGenerator.ContainerFromIndex(comboBox.SelectedIndex) as FrameworkElement;

    private static bool TryGetSelectedCenter(ComboBox comboBox, FrameworkElement dropDown, out double center)
    {
        center = 0;
        if (OpensBelow(comboBox) || SelectedContainer(comboBox) is not FrameworkElement container || !container.IsVisible)
            return false;
        try
        {
            center = container.TranslatePoint(new Point(0, container.ActualHeight / 2), dropDown).Y;
            return center >= 0 && center <= dropDown.ActualHeight;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static CustomPopupPlacement[] PlaceFor(Popup popup, Size popupSize, Size targetSize)
    {
        ComboBox? owner = OwnerOf(popup);
        FrameworkElement? part = owner == null ? null : DropDownOf(owner);
        Thickness margin = part?.Margin ?? default;
        if (owner != null && part != null && TryGetSelectedCenter(owner, part, out double center))
        {
            double y = targetSize.Height / 2 - margin.Top - center;
            return new[] { new CustomPopupPlacement(new Point(-margin.Left, y), PopupPrimaryAxis.None) };
        }
        return new[]
        {
            new CustomPopupPlacement(new Point(-margin.Left, targetSize.Height - margin.Top), PopupPrimaryAxis.Vertical),
            new CustomPopupPlacement(new Point(-margin.Left, -popupSize.Height + margin.Bottom), PopupPrimaryAxis.Vertical),
        };
    }

    private static void OnPopupOpened(object? sender, EventArgs e)
    {
        if (sender is not Popup popup || OwnerOf(popup) is not ComboBox comboBox || DropDownOf(comboBox) is not FrameworkElement dropDown)
            return;
        dropDown.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => Reveal(comboBox, dropDown)));
    }

    private static void Reveal(ComboBox comboBox, FrameworkElement dropDown)
    {
        double height = dropDown.ActualHeight;
        double width = dropDown.ActualWidth;
        if (height <= 0 || width <= 0)
            return;
        double center = TryGetSelectedCenter(comboBox, dropDown, out double selected) ? selected : 0;
        double offsetFromCenter = center - height / 2;
        double initialScale = ClosedRatio;
        double clipLength = height * ClosedRatio;
        double maxOffset = height * (1 - ClosedRatio) / 2;
        if (Math.Abs(offsetFromCenter) > maxOffset)
        {
            double pixelsOff = clipLength / 2 - (height / 2 - Math.Abs(offsetFromCenter));
            initialScale = pixelsOff / height * 2 + ClosedRatio;
        }
        double finalScale = (0.5 + Math.Abs(offsetFromCenter / height)) * 2;
        Rect from = ClipRect(width, height, center, initialScale);
        Rect to = ClipRect(width, height, center, finalScale);

        RectangleGeometry clip = new(from);
        dropDown.Clip = clip;
        RectAnimationUsingKeyFrames animation = new() { Duration = OpenDuration, FillBehavior = FillBehavior.Stop };
        animation.KeyFrames.Add(new SplineRectKeyFrame(to, KeyTime.FromTimeSpan(OpenDuration.TimeSpan), OpenSpline));
        animation.Completed += (_, _) =>
        {
            if (ReferenceEquals(dropDown.Clip, clip))
                dropDown.Clip = null;
        };
        clip.BeginAnimation(RectangleGeometry.RectProperty, animation);
    }

    private static Rect ClipRect(double width, double height, double center, double scale)
    {
        double length = height * scale;
        return new Rect(0, center - length / 2, width, length);
    }
}
