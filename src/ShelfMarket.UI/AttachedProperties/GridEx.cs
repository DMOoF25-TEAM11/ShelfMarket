using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ShelfMarket.UI.AttachedProperties;

// Allows specifying grid row index from the bottom (0 = bottom row).
public static class GridEx
{
    public static readonly DependencyProperty BottomRowIndexProperty =
        DependencyProperty.RegisterAttached(
            "BottomRowIndex",
            typeof(int),
            typeof(GridEx),
            new PropertyMetadata(-1, OnBottomRowIndexChanged));

    public static void SetBottomRowIndex(DependencyObject element, int value)
        => element.SetValue(BottomRowIndexProperty, value);

    public static int GetBottomRowIndex(DependencyObject element)
        => (int)element.GetValue(BottomRowIndexProperty);

    private static void OnBottomRowIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe) return;

        void Apply()
        {
            var grid = FindParentGrid(fe);
            if (grid is null) return;

            int fromBottom = GetBottomRowIndex(fe);
            if (fromBottom < 0) return;

            int rows = grid.RowDefinitions.Count;
            if (rows <= 0) return;

            int target = Math.Clamp((rows - 1) - fromBottom, 0, rows - 1);
            Grid.SetRow(fe, target);
        }

        if (fe.IsLoaded)
        {
            Apply();
        }
        else
        {
            fe.Loaded -= FeOnLoaded;
            fe.Loaded += FeOnLoaded;
        }

        void FeOnLoaded(object? sender, RoutedEventArgs args)
        {
            fe.Loaded -= FeOnLoaded;
            Apply();
        }
    }

    private static Grid? FindParentGrid(DependencyObject start)
    {
        DependencyObject? current = start;
        while (current is not null)
        {
            if (current is Grid g) return g;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}