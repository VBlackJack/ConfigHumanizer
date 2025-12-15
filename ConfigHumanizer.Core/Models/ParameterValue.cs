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

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ConfigHumanizer.Core.Models;

/// <summary>
/// Représente une valeur de paramètre en cours d'édition.
/// </summary>
public class ParameterValue : INotifyPropertyChanged
{
    private object? _value;
    private string? _validationError;
    private bool _isValid = true;

    /// <summary>
    /// Définition du paramètre associé.
    /// </summary>
    public ParameterDefinition Definition { get; }

    /// <summary>
    /// Valeur actuelle du paramètre.
    /// </summary>
    public object? Value
    {
        get => _value;
        set
        {
            if (!Equals(_value, value))
            {
                _value = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayValue));
                OnPropertyChanged(nameof(GeneratedLine));
            }
        }
    }

    /// <summary>
    /// Valeur formatée pour affichage.
    /// </summary>
    public string DisplayValue => FormatValueForDisplay();

    /// <summary>
    /// Ligne de configuration générée.
    /// </summary>
    public string GeneratedLine { get; private set; } = string.Empty;

    /// <summary>
    /// Erreur de validation, le cas échéant.
    /// </summary>
    public string? ValidationError
    {
        get => _validationError;
        set
        {
            if (_validationError != value)
            {
                _validationError = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Indique si la valeur est valide.
    /// </summary>
    public bool IsValid
    {
        get => _isValid;
        set
        {
            if (_isValid != value)
            {
                _isValid = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Sévérité associée à la valeur actuelle (pour Enum).
    /// </summary>
    public Severity? CurrentSeverity
    {
        get
        {
            if (Definition.DataType == ParameterDataType.Enum &&
                Definition.EnumValues != null &&
                Value != null)
            {
                var option = Definition.EnumValues
                    .FirstOrDefault(e => e.Value.Equals(Value.ToString(), StringComparison.OrdinalIgnoreCase));
                return option?.Severity;
            }
            return null;
        }
    }

    public ParameterValue(ParameterDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _value = definition.DefaultValue;
    }

    /// <summary>
    /// Met à jour la ligne de configuration générée.
    /// </summary>
    public void UpdateGeneratedLine(string line)
    {
        if (GeneratedLine != line)
        {
            GeneratedLine = line;
            OnPropertyChanged(nameof(GeneratedLine));
        }
    }

    private string FormatValueForDisplay()
    {
        if (Value == null)
            return string.Empty;

        return Definition.DataType switch
        {
            ParameterDataType.Boolean => FormatBoolean(),
            ParameterDataType.Size => FormatWithUnit(),
            ParameterDataType.Duration => FormatWithUnit(),
            ParameterDataType.Enum => GetEnumDisplayName(),
            _ => Value.ToString() ?? string.Empty
        };
    }

    private string FormatBoolean()
    {
        if (Value is not bool boolValue)
            return Value?.ToString() ?? string.Empty;

        var format = Definition.BooleanFormat ?? "true/false";
        var parts = format.Split('/');
        if (parts.Length != 2)
            return boolValue.ToString().ToLower();

        return boolValue ? parts[0] : parts[1];
    }

    private string FormatWithUnit()
    {
        var valueStr = Value?.ToString() ?? string.Empty;
        return string.IsNullOrEmpty(Definition.Unit)
            ? valueStr
            : $"{valueStr} {Definition.Unit}";
    }

    private string GetEnumDisplayName()
    {
        if (Definition.EnumValues == null || Value == null)
            return Value?.ToString() ?? string.Empty;

        var option = Definition.EnumValues
            .FirstOrDefault(e => e.Value.Equals(Value.ToString(), StringComparison.OrdinalIgnoreCase));

        return option?.DisplayName ?? Value.ToString() ?? string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
