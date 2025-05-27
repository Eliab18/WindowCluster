using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FencesApp.Converters
{
    public class ContrastColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Prioridad 1: Color del TitleContainer (si no es transparente)
            // Prioridad 2: Color del BackgroundPanel
            Color backgroundColor = Colors.Transparent;
            foreach (var value in values)
            {
                if (value is Color color && color.A > 0) // Si tiene opacidad
                {
                    backgroundColor = color;
                    break;
                }
            }

            // Fórmula de luminosidad relativa (WCAG)
            double luminance = 0.2126 * backgroundColor.ScR +
                             0.7152 * backgroundColor.ScG +
                             0.0722 * backgroundColor.ScB;

            return luminance > 0.5 ? Brushes.Black : Brushes.White;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}