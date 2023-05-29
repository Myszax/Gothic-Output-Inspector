using System;
using System.Globalization;
using System.Windows.Data;

namespace WPFUI.Converters
{
    public sealed class VolumeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((float)value == 0f)
                return "0%";
            else if ((float)value == 1f)
                return "100%";
            return $"{((float)value * 100):N2}%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
