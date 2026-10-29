using System.Windows;
using System.Windows.Controls;

namespace Wpf.Ui.Controls;

public class Hyperlink : System.Windows.Controls.Button
{
    public static readonly DependencyProperty NavigateUriProperty = DependencyProperty.Register(
        nameof(NavigateUri), typeof(string), typeof(Hyperlink), new PropertyMetadata(null));

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon), typeof(IconElement), typeof(Hyperlink), new PropertyMetadata(null));

    public string? NavigateUri
    {
        get => (string?)GetValue(NavigateUriProperty);
        set => SetValue(NavigateUriProperty, value);
    }

    public IconElement? Icon
    {
        get => (IconElement?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public Hyperlink()
    {
    }

    protected override void OnClick()
    {
        base.OnClick();

        if (string.IsNullOrEmpty(NavigateUri))
        {
            return;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(NavigateUri) { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
    }
}
