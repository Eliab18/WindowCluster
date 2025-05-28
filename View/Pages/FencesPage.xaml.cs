using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using FencesApp.Helpers;
using FencesApp.Models;

namespace FencesApp.Pages
{
    public partial class FencesPage : Page
    {
        // Colección de fences activos
        // private static readonly List<FenceWindow> fences = new List<FenceWindow>();
        private const string configPath = "Resources/fences.json";

        // Tamaños físicos deseados (en píxeles) según lo que Windows usa
        private const int DESIRED_SMALL_ICON_SIZE = 16;
        private const int DESIRED_MEDIUM_ICON_SIZE = 32;
        private const int DESIRED_LARGE_ICON_SIZE = 48;

        // Propiedades que calculan el tamaño en DI (device-independent) en función del DPI actual.
        // Por ejemplo, si el sistema está en 125% (factor 1.25), entonces:
        // SmallIconSize = 16/1.25 ≈ 13, MediumIconSize = 32/1.25 ≈ 26, LargeIconSize = 48/1.25 ≈ 38.
        private int SmallIconSize => (int)Math.Round((double)DESIRED_SMALL_ICON_SIZE / GetDpiScaleFactor());
        private int MediumIconSize => (int)Math.Round((double)DESIRED_MEDIUM_ICON_SIZE / GetDpiScaleFactor());
        private int LargeIconSize => (int)Math.Round((double)DESIRED_LARGE_ICON_SIZE / GetDpiScaleFactor());

        // Tamaño seleccionado de ícono (inicialmente mediano)
        private int selectedIconSize;
        // Font method
        private IEnumerable<dynamic> allFontItems;

        public FencesPage()
        {
            InitializeComponent();
            this.IsVisibleChanged += FencesPage_IsVisibleChanged; // Suscribirse al cambio de visibilidad
                                                                  // Inicializamos el tamaño seleccionado al mediano calculado
            selectedIconSize = LargeIconSize;
            InitializeIconSizes();
            Loaded += FencesPage_Loaded;
            LoadFences();
            UpdateIconSizeUI();
            LoadFontFamilies();
            SubscribeToFenceEvents(); // Suscribir eventos al iniciar
        }

        private void FencesPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                SubscribeToFenceEvents(); // Re-suscribir los eventos al volver a la vista

