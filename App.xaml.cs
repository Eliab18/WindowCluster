using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using FencesApp.Pages;
using FencesApp.Models;
using Hardcodet.Wpf.TaskbarNotification;

namespace FencesApp
{
    public partial class App : Application
    {

        private TaskbarIcon _taskbarIcon;

        protected override void OnStartup(StartupEventArgs e)
        {


            base.OnStartup(e);

            // Cargar configuración (o crear una nueva si no existe)
            Config = LoadConfig();

            // Cargar el ResourceDictionary según el idioma guardado
            LoadLanguageDictionary(Config.Language);



            // Habilitar DPI alto
            //RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

            // Mejorar la nitidez de texto e imágenes
            RenderOptions.ProcessRenderMode = RenderMode.Default;

            // En OnStartup de App.xaml.cs:

            _taskbarIcon = (TaskbarIcon)Resources["TrayIcon"];
            // Configura la visibilidad según la configuración en lugar de ocultarlo siempre
            _taskbarIcon.Visibility = Config.MinimizeOnClose ? Visibility.Visible : Visibility.Collapsed;


            // Configurar el menú contextual
            if (_taskbarIcon != null && _taskbarIcon.ContextMenu != null)
            {
                foreach (var item in _taskbarIcon.ContextMenu.Items)
                {
                    if (item is MenuItem menuItem)
                    {
                        if (menuItem.Header.ToString() == "Abrir")
                        {
                            menuItem.Click += OnOpenMenuItem_Click;
                        }
                        else if (menuItem.Header.ToString() == "Configuración")
                        {
                            menuItem.Click += OnConfigMenuItem_Click;
                        }
                        else if (menuItem.Header.ToString() == "Salir")
                        {
                            menuItem.Click += OnExitMenuItem_Click;
                        }
                    }
                }
            }

            // **NUEVO:** Llamar al método de carga global de fences
            FenceManager.LoadFences();


            // Resto del código de inicialización...
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Cierra todas las Fence si no está en modo "minimizar al cerrar"
            if (!Config.MinimizeOnClose)
            {
                FenceWindow.CloseAllFences();
            }
            base.OnExit(e);
        }





        private void OnOpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Buscar la ventana principal y mostrarla
            foreach (Window window in Windows)
            {
                if (window is MainWindow mainWindow)
                {
                    mainWindow.Show();
                    mainWindow.WindowState = WindowState.Normal;
                    mainWindow.Activate();
                    break;
                }
            }
        }

        private void OnConfigMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Buscar la ventana principal
            foreach (Window window in Windows)
            {
                if (window is MainWindow mainWindow)
                {
                    // Asegurar que esté visible
                    mainWindow.Show();
                    mainWindow.WindowState = WindowState.Normal;

                    // Navegar a la página de configuración
                    mainWindow.MainFrame.Navigate(new SettingsPage());
                    mainWindow.SetActiveTab("Settings");

                    mainWindow.Activate();
                    break;
                }
            }
        }

        private void OnExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Cerrar todas las ventanas y la aplicación
            Shutdown();

        }
        public AppConfig Config { get; private set; }
        private const string ConfigPath = "Resources/config.json";



        private AppConfig LoadConfig()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<AppConfig>(json);
                }
                catch
                {
                    // Si hay error, se usa configuración por defecto
                    var defaultConfig = new AppConfig();
                    SaveConfig(defaultConfig);
                    return defaultConfig;
                }
            }
            else
            {
                var defaultConfig = new AppConfig();
                SaveConfig(defaultConfig);
                return defaultConfig;
            }
        }

        public void SaveConfig(AppConfig config)
        {
            // Asegurarte de que el directorio del archivo exista
            string directory = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory); // Crear el directorio si no existe
            }

            // Serializar el objeto config a formato JSON
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

            // Guardar el archivo JSON en la ruta especificada
            File.WriteAllText(ConfigPath, json);
        }


        private void LoadLanguageDictionary(string language)
        {
            // Validación: si el idioma no es "en" o "es", se asigna "en" por defecto.
            if (language != "en" && language != "es")
                language = "en";

            string dictionaryPath = $"Resources/StringResources.{language}.xaml";
            try
            {
                var newDictionary = new ResourceDictionary() { Source = new Uri(dictionaryPath, UriKind.Relative) };

                // Eliminar cualquier diccionario que contenga "StringResources." en su Source
                for (int i = 0; i < Resources.MergedDictionaries.Count; i++)
                {
                    var md = Resources.MergedDictionaries[i];
                    if (md.Source != null && md.Source.OriginalString.Contains("StringResources."))
                    {
                        Resources.MergedDictionaries.Remove(md);
                        i--;
                    }
                }

                Resources.MergedDictionaries.Add(newDictionary);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar el idioma: " + ex.Message);
            }
        }
    }
}