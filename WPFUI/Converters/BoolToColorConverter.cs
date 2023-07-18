using System;
using System.Drawing;
using System.Windows.Data;

namespace WPFUI.Converters
{
    public sealed class BoolToColorConverter : IValueConverter
    {
        public Color NormalColor { get; set; } = Color.White;
        public Color MarkedColor { get; set; } = Color.RoyalBlue;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool && value is true)
                return MarkedColor.Name;

            return NormalColor.Name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}