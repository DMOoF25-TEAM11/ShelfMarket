using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ShelfMarket.Domain.Enums;

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

    // Expose month list (ReceiptNumber = 1..12, Name = Danish month)
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

        public static readonly DependencyProperty IsFlexibleProperty =
            DependencyProperty.Register(nameof(IsFlexible), typeof(bool), typeof(MonthYearPicker),
                new FrameworkPropertyMetadata(false, OnIsFlexibleChanged));

        /// <summary>
        /// When true, allows selection of past months/years. When false (default), restricts to current month/year and forward.
        /// </summary>
        public bool IsFlexible
        {
            get => (bool)GetValue(IsFlexibleProperty);
            set => SetValue(IsFlexibleProperty, value);
        }

        private static object CoerceSelectedYear(DependencyObject d, object baseValue)
        {
            var ctl = (MonthYearPicker)d;
            int year = (int)baseValue;
            
            // If flexible, allow any year (with reasonable bounds)
            if (ctl.IsFlexible)
            {
                return Math.Clamp(year, 1900, 2100);
            }
            
            // If not flexible, restrict to current year and forward
            int min = DateTime.Now.Year;
            return year < min ? min : year;
        }

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

        private static void OnIsFlexibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctl = (MonthYearPicker)d;
            ctl.UpdateYearControlsEnabledState();
            // Re-coerce values when flexibility changes
            ctl.CoerceValue(SelectedYearProperty);
            ctl.CoerceValue(SelectedMonthProperty);
        }

        private void UpdateYearControlsEnabledState()
        {
            if (DecrementYearButton != null)
            {
                if (IsFlexible)
                {
                    // In flexible mode, allow going back to reasonable year (1900)
                    DecrementYearButton.IsEnabled = SelectedYear > 1900;
                }
                else
                {
                    // In non-flexible mode, only allow going back to current year
                    int min = DateTime.Now.Year;
                    DecrementYearButton.IsEnabled = SelectedYear > min;
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