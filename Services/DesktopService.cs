using FencesApp.Models;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FencesApp.Services
{
    public class DesktopService
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", CharSet = CharSet.Auto)]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Auto)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private const uint WS_EX_LAYERED = 0x80000;
        private const uint LWA_ALPHA = 2;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE
        {
            public int cx;
            public int cy;
            public SIZE(int cx, int cy)
            {
                this.cx = cx;
                this.cy = cy;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ARGB
        {
            public byte Blue;
            public byte Green;
            public byte Red;
            public byte Alpha;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;

            public BLENDFUNCTION(byte alpha)
            {
                BlendOp = 0;
                BlendFlags = 0;
                SourceConstantAlpha = alpha;
                AlphaFormat = AC_SRC_ALPHA;
            }

            private const byte AC_SRC_OVER = 0;
            private const byte AC_SRC_ALPHA = 1;
        }

        public Window CreateDesktopWindow(Fence fence)
        {
            Window window = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ShowInTaskbar = false,
                Topmost = true,
                Left = 100,
                Top = 100,
                Width = 200,
                Height = 100
            };

            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Nombre del Fence
            TextBox nameTextBox = new TextBox
            {
                Text = fence.Name,
                IsReadOnly = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.Gray,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Opacity = 0.7
            };
            nameTextBox.MouseDown += (s, args) =>
            {
                if (args.ChangedButton == MouseButton.Left && args.ClickCount == 2)
                {
                    nameTextBox.IsReadOnly = false;
                    nameTextBox.Focus();
                    nameTextBox.SelectAll();
                }
            };
            nameTextBox.LostFocus += (s, args) =>
            {
                nameTextBox.IsReadOnly = true;
            };
            nameTextBox.KeyDown += (s, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    nameTextBox.IsReadOnly = true;
                }
            };
            Grid.SetRow(nameTextBox, 0);

            // Contenido del Fence
            WrapPanel contentPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0)
            };
            Grid.SetRow(contentPanel, 1);

            grid.Children.Add(nameTextBox);
            grid.Children.Add(contentPanel);

            window.Content = grid;

            window.Loaded += (s, args) =>
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                SetWindowExTransparent(hwnd);
            };

            return window;
        }

        private void SetWindowExTransparent(IntPtr hwnd)
        {
            var extendedStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(extendedStyle.ToInt64() | WS_EX_LAYERED));
            SetLayeredWindowAttributes(hwnd, 0, 255, LWA_ALPHA);
        }

        private const int GWL_EXSTYLE = -20;
    }
}