using System;
using System.IO;
using System.Reflection;
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
        private static string GetApplicationDirectory()
        {
            // This method remains static. Logging of its direct inputs and outputs
            // will be handled by the callers using the instance-based LogMessage.
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        // LogFilePath will have its components logged in OnStartup.
        // ConfigPath is now relative.
        private static readonly string LogFilePath = Path.Combine(GetApplicationDirectory(), "app_startup.log");
        private static readonly string ConfigPath = Path.Combine("Resources", "config.json");

        private void LogMessage(string message)
        {
            try
            {
                File.AppendAllText(LogFilePath, $"[{DateTime.Now}] {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                // Fallback or error handling for logging failure
                System.Diagnostics.Debug.WriteLine($"Failed to write to log: {ex.Message}");
            }
        }

        private TaskbarIcon _taskbarIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            // --- Set and Log Current Working Directory ---
            LogMessage($"OnStartup: Initial Current Working Directory: {Directory.GetCurrentDirectory()}");
            string appExecutablePath = GetApplicationDirectory();
            LogMessage($"OnStartup: Application Executable Path from GetApplicationDirectory(): {appExecutablePath}");
            try
            {
                Directory.SetCurrentDirectory(appExecutablePath);
                LogMessage($"OnStartup: Current Working Directory set to: {Directory.GetCurrentDirectory()}");
            }
            catch (Exception ex)
            {
                LogMessage($"OnStartup: Error setting current working directory to '{appExecutablePath}': {ex.Message}");
            }
            // --- End Set and Log Current Working Directory ---

            // --- Begin Logging for Static Path Initialization ---
            string rawLocation = Assembly.GetExecutingAssembly().Location;
            LogMessage($"OnStartup: Raw Assembly.GetExecutingAssembly().Location for static paths: {rawLocation}");

            string appDirForStaticFields = GetApplicationDirectory(); // First call relevant to static fields
            LogMessage($"OnStartup: GetApplicationDirectory() call for static paths returned: {appDirForStaticFields}");

            // Log LogFilePath components
            string logFileRelativePart = "app_startup.log";
            LogMessage($"OnStartup: LogFilePath Component 1 (appDir): {appDirForStaticFields}");
            LogMessage($"OnStartup: LogFilePath Component 2 (filename): \"{logFileRelativePart}\"");
            // LogFilePath is already constructed by this point, log its final value.
            LogMessage($"OnStartup: Final static LogFilePath: {LogFilePath}");

            // Log ConfigPath (which is now relative)
            LogMessage($"OnStartup: Static relative ConfigPath: {ConfigPath}");
            // --- End Logging for Static Path Initialization ---

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
        // ConfigPath is now defined above as a relative path.

        private AppConfig LoadConfig()
        {
            // ConfigPath is now a relative static path.
            LogMessage($"LoadConfig() attempting to load from relative ConfigPath: {ConfigPath}");
            if (File.Exists(ConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<AppConfig>(json);
                }
                catch (Exception ex)
                {
                    LogMessage($"Error loading relative config file {ConfigPath}: {ex.Message}. Creating default config.");
                    // Si hay error, se usa configuración por defecto
                    var defaultConfig = new AppConfig();
                    SaveConfig(defaultConfig);
                    return defaultConfig;
                }
            }
            else
            {
                LogMessage($"Relative config file {ConfigPath} not found. Creating default config.");
                var defaultConfig = new AppConfig();
                SaveConfig(defaultConfig);
                return defaultConfig;
            }
        }

        public void SaveConfig(AppConfig config)
        {
            // ConfigPath is now a relative static path.
            LogMessage($"SaveConfig() attempting to save to relative ConfigPath: {ConfigPath}");

            // Asegurarte de que el directorio del archivo exista (relative to CWD)
            string directory = Path.GetDirectoryName(ConfigPath); // Should be "Resources"
            LogMessage($"SaveConfig() ensuring relative directory exists: \"{directory}\"");
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory); // Crear el directorio si no existe
            }

            // Serializar el objeto config a formato JSON
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

            LogMessage($"Saving configuration to relative path: {ConfigPath}");
            // Guardar el archivo JSON en la ruta especificada (relative to CWD)
            File.WriteAllText(ConfigPath, json);
        }


        private void LoadLanguageDictionary(string language)
        {
            // Validación: si el idioma no es "en" o "es", se asigna "en" por defecto.
            if (language != "en" && language != "es")
            {
                LogMessage($"LoadLanguageDictionary() invalid language '{language}' provided, defaulting to 'en'.");
                language = "en";
            }

            // --- LoadLanguageDictionary using Relative URI ---
            string dictionaryPath = $"Resources/StringResources.{language}.xaml";
            LogMessage($"LoadLanguageDictionary({language}): Attempting to load with relative dictionaryPath: {dictionaryPath}");

            try
            {
                Uri resourceUri = new Uri(dictionaryPath, UriKind.Relative);
                // For relative URIs, OriginalString is often more informative than AbsoluteUri
                LogMessage($"LoadLanguageDictionary({language}): Constructed Uri.OriginalString: {resourceUri.OriginalString}");
                var newDictionary = new ResourceDictionary() { Source = resourceUri };

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
                LogMessage($"Error loading language dictionary with relative path {dictionaryPath}: {ex.Message}");
                MessageBox.Show("Error al cargar el idioma: " + ex.Message);
            }
        }
    }
}