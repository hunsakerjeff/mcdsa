using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace DSA.Shell.Converters
{
    public class MarginConverter : IValueConverter
    {
        public MarginType MarginType { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            switch(MarginType)
            {
                case MarginType.Left:
                    return new Thickness(System.Convert.ToDouble(value), 0, 0, 0);
                case MarginType.Top:
                    return new Thickness(0, System.Convert.ToDouble(value), 0, 0);
                case MarginType.Right:
                    return new Thickness(0, 0, System.Convert.ToDouble(value), 0);
                case MarginType.Bottom:
                default:
                    return new Thickness(0, 0, 0, System.Convert.ToDouble(value));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public enum MarginType
    {
        Left,
        Top,
        Right,
        Bottom
    }
}
