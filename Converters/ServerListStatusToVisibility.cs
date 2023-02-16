using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace HydropressDB.Converters
{
    internal class ServerListStatusToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value as ServerListStatus? == null)
                return null;

            switch((ServerListStatus)value)
            {
                //case ServerListStatus.Updating:
                //case ServerListStatus.Failed:
                case ServerListStatus.Updated:
                    return Visibility.Collapsed;
                default:
                    return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
