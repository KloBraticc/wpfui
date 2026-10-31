using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Wpf.Ui.Controls;

public sealed class StickyTabStrip
{
    private static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(120);

    private const double CoverAbove = 14;
    private const double CoverSide = 40;
    private const double CoverFade = 12;

    private static bool _registered;

    private readonly TabControl _tabs;
    private readonly ScrollViewer _scroll;
    private readonly Panel _strip;
    private readonly TranslateTransform _pin = new();
    private Border? _cover;
    private FrameworkElement? _masked;
    private readonly LinearGradientBrush _mask = new()
    {
        MappingMode = BrushMappingMode.Absolute,
        GradientStops = new GradientStopCollection
        {
            new GradientStop(Colors.Transparent, 0),
            new GradientStop(Colors.Black, 1),
        },
    };
    private bool _pinned;

    public static void Register()
    {
        if (_registered)
            return;
        _registered = true;
        EventManager.RegisterClassHandler(typeof(TabControl), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnTabControlLoaded));
    }

    private static void OnTabControlLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TabControl tabs || !ReferenceEquals(e.OriginalSource, tabs))
            return;
        ScrollViewer? scroll = null;
        bool insidePage = false;
        for (DependencyObject? parent = VisualTreeHelper.GetParent(tabs); parent != null; parent = VisualTreeHelper.GetParent(parent))
        {
            if (parent is TabControl)
                return;
            scroll ??= parent as ScrollViewer;
            if (parent is Page)
            {
                insidePage = true;
                break;
            }
        }
        if (!insidePage || scroll == null || FindItemsHost(tabs) is not Panel strip)
            return;
        _ = new StickyTabStrip(tabs, scroll, strip);
    }

    private static Panel? FindItemsHost(DependencyObject root)
    {
        Queue<DependencyObject> pending = new();
        pending.Enqueue(root);
        while (pending.Count > 0)
        {
            DependencyObject node = pending.Dequeue();
            if (node is Panel { IsItemsHost: true } panel)
                return panel;
            int count = VisualTreeHelper.GetChildrenCount(node);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(node, i);
                if (child is not ContentPresenter)
                    pending.Enqueue(child);
            }
        }
        return null;
    }

    private StickyTabStrip(TabControl tabs, ScrollViewer scroll, Panel strip)
    {
        _tabs = tabs;
        _scroll = scroll;
        _strip = strip;
        _strip.RenderTransform = _pin;
        Panel.SetZIndex(_strip, 100);
        _scroll.ScrollChanged += OnScrollChanged;
        _tabs.Unloaded += OnTabsUnloaded;
        _tabs.SelectionChanged += OnSelectionChanged;
        Update();
    }

    private void OnTabsUnloaded(object sender, RoutedEventArgs e)
    {
        _scroll.ScrollChanged -= OnScrollChanged;
        _tabs.Unloaded -= OnTabsUnloaded;
        _tabs.SelectionChanged -= OnSelectionChanged;
        _pin.Y = 0;
        if (_masked != null)
            _masked.OpacityMask = null;
        _masked = null;
        if (_cover != null && _strip.Parent is Panel host)
            host.Children.Remove(_cover);
        SetIndicators(Visibility.Visible);
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, _tabs) || !_pinned)
            return;
        double offset = _scroll.VerticalOffset;
        _tabs.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => RestoreOffset(offset)));
    }

    private void RestoreOffset(double offset)
    {
        if (!_tabs.IsLoaded)
            return;
        _scroll.UpdateLayout();
        _scroll.ScrollToVerticalOffset(Math.Min(offset, _scroll.ScrollableHeight));
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e) => Update();

    private void Update()
    {
        if (!_scroll.IsAncestorOf(_strip))
            return;
        double top = _strip.TransformToAncestor(_scroll).Transform(new Point(0, 0)).Y - _pin.Y;
        double limit = Math.Max(0, _tabs.ActualHeight - _strip.ActualHeight);
        double offset = Math.Clamp(-top, 0, limit);
        _pin.Y = offset;
        SetPinned(offset > 0);
        UpdateMask();
    }

    private void SetIndicators(Visibility visibility)
    {
        foreach (UIElement child in _strip.Children)
        {
            if (child is TabItem item && item.Template?.FindName("SelectionIndicator", item) is UIElement indicator)
                indicator.Visibility = visibility;
        }
    }

    private FrameworkElement? MaskTarget() =>
        (_tabs.Template?.FindName("Border", _tabs) ?? _tabs.Template?.FindName("PART_SelectedContentHost", _tabs)) as FrameworkElement;

    private void UpdateMask()
    {
        if (_masked == null || !_scroll.IsAncestorOf(_masked))
            return;
        double width = Math.Max(1, _masked.ActualWidth);
        double top = -_masked.TransformToAncestor(_scroll).Transform(new Point(0, 0)).Y + _strip.ActualHeight;
        _mask.StartPoint = new Point(width / 2, top);
        _mask.EndPoint = new Point(width / 2, top + CoverFade);
    }

    private Color PageColor()
    {
        Color window = (_tabs.TryFindResource("SolidBackgroundFillColorBaseBrush") as SolidColorBrush)?.Color ?? Color.FromRgb(0x20, 0x20, 0x20);
        if (_tabs.TryFindResource("LayerFillColorDefaultBrush") is not SolidColorBrush layer)
            return window;
        double amount = layer.Color.A / 255.0 * layer.Opacity;
        byte Mix(byte over, byte under) => (byte)Math.Round(over * amount + under * (1 - amount));
        return Color.FromRgb(Mix(layer.Color.R, window.R), Mix(layer.Color.G, window.G), Mix(layer.Color.B, window.B));
    }

    private Border CreateCover()
    {
        Color color = PageColor();
        Border cover = new()
        {
            Height = _strip.ActualHeight + CoverAbove + CoverFade,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(-CoverSide, -CoverAbove, -CoverSide, 0),
            IsHitTestVisible = false,
            Opacity = 0,
            RenderTransform = _pin,
            Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(color, 0),
                    new GradientStop(color, (_strip.ActualHeight + CoverAbove) / (_strip.ActualHeight + CoverAbove + CoverFade)),
                    new GradientStop(Color.FromArgb(0, color.R, color.G, color.B), 1),
                },
                90
            ),
        };
        if (_strip.Parent is Panel host)
        {
            Panel.SetZIndex(cover, 99);
            Grid.SetRow(cover, Grid.GetRow(_strip));
            Grid.SetRowSpan(cover, 2);
            host.Children.Add(cover);
        }
        return cover;
    }

    private void SetPinned(bool pinned)
    {
        if (pinned == _pinned)
            return;
        _pinned = pinned;
        SetIndicators(pinned ? Visibility.Hidden : Visibility.Visible);
        if (OperatingSystem.IsWindows() && MaskTarget() is FrameworkElement target)
        {
            _masked = pinned ? target : null;
            target.OpacityMask = pinned ? _mask : null;
            UpdateMask();
            return;
        }
        Border cover = _cover ??= CreateCover();
        cover.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(pinned ? 1 : 0, FadeDuration));
    }
}
