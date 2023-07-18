using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using WPFUI.Enums;

namespace WPFUI.Converters
{
    public sealed class ComparisonResultTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null || value is not ComparisonResultType comparisonResultType || comparisonResultType != ComparisonResultType.Changed)
                return Visibility.Hidden;

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
