using System.Windows;
using System.Windows.Controls;

namespace ShelfMarket.UI.Views.UserControls.Components;

public partial class MonthYearPicker : UserControl
{
    private bool _updating;

    public MonthYearPicker()
    {
        // Build month items from DanishMonth enum (values 1..12)
        var items = new List<MonthItem>(12);
        foreach (var value in Enum.GetValues<DanishMonth>())
        {
            items.Add(new MonthItem((int)value, value.ToString()));
        }
        Months = items;

        InitializeComponent();

        // Default to now
        _updating = true;
        SelectedYear = DateTime.Now.Year;
        SelectedMonth = DateTime.Now.Month; // 1-12
        SelectedDate = new DateTime(SelectedYear, SelectedMonth, 1);
        _updating = false;

        UpdateYearControlsEnabledState();
    }

    // Expose month list (Number = 1..12, Name = Danish month)
    public IReadOnlyList<MonthItem> Months { get; }

    public static readonly DependencyProperty SelectedDateProperty =
        DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime?), typeof(MonthYearPicker),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDateChanged));

    public DateTime? SelectedDate
    {
        get => (DateTime?)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public static readonly DependencyProperty SelectedYearProperty =
        DependencyProperty.Register(nameof(SelectedYear), typeof(int), typeof(MonthYearPicker),
            new FrameworkPropertyMetadata(DateTime.Now.Year,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnPartChanged,
                CoerceSelectedYear));

    public int SelectedYear
    {
        get => (int)GetValue(SelectedYearProperty);
        set => SetValue(SelectedYearProperty, value);
    }

    // 1..12
    public static readonly DependencyProperty SelectedMonthProperty =
        DependencyProperty.Register(nameof(SelectedMonth), typeof(int), typeof(MonthYearPicker),
            new FrameworkPropertyMetadata(DateTime.Now.Month,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnPartChanged,
                CoerceSelectedMonth));

    public int SelectedMonth
    {
        get => (int)GetValue(SelectedMonthProperty);
        set => SetValue(SelectedMonthProperty, value);
    }


    public static void SetIsMonthYear(DependencyObject element, bool value) =>
        element.SetValue(IsMonthYearProperty, value);

    public static bool GetIsMonthYear(DependencyObject element) =>
        (bool)element.GetValue(IsMonthYearProperty);

    private static void OnIsMonthYearChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DatePicker dp) return;

    private static object CoerceSelectedMonth(DependencyObject d, object baseValue)
    {
        var ctl = (MonthYearPicker)d;
        int month = Math.Clamp((int)baseValue, 1, 12);
        var now = DateTime.Now;

            // If flexible, allow any month
            if (ctl.IsFlexible)
            {
                return month;
            }

            // If not flexible, restrict to current month and forward in current year
            if (ctl.SelectedYear == now.Year && month < now.Month)
                return now.Month;

        return month;
    }

    private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctl = (MonthYearPicker)d;
        if (ctl._updating) return;

        if (e.NewValue is DateTime dt)
        {
            ctl._updating = true;
            ctl.SelectedYear = dt.Year;
            ctl.CoerceValue(SelectedMonthProperty);
            ctl.SelectedMonth = dt.Month; // 1..12
            ctl._updating = false;
            ctl.UpdateYearControlsEnabledState();
        }
    }

    private static void OnPartChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctl = (MonthYearPicker)d;
        if (ctl._updating) return;

            ctl._updating = true;
            try
            {
                // If year changed, ensure month stays valid for current year
                ctl.CoerceValue(SelectedMonthProperty);
                ctl.SelectedDate = new DateTime(ctl.SelectedYear, ctl.SelectedMonth, 1);
            }
            finally
            {
                ctl._updating = false;
                ctl.UpdateYearControlsEnabledState();
            }
        }


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
            }
        }

    private void IncrementYear_Click(object sender, RoutedEventArgs e) => SelectedYear += 1;

        private void DecrementYear_Click(object sender, RoutedEventArgs e)
        {
            if (IsFlexible)
            {
                if (SelectedYear > 1900)
                    SelectedYear -= 1;
            }
            else
            {
                int min = DateTime.Now.Year;
                if (SelectedYear > min)
                    SelectedYear -= 1;
            }
            UpdateYearControlsEnabledState();
        }

    private void YearTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        foreach (var ch in e.Text)
        {
            if (!char.IsDigit(ch))
            {
                e.Handled = true;
                return;
            }
        }
    }

    private void YearTextBox_OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(DataFormats.Text))
        {
            var text = e.DataObject.GetData(DataFormats.Text) as string;
            if (string.IsNullOrEmpty(text)) return;
            foreach (var ch in text)
            {
                if (!char.IsDigit(ch))
                {
                    e.CancelCommand();
                    return;
                }
            }
        }
        else
        {
            e.CancelCommand();
        }
    }

    public readonly struct MonthItem
    {
        public MonthItem(int number, string name)
        {
            Number = number;
            Name = name;

        }

        public int Number { get; }
        public string Name { get; }
    }
}