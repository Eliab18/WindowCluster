using System;
using System.IO; // Espacio de nombres original
using IOPath = System.IO.Path; // Alias para System.IO.Path

namespace FencesApp.Models
{
    public class Item
    {
        public string FilePath { get; set; }
        public string FileName => IOPath.GetFileName(FilePath); // Usa el alias
        public string IconPath { get; set; }
    }
}