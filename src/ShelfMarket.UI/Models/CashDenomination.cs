using ShelfMarket.UI.ViewModels.Abstracts;

namespace ShelfMarket.UI.Models;

/// <summary>
/// Represents a single Danish cash denomination with a mutable count.
/// </summary>
public class CashDenomination : ModelBase
{
    public decimal Value { get; }
    private int _count;

    public int Count
    {
        get => _count;
        set
        {
            if (_count == value) return;
            _count = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Amount));
            CountChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public decimal Amount => Value * Count;

    public event EventHandler? CountChanged;

    public CashDenomination(decimal value)
    {
        Value = value;
    }
}