using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace SharedToolbox
{
    public class VisConverterFlag : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (targetType.Name.Equals("Visibility") == false) Debug.WriteLine("Target not valid in VisConverterFlag");
                if ((values.Count() == 3) && (values[2] is string))
                {
                    string debugg = (string)values[2];
                }
                if (values.Count() >= 2)
                {
                    if (values[0] is string)
                    {
                        int bit = 0;
                        if (int.TryParse((string)values[0], System.Globalization.NumberStyles.HexNumber, null, out bit))
                        {
                            if (values[1] is int)
                            {
                                if ((bit & (int)values[1]) > 0) return System.Windows.Visibility.Visible;
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
