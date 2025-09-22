using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract.Services;
using ShelfMarket.Domain.Enums;

namespace ShelfMarket.UI.Converters;

public sealed class PrivilegeToVisConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PrivilegeLevel privilege)
        {
            var svc = App.HostInstance.Services.GetRequiredService<IPrivilegeService>();
            return Visibility.Visible;
            return svc.CanAccess(privilege) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}