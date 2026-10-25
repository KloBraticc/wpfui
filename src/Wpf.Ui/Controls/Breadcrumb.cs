// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Navigation;
using Wpf.Ui.Common;
using Wpf.Ui.Controls.Interfaces;

namespace Wpf.Ui.Controls;

[TemplatePart(Name = "PART_Root", Type = typeof(System.Windows.Controls.Primitives.ButtonBase))]
public class Breadcrumb : System.Windows.Controls.Control
{
    public static readonly DependencyProperty CurrentProperty = DependencyProperty.Register(nameof(Current),
        typeof(string), typeof(Breadcrumb), new PropertyMetadata(String.Empty));

    public static readonly DependencyProperty NavigationProperty = DependencyProperty.Register(nameof(Navigation),
        typeof(INavigation), typeof(Breadcrumb),
        new PropertyMetadata(null, OnNavigationChanged));

    public static readonly DependencyProperty SubPageProperty = DependencyProperty.Register(nameof(SubPage),
        typeof(string), typeof(Breadcrumb), new PropertyMetadata(String.Empty, OnSubPageChanged));

    private static readonly DependencyPropertyKey HasSubPagePropertyKey = DependencyProperty.RegisterReadOnly(nameof(HasSubPage),
        typeof(bool), typeof(Breadcrumb), new PropertyMetadata(false));

    public static readonly DependencyProperty HasSubPageProperty = HasSubPagePropertyKey.DependencyProperty;

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon),
        typeof(SymbolRegular), typeof(Breadcrumb), new PropertyMetadata(SymbolRegular.Empty));

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.RegisterAttached("Header",
        typeof(string), typeof(Breadcrumb), new PropertyMetadata(null));

    public static readonly RoutedEvent BackRequestedEvent = EventManager.RegisterRoutedEvent("BackRequested",
        RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Breadcrumb));

    private System.Windows.Controls.Primitives.ButtonBase? _root;

    private Frame? _frame;

    private object? _parentContent;

    public string Current
    {
        get => (string)GetValue(CurrentProperty);
        set => SetValue(CurrentProperty, value);
    }

    public INavigation Navigation
    {
        get => GetValue(NavigationProperty) as INavigation;
        set => SetValue(NavigationProperty, value);
    }

    public string SubPage
    {
        get => (string)GetValue(SubPageProperty);
        set => SetValue(SubPageProperty, value);
    }

    public SymbolRegular Icon
    {
        get => (SymbolRegular)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public bool HasSubPage => (bool)GetValue(HasSubPageProperty);

    public static string? GetHeader(DependencyObject element) => (string?)element.GetValue(HeaderProperty);

    public static void SetHeader(DependencyObject element, string? value) => element.SetValue(HeaderProperty, value);

    public static void AddBackRequestedHandler(DependencyObject element, RoutedEventHandler handler)
    {
        if (element is UIElement target)
            target.AddHandler(BackRequestedEvent, handler);
    }

    public static void RemoveBackRequestedHandler(DependencyObject element, RoutedEventHandler handler)
    {
        if (element is UIElement target)
            target.RemoveHandler(BackRequestedEvent, handler);
    }

    public Breadcrumb()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        if (_root != null)
            _root.Click -= OnRootClick;

        base.OnApplyTemplate();

        _root = GetTemplateChild("PART_Root") as System.Windows.Controls.Primitives.ButtonBase;

        if (_root != null)
            _root.Click += OnRootClick;
    }

    protected virtual void OnNavigated(INavigation sender, RoutedNavigationEventArgs e)
    {
        AttachFrame();
        UpdateCurrent(Navigation?.Frame?.Content);
    }

    private void UpdateCurrent(object? content)
    {
        INavigationItem? item = FindItemForContent(content) ?? Navigation?.Current as INavigationItem;

        if (item?.Content is not string pageName || String.IsNullOrEmpty(pageName))
            return;

        Current = pageName;
    }

    private INavigationItem? FindItemForContent(object? content)
    {
        if (content == null || Navigation == null)
            return null;

        Type type = content.GetType();

        foreach (INavigationControl control in Navigation.Items)
            if (control is INavigationItem item && item.PageType == type)
                return item;

        foreach (INavigationControl control in Navigation.Footer)
            if (control is INavigationItem item && item.PageType == type)
                return item;

        return null;
    }

    protected virtual void OnNavigationChanged()
    {
        if (Navigation == null)
            return;

        Navigation.Navigated -= OnNavigated;
        Navigation.Navigated += OnNavigated;
        AttachFrame();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Navigation != null)
        {
            Navigation.Navigated -= OnNavigated;
            Navigation.Navigated += OnNavigated;
        }

        AttachFrame();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (Navigation != null)
            Navigation.Navigated -= OnNavigated;

        DetachFrame();
    }

    private void AttachFrame()
    {
        Frame? frame = Navigation?.Frame;

        if (ReferenceEquals(frame, _frame))
            return;

        DetachFrame();
        _frame = frame;

        if (_frame == null)
            return;

        _frame.Navigated += OnFrameNavigated;
        TrackContent(_frame.Content);
    }

    private void DetachFrame()
    {
        if (_frame != null)
            _frame.Navigated -= OnFrameNavigated;

        _frame = null;
        _parentContent = null;
    }

    private void OnFrameNavigated(object sender, NavigationEventArgs e)
    {
        TrackContent(e.Content);
        UpdateCurrent(e.Content);
    }

    private void TrackContent(object? content)
    {
        BindingOperations.ClearBinding(this, SubPageProperty);

        if (content is DependencyObject element && (String.IsNullOrEmpty(GetHeader(element)) || IsNavigationPage(content)))
            _parentContent = content;

        if (content is DependencyObject page)
        {
            SetBinding(SubPageProperty, new Binding { Source = page, Path = new PropertyPath(HeaderProperty), Mode = BindingMode.OneWay, FallbackValue = String.Empty, TargetNullValue = String.Empty });
            return;
        }

        SubPage = String.Empty;
    }

    private bool IsNavigationPage(object content) => Navigation?.Current is INavigationItem item && item.PageType != null && item.PageType == content.GetType();

    private void OnRootClick(object sender, RoutedEventArgs e)
    {
        if (!HasSubPage)
            return;

        if (_frame?.Content is UIElement current && ReferenceEquals(current, _parentContent))
        {
            current.RaiseEvent(new RoutedEventArgs(BackRequestedEvent, current));
            return;
        }

        if (_frame != null && _parentContent != null && !ReferenceEquals(_frame.Content, _parentContent))
        {
            _frame.Navigate(_parentContent);
            return;
        }

        if (_frame?.CanGoBack == true)
            _frame.GoBack();
    }

    private static void OnSubPageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Breadcrumb breadcrumb)
            breadcrumb.SetValue(HasSubPagePropertyKey, !String.IsNullOrEmpty(e.NewValue as string));
    }

    private static void OnNavigationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Breadcrumb breadcrumb)
            return;

        if (e.OldValue is INavigation oldNavigation)
            oldNavigation.Navigated -= breadcrumb.OnNavigated;

        breadcrumb.OnNavigationChanged();
    }
}
