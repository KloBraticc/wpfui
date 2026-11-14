using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Wpf.Ui.Controls;

public static class ComboBoxSearch
{
    private const string SearchPartName = "PART_Search";

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(ComboBoxSearch),
        new PropertyMetadata(false, OnIsEnabledChanged)
    );

    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.RegisterAttached(
        "Placeholder",
        typeof(string),
        typeof(ComboBoxSearch),
        new PropertyMetadata(string.Empty)
    );

    public static readonly DependencyProperty SearchVisibilityProperty = DependencyProperty.RegisterAttached(
        "SearchVisibility",
        typeof(Visibility),
        typeof(ComboBoxSearch),
        new PropertyMetadata(Visibility.Collapsed)
    );

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static string GetPlaceholder(DependencyObject element) => (string)element.GetValue(PlaceholderProperty);

    public static void SetPlaceholder(DependencyObject element, string value) => element.SetValue(PlaceholderProperty, value);

    public static Visibility GetSearchVisibility(DependencyObject element) => (Visibility)element.GetValue(SearchVisibilityProperty);

    public static void SetSearchVisibility(DependencyObject element, Visibility value) => element.SetValue(SearchVisibilityProperty, value);

    private static void OnIsEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not ComboBox combo)
            return;
        combo.DropDownOpened -= Combo_DropDownOpened;
        combo.DropDownClosed -= Combo_DropDownClosed;
        bool enabled = (bool)e.NewValue;
        SetSearchVisibility(combo, enabled ? Visibility.Visible : Visibility.Collapsed);
        if (enabled)
        {
            combo.DropDownOpened += Combo_DropDownOpened;
            combo.DropDownClosed += Combo_DropDownClosed;
        }
    }

    private static System.Windows.Controls.TextBox? FindBox(ComboBox combo) => combo.Template?.FindName(SearchPartName, combo) as System.Windows.Controls.TextBox;

    private static void Combo_DropDownOpened(object? sender, EventArgs e)
    {
        if (sender is not ComboBox combo || FindBox(combo) is not { } box)
            return;
        box.TextChanged -= Box_TextChanged;
        box.PreviewKeyDown -= Box_PreviewKeyDown;
        box.Text = string.Empty;
        box.Tag = combo;
        box.TextChanged += Box_TextChanged;
        box.PreviewKeyDown += Box_PreviewKeyDown;
        PlayIntro(box.Parent as FrameworkElement ?? box);
        combo.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => box.Focus()));
    }

    private static void PlayIntro(FrameworkElement box)
    {
        if (box.RenderTransform is not System.Windows.Media.TranslateTransform shift || shift.IsFrozen)
        {
            shift = new System.Windows.Media.TranslateTransform();
            box.RenderTransform = shift;
        }
        var ease = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };
        var duration = new Duration(TimeSpan.FromMilliseconds(260));
        shift.BeginAnimation(
            System.Windows.Media.TranslateTransform.YProperty,
            new System.Windows.Media.Animation.DoubleAnimation(12, 0, duration) { EasingFunction = ease }
        );
        box.BeginAnimation(
            UIElement.OpacityProperty,
            new System.Windows.Media.Animation.DoubleAnimation(0, 1, duration) { EasingFunction = ease }
        );
    }

    private static void Combo_DropDownClosed(object? sender, EventArgs e)
    {
        if (sender is not ComboBox combo)
            return;
        if (FindBox(combo) is { } box)
        {
            box.TextChanged -= Box_TextChanged;
            box.PreviewKeyDown -= Box_PreviewKeyDown;
            box.Text = string.Empty;
        }
        combo.Items.Filter = null;
    }

    private static void Box_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox { Tag: ComboBox combo } box)
            return;
        string query = box.Text.Trim();
        object? selected = combo.SelectedItem;
        combo.Items.Filter =
            query.Length == 0
                ? null
                : item => ReferenceEquals(item, selected) || (item?.ToString() ?? string.Empty).Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }

    private static void Box_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox { Tag: ComboBox combo } box)
            return;
        if (e.Key == Key.Escape)
        {
            combo.IsDropDownOpen = false;
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            string query = box.Text.Trim();
            object? match = combo.Items.Cast<object>().FirstOrDefault(item => query.Length > 0 && (item?.ToString() ?? string.Empty).Contains(query, StringComparison.CurrentCultureIgnoreCase));
            if (match != null)
                combo.SelectedItem = match;
            combo.IsDropDownOpen = false;
            e.Handled = true;
        }
    }
}
