using System.Globalization;
using System.Windows.Data;

namespace ShelfMarket.UI.Converters;

public sealed class MonthEnabledConverter : IMultiValueConverter
{
    // values[0] = SelectedYear (int), values[1] = Month Number (int 1..12)
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is null || values.Length < 2) return true;
        if (values[0] is int year && values[1] is int month)
        {
            // Check if IsFlexible is provided (values[2])
            bool isFlexible = values.Length > 2 && values[2] is bool flexible && flexible;

            if (isFlexible)
            {
                // In flexible mode, all months are enabled
                return true;
            }

            // Original non-flexible logic

            var now = DateTime.Now;
            if (year > now.Year) return true;
            if (year < now.Year) return false; // guarded by year coercion, but safe
            return month >= now.Month; // year == now.Year
        }
        //if (values[0] is int year && values[1] is int month)
        //{
        //    var now = DateTime.Now;
        //    if (year > now.Year) return true;
        //    if (year < now.Year) return false; // guarded by year coercion, but safe
        //    return month >= now.Month; // year == now.Year
        //}

        return true;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}