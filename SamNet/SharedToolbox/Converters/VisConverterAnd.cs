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
    public class VisConverterAnd : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // obtain the converter for the target type
            TypeConverter converter = TypeDescriptor.GetConverter(targetType);

            try
            {
                bool visible = true;
                foreach (object value in values)
                    if (value is bool)
                        visible = visible && (bool)value;

                if (visible)
                    return System.Windows.Visibility.Visible;
                else
                    return System.Windows.Visibility.Collapsed;
            }
            catch (Exception)
            {
                return System.Windows.Visibility.Collapsed;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}