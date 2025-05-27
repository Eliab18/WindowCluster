using System;

using System.IO;

using System.Text.Json;

using System.Linq;

using System.Windows;

using System.Windows.Controls;

using Microsoft.Win32;

using FencesApp.Models;



namespace FencesApp.Pages

{

    public partial class SettingsPage : UserControl

    {

        private const string RunLocation = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

        // La ruta de config ya se usa en App.xaml.cs, por lo que en SettingsPage trabajaremos con el App.Config

        private AppConfig _config;



        public SettingsPage()

        {

            InitializeComponent();

            // Cargar la configuración desde la instancia de App

            _config = ((App)Application.Current).Config;



            LoadStartWithWindowsSetting();

            // Inicializar el estado de minimizar al cerrar

            MinimizeOnCloseCheckBox.IsChecked = _config.MinimizeOnClose;

            // Inicializar el ComboBox según el idioma guardado

            SetLanguageComboBox();

        }



        private void SetLanguageComboBox()

        {

            // Seleccionar el ComboBoxItem cuyo Tag coincida con _config.Language

            foreach (ComboBoxItem item in LanguageComboBox.Items)

            {

                if (item.Tag.ToString() == _config.Language)

                {

                    LanguageComboBox.SelectedItem = item;

                    break;

                }

            }

        }



        private void LoadStartWithWindowsSetting()

        {

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunLocation))

            {

                if (key != null)

                {

                    object o = key.GetValue(AppDomain.CurrentDomain.FriendlyName);

                    StartWithWindowsCheckBox.IsChecked = o != null;

                }

            }

        }



        private void StartWithWindowsCheckBox_Checked(object sender, RoutedEventArgs e)

        {

            SetStartup(true);

        }



        private void StartWithWindowsCheckBox_Unchecked(object sender, RoutedEventArgs e)

        {

            SetStartup(false);

        }



        private void SetStartup(bool enabled)

        {

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunLocation, true))

            {

                if (key != null)

                {

                    if (enabled)

                    {

                        key.SetValue(AppDomain.CurrentDomain.FriendlyName, System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

                    }

                    else

                    {

                        key.DeleteValue(AppDomain.CurrentDomain.FriendlyName, false);

                    }

                }

            }

        }



        private void MinimizeOnCloseCheckBox_Checked(object sender, RoutedEventArgs e)

        {

            _config.MinimizeOnClose = true;

            ((App)Application.Current).SaveConfig(_config);

        }



        private void MinimizeOnCloseCheckBox_Unchecked(object sender, RoutedEventArgs e)

        {

            _config.MinimizeOnClose = false;

            ((App)Application.Current).SaveConfig(_config);

        }



        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)

        {

            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem)

            {

                string selectedLanguage = selectedItem.Tag.ToString();

                ChangeLanguage(selectedLanguage);

                // Guardar el idioma en la configuración

                _config.Language = selectedLanguage;

                ((App)Application.Current).SaveConfig(_config);

            }

        }



        private void ChangeLanguage(string language)

        {

            string dictionaryPath = $"Resources/StringResources.{language}.xaml";

            try

            {

                // Cargar el nuevo diccionario

                var newDictionary = new ResourceDictionary() { Source = new Uri(dictionaryPath, UriKind.Relative) };



                // Buscar y remover el diccionario actual de idioma

                var currentDictionary = Application.Current.Resources.MergedDictionaries

                    .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("StringResources."));

                if (currentDictionary != null)

                {

                    Application.Current.Resources.MergedDictionaries.Remove(currentDictionary);

                }



                // Agregar el nuevo diccionario

                Application.Current.Resources.MergedDictionaries.Add(newDictionary);

            }

            catch (Exception ex)

            {

                MessageBox.Show($"Error al cambiar el idioma: {ex.Message}");

            }

        }

    }

}
