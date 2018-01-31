using System;
using Windows.UI.Xaml.Data;
using DSA.Shell.ViewModels.VisualBrowser.ControlBar.CheckInOut;

namespace DSA.Shell.Converters
{
    public class EnumBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string parameterString = parameter as string;
            object parameterValue = Enum.Parse(value.GetType(), parameterString);

            return parameterValue.Equals(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            string parameterString = parameter as string;
          

            return Enum.Parse(typeof(ContactTypeFilter), parameterString);
        }
    }
}
