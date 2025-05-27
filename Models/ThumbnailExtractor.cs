using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.Windows.Media;


namespace FencesApp.Helpers
{
    public static class ThumbnailExtractor
    {
        // Constantes para Shell API
        private const uint SHIL_JUMBO = 0x4;
        private const uint SHIL_EXTRALARGE = 0x2;
        private const uint ILD_TRANSPARENT = 0x1;
        private const uint ILD_IMAGE = 0x20;

        // Tamaños de Thumbnails
        public const int THUMBNAIL_SIZE = 256;

        // Interfaces para Shell API
        [ComImport]
        [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemImageFactory
        {
            [PreserveSig]
            int GetImage(
                [In, MarshalAs(UnmanagedType.Struct)] SIZE size,
                [In] SIIGBF flags,
                [Out] out IntPtr phbm);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE
        {
            public int cx;
            public int cy;

            public SIZE(int cx, int cy)
            {
                this.cx = cx;
                this.cy = cy;
            }
        }

        private enum SIIGBF
        {
            SIIGBF_RESIZETOFIT = 0,
            SIIGBF_BIGGERSIZEOK = 1,
            SIIGBF_MEMORYONLY = 2,
            SIIGBF_ICONONLY = 4,
            SIIGBF_THUMBNAILONLY = 8,
            SIIGBF_INCACHEONLY = 16
        }

        // Método para crear ShellItem desde una ruta
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [In] IntPtr pbc,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [Out, MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory shellItem);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        /// <summary>
        /// Determina si un archivo es un tipo de archivo multimedia que puede tener vista previa
        /// </summary>
        /// 

        // Añade este método a la clase ThumbnailExtractor
        public static BitmapSource GetVideoFrameUsingFFMpeg(string videoPath, int size)
        {
            try
            {
                // Para propósitos de este ejemplo, intentaremos un enfoque diferente con Shell
                SIZE thumbnailSize = new SIZE(size, size);
                IShellItemImageFactory shellItem = null;

                Guid guid = new Guid("7E9FB0D3-919F-4307-AB2E-9B1860310C93");

                try
                {
                    SHCreateItemFromParsingName(videoPath, IntPtr.Zero, guid, out shellItem);

                    if (shellItem != null)
                    {
                        IntPtr hBitmap = IntPtr.Zero;
                        // Intentar con diferentes flags - NO usamos THUMBNAILONLY aquí
                        int hr = shellItem.GetImage(
                            thumbnailSize,
                            SIIGBF.SIIGBF_THUMBNAILONLY,
                            out hBitmap
                        );


                        if (hr == 0 && hBitmap != IntPtr.Zero)
                        {
                            BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                hBitmap,
                                IntPtr.Zero,
                                System.Windows.Int32Rect.Empty,
                                BitmapSizeOptions.FromEmptyOptions());


                            RenderOptions.SetBitmapScalingMode(bitmapSource, BitmapScalingMode.HighQuality);
                            bitmapSource.Freeze();

                            DeleteObject(hBitmap);
                            return bitmapSource;
                        }

                        // Si el método anterior falló, intentar con la API WPF MediaElement
                        // Esto requeriría ejecutarse en un thread de UI

                        DeleteObject(hBitmap);
                    }
                }
                finally
                {
                    if (shellItem != null)
                        Marshal.ReleaseComObject(shellItem);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting video frame: {ex.Message}");
            }

            return null;
        }

        public static bool IsImageFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            string[] imageExtensions = {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp", ".jfif",
        ".heic", ".heif", ".svg", ".raw", ".cr2", ".nef", ".arw"
    };

            string ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            return Array.Exists(imageExtensions, e => e == ext);
        }

        /// <summary>
        /// Extrae la miniatura (thumbnail) de un archivo multimedia usando Shell API
        /// </summary>
        /// 
        public static bool IsMediaFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                return false;

            string extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();

            string[] imageExtensions = {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp", ".jfif",
            ".heic", ".heif", ".svg", ".raw", ".cr2", ".nef", ".arw"
        };

            string[] videoExtensions = {
            ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".mpeg", ".mpg",
            ".m4v", ".3gp", ".3g2", ".m2ts", ".mts", ".ts", ".webm"
        };

            string[] docExtensions = { ".pdf", ".pptx", ".docx", ".xlsx" };

            return Array.Exists(imageExtensions, ext => ext == extension) ||
                   Array.Exists(videoExtensions, ext => ext == extension) ||
                   Array.Exists(docExtensions, ext => ext == extension);
        }

        public static BitmapSource ExtractThumbnail(string filePath, int size)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            IntPtr hBitmap = IntPtr.Zero;

