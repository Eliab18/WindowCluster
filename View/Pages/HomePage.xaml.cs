using System;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace FencesApp.Pages
{
    public partial class HomePage : UserControl
    {
        public HomePage()
        {
            InitializeComponent();
            SetWelcomeMessage();

        }

        /// <summary>
        /// Obtiene el nombre de usuario de Windows y lo formatea. Si no se obtiene un valor, usa "Usuario".
        /// </summary>
        private void SetWelcomeMessage()
        {
            string username = Environment.UserName;

            // Validar si se obtuvo un nombre de usuario; de lo contrario, se usa "Usuario"
            if (string.IsNullOrWhiteSpace(username))
            {
                username = "Usuario";
            }
            else
            {
                // Asegura que la primera letra esté en mayúscula
                username = char.ToUpper(username[0]) + username.Substring(1);
            }

            // Recupera el formato de bienvenida del diccionario de recursos
            string welcomeFormat = Application.Current.FindResource("HomeWelcome") as string;
            if (string.IsNullOrWhiteSpace(welcomeFormat))
            {
                welcomeFormat = "¡Hola, {0}!"; // Valor por defecto
            }

            // Actualiza el TextBlock con el mensaje formateado
            WelcomeTextBlock.Text = string.Format(welcomeFormat, username);
        }


        private void FencesButton_Click(object sender, RoutedEventArgs e)
        {
            // Crea una instancia de FencesPage
            var fencesPage = new FencesPage();
            NavigationService nav = NavigationService.GetNavigationService(this);
            if (nav != null)
            {
                nav.Navigate(fencesPage);
            }
            else
            {
                Application.Current.MainWindow.Content = fencesPage;
            }

            // Actualiza el tab del menú para indicar que Fences está activo.
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.SetActiveTab("Fences");
            }
        }
    }
}
