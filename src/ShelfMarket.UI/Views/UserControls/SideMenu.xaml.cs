using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace ShelfMarket.UI.Views.UserControls;

/// <summary>
/// Interaction logic for SideMenu.xaml
/// </summary>
public partial class SideMenu : UserControl
{
    public SideMenu()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Identifies the <see cref="ItemsSource"/> dependency property.
    /// </summary>
    /// <remarks>This property is used to bind a collection of items to the <see cref="SideMenu"/> control. 
    /// The default value is a predefined array of strings representing menu categories.</remarks>
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(SideMenu), new PropertyMetadata(new[]
        {
            "Reoler",
            "Salg",
            "Økonomi",
            "Arrangementer",
            "Lejere",
            "Vedligeholdelse"
        }));

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
        nameof(SelectedItem), typeof(object), typeof(SideMenu), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }
}
