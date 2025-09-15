using ShelfMarket.UI.ViewModels.Abstracts;

namespace ShelfMarket.UI.ViewModels;

public class StatusBarViewModel : ModelBase
{
    private string _user = string.Empty; // Initialize here

    public string User
    {
        get { return _user; }
        set { _user = value; OnPropertyChanged(); }
    }

    private bool _isDbConnected;

    public bool IsDbConnected
    {
        get { return _isDbConnected; }
        set
        {
            _isDbConnected = value;
            OnPropertyChanged(nameof(IsDbNotConnected));
            OnPropertyChanged();
        }
    }

    public bool IsDbNotConnected => !IsDbConnected;

    private bool _isDbSaving;

    public bool IsDbSaving
    {
        get { return _isDbSaving; }
        set { _isDbSaving = value; OnPropertyChanged(); }
    }



    public StatusBarViewModel()
    {
        User = string.Empty;
        IsDbConnected = false;
        IsDbSaving = false;
    }
}
