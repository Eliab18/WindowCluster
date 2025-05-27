using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FencesApp.Models;

namespace FencesApp
{
    public class Fence : Border
    {
        private FenceData fenceData;
        private TextBlock titleText;
        private TextBox renameTextBox;
        private StackPanel contentPanel;
        private DateTime lastClickTime = DateTime.MinValue;

        public Fence(FenceData data)
        {
            fenceData = data;
            Width = 200;
            Height = 150;
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fenceData.Color));
            CornerRadius = new CornerRadius(10);
            BorderBrush = Brushes.Gray;
            BorderThickness = new Thickness(2);
            Margin = new Thickness(10);
            Padding = new Thickness(5);
            ClipToBounds = true;

            StackPanel mainPanel = new StackPanel { Orientation = Orientation.Vertical };

            titleText = new TextBlock
            {
                Text = fenceData.Title,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.White,
                Opacity = 0.8
            };
            titleText.MouseLeftButtonDown += TitleText_MouseLeftButtonDown;

            renameTextBox = new TextBox
            {
                Visibility = Visibility.Collapsed,
                Text = fenceData.Title,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            renameTextBox.LostFocus += (s, e) => EndRename();
            renameTextBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) EndRename(); };

            contentPanel = new StackPanel { Orientation = Orientation.Vertical, AllowDrop = true };
            contentPanel.Drop += OnDropFile;

            mainPanel.Children.Add(titleText);
            mainPanel.Children.Add(renameTextBox);
            mainPanel.Children.Add(contentPanel);
            Child = mainPanel;

            MouseMove += Fence_MouseMove;
        }

        private void TitleText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DateTime now = DateTime.Now;
            if ((now - lastClickTime).TotalMilliseconds < 500)
                RenameFence();
            lastClickTime = now;
        }

        private void RenameFence()
        {
            titleText.Visibility = Visibility.Collapsed;
            renameTextBox.Visibility = Visibility.Visible;
            renameTextBox.Focus();
            renameTextBox.SelectAll();
        }

        private void EndRename()
        {
            fenceData.Title = renameTextBox.Text;
            titleText.Text = fenceData.Title;
            titleText.Visibility = Visibility.Visible;
            renameTextBox.Visibility = Visibility.Collapsed;
        }

        private void OnDropFile(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                    MoveFileToFence(file);
            }
        }

        private void MoveFileToFence(string filePath)
        {
            if (string.IsNullOrEmpty(fenceData.FolderPath))
            {
                fenceData.FolderPath = Path.Combine("Resources", fenceData.Title);
                Directory.CreateDirectory(fenceData.FolderPath);
            }
            string fileName = Path.GetFileName(filePath);
            string newFilePath = Path.Combine(fenceData.FolderPath, fileName);
            try
            {
                File.Move(filePath, newFilePath);
                TextBlock fileItem = new TextBlock
                {
                    Text = fileName,
                    Foreground = Brushes.White,
                    Margin = new Thickness(5)
                };
                contentPanel.Children.Add(fileItem);
                if (contentPanel.Children.Count * 30 > Height)
                    Height += 30;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al mover archivo: {ex.Message}");
            }
        }

        private void Fence_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragDrop.DoDragDrop(this, new DataObject(DataFormats.Serializable, this), DragDropEffects.Move);
        }

        public string GetTitle() => fenceData.Title;
        public string GetFolderPath() => fenceData.FolderPath;
    }
}
