using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace FencesApp.Pages
{
    public partial class AboutPage : UserControl
    {
        private Storyboard showModalAnimation;
        private Storyboard hideModalAnimation;
        private bool isModalVisible = false;

        public AboutPage()
        {
            InitializeComponent();

            // Obtener las animaciones definidas en XAML
            showModalAnimation = (Storyboard)FindResource("ShowModalAnimation");
            hideModalAnimation = (Storyboard)FindResource("HideModalAnimation");

            // Configurar los targets de la animación
            foreach (var animation in showModalAnimation.Children)
            {
                if (Storyboard.GetTargetName(animation) == null)
                {
                    Storyboard.SetTarget(animation, ModalOverlay);
                }
            }

            foreach (var animation in hideModalAnimation.Children)
            {
                if (Storyboard.GetTargetName(animation) == null)
                {
                    Storyboard.SetTarget(animation, ModalOverlay);
                }
            }

            // Agregar manejador para la finalización de la animación de cierre
            hideModalAnimation.Completed += (s, e) =>
            {
                ModalOverlay.Visibility = Visibility.Collapsed;
                isModalVisible = false;
            };
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowUpdateModalAsync();
        }

        private async Task ShowUpdateModalAsync()
        {
            // Reiniciar el modal al estado inicial usando recursos localizados
            ModalStatusText.Text = GetResourceString("CheckingForUpdates");
            CloseModalButton.Visibility = Visibility.Collapsed;

            // Configurar y mostrar el modal
            ModalOverlay.Visibility = Visibility.Visible;

            // Iniciar animación de apertura
            showModalAnimation.Begin();
            isModalVisible = true;

            // Iniciar animación de rotación para el indicador
            DoubleAnimation rotationAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(1),
                RepeatBehavior = RepeatBehavior.Forever
            };

            // Usar la clase completa para AngleProperty
            ModalRotateTransform.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, rotationAnimation);

            // Simular búsqueda de actualizaciones (5 segundos)
            await Task.Delay(5000);

            // Detener la animación de rotación
            ModalRotateTransform.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, null);

            // Cambiar el mensaje y mostrar el botón para cerrar
            ModalStatusText.Text = GetResourceString("NoUpdatesAvailable");
            CloseModalButton.Visibility = Visibility.Visible;

            // Opcionalmente, cerrar automáticamente después de un tiempo
            await Task.Delay(3000);

            // Solo cerrar automáticamente si el modal sigue abierto
            if (isModalVisible)
            {
                await CloseModalAsync();
            }
        }

        private async Task CloseModalAsync()
        {
            // Iniciar animación de cierre
            hideModalAnimation.Begin();
            // El manejador de evento Completed se encargará de ocultar el modal completamente
        }

        private void CloseModalButton_Click(object sender, RoutedEventArgs e)
        {
            CloseModalAsync();
        }

        // Helper para obtener recursos de cadena localizada
        private string GetResourceString(string key)
        {
            try
            {
                return (string)FindResource(key) ?? key;
            }
            catch
            {
                // Si no se encuentra el recurso, devolver la clave como fallback
                return key;
            }
        }
    }
}