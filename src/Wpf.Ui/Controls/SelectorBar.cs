using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Common;

namespace Wpf.Ui.Controls;

public class SelectorBar : ListBox
{
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
