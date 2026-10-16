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

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.RegisterAttached("Header",
        typeof(string), typeof(Breadcrumb), new PropertyMetadata(null));

    private System.Windows.Controls.Primitives.ButtonBase? _root;

    private Frame? _frame;

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

    public bool HasSubPage => (bool)GetValue(HasSubPageProperty);

    public static string? GetHeader(DependencyObject element) => (string?)element.GetValue(HeaderProperty);

    public static void SetHeader(DependencyObject element, string? value) => element.SetValue(HeaderProperty, value);

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

        if (Navigation?.Current is not INavigationItem item)
            return;

        var pageName = item.Content as string;

        if (String.IsNullOrEmpty(pageName))
            return;

        Current = pageName;
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
    }

    private void OnFrameNavigated(object sender, NavigationEventArgs e)
    {
        TrackContent(e.Content);
    }

    private void TrackContent(object? content)
    {
        BindingOperations.ClearBinding(this, SubPageProperty);

        if (content is DependencyObject page)
        {
            SetBinding(SubPageProperty, new Binding { Source = page, Path = new PropertyPath(HeaderProperty), Mode = BindingMode.OneWay, FallbackValue = String.Empty, TargetNullValue = String.Empty });
            return;
        }

        SubPage = String.Empty;
    }

    private void OnRootClick(object sender, RoutedEventArgs e)
    {
        if (!HasSubPage)
            return;

        if (_frame?.CanGoBack == true)
        {
            _frame.GoBack();
            return;
        }

        if (Navigation != null)
            Navigation.Navigate(Navigation.SelectedPageIndex);
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
