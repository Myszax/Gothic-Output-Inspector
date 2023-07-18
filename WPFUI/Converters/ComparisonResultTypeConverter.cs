using System;
using System.Globalization;
using System.Windows.Data;
using WPFUI.Enums;

namespace WPFUI.Converters;

public sealed class ComparisonResultTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ComparisonResultType comparisonResultType)
            return value;

        return comparisonResultType switch
        {
            ComparisonResultType.Added => "Add",
            ComparisonResultType.Removed => "Remove",
            _ => "Replace",
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}