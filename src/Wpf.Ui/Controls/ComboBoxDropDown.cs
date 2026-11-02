using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Wpf.Ui.Controls;

public static class ComboBoxDropDown
{
    private const string DropDownPartName = "DropDownBorder";

    private const string FaceplatePartName = "PART_ContentPresenter";

    private const string ChevronPartName = "ChevronGrid";

    private const double ClosedRatio = 0.5;

    private const double ChevronVisualSize = 48;

    private static readonly TimeSpan OpenDuration = TimeSpan.FromMilliseconds(250);

    private static readonly TimeSpan CloseDuration = TimeSpan.FromMilliseconds(167);

    private static readonly TimeSpan OpacityChangeDuration = TimeSpan.FromMilliseconds(83);

    private static readonly TimeSpan OpacityChangeBeginTime = CloseDuration - OpacityChangeDuration;

    private static readonly KeySpline SplitSpline = Spline(0, 0, 0, 1);

    private static readonly KeySpline ChevronPressSpline = Spline(0.167, 0.167, 0.65, 1);

    private static readonly KeySpline ChevronReboundSpline = Spline(0.55, 0, 0.75, 1);

    private static readonly KeySpline ChevronSettleSpline = Spline(0.35, 0, 0, 1);

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(ComboBoxDropDown),
        new PropertyMetadata(false, OnIsEnabledChanged)
    );

    public static readonly DependencyProperty IsItemPressedProperty = DependencyProperty.RegisterAttached(
        "IsItemPressed",
        typeof(bool),
        typeof(ComboBoxDropDown),
        new PropertyMetadata(false)
    );

    private static readonly DependencyProperty IsHookedProperty = DependencyProperty.RegisterAttached(
        "IsHooked",
        typeof(bool),
        typeof(ComboBoxDropDown),
        new PropertyMetadata(false)
    );

    private static readonly DependencyProperty SuppressReleaseProperty = DependencyProperty.RegisterAttached(
        "SuppressRelease",
        typeof(bool),
        typeof(ComboBoxDropDown),
        new PropertyMetadata(false)
    );

    static ComboBoxDropDown()
    {
        EventManager.RegisterClassHandler(typeof(ComboBox), UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(OnComboBoxReleased), true);
        EventManager.RegisterClassHandler(typeof(ComboBoxItem), UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(OnItemPressed), true);
        EventManager.RegisterClassHandler(typeof(ComboBoxItem), UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(OnItemReleased), true);
        EventManager.RegisterClassHandler(typeof(ComboBoxItem), UIElement.MouseLeaveEvent, new MouseEventHandler(OnItemLeft), true);
    }

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsItemPressed(DependencyObject element, bool value) => element.SetValue(IsItemPressedProperty, value);

    public static bool GetIsItemPressed(DependencyObject element) => (bool)element.GetValue(IsItemPressedProperty);

    private static KeySpline Spline(double x1, double y1, double x2, double y2)
    {
        KeySpline spline = new(x1, y1, x2, y2);
        spline.Freeze();
        return spline;
    }

    private static void OnComboBoxReleased(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ComboBox comboBox || !(bool)comboBox.GetValue(SuppressReleaseProperty))
            return;
        comboBox.SetValue(SuppressReleaseProperty, false);
        if (comboBox.IsDropDownOpen)
            e.Handled = true;
    }

    private static void OnItemPressed(object sender, MouseButtonEventArgs e) => SetIsItemPressed((DependencyObject)sender, true);

    private static void OnItemReleased(object sender, MouseButtonEventArgs e) => SetIsItemPressed((DependencyObject)sender, false);

    private static void OnItemLeft(object sender, MouseEventArgs e) => SetIsItemPressed((DependencyObject)sender, false);

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

    private static T? Part<T>(ComboBox comboBox, string name)
        where T : class => comboBox.Template?.FindName(name, comboBox) as T;

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
        FrameworkElement? part = owner == null ? null : Part<FrameworkElement>(owner, DropDownPartName);
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
        if (sender is not Popup popup || OwnerOf(popup) is not ComboBox comboBox)
            return;
        if (!(bool)comboBox.GetValue(IsHookedProperty))
        {
            comboBox.SetValue(IsHookedProperty, true);
            comboBox.DropDownClosed += OnDropDownClosed;
            comboBox.PreviewMouseLeftButtonDown += OnComboBoxPressed;
            comboBox.Unloaded += OnComboBoxUnloaded;
        }
        comboBox.SetValue(SuppressReleaseProperty, Mouse.LeftButton == MouseButtonState.Pressed);
        PlayChevronRelease(comboBox);
        AnimateFaceplate(comboBox, opening: true);
        if (Part<FrameworkElement>(comboBox, DropDownPartName) is FrameworkElement dropDown)
            dropDown.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => Split(comboBox, dropDown, opening: true)));
    }

    private static void OnComboBoxUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox comboBox)
            return;
        comboBox.DropDownClosed -= OnDropDownClosed;
        comboBox.PreviewMouseLeftButtonDown -= OnComboBoxPressed;
        comboBox.Unloaded -= OnComboBoxUnloaded;
        comboBox.SetValue(IsHookedProperty, false);
    }

    private static void OnDropDownClosed(object? sender, EventArgs e)
    {
        if (sender is not ComboBox comboBox)
            return;
        AnimateFaceplate(comboBox, opening: false);
        if (Part<FrameworkElement>(comboBox, DropDownPartName) is FrameworkElement dropDown)
            Split(comboBox, dropDown, opening: false);
    }

    private static void OnComboBoxPressed(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ComboBox comboBox || comboBox.IsDropDownOpen || Part<FrameworkElement>(comboBox, ChevronPartName) is not FrameworkElement chevron)
            return;
        TranslateTransform translate = ChevronTransform(chevron);
        double depth = 7.5 / ChevronVisualSize * chevron.ActualHeight;
        DoubleAnimationUsingKeyFrames press = new() { FillBehavior = FillBehavior.HoldEnd };
        press.KeyFrames.Add(new SplineDoubleKeyFrame(depth, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(150.8)), ChevronPressSpline));
        translate.BeginAnimation(TranslateTransform.YProperty, press);
    }

    private static void PlayChevronRelease(ComboBox comboBox)
    {
        if (Part<FrameworkElement>(comboBox, ChevronPartName) is not FrameworkElement chevron)
            return;
        TranslateTransform translate = ChevronTransform(chevron);
        double height = chevron.ActualHeight;
        DoubleAnimationUsingKeyFrames release = new() { FillBehavior = FillBehavior.Stop };
        release.KeyFrames.Add(new DiscreteDoubleKeyFrame(translate.Y, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        release.KeyFrames.Add(new SplineDoubleKeyFrame(-3 / ChevronVisualSize * height, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(83.3)), ChevronReboundSpline));
        release.KeyFrames.Add(new SplineDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)), ChevronSettleSpline));
        translate.BeginAnimation(TranslateTransform.YProperty, release);
    }

    private static TranslateTransform ChevronTransform(FrameworkElement chevron)
    {
        if (chevron.RenderTransform is TranslateTransform current && !current.IsFrozen)
            return current;
        TranslateTransform owned = new();
        chevron.RenderTransform = owned;
        return owned;
    }

    private static void AnimateFaceplate(ComboBox comboBox, bool opening)
    {
        if (Part<UIElement>(comboBox, FaceplatePartName) is not UIElement faceplate)
            return;
        DoubleAnimationUsingKeyFrames animation = new() { FillBehavior = opening ? FillBehavior.HoldEnd : FillBehavior.Stop };
        if (opening)
        {
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(0.5, KeyTime.FromTimeSpan(OpacityChangeDuration)));
        }
        else
        {
            animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(OpacityChangeBeginTime)));
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(CloseDuration)));
        }
        faceplate.BeginAnimation(UIElement.OpacityProperty, animation);
    }

    private static void Split(ComboBox comboBox, FrameworkElement dropDown, bool opening)
    {
        double height = dropDown.ActualHeight;
        double width = dropDown.ActualWidth;
        if (height <= 0 || width <= 0)
            return;
        double center = TryGetSelectedCenter(comboBox, dropDown, out double selected) ? selected : 0;
        double offsetFromCenter = center - height / 2;
        double closedScale = ClosedRatio;
        double clipLength = height * ClosedRatio;
        double maxOffset = height * (1 - ClosedRatio) / 2;
        if (Math.Abs(offsetFromCenter) > maxOffset)
        {
            double pixelsOff = clipLength / 2 - (height / 2 - Math.Abs(offsetFromCenter));
            closedScale = pixelsOff / height * 2 + ClosedRatio;
        }
        double openScale = (0.5 + Math.Abs(offsetFromCenter / height)) * 2;
        Rect from = ClipRect(width, height, center, opening ? closedScale : openScale);
        Rect to = ClipRect(width, height, center, opening ? openScale : closedScale);
        TimeSpan duration = opening ? OpenDuration : CloseDuration;

        RectangleGeometry clip = new(from);
        dropDown.Clip = clip;
        RectAnimationUsingKeyFrames animation = new() { Duration = duration, FillBehavior = opening ? FillBehavior.Stop : FillBehavior.HoldEnd };
        animation.KeyFrames.Add(new SplineRectKeyFrame(to, KeyTime.FromTimeSpan(duration), SplitSpline));
        if (opening)
        {
            animation.Completed += (_, _) =>
            {
                if (ReferenceEquals(dropDown.Clip, clip))
                    dropDown.Clip = null;
            };
        }
        clip.BeginAnimation(RectangleGeometry.RectProperty, animation);
    }

    private static Rect ClipRect(double width, double height, double center, double scale)
    {
        double length = height * scale;
        return new Rect(0, center - length / 2, width, length);
    }
}
