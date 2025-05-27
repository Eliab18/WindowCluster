using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FencesApp.Helpers;
using System.Windows.Media.Imaging;
using static FencesApp.Helpers.IconHelper;
using System.Windows.Media.Effects;
using System.Windows.Interop;

namespace FencesApp.Pages
{
    public partial class FenceWindow : Window
    {
        public static void CloseAllFences()
        {
            foreach (var fence in FenceManager.Fences.ToList())
            {
                fence.Close();
                FenceManager.Fences.Remove(fence); // Eliminar de la lista
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = (int)GetWindowLong(hwnd, GWL_EXSTYLE);
            exStyle |= WS_EX_TOOLWINDOW;  // Agrega el estilo de ventana de tool window
            SetWindowLong(hwnd, GWL_EXSTYLE, (IntPtr)exStyle);
        }

        // Declaraciones de Win32 para modificar el estilo de la ventana
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]

        private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        #region Propiedades y Constructor

        public event Action OnFenceUpdated;
        public event EventHandler FilesChanged;

        // Propiedades públicas
        public string FenceColor { get; private set; }
        public string FolderPath { get; private set; }
        public string Title => FenceTitle.Text;
        public string TitleFontFamily { get; set; } = "Segoe UI";
        public void UpdateTitleFontFamily(string fontNameWithStyle)
        {
            TitleFontFamily = fontNameWithStyle;

            // Dividir el nombre para separar la familia de la descripción del estilo
            string familyName;
            FontWeight weight = FontWeights.Bold;
            FontStyle style = FontStyles.Normal;

            // Extraer información del nombre compuesto (si existe)
            int spaceIndex = fontNameWithStyle.IndexOf(' ');
            if (spaceIndex > 0)
            {
                familyName = fontNameWithStyle.Substring(0, spaceIndex);
                string styleDescription = fontNameWithStyle.Substring(spaceIndex + 1);

                // Determinar peso
                if (styleDescription.Contains("Bold"))
                    weight = FontWeights.Bold;
                else if (styleDescription.Contains("Light"))
                    weight = FontWeights.Light;
                else if (styleDescription.Contains("SemiBold"))
                    weight = FontWeights.SemiBold;
                else if (styleDescription.Contains("Black"))
                    weight = FontWeights.Black;
                else if (styleDescription.Contains("ExtraBold"))
                    weight = FontWeights.ExtraBold;
                else if (styleDescription.Contains("ExtraLight"))
                    weight = FontWeights.ExtraLight;
                else if (styleDescription.Contains("Medium"))
                    weight = FontWeights.Medium;
                else if (styleDescription.Contains("Thin"))
                    weight = FontWeights.Thin;

                // Determinar estilo
                if (styleDescription.Contains("Italic"))
                    style = FontStyles.Italic;
                else if (styleDescription.Contains("Oblique"))
                    style = FontStyles.Oblique;
            }
            else
            {
                familyName = fontNameWithStyle;
                weight = FontWeights.Bold;
            }

            // Actualizar la fuente del título
            FenceTitle.FontFamily = new FontFamily(familyName);
            FenceTitle.FontWeight = weight;
            FenceTitle.FontStyle = style;

            OnFenceUpdated?.Invoke();
        }

        public string TitleTextColor { get; set; } = "#FFFFFFFF"; // Por defecto, blanco
        public string TitleAlignment { get; set; } = "Left";        // "Left", "Center" o "Right"
        public int TitleFontSize { get; set; } = 20;
        public string TitleDesignType { get; set; } = "default";    // "default" o "etiqueta"
        public string TitleBackgroundColor { get; set; } = "#FF333333"; // Para estilo 'etiqueta'



        // Colección de archivos que contiene el fence
        public System.Collections.Generic.List<FileItem> Files { get; private set; } = new System.Collections.Generic.List<FileItem>();

        // Valor del tamaño actual de íconos (actualizado desde FencesPage)
        public int CurrentIconSize { get; set; } = 48;

        // En FenceWindow, modifica las constantes para los tamaños de íconos


