using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SD = System.Drawing;

namespace FencesApp.Helpers
{
    public static class IconHelper
    {
        // Constantes para SHGetFileInfo
        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_LARGEICON = 0x000000000;
        private const uint SHGFI_SMALLICON = 0x000000001;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        private const uint SHGFI_SHELLICONSIZE = 0x000000004;
        private const uint SHGFI_SYSICONINDEX = 0x000004000;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

        // Tamaños estándar de Windows (en unidades DI)
        public const int SMALL_ICON_SIZE = 16;    // Pequeño
        public const int MEDIUM_ICON_SIZE = 32;     // Mediano
        public const int LARGE_ICON_SIZE = 48;      // Grande
        public const int JUMBO_ICON_SIZE = 256;     // Para miniaturas

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [ComImport]
        [Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IImageList
        {
            [PreserveSig] int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);
            [PreserveSig] int ReplaceIcon(int i, IntPtr hicon, ref int pi);
            [PreserveSig] int SetOverlayImage(int iImage, int iOverlay);
            [PreserveSig] int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);
            [PreserveSig] int AddMasked(IntPtr hbmImage, int crMask, ref int pi);
            [PreserveSig] int Draw(ref IMAGELISTDRAWPARAMS pimldp);
            [PreserveSig] int Remove(int i);
            [PreserveSig] int GetIcon(int i, int flags, ref IntPtr picon);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGELISTDRAWPARAMS
        {
            public int cbSize;
            public IntPtr himl;
            public int i;
            public IntPtr hdcDst;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int xBitmap;
            public int yBitmap;
            public int rgbBk;
            public int rgbFg;
            public int fStyle;
            public int dwRop;
            public int fState;
            public int Frame;
            public int crEffect;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
            ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        [DllImport("shell32.dll", EntryPoint = "#727")]
        private extern static int SHGetImageList(int iImageList, ref Guid riid, out IImageList ppv);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CopyImage(IntPtr hImage, uint uType, int cxDesired, int cyDesired, uint fuFlags);

        private const uint IMAGE_ICON = 1;
        private const uint LR_COPYFROMRESOURCE = 0x4000;
        private const uint LR_COPYRETURNORG = 0x4;

        private const int SHIL_SMALL = 0x1;
        private const int SHIL_LARGE = 0x0;
        private const int SHIL_EXTRALARGE = 0x2;
        private const int SHIL_JUMBO = 0x4;
        private const int ILD_TRANSPARENT = 0x1;

        // Se hace público para poder reutilizarlo en otros controles, si es necesario.
        public static double GetDpiScaleFactor()
        {
            var mainWindow = Application.Current?.MainWindow;
            if (mainWindow != null)
            {
                var source = PresentationSource.FromVisual(mainWindow);
                if (source?.CompositionTarget != null)
                    return source.CompositionTarget.TransformToDevice.M11;
            }
            return 1.0;
        }

        /// <summary>
        /// Obtiene el ícono del sistema o miniatura en alta calidad para el tamaño solicitado.
        /// Se ajusta el tamaño solicitado según el DPI para que el resultado se mapee a los tamaños de Windows.
        /// </summary>
        /// <param name="path">Ruta del archivo o carpeta</param>
        /// <param name="requestedSize">Tamaño deseado en unidades DI (16, 32 o 48)</param>
        /// <returns>BitmapSource con el ícono o miniatura</returns>
        public static BitmapSource GetHighQualityIcon(string path, int requestedSize)
        {
            try
            {
                // Calcular el tamaño exacto según el DPI
                double dpiScale = GetDpiScaleFactor();
                int scaledRequestedSize = (int)(requestedSize * dpiScale);

                // Seleccionar el tamaño de ícono más adecuado para minimizar el escalado
                int extractionMethod;
                if (scaledRequestedSize <= 16)
                    extractionMethod = SHIL_SMALL;      // 16x16
                else if (scaledRequestedSize <= 32)
                    extractionMethod = SHIL_LARGE;      // 32x32
                else if (scaledRequestedSize <= 48)
                    extractionMethod = SHIL_EXTRALARGE; // 48x48
                else
                    extractionMethod = SHIL_JUMBO;      // 256x256

                // Obtener el ícono del tamaño seleccionado
                IntPtr hIcon = GetWindowsIcon(path, extractionMethod);
                if (hIcon == IntPtr.Zero)
                    return null;

                // Crear el BitmapSource con la configuración correcta para DPI
                BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                    hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                // Aplicar metadatos DPI para escalar correctamente
                bitmapSource = SetDpiMetadata(bitmapSource, dpiScale);
                bitmapSource.Freeze();
                DestroyIcon(hIcon);
                return bitmapSource;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo ícono: {ex.Message}");
                return null;
            }
        }

        public static BitmapSource ScaleIconWithoutBlur(BitmapSource source, int targetSize)
        {
            // No escalar si ya tiene el tamaño exacto
            if (source.PixelWidth == targetSize && source.PixelHeight == targetSize)
                return source;

            // Para ícones más pequeños, usar NearestNeighbor para evitar desenfoque
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                targetSize, targetSize, 96, 96, PixelFormats.Pbgra32);

            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext dc = drawingVisual.RenderOpen())
            {
                dc.DrawImage(source, new Rect(0, 0, targetSize, targetSize));
            }

