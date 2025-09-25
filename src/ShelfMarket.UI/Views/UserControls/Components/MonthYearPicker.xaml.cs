using System.Windows;
using System.Windows.Controls;

namespace ShelfMarket.UI.Behaviors;

public static class DatePickerMonthYearBehavior
{
    public static readonly DependencyProperty IsMonthYearProperty =
        DependencyProperty.RegisterAttached(
            "IsMonthYear",
            typeof(bool),
            typeof(DatePickerMonthYearBehavior),
            new PropertyMetadata(false, OnIsMonthYearChanged));

    public static void SetIsMonthYear(DependencyObject element, bool value) =>
        element.SetValue(IsMonthYearProperty, value);

    public static bool GetIsMonthYear(DependencyObject element) =>
        (bool)element.GetValue(IsMonthYearProperty);

    private static void OnIsMonthYearChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DatePicker dp) return;

        if ((bool)e.NewValue)
        {
            dp.CalendarOpened += OnCalendarOpened;
            dp.CalendarClosed += OnCalendarClosed;
        }
        else
        {
            dp.CalendarOpened -= OnCalendarOpened;
            dp.CalendarClosed -= OnCalendarClosed;
        }
    }

    private static void OnCalendarOpened(object? sender, RoutedEventArgs e)
    {
        if (sender is not DatePicker dp) return;

        if (dp.Template.FindName("PART_Calendar", dp) is Calendar cal)
        {
            // Initialize calendar to Year mode to choose a month
            cal.DisplayMode = CalendarMode.Year;
            cal.SelectionMode = CalendarSelectionMode.SingleDate;
            cal.DisplayDate = dp.SelectedDate ?? DateTime.Today;

            // Use a local handler tied to this datepicker
            EventHandler<CalendarModeChangedEventArgs> Handler = (s, args) =>
            {
                if (cal.DisplayMode == CalendarMode.Month)
                {
                    // A month was clicked. Set the first day of that month and close the popup.
                    var dt = new DateTime(cal.DisplayDate.Year, cal.DisplayDate.Month, 1);
                    dp.SelectedDate = dt;
                    // Keep calendar in Year mode next time
                    cal.DisplayMode = CalendarMode.Year;
                    dp.IsDropDownOpen = false;
                }
                // Prevent diving into day view by staying in Year->Month only
                if (cal.DisplayMode == CalendarMode.Decade)
                {
                    cal.DisplayMode = CalendarMode.Year;
                }
            };

            // stash handler so we can detach on close
            cal.Tag = Handler;
            cal.DisplayModeChanged += Handler;
        }
    }

    private static void OnCalendarClosed(object? sender, RoutedEventArgs e)
    {
        if (sender is not DatePicker dp) return;
        if (dp.Template.FindName("PART_Calendar", dp) is Calendar cal && cal.Tag is EventHandler<CalendarModeChangedEventArgs> handler)
        {
            cal.DisplayModeChanged -= handler;
            cal.Tag = null;
        }
    }
}