                // Actualizar la UI después de un breve retraso para asegurar que todos los controles están cargados
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                {
                    // Recargar la selección actual
                    if (FenceSelector.SelectedItem != null)
                    {
                        int currentIndex = FenceSelector.SelectedIndex;
                        FenceSelector.SelectedIndex = -1;
                        FenceSelector.SelectedIndex = currentIndex;
                    }

                    // Actualizar explícitamente los radio buttons
                    UpdateTitleRadioButtons();

                    // Actualizar otros elementos visuales
                    UpdateDeleteButtonVisibility();
                }));
            }
        }


        // Método para re-suscribir el evento FilesChanged de cada FenceWindow
        private void SubscribeToFenceEvents()
        {
            foreach (var fence in FenceManager.Fences)
            {
                // Primero desuscribimos para evitar múltiples suscripciones
                fence.FilesChanged -= Fence_FilesChanged;
                fence.FilesChanged += Fence_FilesChanged;
            }
        }

        public void RefreshPage()
        {
            // Recargar la selección actual para actualizar el estado
            if (FenceSelector.SelectedItem != null)
            {
                int currentIndex = FenceSelector.SelectedIndex;
                FenceSelector.SelectedIndex = -1;
                FenceSelector.SelectedIndex = currentIndex;
            }

            // Forzar actualización con alta prioridad
            Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
            {
                UpdateDeleteButtonVisibility();
                UpdateTitleRadioButtons();
            }));
        }

        private void FencesPage_Loaded(object sender, RoutedEventArgs e)
        {
            TitleExpander.UpdateLayout();
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(UpdateTitleRadioButtons));
        }

        /// <summary>
        /// Obtiene el factor DPI de la ventana principal. Por ejemplo, 1.25 si la escala es 125%.
        /// </summary>
        /// <returns>Factor DPI</returns>
        private double GetDpiScaleFactor()
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

        #region Inicialización y Carga de Fences

        private void InitializeIconSizes()
        {
            if (IconSizePanel != null)
            {
                IconSizePanel.Children.Clear();

                // Agregar el Label para el título
                Label titleLabel = new Label
                {
                    Margin = new Thickness(5, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Vincular a recurso dinámico "IconSizeTitle"
                titleLabel.SetResourceReference(ContentControl.ContentProperty, "IconSizeTitle");

                // Si estás usando un estilo específico para los labels
                titleLabel.SetResourceReference(FrameworkElement.StyleProperty, "ModernLabelStyle");

                // Agregar el label al panel
                IconSizePanel.Children.Add(titleLabel);

                // Agregar los radio buttons
                AddDynamicRadioButton(IconSizePanel, "IconSizeSmall", SmallIconSize);
                AddDynamicRadioButton(IconSizePanel, "IconSizeMedium", MediumIconSize);
                AddDynamicRadioButton(IconSizePanel, "IconSizeLarge", LargeIconSize);
            }
        }

        private void AddDynamicRadioButton(StackPanel panel, string resourceKey, int tagValue)
        {
            RadioButton rb = new RadioButton
            {
                Tag = tagValue,
                Margin = new Thickness(5, 0, 5, 0)
            };

            // Vincular el texto dinámicamente a un recurso
            rb.SetResourceReference(ContentControl.ContentProperty, resourceKey);

            // Aplicar estilo si lo estás usando
            rb.SetResourceReference(FrameworkElement.StyleProperty, "ModernRadioButtonStyle");

            rb.Checked += IconSize_Changed;
            panel.Children.Add(rb);
        }
        private void AddRadioButton(StackPanel panel, string content, int tagValue)
        {
            RadioButton rb = new RadioButton
            {
                Content = content,
                Tag = tagValue,
                Margin = new Thickness(5, 0, 5, 0)
            };
            rb.Checked += IconSize_Changed;
            panel.Children.Add(rb);
        }

        private void LoadFences()
        {
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                try
                {
                    AppConfiguration config = JsonSerializer.Deserialize<AppConfiguration>(json);
                    // Si el valor guardado coincide con alguno de los tamaños físicos deseados, convertimos a DI:
                    if (config.IconSize == DESIRED_SMALL_ICON_SIZE ||
                        config.IconSize == DESIRED_MEDIUM_ICON_SIZE ||
                        config.IconSize == DESIRED_LARGE_ICON_SIZE)
                    {
                        double dpi = GetDpiScaleFactor();
                        selectedIconSize = (int)Math.Round((double)config.IconSize / dpi);
                    }
                    else
                    {
                        selectedIconSize = MediumIconSize;
                    }
                    LoadFencesFromConfig(config.Fences);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar la configuración: {ex.Message}",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                UpdateIconSizes();
            }
            FenceSelector.Items.Clear();
            foreach (var fence in FenceManager.Fences)
            {
                if (!FenceSelector.Items.Contains(fence.Title))
                    FenceSelector.Items.Add(fence.Title);
            }
            if (FenceSelector.Items.Count > 0)
                FenceSelector.SelectedIndex = 0;
            UpdateIconSizeUI();

        }

        private void LoadFencesFromConfig(List<FenceData> fenceDataList)
        {
            foreach (var fenceData in fenceDataList)
            {
                if (!FenceManager.Fences.Exists(f => f.Title == fenceData.Title))
                {
                    // Crear el fence con valores explícitos desde la configuración
                    FenceWindow loadedFence = new FenceWindow(fenceData.Title, fenceData.FolderPath, fenceData.Color)
                    {
                        Left = fenceData.PositionX,
                        Top = fenceData.PositionY,
                        TitleTextColor = fenceData.TitleTextColor ?? "#FFFFFFFF",
                        TitleAlignment = fenceData.TitleAlignment ?? "Center",
                        TitleFontSize = fenceData.TitleFontSize > 0 ? fenceData.TitleFontSize : 20,
                        TitleDesignType = fenceData.TitleDesignType ?? "default",
                        TitleBackgroundColor = fenceData.TitleBackgroundColor ?? "#FF333333"
                    };

                    // Aplicar explícitamente los valores a la ventana
                    loadedFence.UpdateTitleTextColor(loadedFence.TitleTextColor);
                    loadedFence.TitleFontFamily = fenceData.TitleFontFamily ?? "Segoe UI";
                    loadedFence.UpdateTitleFontFamily(loadedFence.TitleFontFamily);
                    loadedFence.UpdateTitleAlignment(loadedFence.TitleAlignment);
                    loadedFence.UpdateTitleFontSize(loadedFence.TitleFontSize);
                    loadedFence.UpdateTitleDesignType(loadedFence.TitleDesignType, loadedFence.TitleBackgroundColor);
                    loadedFence.FilesChanged += Fence_FilesChanged;
                    loadedFence.OnFenceUpdated += SaveFences;
                    FenceManager.Fences.Add(loadedFence);
                    loadedFence.Show();

                    // Seleccionar el fence cargado en la UI
                    Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                    {
                        if (FenceSelector.Items.Contains(loadedFence.Title))
                        {
                            FenceSelector.SelectedItem = loadedFence.Title;
                            FenceSelector_SelectionChanged(FenceSelector, null);
                        }
                    }));
                }
            }

            // Actualizar los RadioButtons después de cargar todos los fences
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(UpdateTitleRadioButtons));
        }

        private void Fence_FilesChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (FenceSelector.SelectedItem != null)
                {
                    // Obtener el fence seleccionado actualmente
                    string selectedFenceName = FenceSelector.SelectedItem.ToString();
                    var selectedFence = FenceManager.Fences.FirstOrDefault(f => f.Title == selectedFenceName);

                    // Si el evento provino del fence seleccionado actualmente, actualizar UI
                    if (selectedFence != null && sender == selectedFence)
                    {
                        // Verificar si tiene archivos
                        bool hasFiles = selectedFence.Files.Count > 0;

                        // Actualizar visibilidad del mensaje y botón
                        DeleteWarningText.Visibility = hasFiles ? Visibility.Visible : Visibility.Collapsed;
                        DeleteFenceButton.Visibility = hasFiles ? Visibility.Collapsed : Visibility.Visible;
                    }

                    // Forzar actualización de la UI
                    FenceSelector_SelectionChanged(FenceSelector, null);
                }
            });
        }

        #endregion

        #region Icon y Configuración Visual

        private void UpdateIconSizeUI()
        {
            if (IconSizePanel != null)
            {
                foreach (RadioButton rb in IconSizePanel.Children.OfType<RadioButton>())
                {
                    if (rb.Tag != null && int.TryParse(rb.Tag.ToString(), out int tagSize))
                        rb.IsChecked = (tagSize == selectedIconSize);
                }
            }
        }

        private void IconSize_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && int.TryParse(rb.Tag.ToString(), out int newSize))
            {
                selectedIconSize = newSize;
                UpdateIconSizes();
            }
        }

        private void UpdateIconSizes()
        {
            foreach (var fence in FenceManager.Fences)
            {
                fence.CurrentIconSize = selectedIconSize;
                foreach (var file in fence.Files)
                {
                    var icon = IconHelper.GetHighQualityIcon(file.Path, selectedIconSize);
                    if (icon != null)
                        fence.UpdateIcon(file, icon);
                }
                fence.UpdateIconSize(selectedIconSize);
            }
            SaveFences();
        }


        private void LoadFontFamilies()
        {
            FontFamilySelector.Items.Clear();

            // Lista para almacenar todos los tipos de fuentes con sus variantes
            var fontList = new List<object>();

            foreach (var family in Fonts.SystemFontFamilies.OrderBy(f => f.Source))
            {
                // Agregamos la familia principal
                fontList.Add(new
                {
                    Source = family.Source,
                    FontFamily = new FontFamily(family.Source),
                    DisplayName = family.Source,
                    IsVariant = false,
                    Weight = FontWeights.Normal,
                    Style = FontStyles.Normal
                });

                // Buscamos las variantes de la familia
                var typefaces = family.GetTypefaces();
                foreach (var typeface in typefaces)
                {
                    // Obtenemos peso, estilo e inclinación
                    FontWeight weight = typeface.Weight;
                    FontStyle style = typeface.Style;
                    FontStretch stretch = typeface.Stretch;

                    // Solo agregamos variantes significativas
                    if (weight != FontWeights.Normal || style != FontStyles.Normal || stretch != FontStretches.Normal)
                    {
                        // Crear un nombre descriptivo para la variante
                        string variantDescription = "";

                        if (weight != FontWeights.Normal)
                        {
                            // Convertir el peso numérico a un nombre descriptivo
                            if (weight == FontWeights.Bold) variantDescription += "Bold";
                            else if (weight == FontWeights.Light) variantDescription += "Light";
                            else if (weight == FontWeights.SemiBold) variantDescription += "SemiBold";
                            else if (weight == FontWeights.Black) variantDescription += "Black";
                            else if (weight == FontWeights.ExtraBold) variantDescription += "ExtraBold";
                            else if (weight == FontWeights.ExtraLight) variantDescription += "ExtraLight";
                            else if (weight == FontWeights.Medium) variantDescription += "Medium";
                            else if (weight == FontWeights.Thin) variantDescription += "Thin";
                            else variantDescription += $"W{weight.ToOpenTypeWeight()}"; // Usar valor numérico si no hay nombre común
                        }

                        if (style == FontStyles.Italic)
                        {
                            if (!string.IsNullOrEmpty(variantDescription))
                                variantDescription += " ";
                            variantDescription += "Italic";
                        }
                        else if (style == FontStyles.Oblique)
                        {
                            if (!string.IsNullOrEmpty(variantDescription))
                                variantDescription += " ";
                            variantDescription += "Oblique";
                        }

                        if (stretch != FontStretches.Normal && stretch != FontStretches.Medium)
                        {
                            if (!string.IsNullOrEmpty(variantDescription))
                                variantDescription += " ";

                            if (stretch == FontStretches.Condensed) variantDescription += "Condensed";
                            else if (stretch == FontStretches.Expanded) variantDescription += "Expanded";
                            else if (stretch == FontStretches.ExtraCondensed) variantDescription += "ExtraCondensed";
                            else if (stretch == FontStretches.ExtraExpanded) variantDescription += "ExtraExpanded";
                            else if (stretch == FontStretches.SemiCondensed) variantDescription += "SemiCondensed";
                            else if (stretch == FontStretches.SemiExpanded) variantDescription += "SemiExpanded";
                            else if (stretch == FontStretches.UltraCondensed) variantDescription += "UltraCondensed";
                            else if (stretch == FontStretches.UltraExpanded) variantDescription += "UltraExpanded";
                        }

                        if (!string.IsNullOrEmpty(variantDescription))
                        {
                            string fullName = $"{family.Source} {variantDescription}";

                            fontList.Add(new
                            {
                                Source = fullName,
                                FontFamily = family,
                                DisplayName = fullName,
                                IsVariant = true,
                                Weight = weight,
                                Style = style,
                                Stretch = stretch
                            });
                        }
                    }
                }
            }

            // Store the complete font list
            allFontItems = fontList;



            // Set the initial ItemsSource
            FontFamilySelector.ItemsSource = allFontItems;


        }


        // Add the TextChanged event handler for the search box
        private void FontSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = FontSearchBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                // If search is empty, show all fonts
                FontFamilySelector.ItemsSource = allFontItems;
            }
            else
            {
                // Filter fonts based on search text
                var filteredItems = allFontItems.Where(item =>
                    ((dynamic)item).DisplayName.ToString().ToLower().Contains(searchText));

                FontFamilySelector.ItemsSource = filteredItems;
            }

            // If there are filtered results and search is not empty, open the dropdown
            if (!string.IsNullOrEmpty(searchText) && FontFamilySelector.Items.Count > 0)
            {
                FontFamilySelector.IsDropDownOpen = true;
            }
        }

        private void FontFamilySelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FontFamilySelector.SelectedItem == null) return;

            dynamic selectedFont = FontFamilySelector.SelectedItem;
            string displayName = selectedFont.DisplayName;

            var selectedFence = FenceManager.Fences.FirstOrDefault(f => f.Title == FenceSelector.SelectedItem?.ToString());

            if (selectedFence != null)
            {
                selectedFence.UpdateTitleFontFamily(displayName);
                SaveFences();
            }
        }

        private void UpdateTitleRadioButtons()
        {
            var selectedFence = FenceManager.Fences.FirstOrDefault(f => f.Title == FenceSelector.SelectedItem?.ToString());
            if (selectedFence != null)
            {
                // Usar BeginInvoke con prioridad alta para asegurar que se aplican los cambios
                Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
                {
                    // Alineación
                    foreach (RadioButton rb in FindRadioButtonsByGroupName(TitleExpander, "TitleAlignment"))
                    {
                        rb.IsChecked = (rb.Tag?.ToString() == selectedFence.TitleAlignment);
                    }

                    // Tamaño
                    foreach (RadioButton rb in FindRadioButtonsByGroupName(TitleExpander, "TitleFontSize"))
                    {
                        rb.IsChecked = (rb.Tag?.ToString() == selectedFence.TitleFontSize.ToString());
                    }

                    // Diseño - Modificado para usar el TitleDesignType almacenado
                    foreach (RadioButton rb in FindRadioButtonsByGroupName(TitleExpander, "TitleDesign"))
                    {
                        rb.IsChecked = (rb.Tag?.ToString() == selectedFence.TitleDesignType);
                    }

                    // Actualizar la visibilidad del panel de configuración de fondo
                    if (TitleBackgroundSettings != null)
                    {
                        TitleBackgroundSettings.Visibility =
                            (selectedFence.TitleDesignType == "etiqueta") ? Visibility.Visible : Visibility.Collapsed;
                    }
                }));
            }
            // Fuente
            if (selectedFence != null)
            {
                var fontFamily = selectedFence.TitleFontFamily;
                FontFamilySelector.SelectedItem = FontFamilySelector.Items
                    .OfType<dynamic>()
                    .FirstOrDefault(f => f.Source == fontFamily);
            }
        }

        private IEnumerable<RadioButton> FindRadioButtonsByGroupName(DependencyObject parent, string groupName)
        {
            if (parent != null)
            {
                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                    if (child is RadioButton rb && rb.GroupName == groupName)
                        yield return rb;
                    foreach (var descendant in FindRadioButtonsByGroupName(child, groupName))
                        yield return descendant;
                }
            }
        }
        #endregion

        #region Eventos de Renombrado y Cambio de Color

        // Método para renombrar el fence
        private void RenameFence_Click(object sender, RoutedEventArgs e)
        {
            string newName = FenceNameInput.Text.Trim();
            if (!IsValidFenceName(newName))
            {
                MessageBox.Show("El nombre solo puede contener letras y números.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string oldName = FenceSelector.SelectedItem?.ToString();
            var selectedFence = FenceManager.Fences.FirstOrDefault(f => f.Title == oldName);
            if (selectedFence != null)
            {
                string newPath = Path.Combine("Fences", newName);
                if (!Directory.Exists(newPath))
                {
                    Directory.Move(selectedFence.FolderPath, newPath);
                    selectedFence.UpdateTitle(newName);
                    selectedFence.UpdateFolderPath(newPath);
                    int currentIndex = FenceSelector.SelectedIndex;
                    FenceSelector.Items[currentIndex] = newName;
                    SaveFences();
                    // Forzar actualización de la UI
                    FenceSelector.SelectedIndex = -1;
                    FenceSelector.SelectedIndex = currentIndex;
                    FenceSelector_SelectionChanged(FenceSelector, null);
                }
                else
                {
                    MessageBox.Show("Ya existe un Fence con este nombre.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private bool IsValidFenceName(string name)
        {
            return Regex.IsMatch(name, @"^[a-zA-Z0-9\s]+$");
        }

        // Método para cambiar el color del fence (combina color y opacidad)
        private void ChangeFenceColor_Click(object sender, RoutedEventArgs e)
        {
            if (FenceSelector.SelectedItem == null) return;
            var selectedFence = FenceManager.Fences.FirstOrDefault(f => f.Title == FenceSelector.SelectedItem.ToString());
            if (selectedFence == null) return;

            string selectedColor = GetSelectedColor();
            string selectedOpacity = (OpacitySelector.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "FF";
            if (selectedColor.Length == 7)
                selectedColor = $"#{selectedOpacity}{selectedColor.Substring(1)}";
            selectedFence.UpdateColor(selectedColor);
            SaveFences();
            FenceSelector_SelectionChanged(FenceSelector, null);
        }

        private string GetSelectedColor()
        {
            string customColor = CustomColorInput.Text.Trim();
            if (!string.IsNullOrEmpty(customColor) && Regex.IsMatch(customColor, "^[A-Fa-f0-9]{6}$"))
                return "#" + customColor;
            else if (ColorSelector.SelectedItem != null)
                return (ColorSelector.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "#545e75";
            else
            {
                var selectedFence = FenceManager.Fences.FirstOrDefault(f => f.Title == FenceSelector.SelectedItem?.ToString());
                if (selectedFence != null && !string.IsNullOrEmpty(selectedFence.FenceColor) && selectedFence.FenceColor.Length == 9)
                    return "#" + selectedFence.FenceColor.Substring(3);
                return "#545e75";
            }
        }

        // Aplica el color personalizado del fondo del título (para estilo 'etiqueta')
        private void ApplyTitleBgColor_Click(object sender, RoutedEventArgs e)
        {
            string selectedBgColor = (TitleBgColorSelector.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "#FF333333";
            string customBgColor = CustomTitleBgColorInput.Text.Trim();

            if (!string.IsNullOrEmpty(customBgColor))
            {
                if (Regex.IsMatch(customBgColor, "^[A-Fa-f0-9]{6}$"))
                {
                    // Agregar la opacidad para el valor interno
                    selectedBgColor = "#FF" + customBgColor.ToUpper();

                    // Para mostrar en la UI, usar solo el código de 6 caracteres
                    string displayColor = "#" + customBgColor.ToUpper();

                    // Actualizar el ComboBox mostrando solo el código de 6 caracteres
                    UpdateColorSelector(TitleBgColorSelector, selectedBgColor, "Hex Color - " + displayColor);
                }
                else
                {
                    MessageBox.Show("El color personalizado del fondo del título debe ser un código hexadecimal válido (Ej: FFAABB).",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var selectedFence = FenceManager.Fences.FirstOrDefault(f => f.Title == FenceSelector.SelectedItem?.ToString());
            if (selectedFence != null)
            {
                // Actualizar el diseño a "etiqueta" y el color de fondo
                selectedFence.UpdateTitleDesignType("etiqueta", selectedBgColor);

                // Forzar la actualización de los radio buttons
                Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
                {
                    // Marcar el RadioButton de etiqueta como seleccionado
                    foreach (RadioButton rb in FindRadioButtonsByGroupName(TitleExpander, "TitleDesign"))
                    {
                        rb.IsChecked = (rb.Tag?.ToString() == "etiqueta");
                    }

                    // Asegurar que el panel de configuración de fondo sea visible
                    TitleBackgroundSettings.Visibility = Visibility.Visible;
                }));

                SaveFences();
            }
        }

        // Aplica el color del texto del título
        private void ApplyTitleColor_Click(object sender, RoutedEventArgs e)
        {
            string selectedColor = (TitleColorSelector.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "#FFFFFFFF";
            string customColor = CustomTitleColorInput.Text.Trim();

            if (!string.IsNullOrEmpty(customColor))
            {
                if (Regex.IsMatch(customColor, "^[A-Fa-f0-9]{6}$"))
                {
                    // Agregar la opacidad para el valor interno
                    selectedColor = "#FF" + customColor.ToUpper();

                    // Para mostrar en la UI, usar el código de 6 caracteres con #
                    string displayText = "Hex Color - #" + customColor.ToUpper();

                    // Actualizar el ComboBox con el texto de visualización explícito
                    UpdateColorSelector(TitleColorSelector, selectedColor, displayText);
                }
                else
                {
                    MessageBox.Show("El color personalizado debe ser un código hexadecimal válido (Ej: FFFFFF).",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var selectedFence = FenceManager.Fences.FirstOrDefault(f => f.Title == FenceSelector.SelectedItem?.ToString());
            if (selectedFence != null)
            {
                selectedFence.UpdateTitleTextColor(selectedColor);
                SaveFences();
            }
        }

        // Evento para cambiar el tamaño del título (radio buttons)
        private void TitleFontSize_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag != null && int.TryParse(rb.Tag.ToString(), out int fontSize))
            {
                var selectedFence = FenceManager.Fences.FirstOrDefault(f => f.Title == FenceSelector.SelectedItem?.ToString());
                if (selectedFence != null)
                {
                    selectedFence.UpdateTitleFontSize(fontSize);
                    SaveFences();
                }
            }
        }

        // Evento para cambiar el diseño del título (default o etiqueta)
        private void TitleDesign_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag != null)
            {
                string design = rb.Tag.ToString();
                var selectedFence = FenceManager.Fences.FirstOrDefault(f => f.Title == FenceSelector.SelectedItem?.ToString());
                if (selectedFence != null)
                {
                    // Actualizar la visibilidad de las configuraciones de fondo
                    TitleBackgroundSettings.Visibility = (design == "etiqueta") ? Visibility.Visible : Visibility.Collapsed;

                    // Si cambiamos a etiqueta, inicializar el selector de color
                    if (design == "etiqueta")
                    {
                        Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
                        {
                            if (TitleBgColorSelector != null && TitleBgColorSelector.Items.Count > 0)
                            {
                                // Verificar si hay un color de fondo personalizado guardado
                                string savedBgColor = selectedFence.TitleBackgroundColor;

                                // Si no hay color guardado o es el color por defecto, seleccionar el primer ítem
                                if (string.IsNullOrEmpty(savedBgColor) || savedBgColor == "#FF333333")
                                {
                                    TitleBgColorSelector.SelectedIndex = 0;
                                }
                                else
                                {
                                    // Intentar encontrar el color en los ítems del ComboBox
                                    bool found = false;
                                    foreach (ComboBoxItem item in TitleBgColorSelector.Items)
                                    {
                                        if (item.Tag.ToString() == savedBgColor)
                                        {
                                            TitleBgColorSelector.SelectedItem = item;
                                            found = true;
                                            break;
                                        }
                                    }

                                    // Si no se encontró, mostrar el código hex
                                    if (!found)
                                    {
                                        TitleBgColorSelector.SelectedIndex = -1;
                                        CustomTitleBgColorInput.Text = savedBgColor.StartsWith("#") ?
                                            savedBgColor.Substring(1) : savedBgColor;
                                    }
                                }

                                TitleBgColorSelector.UpdateLayout();
                            }
                        }));
                    }

                    // Actualizar el tipo de diseño y guardar
                    selectedFence.UpdateTitleDesignType(design,
                        design == "etiqueta" ? selectedFence.TitleBackgroundColor : null);
                    SaveFences();
                }
            }
        }



        private void CreateFence_Click(object sender, RoutedEventArgs e)
        {
            string newFenceText = (string)FindResource("NewFence");
            string fenceName = $"{newFenceText} {(FenceManager.Fences.Count + 1)}";
            string fencePath = Path.Combine("Fences", fenceName);
            Directory.CreateDirectory(fencePath);

            // Definir valores por defecto explícitos
            FenceWindow newFence = new FenceWindow(fenceName, fencePath)
            {
                TitleAlignment = "Center",        // Alineación por defecto: Left
                TitleDesignType = "default",    // Diseño por defecto: default
                TitleFontSize = 20,             // Tamaño por defecto: 20 (medium)
                TitleTextColor = "#FFFFFFFF",   // Color por defecto: blanco
                TitleBackgroundColor = "#FF333333" // Fondo de etiqueta por defecto
            };

            newFence.FilesChanged += Fence_FilesChanged;
            newFence.OnFenceUpdated += FenceManager.SaveFences;  // Enlaza al método global
            FenceManager.Fences.Add(newFence);
            newFence.Show();

            // Actualiza el combobox
            FenceSelector.Items.Add(newFence.Title);
            FenceSelector.SelectedItem = newFence.Title;


            TitleExpander.Visibility = Visibility.Visible;
            if (FindName("WindowExpander") is Expander windowExpander)
                windowExpander.Visibility = Visibility.Visible;

            // Actualizar la UI con un ligero retraso para asegurar que los controles estén cargados
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                UpdateTitleRadioButtons();
            }));

            SaveFences();
        }

        // Agrega estos manejadores de eventos a tu clase FencesPage
        private void TitleExpander_Expanded(object sender, RoutedEventArgs e)
        {
            if (TitleExpander.IsExpanded)
            {
                WindowExpander.IsExpanded = false;
                Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(UpdateTitleRadioButtons));
            }
        }

        private void WindowExpander_Expanded(object sender, RoutedEventArgs e)
        {
            // Si el expander de ventana se expande, colapsa el de título
            if (WindowExpander.IsExpanded)
            {
                TitleExpander.IsExpanded = false;
            }
        }

        private void UpdateExpandersVisibility()
        {
            bool shouldShowExpanders = FenceManager.Fences.Count > 0 || FenceSelector.Items.Count > 0;

            Visibility visibilityState = shouldShowExpanders ? Visibility.Visible : Visibility.Collapsed;

            // Actualizar visibilidad de los expanders
            TitleExpander.Visibility = visibilityState;

            // Para el segundo expander (ajusta según cómo lo refieras en tu código)
            if (FindName("WindowExpander") is Expander windowExpander)
            {
                windowExpander.Visibility = visibilityState;
            }
            // O la alternativa que estés usando
        }



        private void UpdateDeleteButtonVisibility()
        {
            if (FenceSelector.SelectedItem != null)
            {
                string selectedFenceName = FenceSelector.SelectedItem.ToString();
                var selectedFence = FenceManager.Fences.FirstOrDefault(f => f.Title == selectedFenceName);

                if (selectedFence != null)
                {
                    bool hasFiles = selectedFence.Files.Count > 0;
                    DeleteWarningText.Visibility = hasFiles ? Visibility.Visible : Visibility.Collapsed;
                    DeleteFenceButton.Visibility = hasFiles ? Visibility.Collapsed : Visibility.Visible;
                }
            }
        }
        private void DeleteFence_Click(object sender, RoutedEventArgs e)
        {
            if (FenceSelector.SelectedItem == null) return;

            string selectedFenceName = FenceSelector.SelectedItem.ToString();

            var selectedFence = FenceManager.Fences.FirstOrDefault(f => f.Title == selectedFenceName);


            if (selectedFence != null)
            {
                // Verificar en tiempo real si el fence tiene archivos
                bool hasFiles = selectedFence.Files.Count > 0;

                if (hasFiles)
                {
                    // Mostrar advertencia si hay archivos
                    DeleteWarningText.Visibility = Visibility.Visible;
                    DeleteFenceButton.Visibility = Visibility.Collapsed;
                    return; // Salir del método sin eliminar
                }
                else
                {
                    // Ocultar advertencia si no hay archivos
                    DeleteWarningText.Visibility = Visibility.Collapsed;
                    DeleteFenceButton.Visibility = Visibility.Visible;

                    // Proceder con la eliminación
                    Directory.Delete(selectedFence.FolderPath, true);

                    // Guardar el índice actual antes de eliminar
                    int currentIndex = FenceSelector.SelectedIndex;

                    // Eliminar fence de la lista y del ComboBox

                    FenceManager.Fences.Remove(selectedFence);

                    FenceSelector.Items.Remove(selectedFenceName);
                    selectedFence.Close();

                    // Seleccionar el siguiente fence (o el anterior si era el último)
                    if (FenceSelector.Items.Count > 0)
                    {
                        // Si eliminamos el último item, seleccionar el nuevo último
                        if (currentIndex >= FenceSelector.Items.Count)
                            currentIndex = FenceSelector.Items.Count - 1;

                        FenceSelector.SelectedIndex = currentIndex;
                    }
                    else
                    {
                        // No quedan fences, ocultar configuraciones
                        FenceSettings.Visibility = Visibility.Collapsed;
                        FenceColorSettings.Visibility = Visibility.Collapsed;
                        FenceDeleteSettings.Visibility = Visibility.Collapsed;
                        UpdateExpandersVisibility();
                    }

                    SaveFences();
                }
            }
        }



        // Evento para cambiar la alineación del título
        private void TitleAlignment_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag != null)
            {
                string alignment = rb.Tag.ToString();
                var selectedFence = FenceManager.Fences.FirstOrDefault(f => f.Title == FenceSelector.SelectedItem?.ToString());
                if (selectedFence != null)
                {
                    selectedFence.UpdateTitleAlignment(alignment);
                    SaveFences();
                }
            }
        }

        private void SaveFences()
        {
            AppConfiguration config = new AppConfiguration
            {
                IconSize = selectedIconSize,
                Fences = FenceManager.Fences.Select(f => new FenceData

                {
                    Title = f.Title,
                    PositionX = f.Left,
                    PositionY = f.Top,
                    Color = f.FenceColor,
                    FolderPath = f.FolderPath,
                    Opacity = (!string.IsNullOrEmpty(f.FenceColor) && f.FenceColor.Length == 9)
                              ? f.FenceColor.Substring(1, 2) : "FF",
                    TitleTextColor = f.TitleTextColor,
                    TitleFontFamily = f.TitleFontFamily,
                    TitleAlignment = f.TitleAlignment,
                    TitleFontSize = f.TitleFontSize,
                    TitleDesignType = f.TitleDesignType,
                    TitleBackgroundColor = f.TitleBackgroundColor

                }).ToList()
            };
            try
            {
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la configuración: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Evento para actualizar los radio buttons al expandir el expander
        //private void TitleExpander_Expanded(object sender, RoutedEventArgs e)
        //{
        // TitleExpander.UpdateLayout();
        //  UpdateTitleRadioButtons();
        //}

        // Evento para validar el input al renombrar
        private void FenceNameInput_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-Z0-9\s]+$");
        }



        // Método para actualizar el ComboBox con un color personalizado
        // Método para actualizar el ComboBox con un color personalizado
        private void UpdateColorSelector(ComboBox selector, string hexColor, string displayText = null)
        {
            // Si no se proporciona texto de visualización, usar el valor hexadecimal
            if (displayText == null)
            {
                // Para mostrar colores sin la parte de opacidad
                displayText = hexColor.Length > 7 ?
                    "Hex Color - #" + hexColor.Substring(3) :
                    "Hex Color - " + hexColor;
            }

            // Primero, verificamos si ya existe un ítem "Hex Color" en el combobox
            ComboBoxItem hexItem = null;

            foreach (ComboBoxItem item in selector.Items)
            {
                // Verificar si Content es null para evitar NullReferenceException
                if (item.Content != null && item.Content.ToString().StartsWith("Hex Color - "))
                {
                    hexItem = item;
                    break;
                }
            }

            // Si no existe, lo creamos y agregamos
            if (hexItem == null)
            {
                hexItem = new ComboBoxItem
                {
                    Content = displayText,
                    Tag = hexColor  // Guardar el valor completo con opacidad
                };

                // Aplicamos el mismo estilo que los demás ítems
                hexItem.SetResourceReference(FrameworkElement.StyleProperty, "ModernComboBoxItemStyle");

                // Lo agregamos al final de la lista
                selector.Items.Add(hexItem);
            }
            else
            {
                // Si ya existe, actualizamos su contenido y tag
                hexItem.Content = displayText;
                hexItem.Tag = hexColor;  // Guardar el valor completo con opacidad
            }

            // Seleccionamos el ítem
            selector.SelectedItem = hexItem;

            // Limpiamos el campo de texto personalizado
            TextBox customInput = GetAssociatedTextBox(selector);
            if (customInput != null)
            {
                customInput.Text = "";
            }
        }

        // Método auxiliar para obtener el TextBox asociado con un ComboBox específico
        private TextBox GetAssociatedTextBox(ComboBox selector)
        {
            if (selector == ColorSelector)
                return CustomColorInput;
            else if (selector == TitleColorSelector)
                return CustomTitleColorInput;
            else if (selector == TitleBgColorSelector)
                return CustomTitleBgColorInput;

            return null;

        }


        private void FenceSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FenceSelector.SelectedItem != null)
            {
                FenceSettings.Visibility = Visibility.Visible;
                FenceColorSettings.Visibility = Visibility.Visible;
                FenceOptionsPanel.Visibility = Visibility.Visible;
                FenceDeleteSettings.Visibility = Visibility.Visible;

                string selectedFenceName = FenceSelector.SelectedItem.ToString();
                var selectedFence = FenceManager.Fences.FirstOrDefault(f => f.Title == selectedFenceName);
                if (selectedFence != null)
                {
                    FenceNameInput.Text = selectedFence.Title;
                    bool hasFiles = selectedFence.Files.Count > 0;
                    DeleteWarningText.Visibility = hasFiles ? Visibility.Visible : Visibility.Collapsed;
                    DeleteFenceButton.Visibility = hasFiles ? Visibility.Collapsed : Visibility.Visible;


                    // Actualización de controles de color y opacidad
                    if (!string.IsNullOrEmpty(selectedFence.FenceColor) && selectedFence.FenceColor.StartsWith("#"))
                    {
                        string normalizedColor;
                        if (selectedFence.FenceColor.Length == 7)
                        {
                            // Si es #RRGGBB, se normaliza a #FFRRGGBB
                            normalizedColor = "#FF" + selectedFence.FenceColor.Substring(1);
                        }
                        else
                        {
                            normalizedColor = selectedFence.FenceColor;
                        }

                        if (normalizedColor.Length == 9)
                        {
                            string alpha = normalizedColor.Substring(1, 2);
                            string rgb = "#" + normalizedColor.Substring(3, 6);
                            bool foundColor = false;

                            // Búsqueda en colores predefinidos
                            foreach (ComboBoxItem item in ColorSelector.Items)
                            {
                                if (string.Equals(item.Tag?.ToString(), rgb, StringComparison.OrdinalIgnoreCase))
                                {
                                    ColorSelector.SelectedItem = item;
                                    foundColor = true;
                                    break;
                                }
                            }

                            // Si no se encontró, añadir como color hex personalizado
                            if (!foundColor)
                            {
                                if (rgb.Equals("#545e75", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Es el color default, seleccionarlo
                                    string defaultColor = (string)Application.Current.FindResource("ColorDefault");
                                    foreach (ComboBoxItem item in ColorSelector.Items)
                                    {
                                        if (item.Content.ToString() == defaultColor)
                                        {
                                            ColorSelector.SelectedItem = item;
                                            foundColor = true;
                                            break;
                                        }
                                    }
                                }

                                if (!foundColor)
                                {
                                    // Agregar/actualizar como color personalizado
                                    UpdateColorSelector(ColorSelector, rgb);
                                }
                            }

                            // Actualizar selector de opacidad
                            bool foundOpacity = false;
                            foreach (ComboBoxItem opItem in OpacitySelector.Items)
                            {
                                if (string.Equals(opItem.Tag.ToString(), alpha, StringComparison.OrdinalIgnoreCase))
                                {
                                    OpacitySelector.SelectedItem = opItem;
                                    foundOpacity = true;
                                    break;
                                }
                            }
                            if (!foundOpacity)
                                OpacitySelector.SelectedIndex = 3;
                        }
                    }

                    else
                    {
                        string defaultColor = (string)Application.Current.FindResource("ColorDefault");
                        foreach (ComboBoxItem item in ColorSelector.Items)
                        {
                            if (item.Content.ToString() == defaultColor)
                            {
                                ColorSelector.SelectedItem = item;
                                break;
                            }
                        }
                        OpacitySelector.SelectedIndex = 0;
                    }


                    if (selectedFence.TitleDesignType == "etiqueta")
                    {
                        TitleBackgroundSettings.Visibility = Visibility.Visible;

                        if (TitleBgColorSelector != null)
                        {
                            // Normalize the color format for comparison
                            string bgColor = selectedFence.TitleBackgroundColor;
                            bool foundBgColor = false;

                            // Buscar el color exacto en los predefinidos
                            foreach (ComboBoxItem item in TitleBgColorSelector.Items)
                            {
                                string itemTag = item.Tag?.ToString();
                                if (string.Equals(itemTag, bgColor, StringComparison.OrdinalIgnoreCase))
                                {
                                    TitleBgColorSelector.SelectedItem = item;
                                    foundBgColor = true;
                                    break;
                                }
                            }

                            // Si no se encontró, y es un color distinto al predeterminado
                            if (!foundBgColor && bgColor != null && bgColor != "#FF333333")
                            {
                                // Agregar/actualizar como color personalizado
                                UpdateColorSelector(TitleBgColorSelector, bgColor);
                            }
                            else if (!foundBgColor)
                            {
                                // Es el color por defecto o nulo, seleccionar el primero
                                TitleBgColorSelector.SelectedIndex = 0;
                            }
                        }
                    }
                    else
                    {
                        TitleBackgroundSettings.Visibility = Visibility.Collapsed;
                    }

                    // Actualización del color del título
                    // En la sección donde maneja la actualización del color del título
                    // En la sección de FenceSelector_SelectionChanged donde maneja la actualización del color del título
                    if (!string.IsNullOrEmpty(selectedFence.TitleTextColor))
                    {
                        string titleColor = selectedFence.TitleTextColor;
                        bool foundTitleColor = false;

                        // Buscar en los colores predefinidos
                        foreach (ComboBoxItem item in TitleColorSelector.Items)
                        {
                            if (string.Equals(item.Tag?.ToString(), titleColor, StringComparison.OrdinalIgnoreCase))
                            {
                                TitleColorSelector.SelectedItem = item;
                                foundTitleColor = true;
                                break;
                            }
                        }

                        // Si no se encontró en los colores predefinidos
                        if (!foundTitleColor)
                        {
                            // Extraer el color sin la opacidad para mostrar
                            string displayColor = titleColor.Length >= 9 ?
                                "#" + titleColor.Substring(3) :
                                titleColor.StartsWith("#") ? titleColor : "#" + titleColor;

                            // Crear el texto de visualización
                            string displayText = "Hex Color - " + displayColor;

                            // Agregar/actualizar como color personalizado con texto de visualización explícito
                            UpdateColorSelector(TitleColorSelector, titleColor, displayText);
                        }
                    }
                    else
                    {
                        // Valor predeterminado
                        TitleColorSelector.SelectedIndex = 0;
                        CustomTitleColorInput.Text = "";
                    }

                    // Controlar la visibilidad del fondo de título según el diseño
                    TitleBackgroundSettings.Visibility = (selectedFence.TitleDesignType == "etiqueta")
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                    Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
                    {
                        UpdateTitleRadioButtons();

                        // Verificar el estado de TitleBackgroundSettings según el diseño del título
                        if (selectedFence != null)
                        {
                            TitleBackgroundSettings.Visibility =
                                (selectedFence.TitleDesignType == "etiqueta") ? Visibility.Visible : Visibility.Collapsed;
                        }
                    }));
                }
            }
        }


        // Aplica un color personalizado para el fence
        private void ApplyCustomColor_Click(object sender, RoutedEventArgs e)
        {
            if (CustomColorInput.Text.Length == 6 && Regex.IsMatch(CustomColorInput.Text, "^[A-Fa-f0-9]{6}$"))
            {
                // Agrega opacidad completa "FF" al color ingresado
                string hexColor = "#FF" + CustomColorInput.Text.ToUpper();

                if (FenceSelector.SelectedItem != null)
                {
                    string selectedFenceName = FenceSelector.SelectedItem.ToString();
                    var selectedFence = FenceManager.Fences.FirstOrDefault(f => f.Title == selectedFenceName);
                    if (selectedFence != null)
                    {
                        // Actualiza el color del fence con el formato #AARRGGBB
                        selectedFence.UpdateColor(hexColor);

                        // Actualizar el ComboBox con el color personalizado (sin la parte de opacidad)
                        UpdateColorSelector(ColorSelector, "#" + CustomColorInput.Text.ToUpper());

                        SaveFences();
                    }
                }
            }
            else
            {
                MessageBox.Show("Ingrese un color hexadecimal válido (6 caracteres).", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion
    }

}