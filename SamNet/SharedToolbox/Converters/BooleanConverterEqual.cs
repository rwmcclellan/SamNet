using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace SharedToolbox
{
    public class BooleanConverterEqual : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (targetType.Name.Equals("Visibility")) Debug.WriteLine("Visibility not valid in BooleanConverterEqual");
                if (values.Count() == 2)
                {
                    if (values[0] is string)
                    {
                        int value0 = 0;
                        if (Int32.TryParse((string)values[0], out value0))
                        {
                            if (values[1] is int)
                            {
                                if (value0 == (int)values[1]) return true;
                            }
                        }
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
