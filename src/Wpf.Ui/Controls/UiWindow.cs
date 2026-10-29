using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Shell;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Wpf.Ui.Interop;
using BackdropKind = Wpf.Ui.Controls.WindowBackdropType;

namespace Wpf.Ui.Controls;

public class UiWindow : System.Windows.Window
{
    public static readonly DependencyProperty ExtendsContentIntoTitleBarProperty = DependencyProperty.Register(
        nameof(ExtendsContentIntoTitleBar),
        typeof(bool),
        typeof(UiWindow),
        new PropertyMetadata(false, OnExtendsContentIntoTitleBarChanged));

    public static readonly DependencyProperty WindowCornerPreferenceProperty = DependencyProperty.Register(
        nameof(WindowCornerPreference),
        typeof(WindowCornerPreference),
        typeof(UiWindow),
        new PropertyMetadata(WindowCornerPreference.Round, OnCornerPreferenceChanged));

    public static readonly DependencyProperty WindowBackdropTypeProperty = DependencyProperty.Register(
        nameof(WindowBackdropType),
        typeof(Appearance.BackgroundType),
        typeof(UiWindow),
        new PropertyMetadata(Appearance.BackgroundType.None, OnBackdropTypeChanged));

    public bool ExtendsContentIntoTitleBar
    {
        get => (bool)GetValue(ExtendsContentIntoTitleBarProperty);
        set => SetValue(ExtendsContentIntoTitleBarProperty, value);
    }

    public WindowCornerPreference WindowCornerPreference
    {
        get => (WindowCornerPreference)GetValue(WindowCornerPreferenceProperty);
        set => SetValue(WindowCornerPreferenceProperty, value);
    }

    public Appearance.BackgroundType WindowBackdropType
    {
        get => (Appearance.BackgroundType)GetValue(WindowBackdropTypeProperty);
        set => SetValue(WindowBackdropTypeProperty, value);
    }

    private WindowInteropHelper? _interopHelper;

    protected WindowInteropHelper InteropHelper => _interopHelper ??= new WindowInteropHelper(this);

    static UiWindow()
    {
        HeightProperty.OverrideMetadata(typeof(UiWindow), new FrameworkPropertyMetadata(600d));
        WidthProperty.OverrideMetadata(typeof(UiWindow), new FrameworkPropertyMetadata(1100d));
        MinHeightProperty.OverrideMetadata(typeof(UiWindow), new FrameworkPropertyMetadata(320d));
        MinWidthProperty.OverrideMetadata(typeof(UiWindow), new FrameworkPropertyMetadata(460d));
    }

    public UiWindow()
    {
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        RemoveNativeWindowBorder();
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        RemoveNativeWindowBorder();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ApplyWindowCornerPreferenceInternal(WindowCornerPreference);
        ApplyWindowBackdropInternal(WindowBackdropType);
        ExtendsContentIntoTitleBarInternal(ExtendsContentIntoTitleBar);
        RemoveNativeWindowBorder();
    }

    protected virtual void OnExtendsContentIntoTitleBarChanged(bool oldValue, bool newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        ExtendsContentIntoTitleBarInternal(newValue);
    }

    private static void OnExtendsContentIntoTitleBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UiWindow window)
        {
            window.OnExtendsContentIntoTitleBarChanged((bool)e.OldValue, (bool)e.NewValue);
        }
    }

    protected virtual void OnCornerPreferenceChanged(WindowCornerPreference oldValue, WindowCornerPreference newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        ApplyWindowCornerPreferenceInternal(newValue);
    }

    private static void OnCornerPreferenceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UiWindow window)
        {
            window.OnCornerPreferenceChanged((WindowCornerPreference)e.OldValue, (WindowCornerPreference)e.NewValue);
        }
    }

    protected virtual void OnBackdropTypeChanged(Appearance.BackgroundType oldValue, Appearance.BackgroundType newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        ApplyWindowBackdropInternal(newValue);
    }

    private static void OnBackdropTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UiWindow window)
        {
            window.OnBackdropTypeChanged((Appearance.BackgroundType)e.OldValue, (Appearance.BackgroundType)e.NewValue);
        }
    }

    private void ApplyWindowCornerPreferenceInternal(WindowCornerPreference cornerPreference)
    {
        IntPtr handle = InteropHelper.Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        _ = UnsafeNativeMethods.ApplyWindowCornerPreference(handle, cornerPreference);
    }

    private void ApplyWindowBackdropInternal(Appearance.BackgroundType backdropType)
    {
        BackdropKind mapped = MapBackdropType(backdropType);
        if (mapped == BackdropKind.None)
        {
            _ = WindowBackdrop.RemoveBackdrop(this);
            return;
        }

        if (!ExtendsContentIntoTitleBar)
        {
            return;
        }

        _ = Wpf.Ui.Controls.WindowBackdrop.ApplyBackdrop(this, mapped);
    }

    private static BackdropKind MapBackdropType(Appearance.BackgroundType backdropType)
    {
        return backdropType switch
        {
            Appearance.BackgroundType.Mica => BackdropKind.Mica,
            Appearance.BackgroundType.Acrylic => BackdropKind.Acrylic,
            Appearance.BackgroundType.Tabbed => BackdropKind.Tabbed,
            _ => BackdropKind.None
        };
    }

    private void ExtendsContentIntoTitleBarInternal(bool extendContent)
    {
        if (!extendContent)
        {
            WindowChrome.SetWindowChrome(this, null);
            return;
        }

        _ = UnsafeNativeMethods.RemoveWindowTitlebarContents(this);
        WindowChrome.SetWindowChrome(
            this,
            new WindowChrome
            {
                CaptionHeight = 1,
                CornerRadius = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000) ? new CornerRadius(0) : new CornerRadius(4),
                GlassFrameThickness = new Thickness(-1),
                ResizeBorderThickness = ResizeMode == ResizeMode.NoResize ? new Thickness(0) : SystemParameters.WindowResizeBorderThickness,
                UseAeroCaptionButtons = false
            });
    }

    private unsafe void RemoveNativeWindowBorder()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            return;
        }

        IntPtr handle = InteropHelper.Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        int color = unchecked((int)0xFFFFFFFE);
        _ = PInvoke.DwmSetWindowAttribute(
            new HWND(handle),
            DWMWINDOWATTRIBUTE.DWMWA_BORDER_COLOR,
            &color,
            sizeof(int));
    }
}
