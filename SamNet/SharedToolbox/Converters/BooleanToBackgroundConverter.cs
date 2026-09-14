using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace SharedToolbox
{
    public class BooleanToBackgroundConverter : IValueConverter
    {
        // You can make these public properties so you can set them in XAML if needed
        public Brush TrueBackground { get; set; } = new SolidColorBrush(Color.FromRgb(235, 248, 255)); // #EBF8FF - light blue
        public Brush FalseBackground { get; set; } = Brushes.White; // or Brushes.Transparent, #F8F9FA, etc.

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isTrue && isTrue)
                return TrueBackground;

            return FalseBackground;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Usually not needed for one-way bindings like Background → return DependencyProperty.UnsetValue;
            throw new NotImplementedException();
        }
    }
}
