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
    public class BooleanToBorderBrushConverter : IValueConverter
    {
        // Make these configurable via properties if you want (optional)
        public Brush TrueBrush { get; set; } = new SolidColorBrush(Color.FromRgb(66, 153, 225)); // #4299E1 - nice blue
        public Brush FalseBrush { get; set; } = new SolidColorBrush(Color.FromRgb(226, 232, 240)); // #E2E8F0 - light gray

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
                return TrueBrush;

            return FalseBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // One-way binding usually → no need to implement
            return DependencyProperty.UnsetValue;
        }
    }
}
