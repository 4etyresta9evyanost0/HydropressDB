using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace HydropressDB.Converters
{
    public class ServerListStatusToBoolean : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value as ServerListStatus? == null)
            {
                return null;
            }

            if ((string)parameter == "Failed")
            {
                return (value as ServerListStatus?) == ServerListStatus.Failed;
            }

            if ((string)parameter == "Updated")
            {
                return (value as ServerListStatus?) == ServerListStatus.Updated;
            }

            if ((string)parameter == "Updating")
            {
                return (value as ServerListStatus?) == ServerListStatus.Updating;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
