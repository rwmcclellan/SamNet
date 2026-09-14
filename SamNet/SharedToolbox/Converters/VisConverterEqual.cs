using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace SharedToolbox
{
    public class VisConverterEqual : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (targetType.Name.Equals("Visibility") == false) Debug.WriteLine("Target not valid in VisConverterEqual");
                if (values.Count() == 2)
                {
                    if (values[0] is string)
                    {
                        int value0 = 0;
                        if (Int32.TryParse((string)values[0], out value0))
                        {
                            if (values[1] is int)
                            {
                                if (value0 == (int)values[1]) return System.Windows.Visibility.Visible;
                            }
                        }
                    }
                }
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
