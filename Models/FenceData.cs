using System.ComponentModel;

namespace FencesApp.Models
{
    public class FenceData : INotifyPropertyChanged
    {
        private string title;
        public string Title
        {
            get => title;
            set { title = value; OnPropertyChanged(nameof(Title)); }
        }
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public string Color { get; set; }

        private string _titleFontFamily = "Segoe UI Bold";

        public string TitleFontFamily
        {
            get => _titleFontFamily;
            set { _titleFontFamily = value; OnPropertyChanged(nameof(TitleFontFamily)); }
        }

        // Añadir estas propiedades en FenceData.cs si aún no existen
        public string TitleFontWeight { get; set; } = "Bold";
        public string TitleFontStyle { get; set; } = "Normal";


        public string FolderPath { get; set; }
        public string Opacity { get; set; }

        private string titleTextColor = "#FFFFFFFF";
        public string TitleTextColor
        {
            get => titleTextColor;
            set { titleTextColor = value; OnPropertyChanged(nameof(TitleTextColor)); }
        }

        private string titleAlignment = "Center";
        public string TitleAlignment
        {
            get => titleAlignment;
            set { titleAlignment = value; OnPropertyChanged(nameof(TitleAlignment)); }
        }

        private int titleFontSize = 20;
        public int TitleFontSize
        {
            get => titleFontSize;
            set { titleFontSize = value; OnPropertyChanged(nameof(TitleFontSize)); }
        }

        private string titleDesignType = "default";
        public string TitleDesignType
        {
            get => titleDesignType;
            set { titleDesignType = value; OnPropertyChanged(nameof(TitleDesignType)); }
        }

        private string titleBackgroundColor = "#FF333333";
        public string TitleBackgroundColor
        {
            get => titleBackgroundColor;
            set { titleBackgroundColor = value; OnPropertyChanged(nameof(TitleBackgroundColor)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class AppConfiguration
    {
        public int IconSize { get; set; } = 16;
        public List<FenceData> Fences { get; set; } = new List<FenceData>();
    }
}
