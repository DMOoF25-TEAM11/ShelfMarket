using System.Collections.ObjectModel;
using ShelfMarket.Application.Abstract;
using ShelfMarket.UI.Commands;
using ShelfMarket.UI.Models;

namespace ShelfMarket.UI.ViewModels.Reports;

public class ReportDailyCashViewModel : ReportViewModelBase
{
    private readonly ISalesRepository _repository;
    private CancellationTokenSource? _loadCts;

    public ReportDailyCashViewModel(ISalesRepository repository) : base("Kasserapport")
    {
        _repository = repository;
        Date = DateTime.Now;
        RefreshCommand = new RelayCommand(async () => await LoadAsync());

        var values = new decimal[]
        {
            1000, 500, 200, 100, 50,
            20, 10, 5, 2, 1, 0.5m
        };
        foreach (var v in values)
        {
            var d = new CashDenomination(v);
            d.CountChanged += (_, _) => { Recalculate(); };
            Denominations.Add(d);
        }

        Recalculate();
        _ = LoadAsync();
    }

    public ObservableCollection<CashDenomination> Denominations { get; } = new();

    private DateTime _date = DateTime.Today;
    public DateTime Date
    {
        get => _date;
        set
        {
            var normalized = value.Date;
            if (_date == normalized) return;
            _date = normalized;
            OnPropertyChanged();
            _ = LoadAsync();
        }
    }

    private decimal _openingCash;
    public decimal OpeningCash
    {
        get => _openingCash;
        set
        {
            if (_openingCash == value) return;
            _openingCash = value;
            OnPropertyChanged();
            Recalculate();
        }
    }

    private decimal _cashSalesSystem;
    public decimal CashSalesSystem
    {
        get => _cashSalesSystem;
        private set
        {
            if (_cashSalesSystem == value) return;
            _cashSalesSystem = value;
            OnPropertyChanged();
            Recalculate();
        }
    }

    private decimal _expectedCash;
    public decimal ExpectedCash
    {
        get => _expectedCash;
        private set
        {
            if (_expectedCash == value) return;
            _expectedCash = value;
            OnPropertyChanged();
        }
    }

    private decimal _countedCash;
    public decimal CountedCash
    {
        get => _countedCash;
        private set
        {
            if (_countedCash == value) return;
            _countedCash = value;
            OnPropertyChanged();
        }
    }

    public decimal Difference => CountedCash - ExpectedCash;

    private string? _note;
    public string? Note
    {
        get => _note;
        set
        {
            if (_note == value) return;
            _note = value;
            OnPropertyChanged();
        }
    }

    public RelayCommand RefreshCommand { get; }

    private async Task LoadAsync()
    {
        _loadCts?.Cancel();
        var cts = new CancellationTokenSource();
        _loadCts = cts;

        var snapshotDate = Date;
        try
        {
            var sales = await _repository.GetCashSalesAsync(snapshotDate);
            if (!cts.IsCancellationRequested && snapshotDate == Date)
            {
                CashSalesSystem = sales;
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void Recalculate()
    {
        CountedCash = OpeningCash + Denominations.Sum(d => d.Amount);
        ExpectedCash = OpeningCash + CashSalesSystem;
        OnPropertyChanged(nameof(Difference));
    }
}
