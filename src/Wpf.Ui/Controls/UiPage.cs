using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Wpf.Ui.Controls;

[ContentProperty(nameof(Content))]
public class UiPage : System.Windows.Controls.Page
{
    public static readonly DependencyProperty ScrollableProperty = DependencyProperty.Register(
        nameof(Scrollable),
        typeof(bool),
        typeof(UiPage),
        new PropertyMetadata(false, OnScrollableChanged));

    public static readonly DependencyProperty ScrollHostProperty = DependencyProperty.Register(
        nameof(ScrollHost),
        typeof(ScrollViewer),
        typeof(UiPage),
        new PropertyMetadata(null));

    [Bindable(true)]
    public bool Scrollable
    {
        get => (bool)GetValue(ScrollableProperty);
        set => SetValue(ScrollableProperty, value);
    }

    [Bindable(false)]
    public ScrollViewer? ScrollHost
    {
        get => (ScrollViewer?)GetValue(ScrollHostProperty);
        private set => SetValue(ScrollHostProperty, value);
    }

    private bool _scrollHostWrapped;

    public UiPage()
    {
        DataContext = this;
        Loaded += OnUiPageLoaded;
    }

    private void OnUiPageLoaded(object sender, RoutedEventArgs e)
    {
        WrapContentIntoScrollHost();
    }

    private static void OnScrollableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UiPage page)
        {
            page.WrapContentIntoScrollHost();
        }
    }

    private void WrapContentIntoScrollHost()
    {
        if (!Scrollable || Content is null || _scrollHostWrapped)
        {
            return;
        }

        if (Content is ScrollViewer existing)
        {
            ScrollHost = existing;
            return;
        }

        _scrollHostWrapped = true;
        DynamicScrollViewer viewer = new()
        {
            Content = Content,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        Content = viewer;
        ScrollHost = viewer;
    }
}
