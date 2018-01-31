using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace DSA.Shell.Converters
{
    public class ScreenPositionConverter : IValueConverter
    {
        public ScreenPositionType PositionType { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var dims = new []{ Window.Current.Bounds.Width, Window.Current.Bounds.Height};
            var width = dims.Max();
            var height = dims.Min();

            var factor = PositionType == ScreenPositionType.X
                            ? width / 1024.0
                            : height / 768.0;

            return System.Convert.ToDouble(value) * factor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public enum ScreenPositionType
    {
        X,
        Y
    }
}
