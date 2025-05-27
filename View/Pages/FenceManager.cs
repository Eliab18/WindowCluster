using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using FencesApp.Models;
using FencesApp.Pages;

namespace FencesApp
{
    public static class FenceManager
    {
        // Lista global de FenceWindow
        public static List<FenceWindow> Fences { get; } = new List<FenceWindow>();
        private const string configPath = "Resources/fences.json";

        public static void LoadFences()
        {
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                try
                {
                    AppConfiguration config = JsonSerializer.Deserialize<AppConfiguration>(json);

                    // Si la configuración contiene fences, recórrelos
                    foreach (var fenceData in config.Fences)
                    {
                        // Evitar duplicados basados en el título
                        if (!Fences.Exists(f => f.Title == fenceData.Title))
                        {
                            // Crear la instancia del fence con los valores de la configuración
                            FenceWindow loadedFence = new FenceWindow(
                                fenceData.Title,
                                fenceData.FolderPath,
                                fenceData.Color)
                            {
                                Left = fenceData.PositionX,
                                Top = fenceData.PositionY,
                                TitleTextColor = fenceData.TitleTextColor ?? "#FFFFFFFF",
                                TitleAlignment = fenceData.TitleAlignment ?? "Center",
                                TitleFontSize = fenceData.TitleFontSize > 0 ? fenceData.TitleFontSize : 20,
                                TitleDesignType = fenceData.TitleDesignType ?? "default",
                                TitleBackgroundColor = fenceData.TitleBackgroundColor ?? "#FF333333"
                            };

                            // Actualizar propiedades visuales
                            loadedFence.UpdateTitleTextColor(loadedFence.TitleTextColor);
                            loadedFence.TitleFontFamily = fenceData.TitleFontFamily ?? "Segoe UI";
                            loadedFence.UpdateTitleFontFamily(loadedFence.TitleFontFamily);
                            loadedFence.UpdateTitleAlignment(loadedFence.TitleAlignment);
                            loadedFence.UpdateTitleFontSize(loadedFence.TitleFontSize);
                            loadedFence.UpdateTitleDesignType(loadedFence.TitleDesignType, loadedFence.TitleBackgroundColor);

                            // Suscribir eventos
                            loadedFence.FilesChanged += (s, e) => { /* Puedes dejar o extraer esta suscripción a otro método */ };
                            loadedFence.OnFenceUpdated += SaveFences;

                            // Agregar a la lista global y mostrar la ventana
                            Fences.Add(loadedFence);
                            loadedFence.Show();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar la configuración: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public static void SaveFences()
        {
            AppConfiguration config = new AppConfiguration
            {
                // Si manejas el tamaño de íconos de forma global, inclúyelo aquí
                Fences = Fences.Select(f => new FenceData
                {
                    Title = f.Title,
                    PositionX = f.Left,
                    PositionY = f.Top,
                    Color = f.FenceColor,
                    FolderPath = f.FolderPath,
                    // Extraer la opacidad de FenceColor si está en formato #AARRGGBB
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
                MessageBox.Show($"Error al guardar la configuración: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
