using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;
using FencesApp.Pages;
using Hardcodet.Wpf.TaskbarNotification;
using System.Reflection;

namespace FencesApp
{
    public partial class MainWindow : Window
    {
        // Instancia del ícono en la bandeja, que definiremos en App.xaml
        private TaskbarIcon _taskbarIcon;

        public MainWindow()
        {
            try
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                InitializeComponent();
                stopwatch.Stop();
                LogStartup($"[PERF] InitializeComponent took {stopwatch.ElapsedMilliseconds} ms");

                // Agregar log para debugging del inicio automático
                LogStartup("MainWindow constructor iniciado");

                // IMPORTANTE: Cargar configuración al inicio
                // LoadConfig(); // Removed as per optimization task
                // LogStartup($"[PERF] LoadConfig (MainWindow) call removed from constructor"); // Optional: Log removal

                Loaded += (s, e) =>
                {
                    TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
                    TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
                };

                try
                {
                    // Este código es suficiente
                    _taskbarIcon = (TaskbarIcon)Application.Current.Resources["TrayIcon"];

                    // Suscribirse al evento TrayLeftMouseUp
                    _taskbarIcon.TrayLeftMouseUp += TrayIcon_MouseLeftMouseUp;

                    // Suscribirse al evento Closing para interceptar el cierre y minimizar a la bandeja
                    this.Closing += MainWindow_Closing;

                    LogStartup("TaskbarIcon configurado correctamente");
                }
                catch (Exception ex)
                {
                    LogStartup($"Error configurando TaskbarIcon: {ex.Message}");
                    // Si hay error con el TaskbarIcon, al menos la ventana principal debe funcionar
                }
            }
            catch (Exception ex)
            {
                LogStartup($"Error crítico en constructor: {ex.Message}");
                LogStartup($"StackTrace: {ex.StackTrace}");
                // Mostrar mensaje de error para debugging
                MessageBox.Show($"Error al iniciar la aplicación: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogStartup(string message)
        {
            try
            {
                string logPath = Path.Combine(GetApplicationDirectory(), "startup.log");
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
                File.AppendAllText(logPath, logEntry);
            }
            catch
            {
                // Si no se puede escribir el log, no hacer nada para evitar errores
            }
        }

        private string GetApplicationDirectory()
        {
            // Obtener el directorio donde está el ejecutable
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                // Verificar si la app tiene configuración válida
                var app = Application.Current as App;
                if (app?.Config?.MinimizeOnClose == true)
                {
                    LogStartup("Minimizando a bandeja al cerrar");
                    e.Cancel = true;
                    this.Hide();
                    if (_taskbarIcon != null)
                    {
                        _taskbarIcon.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    LogStartup("Cerrando aplicación completamente");
                    // Cerrar completamente la aplicación
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                LogStartup($"Error en MainWindow_Closing: {ex.Message}");
                // En caso de error, cerrar normalmente
                Application.Current.Shutdown();
            }
        }

        // Este método se ejecuta cuando se hace clic en el ícono de la bandeja y restaura la ventana
        private void TrayIcon_MouseLeftMouseUp(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            if (_taskbarIcon != null)
            {
                _taskbarIcon.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LogStartup("Window_Loaded iniciado");

            try
            {
                // Verificar si debe iniciar minimizado en la bandeja
                var app = Application.Current as App;
                if (app?.Config?.MinimizeOnClose == true)
                {
                    LogStartup("Configurado para minimizar al inicio");
                    // Iniciar minimizado en la bandeja
                    this.Hide();
                    if (_taskbarIcon != null)
                    {
                        _taskbarIcon.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    LogStartup("Mostrando ventana normalmente");
                    // Asegurar que la ventana sea visible
                    this.Show();
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                }

                // Navegar a la HomePage al iniciar
                Stopwatch stopwatchPageNav = new Stopwatch();
                stopwatchPageNav.Start();
                HomePage homePage = new HomePage();
                MainFrame.Content = homePage;
                stopwatchPageNav.Stop();
                LogStartup($"[PERF] HomePage creation and navigation took {stopwatchPageNav.ElapsedMilliseconds} ms");

                // Recorrer los botones del menú y asignar el fondo activo al que tenga Tag "Home"
                if (MenuPanel != null)
                {
                    foreach (var child in MenuPanel.Children)
                    {
                        if (child is Button btn)
                        {
                            // Si el botón tiene Tag "Home", se le asigna el color activo
                            if (btn.Tag?.ToString() == "Home")
                            {
                                btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1ABC9C"));
                            }
                            else
                            {
                                // Para los demás se deja transparente
                                btn.Background = Brushes.Transparent;
                            }
                        }
                    }
                }

                LogStartup("Window_Loaded completado exitosamente");
            }
            catch (Exception ex)
            {
                LogStartup($"Error en Window_Loaded: {ex.Message}");
                LogStartup($"StackTrace: {ex.StackTrace}");
                // Mostrar la ventana aunque haya errores
                this.Show();
                this.WindowState = WindowState.Normal;
            }
        }

        public void SetActiveTab(string tag)
        {
            // Recorre todos los botones del menú y resetea su fondo a Transparente,
            // asignando el color activo al botón cuyo Tag coincida.
            foreach (var child in MenuPanel.Children)
            {
                if (child is Button btn)
                {
                    if (btn.Tag?.ToString() == tag)
                        btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1ABC9C"));
                    else
                        btn.Background = Brushes.Transparent;
                }
            }
        }

        private void NavigateToPage(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton == null)
                return;

            // Recorre todos los botones del menú y resetea su fondo a Transparente
            foreach (var child in MenuPanel.Children)
            {
                if (child is Button btn)
                {
                    btn.Background = Brushes.Transparent;
                }
            }

            // Establece el fondo del botón clickeado para marcarlo como activo
            clickedButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1ABC9C"));

            // Navega a la página según el Tag del botón
            string pageTag = clickedButton.Tag?.ToString();
            if (pageTag == "Home")
            {
                MainFrame.Navigate(new HomePage());
            }
            else if (pageTag == "Fences")
            {
                MainFrame.Navigate(new FencesPage());
            }
            else if (pageTag == "Settings")
            {
                MainFrame.Navigate(new SettingsPage());
            }
            else if (pageTag == "About")
            {
                MainFrame.Navigate(new AboutPage());
            }
        }

        private void LoadConfig()
        {
            try
            {
                // Usar ruta absoluta basada en la ubicación del ejecutable
                string appDir = GetApplicationDirectory();
                string configPath = Path.Combine(appDir, "Resources", "config.json");

                LogStartup($"Buscando config en: {configPath}");

                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    AppConfig config = JsonConvert.DeserializeObject<AppConfig>(json);
                    LogStartup("Configuración cargada exitosamente");
                    // Aplicar configuración (ejemplo: tema, idioma, etc.)
                }
                else
                {
                    // Crear configuración por defecto
                    AppConfig defaultConfig = new AppConfig();
                    string resourcesDir = Path.Combine(appDir, "Resources");

                    if (!Directory.Exists(resourcesDir))
                    {
                        Directory.CreateDirectory(resourcesDir);
                    }

                    File.WriteAllText(configPath, JsonConvert.SerializeObject(defaultConfig, Formatting.Indented));
                    LogStartup("Configuración por defecto creada");
                }
            }
            catch (Exception ex)
            {
                LogStartup($"Error cargando configuración: {ex.Message}");
            }
        }
    }

    public class AppConfig

    {

        public string Theme { get; set; } = "Light";

        public string Language { get; set; } = "en";

        public bool MinimizeOnClose { get; set; } = false;

        public bool StartWithWindows { get; set; } = true;


    }
}