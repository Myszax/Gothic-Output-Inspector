using System;
using System.Globalization;
using System.Windows.Data;

namespace WPFUI.Converters
{
    public sealed class PercentageCoverageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string zero = $"0({0:N2})%";

            if (values.Length != 2 || values[0] is not int || values[1] is not int)
                return zero;

            var totalAmount = (int)values[1];
            if (totalAmount == 0)
                return zero;

            var currentAmount = (int)values[0];
            var percentageValue = decimal.Divide(currentAmount, totalAmount) * 100;

            return $"{currentAmount}({percentageValue:N2}%)";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
