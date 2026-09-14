using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SharedToolbox
{
    public class VisConverterThreeState : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // obtain the converter for the target type
            TypeConverter converter = TypeDescriptor.GetConverter(targetType);
            try
            {
                if (value is int)
                {
                    if ((int)value == 0)
                    {
                        return System.Windows.Visibility.Visible;
                    }
                    else if ((int)value == 1)
                    {
                        return System.Windows.Visibility.Hidden;
                    }
                }
                return System.Windows.Visibility.Collapsed;
            }
            catch (Exception)
            {
                return System.Windows.Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}