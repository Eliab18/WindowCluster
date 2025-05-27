using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace Fance_App.Converters
{
    public class FaceNameToWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string faceName = value as string;
            if (faceName != null)
            {
                if (faceName.Contains("Bold", StringComparison.OrdinalIgnoreCase))
                    return FontWeights.Bold;
                if (faceName.Contains("Light", StringComparison.OrdinalIgnoreCase))
                    return FontWeights.Light;
                if (faceName.Contains("SemiBold", StringComparison.OrdinalIgnoreCase))
                    return FontWeights.SemiBold;
                // Añade más condiciones según sea necesario
            }
            return FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
