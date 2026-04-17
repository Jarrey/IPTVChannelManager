using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace IPTVChannelManager.Common
{
    public class ImageConverter : IValueConverter
    {
        private static readonly ConcurrentDictionary<string, BitmapImage> _cache = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var url = value?.ToString();
            if (string.IsNullOrEmpty(url))
                return null;

            if (_cache.TryGetValue(url, out var cached))
                return cached;

            try
            {
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(url, UriKind.Relative);
                bi.DecodePixelWidth = 80;
                bi.CacheOption = BitmapCacheOption.Default;
                bi.EndInit();

                _cache.TryAdd(url, bi);
                return bi;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
