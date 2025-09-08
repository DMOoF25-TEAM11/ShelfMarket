using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Interfaces;
using ShelfMarket.Domain.Entities;
using ShelfMarket.UI.ViewModels.Abstracts;

namespace ShelfMarket.UI.ViewModels;

/// <summary>
/// ViewModel for managing ShelfType entities in the UI.
/// Handles form state, validation, and command logic for ShelfType CRUD operations.
/// </summary>
public sealed class ShelfTypeViewModel : ViewModelBase<IShelfTypeRepository, ShelfType>
{
    /// <summary>
    /// The display name for the entity, used in UI messages.
    /// </summary>
    private new const string _entityName = "Reol Type";

    #region Form Fields

    private string _name = string.Empty;

    /// <summary>
    /// Gets or sets the name of the shelf type.
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
                RefreshCommandStates();
            }
        }
    }

    private string _description = string.Empty;

    /// <summary>
    /// Gets or sets the description of the shelf type.
    /// </summary>
    public string Description
    {
        get => _description;
        set
        {
            if (_description != value)
            {
                _description = value;
                OnPropertyChanged();
                RefreshCommandStates();
            }
        }
    }
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ShelfTypeViewModel"/> class.
    /// </summary>
    /// <param name="selected">Optional repository instance to use. If null, resolves from DI container.</param>
    public ShelfTypeViewModel(IShelfTypeRepository? selected = null) : base(selected ?? App.HostInstance.Services.GetRequiredService<IShelfTypeRepository>())
    {
        // Initialize commands and other properties here
    }

    #region Load handler
    #endregion

    #region CanXXX Command States
    /// <summary>
    /// Determines whether the add operation can be performed based on form validation.
    /// </summary>
    /// <returns>True if add is allowed; otherwise, false.</returns>
    protected override bool CanAdd() =>
        base.CanAdd() &&
        !string.IsNullOrWhiteSpace(Description) &&
        !string.IsNullOrWhiteSpace(Name);

    /// <summary>
    /// Determines whether the save operation can be performed based on form validation and changes.
    /// </summary>
    /// <returns>True if save is allowed; otherwise, false.</returns>
    protected override bool CanSave() =>
        base.CanSave() &&
        !string.IsNullOrWhiteSpace(Description) &&
        !string.IsNullOrWhiteSpace(Name) &&
        (Name != CurrentEntity?.Name || Description != CurrentEntity?.Description);
    #endregion

    #region Command Handlers
    /// <summary>
    /// Resets the form fields to their default values.
    /// </summary>
    /// <returns>A completed task.</returns>
    protected override async Task OnResetFormAsync()
    {
        CurrentEntity = null;
        Description = string.Empty;
        Name = string.Empty;
        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates a new <see cref="ShelfType"/> entity from the current form values.
    /// </summary>
    /// <returns>The newly created ShelfType entity.</returns>
    protected override async Task<ShelfType> OnAddFormAsync()
    {
        var entity = new ShelfType(Name, Description);
        await Task.CompletedTask;
        return entity;
    }

    /// <summary>
    /// Updates the current entity with the form values before saving.
    /// </summary>
    /// <returns>A completed task.</returns>
    protected override async Task OnSaveFormAsync()
    {
        if (CurrentEntity == null)
        {
            Error = _errorEntityNotFound;
            return;
        }
        CurrentEntity.Name = Name;
        CurrentEntity.Description = Description;
        await Task.CompletedTask;
    }

    /// <summary>
    /// Loads the form fields from the specified <see cref="ShelfType"/> entity.
    /// </summary>
    /// <param name="entity">The entity to load values from.</param>
    /// <returns>A completed task.</returns>
    protected override async Task OnLoadFormAsync(ShelfType entity)
    {
        Name = entity.Name;
        Description = entity.Description;
        await Task.CompletedTask;
    }
    #endregion

}
