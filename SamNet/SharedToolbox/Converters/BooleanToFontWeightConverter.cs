using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SharedToolbox
{
    public class BooleanToFontWeightConverter : IValueConverter
    {
        // Optional: make configurable via properties
        public FontWeight TrueWeight { get; set; } = FontWeights.Bold;
        public FontWeight FalseWeight { get; set; } = FontWeights.Normal;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isTrue && isTrue)
            {
                return TrueWeight;
            }

            return FalseWeight;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Rarely needed for FontWeight (one-way binding)
            // Could return true if value == TrueWeight, but usually just Unset
            return DependencyProperty.UnsetValue;
        }
    }
}
