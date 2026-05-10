using System.Globalization;

namespace StokBarangMAUI.Converters
{
    public class StokColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int stok && stok == 0)
                return Color.FromArgb("#dc3545");
            return Color.FromArgb("#28a745");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