            renderBitmap.Render(drawingVisual);
            renderBitmap.Freeze();
            return renderBitmap;

        }

        // Método auxiliar para establecer metadatos DPI en el BitmapSource.
        private static BitmapSource SetDpiMetadata(BitmapSource source, double dpiScale)
        {
            int dpi = (int)(96 * dpiScale);
            FormatConvertedBitmap formattedBitmap = new FormatConvertedBitmap(
                source,
                source.Format,
                null,
                0);

            BitmapMetadata metadata = new BitmapMetadata("png");
            metadata.SetQuery("/tEXt/Software", "DpiAwareIcon");

            BitmapFrame frame = BitmapFrame.Create(
                formattedBitmap,
                null, // thumbnail
                metadata,
                null  // colorContexts
            );
            return frame;
        }

        // Método para obtener directamente el ícono del ImageList de Windows.
        private static IntPtr GetWindowsIcon(string path, int imageListType)
        {
            try
            {
                SHFILEINFO shfi = new SHFILEINFO();
                uint flags = SHGFI_SYSICONINDEX;

                if (Directory.Exists(path))
                    SHGetFileInfo(path, FILE_ATTRIBUTE_DIRECTORY, ref shfi, (uint)Marshal.SizeOf(shfi), flags);
                else if (!File.Exists(path))
                    SHGetFileInfo(path, 0, ref shfi, (uint)Marshal.SizeOf(shfi), flags | SHGFI_USEFILEATTRIBUTES);
                else
                    SHGetFileInfo(path, 0, ref shfi, (uint)Marshal.SizeOf(shfi), flags);

                Guid iidImageList = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");
                IImageList imageList;
                int result = SHGetImageList(imageListType, ref iidImageList, out imageList);

                if (result != 0)
                    return IntPtr.Zero;

                IntPtr hIcon = IntPtr.Zero;
                imageList.GetIcon(shfi.iIcon, ILD_TRANSPARENT, ref hIcon);

                if (shfi.hIcon != IntPtr.Zero)
                    DestroyIcon(shfi.hIcon);

                return hIcon;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        // Configuración del control Image para alta calidad.
        public static void ConfigureImageControlForHighQuality(System.Windows.Controls.Image imageControl)
        {
            // Para íconos pequeños, usar NearestNeighbor para conservar nitidez
            // Para íconos grandes, usar HighQuality para un mejor resultado visual
            if (imageControl.ActualWidth <= 24 || imageControl.ActualHeight <= 24)
                RenderOptions.SetBitmapScalingMode(imageControl, BitmapScalingMode.NearestNeighbor);
            else
                RenderOptions.SetBitmapScalingMode(imageControl, BitmapScalingMode.HighQuality);

            imageControl.SnapsToDevicePixels = true;
            imageControl.UseLayoutRounding = true;
            RenderOptions.SetEdgeMode(imageControl, EdgeMode.Aliased);
            RenderOptions.SetCachingHint(imageControl, CachingHint.Cache);

            // Evitar estiramiento automático
            imageControl.Stretch = Stretch.None;
            imageControl.StretchDirection = StretchDirection.Both;
        }
    }
}
