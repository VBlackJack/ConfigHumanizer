// Copyright 2025 Julien Bombled
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ConfigHumanizer.Core.Factories;
using ConfigHumanizer.Core.Models;
using ConfigHumanizer.Core.Services;
using ConfigHumanizer.UI.Helpers;

namespace ConfigHumanizer.UI.ViewModels;

/// <summary>
/// ViewModel pour l'éditeur de paramètres WYSIWYG.
/// </summary>
public class ParameterEditorViewModel : INotifyPropertyChanged
{
    private readonly ParameterSchemaEngine _schemaEngine;
    private readonly ParameterValidationService _validationService;

    private ParameterSchema? _currentSchema;
    private ParameterCategory? _selectedCategory;
    private ParameterDefinition? _selectedParameter;
    private ParameterValue? _currentValue;
    private string _searchText = string.Empty;
    private string _generatedLine = string.Empty;
    private bool _canAdd;

    public ParameterEditorViewModel(ParameterSchemaEngine schemaEngine)
    {
        _schemaEngine = schemaEngine ?? throw new ArgumentNullException(nameof(schemaEngine));
        _validationService = new ParameterValidationService();

        Categories = new ObservableCollection<ParameterCategory>();
        FilteredParameters = new ObservableCollection<ParameterDefinition>();

        AddCommand = new RelayCommand(_ => Add(), _ => CanAdd);
        CancelCommand = new RelayCommand(_ => Cancel());
    }

    #region Properties

