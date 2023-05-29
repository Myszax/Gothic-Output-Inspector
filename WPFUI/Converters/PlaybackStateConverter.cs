using NAudio.Wave;
using System;
using System.Globalization;
using System.Windows.Data;

namespace WPFUI.Converters
{
    public sealed class PlaybackStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not PlaybackState playbackState)
                return value;

            return playbackState switch
            {
                PlaybackState.Stopped or PlaybackState.Paused => "Solid_Play",
                PlaybackState.Playing => "Solid_Pause",
                _ => value,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
