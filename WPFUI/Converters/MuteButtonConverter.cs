using System;
using System.Globalization;
using System.Windows.Data;

namespace WPFUI.Converters
{
    public sealed class MuteButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Boolean muted)
                return value;

            if (muted)
                return "Solid_VolumeXmark";

            return "Solid_VolumeHigh";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
