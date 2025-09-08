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
    /// <summary>
    /// Loads a ShelfType entity by its identifier and populates the form fields.
    /// </summary>
    /// <param name="id">The identifier of the ShelfType to load.</param>
    public async Task LoadAsync(Guid id)
    {
        Error = null;
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity != null)
            {
                _currentEntity = entity; // keep current entity in sync with selection
                Name = entity.Name;
                Description = entity.Description;
                IsEditMode = true;
            }
            else
            {
                _currentEntity = null;   // clear when not found
                IsEditMode = false;
                Error = _errorEntityNotFound;
            }
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            RefreshCommandStates();
        }
    }
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
        var shelfType = new ShelfType(Name, Description);
        await Task.CompletedTask;
        return shelfType;
    }

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
    #endregion

    /// <summary>
    /// Gets the identifier of the specified <see cref="ShelfType"/> entity.
    /// </summary>
    /// <param name="entity">The ShelfType entity.</param>
    /// <returns>The entity's identifier, or null if not set.</returns>
    protected override Guid? GetEntityId(ShelfType entity) => entity.Id;
}
