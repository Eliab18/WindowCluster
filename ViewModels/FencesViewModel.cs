using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using FencesApp.Models;

namespace FencesApp.ViewModels
{
    public class FencesViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<FenceData> Fences { get; set; } = new ObservableCollection<FenceData>();

        public ICommand CreateFenceCommand { get; }

        public FencesViewModel()
        {
            CreateFenceCommand = new RelayCommand(CreateFence);
        }

        private void CreateFence(object parameter)
        {
            string newFenceTitle = "New Fence " + (Fences.Count + 1);
            FenceData newFence = new FenceData { Title = newFenceTitle, FolderPath = System.IO.Path.Combine("Fences", newFenceTitle) };
            Fences.Add(newFence);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