        public FenceWindow(string title, string folderPath, string color = "#545e75")
        {
            InitializeComponent();
            FenceTitle.Text = title;
            this.ShowInTaskbar = false;
            this.Show();
            // Aplica Segoe UI Bold por defecto
            FenceTitle.FontFamily = new FontFamily("Segoe UI");
            FenceTitle.FontWeight = FontWeights.Bold; // <-- Añade esta línea

            FolderPath = folderPath;
            Directory.CreateDirectory(FolderPath);
            UpdateColor(color);
            LoadFiles();
            this.LocationChanged += (s, e) => { OnFenceUpdated?.Invoke(); };
            FenceManager.Fences.Add(this); // Registrar la ventana
            this.Closed += (s, e) => FenceManager.Fences.Remove(this); // Eliminar al cerrar
        }

        private void FenceWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Es seguro actualizar el título una vez cargado el control
            UpdateTitleTextColor(TitleTextColor);
        }

        #endregion

        #region Actualización de Apariencia y Propiedades

        public void UpdateColor(string color, string opacityHex = null)
        {
            try
            {
                // Combina opacidad si el color es en formato #RRGGBB y se proporciona opacidad
                string finalColor = (color.StartsWith("#") && color.Length == 7 && !string.IsNullOrEmpty(opacityHex))
                    ? $"#{opacityHex}{color.Substring(1)}"
                    : color;
                FenceColor = finalColor;
                Color c = (Color)ColorConverter.ConvertFromString(finalColor);
                BackgroundPanel.Background = new SolidColorBrush(c);
                OnFenceUpdated?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al aplicar el color: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void UpdateFolderPath(string newFolderPath)
        {
            FolderPath = newFolderPath;
            OnFenceUpdated?.Invoke();
        }

        public void UpdateTitle(string newTitle)
        {
            FenceTitle.Text = newTitle;
            OnFenceUpdated?.Invoke();
        }

        public void UpdateTitleTextColor(string hexColor)
        {
            TitleTextColor = hexColor;
            FenceTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor));
            OnFenceUpdated?.Invoke();
        }

        public void UpdateTitleAlignment(string alignment)
        {
            switch (alignment)
            {
                case "Left":
                    FenceTitle.HorizontalAlignment = HorizontalAlignment.Left;
                    break;
                case "Center":
                    FenceTitle.HorizontalAlignment = HorizontalAlignment.Center;
                    break;
                case "Right":
                    FenceTitle.HorizontalAlignment = HorizontalAlignment.Right;
                    break;
            }
            TitleAlignment = alignment;
            OnFenceUpdated?.Invoke();
        }

        public void UpdateTitleFontSize(int fontSize)
        {
            TitleFontSize = fontSize;
            FenceTitle.FontSize = fontSize;
            OnFenceUpdated?.Invoke();
        }