    /// <summary>
    /// Schéma de configuration actuel.
    /// </summary>
    public ParameterSchema? CurrentSchema
    {
        get => _currentSchema;
        set
        {
            if (_currentSchema != value)
            {
                _currentSchema = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormatName));
                OnPropertyChanged(nameof(HasSchema));
                LoadCategories();
            }
        }
    }

    /// <summary>
    /// Nom du format actuel.
    /// </summary>
    public string FormatName => _currentSchema?.FormatName ?? "Non détecté";

    /// <summary>
    /// Indique si un schéma est chargé.
    /// </summary>
    public bool HasSchema => _currentSchema != null;

    /// <summary>
    /// Catégories de paramètres.
    /// </summary>
    public ObservableCollection<ParameterCategory> Categories { get; }

    /// <summary>
    /// Catégorie sélectionnée.
    /// </summary>
    public ParameterCategory? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (_selectedCategory != value)
            {
                _selectedCategory = value;
                OnPropertyChanged();
                FilterParameters();
            }
        }
    }

    /// <summary>
    /// Paramètres filtrés à afficher.
    /// </summary>
    public ObservableCollection<ParameterDefinition> FilteredParameters { get; }

    /// <summary>
    /// Paramètre sélectionné.
    /// </summary>
    public ParameterDefinition? SelectedParameter
    {
        get => _selectedParameter;
        set
        {
            if (_selectedParameter != value)
            {
                _selectedParameter = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedParameter));
                OnPropertyChanged(nameof(IsEnumParameter));
                OnPropertyChanged(nameof(IsBooleanParameter));
                OnPropertyChanged(nameof(IsIntegerParameter));
                OnPropertyChanged(nameof(IsStringParameter));
                CreateParameterValue();
            }
        }
    }

    /// <summary>
    /// Indique si un paramètre est sélectionné.
    /// </summary>
    public bool HasSelectedParameter => _selectedParameter != null;

    /// <summary>
    /// Valeur actuelle en cours d'édition.
    /// </summary>
    public ParameterValue? CurrentValue
    {
        get => _currentValue;
        private set
        {
            if (_currentValue != value)
            {
                if (_currentValue != null)
                {
                    _currentValue.PropertyChanged -= CurrentValue_PropertyChanged;
                }

                _currentValue = value;

                if (_currentValue != null)
                {
                    _currentValue.PropertyChanged += CurrentValue_PropertyChanged;
                }

                OnPropertyChanged();
                UpdateGeneratedLine();
            }
        }
    }

    /// <summary>
    /// Texte de recherche.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                OnPropertyChanged();
                FilterParameters();
            }
        }
    }

    /// <summary>
    /// Ligne de configuration générée.
    /// </summary>
    public string GeneratedLine
    {
        get => _generatedLine;
        private set
        {
            if (_generatedLine != value)
            {
                _generatedLine = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Indique si on peut ajouter le paramètre.
    /// </summary>
    public bool CanAdd
    {
        get => _canAdd;
        private set
        {
            if (_canAdd != value)
            {
                _canAdd = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    // Helpers pour le type de paramètre
    public bool IsEnumParameter => _selectedParameter?.DataType == ParameterDataType.Enum;
    public bool IsBooleanParameter => _selectedParameter?.DataType == ParameterDataType.Boolean;
    public bool IsIntegerParameter => _selectedParameter?.DataType is ParameterDataType.Integer
        or ParameterDataType.Port;
    public bool IsStringParameter => _selectedParameter?.DataType is ParameterDataType.String
        or ParameterDataType.Path or ParameterDataType.IpAddress
        or ParameterDataType.Duration or ParameterDataType.Size;

    #endregion

    #region Commands

    public ICommand AddCommand { get; }
    public ICommand CancelCommand { get; }

    /// <summary>
    /// Événement déclenché quand l'utilisateur clique sur Ajouter.
    /// </summary>
    public event EventHandler<ParameterAddedEventArgs>? ParameterAdded;

    /// <summary>
    /// Événement déclenché quand l'utilisateur annule.
    /// </summary>
    public event EventHandler? Cancelled;

    #endregion

    #region Methods

    /// <summary>
    /// Initialise l'éditeur pour un fichier donné.
    /// </summary>
    public void Initialize(string? filePath)
    {
        CurrentSchema = _schemaEngine.GetSchemaForFile(filePath);
    }

    /// <summary>
    /// Initialise l'éditeur avec un schéma spécifique.
    /// </summary>
    public void Initialize(ParameterSchema schema)
    {
        CurrentSchema = schema;
    }

    private void LoadCategories()
    {
        Categories.Clear();
        SelectedCategory = null;
        SelectedParameter = null;

        if (_currentSchema == null)
            return;

        foreach (var category in _currentSchema.ParameterCategories.OrderBy(c => c.Order))
        {
            Categories.Add(category);
        }

        if (Categories.Count > 0)
        {
            SelectedCategory = Categories[0];
        }
    }

    private void FilterParameters()
    {
        FilteredParameters.Clear();

        IEnumerable<ParameterDefinition> parameters;

        if (_selectedCategory != null)
        {
            parameters = _selectedCategory.Parameters;
        }
        else if (_currentSchema != null)
        {
            parameters = _currentSchema.GetAllParameters();
        }
        else
        {
            return;
        }

        // Appliquer le filtre de recherche
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var search = _searchText.ToLowerInvariant();
            parameters = parameters.Where(p =>
                p.Key.ToLowerInvariant().Contains(search) ||
                p.DisplayName.ToLowerInvariant().Contains(search) ||
                (p.Description?.ToLowerInvariant().Contains(search) ?? false));
        }

        foreach (var param in parameters)
        {
            FilteredParameters.Add(param);
        }

        // Sélectionner le premier si aucune sélection
        if (SelectedParameter == null && FilteredParameters.Count > 0)
        {
            SelectedParameter = FilteredParameters[0];
        }
    }

    private void CreateParameterValue()
    {
        if (_selectedParameter == null)
        {
            CurrentValue = null;
            return;
        }

        CurrentValue = new ParameterValue(_selectedParameter);
        UpdateGeneratedLine();
    }

    private void CurrentValue_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ParameterValue.Value))
        {
            ValidateAndUpdate();
            UpdateGeneratedLine();
        }
    }

    private void ValidateAndUpdate()
    {
        if (_currentValue == null)
        {
            CanAdd = false;
            return;
        }

        _validationService.ValidateAndUpdate(_currentValue);
        CanAdd = _currentValue.IsValid && _currentValue.Value != null;
    }

    private void UpdateGeneratedLine()
    {
        if (_currentSchema == null || _selectedParameter == null || _currentValue?.Value == null)
        {
            GeneratedLine = string.Empty;
            return;
        }

        GeneratedLine = WriterFactory.GenerateLine(_currentSchema, _selectedParameter, _currentValue.Value);
        _currentValue.UpdateGeneratedLine(GeneratedLine);
    }

    private void Add()
    {
        if (!CanAdd || _currentValue == null || string.IsNullOrEmpty(GeneratedLine))
            return;

        ParameterAdded?.Invoke(this, new ParameterAddedEventArgs(GeneratedLine, _selectedParameter!));
    }

    private void Cancel()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}

/// <summary>
/// Arguments de l'événement ParameterAdded.
/// </summary>
public class ParameterAddedEventArgs : EventArgs
{
    public string GeneratedLine { get; }
    public ParameterDefinition Parameter { get; }

    public ParameterAddedEventArgs(string generatedLine, ParameterDefinition parameter)
    {
        GeneratedLine = generatedLine;
        Parameter = parameter;
    }
}
