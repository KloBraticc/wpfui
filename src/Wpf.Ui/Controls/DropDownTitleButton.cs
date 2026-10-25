using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Wpf.Ui.Controls;

public class DropDownTitleButton : Button
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text),
        typeof(string), typeof(DropDownTitleButton), new PropertyMetadata(String.Empty));

    public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(nameof(IsDropDownOpen),
        typeof(bool), typeof(DropDownTitleButton), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsDropDownOpenChanged));

    public static readonly DependencyProperty DropDownMenuProperty = DependencyProperty.Register(nameof(DropDownMenu),
        typeof(ContextMenu), typeof(DropDownTitleButton), new PropertyMetadata(null, OnDropDownMenuChanged));

    public static readonly DependencyProperty ChevronSizeProperty = DependencyProperty.Register(nameof(ChevronSize),
        typeof(double), typeof(DropDownTitleButton), new PropertyMetadata(11.0));

    public static readonly DependencyProperty DropDownOffsetProperty = DependencyProperty.Register(nameof(DropDownOffset),
        typeof(Point), typeof(DropDownTitleButton), new PropertyMetadata(new Point(0, 4)));

    static DropDownTitleButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DropDownTitleButton), new FrameworkPropertyMetadata(typeof(DropDownTitleButton)));
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    public ContextMenu? DropDownMenu
    {
        get => (ContextMenu?)GetValue(DropDownMenuProperty);
        set => SetValue(DropDownMenuProperty, value);
    }

    public double ChevronSize
    {
        get => (double)GetValue(ChevronSizeProperty);
        set => SetValue(ChevronSizeProperty, value);
    }

    public Point DropDownOffset
    {
        get => (Point)GetValue(DropDownOffsetProperty);
        set => SetValue(DropDownOffsetProperty, value);
    }

    protected override void OnClick()
    {
        base.OnClick();

        if (DropDownMenu != null)
            IsDropDownOpen = !IsDropDownOpen;
    }

    private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DropDownTitleButton button || button.DropDownMenu is not ContextMenu menu)
            return;

        bool open = (bool)e.NewValue;

        if (open)
        {
            menu.PlacementTarget = button;
            menu.Placement = PlacementMode.Bottom;
            menu.HorizontalOffset = button.DropDownOffset.X;
            menu.VerticalOffset = button.DropDownOffset.Y;
        }

        if (menu.IsOpen != open)
            menu.IsOpen = open;
    }

    private static void OnDropDownMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DropDownTitleButton button)
            return;

        if (e.OldValue is ContextMenu oldMenu)
        {
            oldMenu.Opened -= button.OnMenuOpened;
            oldMenu.Closed -= button.OnMenuClosed;
        }

        if (e.NewValue is ContextMenu newMenu)
        {
            newMenu.Opened += button.OnMenuOpened;
            newMenu.Closed += button.OnMenuClosed;
        }
    }

    private void OnMenuOpened(object sender, RoutedEventArgs e)
    {
        IsDropDownOpen = true;
    }

    private void OnMenuClosed(object sender, RoutedEventArgs e)
    {
        IsDropDownOpen = false;
    }
}
