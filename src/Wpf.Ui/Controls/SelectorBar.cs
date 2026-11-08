using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Common;

namespace Wpf.Ui.Controls;

public class SelectorBar : ListBox
{
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(nameof(Orientation),
        typeof(Orientation), typeof(SelectorBar), new PropertyMetadata(Orientation.Horizontal));

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    protected override bool IsItemItsOwnContainerOverride(object item) => item is SelectorBarItem;

    protected override DependencyObject GetContainerForItemOverride() => new SelectorBarItem();
}

public class SelectorBarItem : ListBoxItem
{
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon),
        typeof(SymbolRegular), typeof(SelectorBarItem), new PropertyMetadata(SymbolRegular.Empty));

    public SymbolRegular Icon
    {
        get => (SymbolRegular)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
}
