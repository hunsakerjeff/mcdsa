using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace DSA.Shell.Converters
{
    public class BooleanToSelectionModeConverter : IValueConverter
    {
        public bool IsReversed { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var val = System.Convert.ToBoolean(value); 
            if (IsReversed)
            {
                val = val == false;
            }

            return val ? ListViewSelectionMode.Single : ListViewSelectionMode.None;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
