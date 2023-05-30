using System;
using System.Globalization;
using System.Windows.Data;

namespace WPFUI.Converters
{
    public sealed class PercentageCoverageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if ((int)values[1] == 0)
                return $"{(int)values[0]}({(int)values[0]:N2}%)";

            return $"{(int)values[0]}({Decimal.Divide((int)values[0], (int)values[1]) * 100:N2}%)";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
