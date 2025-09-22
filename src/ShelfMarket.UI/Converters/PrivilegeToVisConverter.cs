using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract.Services;
using ShelfMarket.Domain.Enums;

namespace ShelfMarket.UI.Converters;

public sealed class PrivilegeToVisConverter : IValueConverter, IMultiValueConverter
{
    // Single-binding (kept for compatibility)
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PrivilegeLevel privilege)
        {
            var svc = App.HostInstance.Services.GetRequiredService<IPrivilegeService>();
            return svc.CanAccess(privilege) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;

    // MultiBinding: [0] = PrivilegeLevel, [1] = CurrentLevel (only to trigger re-eval)
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is { Length: >= 1 } && values[0] is PrivilegeLevel privilege)
        {
            var svc = App.HostInstance.Services.GetRequiredService<IPrivilegeService>();
            return svc.CanAccess(privilege) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => new object[] { Binding.DoNothing, Binding.DoNothing };
}