            try
            {
                // Usar la API de Shell para extraer la miniatura
                SIZE thumbnailSize = new SIZE(size, size);
                IShellItemImageFactory shellItem = null;

                Guid guid = new Guid("7E9FB0D3-919F-4307-AB2E-9B1860310C93"); // IShellItemImageFactory

                try
                {
                    SHCreateItemFromParsingName(filePath, IntPtr.Zero, guid, out shellItem);

                    if (shellItem != null)
                    {
                        // Intentar diferentes combinaciones de flags para extraer la miniatura
                        int hr = shellItem.GetImage(
                            thumbnailSize,
                            SIIGBF.SIIGBF_THUMBNAILONLY | SIIGBF.SIIGBF_BIGGERSIZEOK,
                            out hBitmap
                        );

                        if (hr != 0 || hBitmap == IntPtr.Zero)
                        {
                            // Intentar solo con THUMBNAILONLY
                            hr = shellItem.GetImage(
                                thumbnailSize,
                                SIIGBF.SIIGBF_THUMBNAILONLY,
                                out hBitmap
                            );
                        }

                        if (hr != 0 || hBitmap == IntPtr.Zero)
                        {
                            // Intentar con solo RESIZETOFIT
                            hr = shellItem.GetImage(
                                thumbnailSize,
                                SIIGBF.SIIGBF_RESIZETOFIT,
                                out hBitmap
                            );
                        }

                        if (hr != 0 || hBitmap == IntPtr.Zero)
                        {
                            // Intentar como última opción con ICONONLY
                            hr = shellItem.GetImage(
                                thumbnailSize,
                                SIIGBF.SIIGBF_ICONONLY,
                                out hBitmap
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en SHCreateItemFromParsingName para {filePath}: {ex.Message}");
                }
                finally
                {
                    if (shellItem != null)
                        Marshal.ReleaseComObject(shellItem);
                }

                // Si tenemos un bitmap válido, convertirlo a BitmapSource
                if (hBitmap != IntPtr.Zero)
                {
                    // Crear BitmapSource desde HBITMAP
                    BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        System.Windows.Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(size, size));

                    // Configurar para mejor calidad
                    System.Windows.Media.RenderOptions.SetBitmapScalingMode(bitmapSource, System.Windows.Media.BitmapScalingMode.HighQuality);
                    bitmapSource.Freeze(); // Mejora el rendimiento

                    return bitmapSource;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting thumbnail for {filePath}: {ex.Message}");
            }
            finally
            {
                // Liberar recursos
                if (hBitmap != IntPtr.Zero)
                    DeleteObject(hBitmap);
            }

            return null;
        }

        public static BitmapSource ExtractVideoThumbnail(string videoPath, int size)
        {
            try
            {
                // Usar la API de Shell para extraer la miniatura
                SIZE thumbnailSize = new SIZE(size, size);
                IShellItemImageFactory shellItem = null;

                Guid guid = new Guid("7E9FB0D3-919F-4307-AB2E-9B1860310C93"); // IShellItemImageFactory

                SHCreateItemFromParsingName(videoPath, IntPtr.Zero, guid, out shellItem);

                if (shellItem != null)
                {
                    IntPtr hBitmap = IntPtr.Zero;

                    int hr = shellItem.GetImage(thumbnailSize, SIIGBF.SIIGBF_RESIZETOFIT | SIIGBF.SIIGBF_BIGGERSIZEOK, out hBitmap);


                    if (hr != 0 || hBitmap == IntPtr.Zero)
                    {
                        hr = shellItem.GetImage(thumbnailSize, SIIGBF.SIIGBF_THUMBNAILONLY, out hBitmap);
                    }

                    if (hr != 0 || hBitmap == IntPtr.Zero)
                    {
                        hr = shellItem.GetImage(thumbnailSize, SIIGBF.SIIGBF_RESIZETOFIT, out hBitmap);
                    }

                    if (hr != 0 || hBitmap == IntPtr.Zero)
                    {
                        hr = shellItem.GetImage(thumbnailSize, SIIGBF.SIIGBF_ICONONLY, out hBitmap);
                    }

                    if (hBitmap != IntPtr.Zero)
                    {
                        BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap,
                            IntPtr.Zero,
                            System.Windows.Int32Rect.Empty,
                            BitmapSizeOptions.FromWidthAndHeight(size, size));

                        System.Windows.Media.RenderOptions.SetBitmapScalingMode(bitmapSource, System.Windows.Media.BitmapScalingMode.HighQuality);
                        bitmapSource.Freeze();

                        DeleteObject(hBitmap);
                        Marshal.ReleaseComObject(shellItem);

                        return bitmapSource;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting video thumbnail for {videoPath}: {ex.Message}");
            }

            return null;
        }


        public static bool IsVideoFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            string[] videoExtensions = {
            ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".mpeg", ".mpg",
            ".m4v", ".3gp", ".3g2", ".m2ts", ".mts", ".ts", ".webm"
        };

            string ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            return Array.Exists(videoExtensions, e => e == ext);
        }

        /// <summary>
        /// Método alternativo para extraer miniatura de imágenes directamente
        /// </summary>
        public static BitmapSource ExtractImageThumbnail(string imagePath, int size)
        {
            try
            {
                if (!System.IO.File.Exists(imagePath))
                    return null;

                string ext = System.IO.Path.GetExtension(imagePath).ToLowerInvariant();
                if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".bmp" && ext != ".gif")
                    return null;

                using (var originalImage = new Bitmap(imagePath))
                {
                    // Calcular proporciones para mantener relación de aspecto
                    int width, height;
                    if (originalImage.Width > originalImage.Height)
                    {
                        width = size;
                        height = (int)(originalImage.Height * size / (float)originalImage.Width);
                    }
                    else
                    {
                        height = size;
                        width = (int)(originalImage.Width * size / (float)originalImage.Height);
                    }

                    // Crear thumbnail
                    using (var thumbnail = new Bitmap(width, height))
                    {
                        using (var g = Graphics.FromImage(thumbnail))
                        {
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                            g.DrawImage(originalImage, 0, 0, width, height);

                            // Convertir a BitmapSource
                            using (MemoryStream ms = new MemoryStream())
                            {
                                thumbnail.Save(ms, ImageFormat.Png);
                                ms.Position = 0;

                                BitmapImage bitmapImage = new BitmapImage();
                                bitmapImage.BeginInit();
                                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                                bitmapImage.StreamSource = ms;
                                bitmapImage.EndInit();
                                bitmapImage.Freeze();

                                return bitmapImage;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating image thumbnail: {ex.Message}");
                return null;
            }
        }
    }
}