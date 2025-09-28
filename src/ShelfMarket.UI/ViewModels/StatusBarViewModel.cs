using ShelfMarket.UI.ViewModels.Abstracts;

namespace ShelfMarket.UI.ViewModels;

/// <summary>
/// View model for an application status bar, exposing user / database connectivity
/// and persistence activity indicators suitable for data binding in a WPF UI.
/// </summary>
/// <remarks>
/// Typical bindings:
///  - <see cref="User"/>: Displays current signed-in user or privilege descriptor.
///  - <see cref="IsDbConnected"/> / <see cref="IsDbNotConnected"/>: Toggle visual indicators (e.g. green/red icon).
///  - <see cref="IsDbSaving"/>: Show a progress animation / spinner while save operations are in progress.
/// All properties raise <c>PropertyChanged</c> notifications to keep the UI in sync.
/// </remarks>
public class StatusBarViewModel : ModelBase
{
    private string _user = string.Empty;

    /// <summary>
    /// Gets or sets the display name / identifier of the current user or session context.
    /// </summary>
    public string User
    {
        get => _user;
        set
        {
            if (_user == value) return;
            _user = value;
            OnPropertyChanged();
        }
    }

    private bool _isDbConnected;

    /// <summary>
    /// Gets or sets a value indicating whether the application currently has an active
    /// (healthy) connection to the database / persistence layer.
    /// </summary>
    /// <remarks>
    /// Setting this property also raises <see cref="IsDbNotConnected"/> change notification
    /// to simplify inverse bindings in XAML.
    /// </remarks>
    public bool IsDbConnected
    {
        get => _isDbConnected;
        set
        {
            if (_isDbConnected == value) return;
            _isDbConnected = value;
            OnPropertyChanged(nameof(IsDbNotConnected));
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets a convenience inverse of <see cref="IsDbConnected"/> for direct binding (e.g. visibility triggers).
    /// </summary>
    public bool IsDbNotConnected => !IsDbConnected;

    private bool _isDbSaving;

    /// <summary>
    /// Gets or sets a value indicating whether a database save / persistence operation
    /// is currently in progress (used to display a busy indicator).
    /// </summary>
    public bool IsDbSaving
    {
        get => _isDbSaving;
        set
        {
            if (_isDbSaving == value) return;
            _isDbSaving = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusBarViewModel"/> with default
    /// (empty / disconnected / idle) state values.
    /// </summary>
    public StatusBarViewModel()
    {
        User = string.Empty;
        IsDbConnected = false;
        IsDbSaving = false;
    }
}
