// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Wpf.Ui.TitleBar
{
    /// <summary>
    /// Enables Windows 11 Snap Layout functionality for a custom <see cref="Controls.TitleBar"/>.
    /// </summary>
    internal sealed partial class SnapLayout : IDisposable
    {
        private SnapLayoutButton[] _buttons = Array.Empty<SnapLayoutButton>();
        private HwndSource? _source;
        private Window? _window;
        private bool _disposed;
        private ApplicationTheme _currentTheme;
        private SolidColorBrush _currentHoverColor = Brushes.Transparent;

        /// <summary>
        /// Gets or sets the current theme.
        /// </summary>
        public ApplicationTheme Theme
        {
            get => _currentTheme;
            set
            {
                _currentTheme = value;
                _currentHoverColor = value == ApplicationTheme.Light ? HoverColorLight : HoverColorDark;
            }
        }

        /// <summary>
        /// Default button background.
        /// </summary>
        public SolidColorBrush DefaultButtonBackground { get; set; } = Brushes.Transparent;

        public bool IsActive => _source != null && !_disposed;

        /// <summary>
        /// Hover color for light theme.
        /// </summary>
        public SolidColorBrush HoverColorLight { get; set; } = Brushes.Transparent;

        /// <summary>
        /// Hover color for dark theme.
        /// </summary>
        public SolidColorBrush HoverColorDark { get; set; } = Brushes.Transparent;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnapLayout"/> class.
        /// </summary>
        private SnapLayout(Window window, Wpf.Ui.Controls.Button maximizeButton, Wpf.Ui.Controls.Button restoreButton)
        {
            if (window == null) return;

            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            var dpiScale = PInvoke.GetDpiForWindow(new HWND(hwnd)) / 96.0;

            _buttons = new[]
            {
                new SnapLayoutButton(maximizeButton, TitleBarButtonType.Maximize, dpiScale),
                new SnapLayoutButton(restoreButton, TitleBarButtonType.Restore, dpiScale)
            };

            _window = window;
            _source = HwndSource.FromHwnd(hwnd);
            _source?.AddHook(WndProc);
            _window.Closed += OnWindowClosed;
        }

        /// <summary>
        /// Checks if Snap Layouts are supported on the current OS.
        /// </summary>
        public static bool IsSupported() => Win32.Utilities.IsOSWindows11OrNewer;

        /// <summary>
        /// Registers Snap Layout for the provided window and title bar buttons.
        /// </summary>
        public static SnapLayout Register(Window window, Wpf.Ui.Controls.Button maximizeButton, Wpf.Ui.Controls.Button restoreButton)
            => new(window, maximizeButton, restoreButton);

        /// <summary>
        /// Processes native Win32 window messages.
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x02E0)
            {
                var dpiScale = PInvoke.GetDpiForWindow(new HWND(hwnd)) / 96.0;
                foreach (var button in _buttons)
                    button.UpdateScale(dpiScale);
            }

            var message = (uint)msg;

            switch (message)
            {
                case PInvoke.WM_MOVE:
                    // Reserved for future DPI scaling updates.
                    break;

                case PInvoke.WM_NCMOUSELEAVE:
                case PInvoke.WM_MOUSEMOVE:
                case PInvoke.WM_CAPTURECHANGED:
                    RemoveHoverFromAll();
                    break;

                case PInvoke.WM_MOUSELEAVE:
                    RemoveHoverFromAll();
                    break;

                case PInvoke.WM_ACTIVATEAPP:
                    if (wParam == IntPtr.Zero)
                        RemoveHoverFromAll();
                    break;

                case PInvoke.WM_NCMOUSEMOVE:
                    if (wParam.ToInt64() != (long)PInvoke.HTMAXBUTTON)
                        RemoveHoverFromAll();
                    break;

                case PInvoke.WM_NCLBUTTONDOWN:
                    HandleMouseDown(lParam, ref handled);
                    break;

                case PInvoke.WM_NCLBUTTONUP:
                    HandleMouseUp(lParam, ref handled);
                    break;

                case PInvoke.WM_NCHITTEST:
                    return HandleHitTest(lParam, ref handled);
            }

            return IntPtr.Zero;
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _source?.RemoveHook(WndProc);
            _source = null;
            if (_window != null)
            {
                _window.Closed -= OnWindowClosed;
                _window = null;
            }
            foreach (var button in _buttons)
                button.Dispose();
            _buttons = Array.Empty<SnapLayoutButton>();
            GC.SuppressFinalize(this);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TrackMouseEventInfo
        {
            public int cbSize;
            public uint dwFlags;
            public IntPtr hwndTrack;
            public uint dwHoverTime;
        }

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool TrackMouseEvent(ref TrackMouseEventInfo eventTrack);

        private void TrackNonClientLeave()
        {
            if (_source == null)
                return;

            var info = new TrackMouseEventInfo
            {
                cbSize = Marshal.SizeOf<TrackMouseEventInfo>(),
                dwFlags = 0x00000002 | 0x00000010,
                hwndTrack = _source.Handle
            };
            TrackMouseEvent(ref info);
        }

        private void RemoveHoverFromAll()
        {
            foreach (var btn in _buttons)
                btn.RemoveHover(DefaultButtonBackground);
        }

        private void HandleMouseDown(IntPtr lParam, ref bool handled)
        {
            foreach (var btn in _buttons)
            {
                if (!btn.IsMouseOver(lParam)) continue;
                btn.IsClickedDown = true;
                btn.SetPressed(true);
                handled = true;
            }
        }

        private void HandleMouseUp(IntPtr lParam, ref bool handled)
        {
            foreach (var btn in _buttons)
            {
                if (btn.IsClickedDown && btn.IsMouseOver(lParam))
                {
                    btn.InvokeClick();
                    handled = true;
                    break;
                }
            }
        }

        private IntPtr HandleHitTest(IntPtr lParam, ref bool handled)
        {
            foreach (var btn in _buttons)
            {
                if (btn.IsMouseOver(lParam))
                {
                    if (!btn.IsHovered)
                        TrackNonClientLeave();
                    btn.Hover(_currentHoverColor);
                    handled = true;
                    return new IntPtr((int)PInvoke.HTMAXBUTTON);
                }

                btn.RemoveHover(DefaultButtonBackground);
            }

            return IntPtr.Zero;
        }
    }
}