        public void UpdateTitleDesignType(string designType, string backgroundHex = null)
        {
            TitleDesignType = designType;
            TitleBackgroundColor = backgroundHex;
            if (designType == "etiqueta")
            {
                TitleContainer.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(backgroundHex ?? "#FF333333"));
                // Establece solo las esquinas superiores como redondeadas (izquierda, derecha, 0, 0)
                TitleContainer.CornerRadius = new CornerRadius(10, 10, 0, 0);
            }
            else
            {
                TitleContainer.Background = Brushes.Transparent;
                TitleContainer.CornerRadius = new CornerRadius(0);
            }
            OnFenceUpdated?.Invoke();
        }

        #endregion

        #region Gestión de Archivos y UI

        private void LoadFiles()
        {
            Files.Clear();
            FileContainer.Children.Clear();

            if (Directory.Exists(FolderPath))
            {
                foreach (var file in Directory.GetFiles(FolderPath))
                    AddFile(file);
                foreach (var dir in Directory.GetDirectories(FolderPath))
                    AddFile(dir);
            }
        }

        private void AddFile(string path)
        {
            var fileInfo = new FileInfo(path);
            bool isDirectory = fileInfo.Attributes.HasFlag(FileAttributes.Directory);
            string fileName = isDirectory ? fileInfo.Name : Path.GetFileNameWithoutExtension(fileInfo.Name);
            FileItem fileItem = new FileItem
            {
                Path = path,
                Name = fileName,
                IsDirectory = isDirectory,
                Icon = GetSystemIcon(path)
            };

            Files.Add(fileItem);
            AddFileControl(fileItem);
            OnFilesChanged();
        }

        private void AddFileControl(FileItem file)
        {
            UIElement control = CreateFileControl(file);
            FileContainer.Children.Add(control);
        }



        // Create UI control for a file item
        private UIElement CreateFileControl(FileItem file)
        {
            Grid fileControl = new Grid
            {
                Margin = new Thickness(5),
                Tag = file.Path,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                DataContext = file
            };

            Border fileBackground = new Border
            {
                CornerRadius = new CornerRadius(2),
                Background = Brushes.Transparent,
                Padding = new Thickness(2)
            };

            fileControl.MouseMove += FileControl_MouseMove;
            fileControl.MouseLeftButtonDown += FileControl_MouseLeftButtonDown;
            fileControl.MouseEnter += FileControl_MouseEnter;
            fileControl.MouseLeave += FileControl_MouseLeave;

            StackPanel contentStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Creación del control Image (sin cambios en su estructura)
            Image iconImage = new Image
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Stretch = Stretch.None,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true
            };

            RenderOptions.SetBitmapScalingMode(iconImage, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(iconImage, EdgeMode.Aliased);

            // --- Extracción de miniatura o ícono ---
            BitmapSource iconSource = null;
            if (ThumbnailExtractor.IsMediaFile(file.Path))
            {
                if (ThumbnailExtractor.IsImageFile(file.Path))
                {
                    // Extraer la miniatura de imagen con el tamaño actual
                    iconSource = ThumbnailExtractor.ExtractImageThumbnail(file.Path, CurrentIconSize);
                }
                else if (ThumbnailExtractor.IsVideoFile(file.Path))
                {
                    // Para vídeos, extraer la miniatura usando el tamaño completo (THUMBNAIL_SIZE, p.ej. 256)
                    iconSource = ThumbnailExtractor.ExtractVideoThumbnail(file.Path, ThumbnailExtractor.THUMBNAIL_SIZE);
                }
            }

            if (iconSource == null)
            {
                iconSource = IconHelper.GetHighQualityIcon(file.Path, CurrentIconSize);
            }

            if (iconSource != null)
            {
                double dpi = IconHelper.GetDpiScaleFactor();
                iconImage.Source = iconSource;
                iconImage.Width = iconSource.PixelWidth / dpi;
                iconImage.Height = iconSource.PixelHeight / dpi;
                file.Icon = iconSource;
            }
            else
            {
                iconImage.Width = CurrentIconSize;
                iconImage.Height = CurrentIconSize;
            }

            if (iconSource != null && iconSource.PixelWidth != iconSource.PixelHeight)
            {
                double aspectRatio = (double)iconSource.PixelWidth / iconSource.PixelHeight;
                if (aspectRatio > 1)
                {
                    iconImage.Width = CurrentIconSize;
                    iconImage.Height = CurrentIconSize / aspectRatio;
                }
                else
                {
                    iconImage.Width = CurrentIconSize * aspectRatio;
                    iconImage.Height = CurrentIconSize;
                }
            }

            IconHelper.ConfigureImageControlForHighQuality(iconImage);

            // --- Creación del texto con sombra, contorno y texto principal ---
            Grid textContainer = new Grid
            {
                Margin = new Thickness(0, 3, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // 1. Sombra: se simula con una copia central densa y copias adicionales translúcidas en los bordes.
            Grid shadowGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            TextBlock centralShadow = new TextBlock
            {
                Text = file.Name,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                MaxWidth = 80,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0))
            };
            centralShadow.RenderTransform = new TranslateTransform(0, 1);
            shadowGrid.Children.Add(centralShadow);

            var outerOffsets = new System.Collections.Generic.List<TranslateTransform>
    {
        new TranslateTransform(-1, 2),
        new TranslateTransform(1, 2),
        new TranslateTransform(0, 2),
        new TranslateTransform(-1, 3),
        new TranslateTransform(1, 3)
    };

            foreach (var offset in outerOffsets)
            {
                TextBlock outerShadow = new TextBlock
                {
                    Text = file.Name,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    MaxWidth = 80,
                    Foreground = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0))
                };
                outerShadow.RenderTransform = offset;
                shadowGrid.Children.Add(outerShadow);
            }

            // 2. Contorno: se utilizan 4 copias en direcciones cardinales para un contorno sutil.
            Grid outlineGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var outlineOffsets = new System.Collections.Generic.List<TranslateTransform>
    {
        new TranslateTransform(1, 0),
        new TranslateTransform(-1, 0),
        new TranslateTransform(0, 1),
        new TranslateTransform(0, -1)
    };

            foreach (var transform in outlineOffsets)
            {
                TextBlock outlineText = new TextBlock
                {
                    Text = file.Name,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    MaxWidth = 80,
                    Foreground = Brushes.Black
                };
                outlineText.RenderTransform = transform;
                outlineGrid.Children.Add(outlineText);
            }

            // 3. Texto principal en blanco (se usa el tamaño de fuente predeterminado)
            TextBlock mainText = new TextBlock
            {
                Text = file.Name,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                MaxWidth = 80,
                Foreground = Brushes.White
            };

            // Se agregan las capas en el siguiente orden: sombra, contorno y finalmente el texto principal.
            textContainer.Children.Add(shadowGrid);
            textContainer.Children.Add(outlineGrid);
            textContainer.Children.Add(mainText);

            contentStack.Children.Add(iconImage);
            contentStack.Children.Add(textContainer);

            fileBackground.Child = contentStack;
            fileControl.Children.Add(fileBackground);

            return fileControl;
        }





        // Y ajusta el ConfigureImageControlForHighQuality
        public static void ConfigureImageControlForHighQuality(System.Windows.Controls.Image imageControl)
        {
            RenderOptions.SetBitmapScalingMode(imageControl, BitmapScalingMode.HighQuality);
            imageControl.SnapsToDevicePixels = true;
            RenderOptions.SetEdgeMode(imageControl, EdgeMode.Aliased);
            imageControl.Stretch = Stretch.None;
            imageControl.StretchDirection = StretchDirection.Both;

        }
        private ImageSource GetSystemIcon(string path)
        {
            return IconHelper.GetHighQualityIcon(path, CurrentIconSize);
        }

        public void UpdateIcon(FileItem file, ImageSource icon)
        {
            file.Icon = icon;
            foreach (var child in FileContainer.Children)
            {
                if (child is Grid grid && grid.Tag?.ToString() == file.Path)
                {
                    if (grid.Children[0] is Border border &&
                        border.Child is StackPanel stack &&
                        stack.Children.Count > 0 &&
                        stack.Children[0] is Image iconImage)
                    {
                        iconImage.Source = icon;
                    }
                    break;
                }
            }
        }

        // En FenceWindow, modifica las constantes para los tamaños de íconos
        public static class IconSizes
        {
            // Estos son los tamaños estándar de Windows (sin escalar)
            public const int Small = 16;        // Menús contextuales, barra de título
            public const int Medium = 24;       // Barra de tareas, resultados de búsqueda
            public const int Large = 32;        // Anclajes de inicio (escala 100%)
            public const int ExtraLarge = 48;   // Barra de tareas (escala 200%)
            public const int Jumbo = 256;       // Anclajes de inicio (escala 400%)
        }

        // Y luego en la UI, cuando crees los controles de imagen:
        public void UpdateIconSize(int newSize)
        {
            CurrentIconSize = newSize;
            double dpi = IconHelper.GetDpiScaleFactor();
            int physicalSize = (int)(newSize * dpi);  // Tamaño en píxeles físicos

            foreach (var child in FileContainer.Children)
            {
                if (child is Grid grid && grid.DataContext is FileItem file)
                {
                    Image iconImage = FindImageInGrid(grid);
                    if (iconImage != null)
                    {
                        // Obtener un ícono fresco del tamaño correcto
                        // Obtener un ícono o miniatura fresco del tamaño correcto
                        BitmapSource iconSource = null;
                        if (ThumbnailExtractor.IsMediaFile(file.Path))
                        {
                            if (ThumbnailExtractor.IsImageFile(file.Path))
                                iconSource = ThumbnailExtractor.ExtractImageThumbnail(file.Path, newSize);
                            else if (ThumbnailExtractor.IsVideoFile(file.Path))
                                iconSource = ThumbnailExtractor.ExtractVideoThumbnail(file.Path, newSize);
                        }
                        if (iconSource == null)
                        {
                            // Si no es multimedia o falló la extracción, usa el ícono de alta calidad
                            iconSource = IconHelper.GetHighQualityIcon(file.Path, newSize);
                        }

                        if (iconSource != null)
                        {
                            // Configurar imagen para máxima nitidez
                            RenderOptions.SetBitmapScalingMode(iconImage, BitmapScalingMode.NearestNeighbor);

                            // Convertir el tamaño a unidades DI
                            iconImage.Width = iconSource.PixelWidth / dpi;
                            iconImage.Height = iconSource.PixelHeight / dpi;
                            iconImage.Source = iconSource;
                            file.Icon = iconSource;
                        }
                        else
                        {
                            iconImage.Width = newSize;
                            iconImage.Height = newSize;
                        }

                    }
                }
            }
            OnFenceUpdated?.Invoke();
        }

        // Función auxiliar para encontrar el control Image en el Grid
        private Image FindImageInGrid(Grid grid)
        {
            if (grid.Children[0] is Border border &&
                border.Child is StackPanel stack &&
                stack.Children.Count > 0 &&
                stack.Children[0] is Image image)
            {
                return image;
            }
            return null;
        }


        // Refresca la UI del contenedor de archivos
        private void RefreshFileContainer()
        {
            FileContainer.Children.Clear();
            foreach (var file in Files)
            {
                AddFileControl(file);
            }
            OnFenceUpdated?.Invoke();
        }

        // Elimina un archivo del fence (única definición)
        private void RemoveFileFromFence(FileItem file)
        {
            Files.Remove(file);
            UIElement toRemove = null;
            foreach (UIElement element in FileContainer.Children)
            {
                if (element is Grid grid && grid.Tag?.ToString() == file.Path)
                {
                    toRemove = element;
                    break;
                }
            }
            if (toRemove != null)
                FileContainer.Children.Remove(toRemove);
            OnFilesChanged();
        }

        private void OnFilesChanged()
        {
            FilesChanged?.Invoke(this, EventArgs.Empty);
        }

        // Abre el archivo o carpeta en doble clic
        private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string filePath)
            {
                if (File.Exists(filePath) || Directory.Exists(filePath))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al abrir el archivo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        #endregion

        #region Eventos de la Cabecera y Menú Hamburguesa

        // Agrega esto a la declaración de eventos en la parte superior de la clase
        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            // El ratón ha salido completamente de la ventana
            // Ocultar el botón hamburguesa y restaurar el cursor
            HamburgerButton.Opacity = 0;
            this.Cursor = Cursors.Arrow;
        }


        private void TitleContainer_MouseEnter(object sender, MouseEventArgs e)
        {
            // Mostrar el botón hamburguesa al pasar el mouse por la cabecera
            HamburgerButton.Opacity = 1;
            // Cambiar el cursor a "mover" para indicar que se puede arrastrar la ventana
            this.Cursor = Cursors.SizeAll; // Cambiado de Cursors.SizeAll a un cursor más apropiado para mover
        }

        private void TitleContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            // Obtener la posición actual del mouse con respecto a la ventana completa
            Point mousePos = e.GetPosition(this);

            // Obtener los límites del TitleContainer respecto a la ventana
            Rect titleBounds = TitleContainer.TransformToAncestor(this)
                              .TransformBounds(new Rect(0, 0, TitleContainer.ActualWidth, TitleContainer.ActualHeight));

            // Si el mouse está fuera del TitleContainer pero dentro de la ventana
            if (!titleBounds.Contains(mousePos) &&
                mousePos.X >= 0 && mousePos.Y >= 0 &&
                mousePos.X <= this.ActualWidth && mousePos.Y <= this.ActualHeight)
            {
                // Ocultar el botón hamburguesa y restaurar el cursor normal
                HamburgerButton.Opacity = 0;
                this.Cursor = Cursors.Arrow;
            }
            // No necesitamos manejar el caso de cuando sale de la ventana completa
            // ya que eso lo manejará Window_MouseLeave
        }

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            // Mostrar el menú al hacer clic en el botón hamburguesa
            HamburgerMenu.PlacementTarget = HamburgerButton;
            HamburgerMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            HamburgerMenu.IsOpen = true;

            // Suscribirse al evento Closed del menú para restablecer el cursor
            HamburgerMenu.Closed += HamburgerMenu_Closed;
        }

        private void HamburgerMenu_Closed(object sender, RoutedEventArgs e)
        {
            // Limpiar la suscripción al evento para evitar fugas de memoria
            HamburgerMenu.Closed -= HamburgerMenu_Closed;

            // Verificar la posición actual del ratón
            Point mousePosition = Mouse.GetPosition(this);
            Rect titleBounds = TitleContainer.TransformToAncestor(this)
                              .TransformBounds(new Rect(0, 0, TitleContainer.ActualWidth, TitleContainer.ActualHeight));

            // Actualizar el cursor según la posición actual del ratón
            if (!titleBounds.Contains(mousePosition))
            {
                this.Cursor = Cursors.Arrow;
                HamburgerButton.Opacity = 0;
            }
            else
            {
                this.Cursor = Cursors.SizeAll;
                HamburgerButton.Opacity = 1;
            }
        }

        private void AbrirRaiz_Click(object sender, RoutedEventArgs e)
        {
            // Abrir la carpeta raíz del fence en el explorador de Windows
            try
            {
                if (Directory.Exists(FolderPath))
                {
                    Process.Start(new ProcessStartInfo(FolderPath) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("La carpeta raíz no existe", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir la carpeta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Drag & Drop y Ordenamiento

        private void FileControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is Grid control && e.LeftButton == MouseButtonState.Pressed)
            {
                if (control.DataContext is FileItem file)
                {
                    DataObject data = new DataObject("FENCE_FILE", file);
                    DragDrop.DoDragDrop(control, data, DragDropEffects.Move);
                }
            }
        }

        private void FileControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                FileList_MouseDoubleClick(sender, e);
                e.Handled = true;
            }
        }

        private void FileControl_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Grid control && control.Children.Count > 0 && control.Children[0] is Border border)
            {
                border.Background = new SolidColorBrush(Color.FromArgb(128, 153, 153, 153));
                control.Cursor = Cursors.Hand;
            }
        }

        private void FileControl_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Grid control && control.Children.Count > 0 && control.Children[0] is Border border)
            {
                border.Background = Brushes.Transparent;
                control.Cursor = Cursors.Arrow;
            }
        }

        private void FileContainer_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("FENCE_FILE"))
            {
                var droppedItem = e.Data.GetData("FENCE_FILE") as FileItem;
                if (droppedItem == null)
                    return;

                Point dropPosition = e.GetPosition(FileContainer);
                UIElement targetElement = null;
                foreach (UIElement element in FileContainer.Children)
                {
                    var transform = element.TransformToAncestor(FileContainer);
                    Rect bounds = transform.TransformBounds(new Rect(new Point(0, 0), element.RenderSize));
                    if (bounds.Contains(dropPosition))
                    {
                        targetElement = element;
                        break;
                    }
                }

                UIElement draggedElement = null;
                foreach (UIElement element in FileContainer.Children)
                {
                    if (element is Grid grid && grid.Tag?.ToString() == droppedItem.Path)
                    {
                        draggedElement = element;
                        break;
                    }
                }

                if (targetElement != null && draggedElement != null && targetElement != draggedElement)
                {
                    int targetIndex = FileContainer.Children.IndexOf(targetElement);
                    FileContainer.Children.Remove(draggedElement);
                    FileContainer.Children.Insert(targetIndex, draggedElement);
                }
                else if (draggedElement != null)
                {
                    int targetIndex = 0;
                    foreach (UIElement element in FileContainer.Children)
                    {
                        Point pos = element.TranslatePoint(new Point(0, 0), FileContainer);
                        if (dropPosition.X > pos.X)
                            targetIndex++;
                    }
                    FileContainer.Children.Remove(draggedElement);
                    FileContainer.Children.Insert(Math.Min(targetIndex, FileContainer.Children.Count), draggedElement);
                }
            }
        }

        private void FileList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] items = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                foreach (var item in items)
                {
                    string itemName = Path.GetFileName(item);
                    string destination = Path.Combine(FolderPath, itemName);
                    try
                    {
                        if (Directory.Exists(item))
                            Directory.Move(item, destination);
                        else if (File.Exists(item))
                            File.Move(item, destination);
                        else
                            throw new Exception("El elemento no es un archivo ni una carpeta válida.");
                        AddFile(destination);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al mover: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            OnFilesChanged();
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                FileList_Drop(sender, e);
                return;
            }
            else if (e.Data.GetDataPresent("FENCE_FILE"))
            {
                var droppedItem = e.Data.GetData("FENCE_FILE") as FileItem;
                if (droppedItem == null)
                    return;

                Point dropPosWindow = e.GetPosition(this);
                double tolerance = 20;
                if (dropPosWindow.X > tolerance && dropPosWindow.Y > tolerance &&
                    dropPosWindow.X < this.ActualWidth - tolerance &&
                    dropPosWindow.Y < this.ActualHeight - tolerance)
                {
                    FileContainer_Drop(sender, e);
                }
                else
                {
                    RemoveFileFromFence(droppedItem);
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string destination = Path.Combine(desktopPath, Path.GetFileName(droppedItem.Path));
                    try
                    {
                        if (Directory.Exists(droppedItem.Path))
                            Directory.Move(droppedItem.Path, destination);
                        else if (File.Exists(droppedItem.Path))
                            File.Move(droppedItem.Path, destination);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al mover al escritorio: {ex.Message}");
                    }
                }
            }
            OnFilesChanged();
        }

        private void SoltarArchivos_Click(object sender, RoutedEventArgs e)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            // Crear una copia de la lista para evitar modificarla mientras se itera
            var itemsToRelease = Files.ToList();

            foreach (var file in itemsToRelease)
            {
                // Remover el archivo de la UI y de la lista
                RemoveFileFromFence(file);

                // Forzar la liberación de recursos gráficos (esto puede ayudar a soltar cualquier bloqueo)
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Construir la ruta de destino y asegurarse de que sea única
                string destination = Path.Combine(desktopPath, Path.GetFileName(file.Path));
                destination = GetUniqueDestination(destination);

                try
                {
                    if (Directory.Exists(file.Path))
                        Directory.Move(file.Path, destination);
                    else if (File.Exists(file.Path))
                        File.Move(file.Path, destination);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al mover {file.Name} al escritorio: {ex.Message}");
                }
            }
            OnFilesChanged();
        }

        // Método auxiliar para obtener un nombre de destino único en caso de conflicto
        private string GetUniqueDestination(string destination)
        {
            string dir = Path.GetDirectoryName(destination);
            string filename = Path.GetFileNameWithoutExtension(destination);
            string extension = Path.GetExtension(destination);
            int count = 1;
            while (File.Exists(destination) || Directory.Exists(destination))
            {
                destination = Path.Combine(dir, $"{filename}({count}){extension}");
                count++;
            }
            return destination;
        }


        #endregion

        #region Ordenamiento de Archivos

        private void OrdenarPorNombre_Click(object sender, RoutedEventArgs e)
        {
            Files = Files.OrderBy(f => f.Name).ToList();
            RefreshFileContainer();
        }

        private void OrdenarPorFecha_Click(object sender, RoutedEventArgs e)
        {
            Files = Files.OrderBy(f => File.GetLastWriteTime(f.Path)).ToList();
            RefreshFileContainer();
        }

        private void OrdenarPorTipo_Click(object sender, RoutedEventArgs e)
        {
            Files = Files.OrderBy(f => f.IsDirectory ? "0" : Path.GetExtension(f.Path))
                         .ThenBy(f => f.Name)
                         .ToList();
            RefreshFileContainer();
        }

        #endregion

        #region Otros Eventos

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        #endregion

        #region Métodos API

        [StructLayout(LayoutKind.Sequential)]
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

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, int dwFileAttributes,
            ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_LARGEICON = 0x000000000;

        #endregion
    }





    // Modelo FileItem
    public class FileItem
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public bool IsDirectory { get; set; }
        public ImageSource Icon { get; set; }
    }
}