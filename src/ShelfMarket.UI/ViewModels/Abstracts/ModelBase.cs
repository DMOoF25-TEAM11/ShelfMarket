using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ShelfMarket.UI.ViewModels.Abstracts;

/// <summary>
/// Provides a base class for view models, implementing <see cref="INotifyPropertyChanged"/> 
/// to support property change notifications in WPF MVVM scenarios.
/// </summary>
public abstract class ModelBase : INotifyPropertyChanged
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for the specified property.
    /// </summary>
    /// <param name="name">The name of the property that changed. This is optional and
    /// will be automatically provided by the compiler if not specified.</param>
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
