// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Wpf.Ui.Controls.Interfaces;

namespace Wpf.Ui.Controls;

/// <summary>
/// Inherited from the <see cref="System.Windows.Controls.Button"/>, adding <see cref="Common.SymbolRegular"/>.
/// </summary>
public class Button : System.Windows.Controls.Button, IIconControl, IAppearanceControl
{
    private static readonly DoubleAnimationUsingKeyFrames LayerIn = CreateLayerAnimation(1d);

    private static readonly DoubleAnimationUsingKeyFrames LayerOut = CreateLayerAnimation(0d);

    private Border? _hoverLayer;

    private Border? _pressLayer;

    private bool _hoverShown;

    private bool _pressShown;

    private static DoubleAnimationUsingKeyFrames CreateLayerAnimation(double value)
    {
        DoubleAnimationUsingKeyFrames animation = new();
        animation.KeyFrames.Add(
            new LinearDoubleKeyFrame(value, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(83d)))
        );
        animation.Freeze();
        return animation;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _hoverLayer = null;
        _pressLayer = null;
        _hoverShown = false;
        _pressShown = false;
        UpdateLayers();
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == IsMouseOverProperty || e.Property == IsPressedProperty)
            UpdateLayers();
    }

    private void UpdateLayers()
    {
        bool hover = (bool)GetValue(IsMouseOverProperty) && !IsPressed;
        if (hover != _hoverShown && EnsureLayer(ref _hoverLayer, hover, MouseOverBackgroundProperty, "ControlElevationBorderBrush", 1))
        {
            _hoverShown = hover;
            _hoverLayer!.BeginAnimation(OpacityProperty, hover ? LayerIn : LayerOut);
        }

        if (IsPressed != _pressShown && EnsureLayer(ref _pressLayer, IsPressed, PressedBackgroundProperty, "ButtonPressedStrokeBrush", _hoverLayer != null ? 2 : 1))
        {
            _pressShown = IsPressed;
            _pressLayer!.BeginAnimation(OpacityProperty, IsPressed ? LayerIn : LayerOut);
            if (Appearance is Common.ControlAppearance.Secondary or Common.ControlAppearance.Transparent && Template?.FindName("ContentBorder", this) is Border content)
                content.BeginAnimation(OpacityProperty, IsPressed ? LayerOut : LayerIn);
        }
    }

    private bool EnsureLayer(ref Border? layer, bool needed, DependencyProperty background, string borderBrushKey, int offset)
    {
        if (layer != null)
            return true;
        if (!needed || Template?.FindName("ContentBorder", this) is not Border content || VisualTreeHelper.GetParent(content) is not Panel root)
            return false;

        layer = new Border { Opacity = 0d, IsHitTestVisible = false };
        layer.SetBinding(Border.BackgroundProperty, new Binding { Source = this, Path = new PropertyPath(background) });
        layer.SetResourceReference(Border.BorderBrushProperty, borderBrushKey);
        layer.SetBinding(Border.BorderThicknessProperty, new Binding { Source = this, Path = new PropertyPath(BorderThicknessProperty) });
        layer.SetBinding(Border.CornerRadiusProperty, new Binding { Source = this, Path = new PropertyPath(Border.CornerRadiusProperty) });
        root.Children.Insert(root.Children.IndexOf(content) + offset, layer);
        return true;
    }

    /// <summary>
    /// Property for <see cref="Icon"/>.
    /// </summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon),
        typeof(Common.SymbolRegular), typeof(Button),
        new PropertyMetadata(Common.SymbolRegular.Empty));

    /// <summary>
    /// Property for <see cref="IconFilled"/>.
    /// </summary>
    public static readonly DependencyProperty IconFilledProperty = DependencyProperty.Register(nameof(IconFilled),
        typeof(bool), typeof(Button), new PropertyMetadata(false));

    /// <summary>
    /// Property for <see cref="IconForeground"/>.
    /// </summary>
    public static readonly DependencyProperty IconForegroundProperty = DependencyProperty.Register(nameof(IconForeground),
        typeof(Brush), typeof(Button), new FrameworkPropertyMetadata(SystemColors.ControlTextBrush,
            FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>
    /// Property for <see cref="Appearance"/>.
    /// </summary>
    public static readonly DependencyProperty AppearanceProperty = DependencyProperty.Register(nameof(Appearance),
        typeof(Common.ControlAppearance), typeof(Button),
        new PropertyMetadata(Common.ControlAppearance.Primary));

    /// <summary>
    /// Property for <see cref="MouseOverBackground"/>.
    /// </summary>
    public static readonly DependencyProperty MouseOverBackgroundProperty = DependencyProperty.Register(nameof(MouseOverBackground),
        typeof(Brush), typeof(Button),
        new PropertyMetadata(Border.BackgroundProperty.DefaultMetadata.DefaultValue));

    /// <summary>
    /// Property for <see cref="MouseOverBorderBrush"/>.
    /// </summary>
    public static readonly DependencyProperty MouseOverBorderBrushProperty = DependencyProperty.Register(nameof(MouseOverBorderBrush),
        typeof(Brush), typeof(Button),
        new PropertyMetadata(Border.BorderBrushProperty.DefaultMetadata.DefaultValue));

    /// <summary>
    /// Property for <see cref="PressedForeground"/>.
    /// </summary>
    public static readonly DependencyProperty PressedForegroundProperty = DependencyProperty.Register(nameof(PressedForeground),
        typeof(Brush), typeof(Button), new FrameworkPropertyMetadata(SystemColors.ControlTextBrush,
            FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>
    /// Property for <see cref="PressedBackground"/>.
    /// </summary>
    public static readonly DependencyProperty PressedBackgroundProperty = DependencyProperty.Register(nameof(PressedBackground),
        typeof(Brush), typeof(Button),
        new PropertyMetadata(Border.BackgroundProperty.DefaultMetadata.DefaultValue));

    /// <summary>
    /// Property for <see cref="PressedBorderBrush"/>.
    /// </summary>
    public static readonly DependencyProperty PressedBorderBrushProperty = DependencyProperty.Register(nameof(PressedBorderBrush),
        typeof(Brush), typeof(Button),
        new PropertyMetadata(Border.BorderBrushProperty.DefaultMetadata.DefaultValue));

    /// <inheritdoc />
    [Bindable(true), Category("Appearance")]
    public Common.SymbolRegular Icon
    {
        get => (Common.SymbolRegular)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <inheritdoc />
    [Bindable(true), Category("Appearance")]
    public bool IconFilled
    {
        get => (bool)GetValue(IconFilledProperty);
        set => SetValue(IconFilledProperty, value);
    }

    /// <summary>
    /// Foreground of the <see cref="Wpf.Ui.Controls.SymbolIcon"/>.
    /// </summary>
    [Bindable(true), Category("Appearance")]
    public Brush IconForeground
    {
        get => (Brush)GetValue(IconForegroundProperty);
        set => SetValue(IconForegroundProperty, value);
    }

    /// <inheritdoc />
    [Bindable(true), Category("Appearance")]
    public Common.ControlAppearance Appearance
    {
        get => (Common.ControlAppearance)GetValue(AppearanceProperty);
        set => SetValue(AppearanceProperty, value);
    }

    /// <summary>
    /// Background <see cref="Brush"/> when the user interacts with an element with a pointing device.
    /// </summary>
    [Bindable(true), Category("Appearance")]
    public Brush MouseOverBackground
    {
        get => (Brush)GetValue(MouseOverBackgroundProperty);
        set => SetValue(MouseOverBackgroundProperty, value);
    }

    /// <summary>
    /// Border <see cref="Brush"/> when the user interacts with an element with a pointing device.
    /// </summary>
    [Bindable(true), Category("Appearance")]
    public Brush MouseOverBorderBrush
    {
        get => (Brush)GetValue(MouseOverBorderBrushProperty);
        set => SetValue(MouseOverBorderBrushProperty, value);
    }

    /// <summary>
    /// Foreground when pressed.
    /// </summary>
    [Bindable(true), Category("Appearance")]
    public Brush PressedForeground
    {
        get => (Brush)GetValue(PressedForegroundProperty);
        set => SetValue(PressedForegroundProperty, value);
    }

    /// <summary>
    /// Background <see cref="Brush"/> when the user clicks the button.
    /// </summary>
    [Bindable(true), Category("Appearance")]
    public Brush PressedBackground
    {
        get => (Brush)GetValue(PressedBackgroundProperty);
        set => SetValue(PressedBackgroundProperty, value);
    }

    /// <summary>
    /// Border <see cref="Brush"/> when the user clicks the button.
    /// </summary>
    [Bindable(true), Category("Appearance")]
    public Brush PressedBorderBrush
    {
        get => (Brush)GetValue(PressedBorderBrushProperty);
        set => SetValue(PressedBorderBrushProperty, value);
    }
}
