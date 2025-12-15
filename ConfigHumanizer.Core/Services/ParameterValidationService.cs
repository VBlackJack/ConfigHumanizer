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

using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using ConfigHumanizer.Core.Models;

namespace ConfigHumanizer.Core.Services;

/// <summary>
/// Résultat de validation d'un paramètre.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Indique si la validation a réussi.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Message d'erreur si la validation a échoué.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Avertissements (non bloquants).
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Crée un résultat de validation réussi.
    /// </summary>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Crée un résultat de validation échoué.
    /// </summary>
    public static ValidationResult Failure(string errorMessage) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Service de validation des valeurs de paramètres.
/// </summary>
public class ParameterValidationService
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Valide une valeur pour un paramètre donné.
    /// </summary>
    /// <param name="definition">Définition du paramètre.</param>
    /// <param name="value">Valeur à valider.</param>
    /// <returns>Résultat de la validation.</returns>
    public ValidationResult Validate(ParameterDefinition definition, object? value)
    {
        // Vérifier si requis
        if (definition.Validation?.Required == true && IsEmpty(value))
        {
            return ValidationResult.Failure(
                definition.Validation.ErrorMessage ?? "Ce paramètre est obligatoire");
        }

        // Valeur vide acceptée si non requis
        if (IsEmpty(value))
        {
            return ValidationResult.Success();
        }

        // Validation par type
        var result = definition.DataType switch
        {
            ParameterDataType.String => ValidateString(definition, value),
            ParameterDataType.Integer => ValidateInteger(definition, value),
            ParameterDataType.Boolean => ValidateBoolean(definition, value),
            ParameterDataType.Enum => ValidateEnum(definition, value),
            ParameterDataType.Path => ValidatePath(definition, value),
            ParameterDataType.IpAddress => ValidateIpAddress(definition, value),
            ParameterDataType.Port => ValidatePort(definition, value),
            ParameterDataType.Duration => ValidateDuration(definition, value),
            ParameterDataType.Size => ValidateSize(definition, value),
            _ => ValidationResult.Success()
        };

        if (!result.IsValid)
        {
            return result;
        }

        // Vérifier les valeurs interdites
        if (definition.Validation?.ForbiddenValues != null)
        {
            var stringValue = value?.ToString() ?? string.Empty;
            if (definition.Validation.ForbiddenValues.Contains(stringValue, StringComparer.OrdinalIgnoreCase))
            {
                return ValidationResult.Failure($"La valeur '{stringValue}' n'est pas autorisée");
            }
        }

        return result;
    }

    /// <summary>
    /// Met à jour un ParameterValue avec le résultat de validation.
    /// </summary>
    public void ValidateAndUpdate(ParameterValue paramValue)
    {
        var result = Validate(paramValue.Definition, paramValue.Value);
        paramValue.IsValid = result.IsValid;
        paramValue.ValidationError = result.ErrorMessage;
    }

    private static bool IsEmpty(object? value)
    {
        return value == null ||
               (value is string s && string.IsNullOrWhiteSpace(s));
    }

    private ValidationResult ValidateString(ParameterDefinition definition, object? value)
    {
        var stringValue = value?.ToString() ?? string.Empty;

        // Validation par pattern regex
        if (!string.IsNullOrEmpty(definition.Validation?.Pattern))
        {
            try
            {
                if (!Regex.IsMatch(stringValue, definition.Validation.Pattern,
                    RegexOptions.None, RegexTimeout))
                {
                    return ValidationResult.Failure(
                        definition.Validation.ErrorMessage ??
                        $"Le format n'est pas valide (attendu: {definition.Validation.Pattern})");
                }
            }
            catch (RegexMatchTimeoutException)
            {
                Debug.WriteLine($"Regex timeout pour pattern: {definition.Validation.Pattern}");
                return ValidationResult.Success(); // Ne pas bloquer sur timeout
            }
            catch (ArgumentException ex)
            {
                Debug.WriteLine($"Pattern regex invalide: {ex.Message}");
                return ValidationResult.Success();
            }
        }

        return ValidationResult.Success();
    }

    private static ValidationResult ValidateInteger(ParameterDefinition definition, object? value)
    {
        if (!TryParseInteger(value, out var intValue))
        {
            return ValidationResult.Failure("La valeur doit être un nombre entier");
        }

        var validation = definition.Validation;
        if (validation?.Min.HasValue == true && intValue < validation.Min.Value)
        {
            return ValidationResult.Failure(
                validation.ErrorMessage ?? $"La valeur minimale est {validation.Min}");
        }

        if (validation?.Max.HasValue == true && intValue > validation.Max.Value)
        {
            return ValidationResult.Failure(
                validation.ErrorMessage ?? $"La valeur maximale est {validation.Max}");
        }

        return ValidationResult.Success();
    }

    private static ValidationResult ValidateBoolean(ParameterDefinition definition, object? value)
    {
        if (value is bool)
        {
            return ValidationResult.Success();
        }

        var stringValue = value?.ToString()?.ToLowerInvariant() ?? string.Empty;
        var validValues = new[] { "true", "false", "yes", "no", "on", "off", "1", "0" };

        if (!validValues.Contains(stringValue))
        {
            return ValidationResult.Failure(
                $"La valeur booléenne n'est pas valide. Valeurs acceptées: {string.Join(", ", validValues)}");
        }

        return ValidationResult.Success();
    }

    private static ValidationResult ValidateEnum(ParameterDefinition definition, object? value)
    {
        if (definition.EnumValues == null || definition.EnumValues.Count == 0)
        {
            return ValidationResult.Success();
        }

        var stringValue = value?.ToString() ?? string.Empty;
        var validValues = definition.EnumValues.Select(e => e.Value).ToList();

        if (!validValues.Contains(stringValue, StringComparer.OrdinalIgnoreCase))
        {
            return ValidationResult.Failure(
                $"Valeur non valide. Options: {string.Join(", ", validValues)}");
        }

        return ValidationResult.Success();
    }

    private ValidationResult ValidatePath(ParameterDefinition definition, object? value)
    {
        var path = value?.ToString() ?? string.Empty;

        // Validation basique de chemin
        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            return ValidationResult.Failure("Le chemin contient des caractères invalides");
        }

        // Validation par pattern si spécifié
        if (!string.IsNullOrEmpty(definition.Validation?.Pattern))
        {
            return ValidateString(definition, value);
        }

        return ValidationResult.Success();
    }

    private static ValidationResult ValidateIpAddress(ParameterDefinition definition, object? value)
    {
        var stringValue = value?.ToString() ?? string.Empty;

        // Accepter les valeurs spéciales
        if (stringValue is "0.0.0.0" or "::" or "localhost" or "*")
        {
            return ValidationResult.Success();
        }

        // Accepter CIDR notation
        var ipPart = stringValue.Split('/')[0];

        if (!IPAddress.TryParse(ipPart, out _))
        {
            return ValidationResult.Failure("Adresse IP non valide");
        }

        return ValidationResult.Success();
    }

    private static ValidationResult ValidatePort(ParameterDefinition definition, object? value)
    {
        if (!TryParseInteger(value, out var port))
        {
            return ValidationResult.Failure("Le port doit être un nombre");
        }

        var min = definition.Validation?.Min ?? 1;
        var max = definition.Validation?.Max ?? 65535;

        if (port < min || port > max)
        {
            return ValidationResult.Failure($"Le port doit être entre {min} et {max}");
        }

        var result = ValidationResult.Success();

        // Avertissements pour ports privilégiés
        if (port < 1024)
        {
            result.Warnings.Add("Ports < 1024 nécessitent des privilèges root");
        }

        return result;
    }

    private static ValidationResult ValidateDuration(ParameterDefinition definition, object? value)
    {
        var stringValue = value?.ToString() ?? string.Empty;

        // Parser les durées avec unités (ex: "30s", "5m", "1h")
        if (!TryParseDuration(stringValue, out var seconds))
        {
            return ValidationResult.Failure(
                "Format de durée non valide. Exemples: 30, 30s, 5m, 1h, 1d");
        }

        var validation = definition.Validation;
        if (validation?.Min.HasValue == true && seconds < validation.Min.Value)
        {
            return ValidationResult.Failure($"La durée minimale est {validation.Min} secondes");
        }

        if (validation?.Max.HasValue == true && seconds > validation.Max.Value)
        {
            return ValidationResult.Failure($"La durée maximale est {validation.Max} secondes");
        }

        return ValidationResult.Success();
    }

    private static ValidationResult ValidateSize(ParameterDefinition definition, object? value)
    {
        var stringValue = value?.ToString() ?? string.Empty;

        // Parser les tailles avec unités (ex: "1024", "1K", "10M", "1G")
        if (!TryParseSize(stringValue, out var bytes))
        {
            return ValidationResult.Failure(
                "Format de taille non valide. Exemples: 1024, 1K, 10M, 1G");
        }

        var validation = definition.Validation;
        if (validation?.Min.HasValue == true && bytes < validation.Min.Value)
        {
            return ValidationResult.Failure($"La taille minimale est {validation.Min} octets");
        }

        if (validation?.Max.HasValue == true && bytes > validation.Max.Value)
        {
            return ValidationResult.Failure($"La taille maximale est {validation.Max} octets");
        }

        return ValidationResult.Success();
    }

    private static bool TryParseInteger(object? value, out long result)
    {
        result = 0;

        if (value is int intVal)
        {
            result = intVal;
            return true;
        }

        if (value is long longVal)
        {
            result = longVal;
            return true;
        }

        return long.TryParse(value?.ToString(), out result);
    }

    private static bool TryParseDuration(string value, out long seconds)
    {
        seconds = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        value = value.Trim().ToLowerInvariant();

        // Nombre seul = secondes
        if (long.TryParse(value, out seconds))
            return true;

        // Avec unité
        var match = Regex.Match(value, @"^(\d+)\s*(s|m|h|d|w)?$", RegexOptions.None, RegexTimeout);
        if (!match.Success)
            return false;

        if (!long.TryParse(match.Groups[1].Value, out var number))
            return false;

        var unit = match.Groups[2].Value;
        seconds = unit switch
        {
            "s" or "" => number,
            "m" => number * 60,
            "h" => number * 3600,
            "d" => number * 86400,
            "w" => number * 604800,
            _ => number
        };

        return true;
    }

    private static bool TryParseSize(string value, out long bytes)
    {
        bytes = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        value = value.Trim().ToUpperInvariant();

        // Nombre seul = octets
        if (long.TryParse(value, out bytes))
            return true;

        // Avec unité
        var match = Regex.Match(value, @"^(\d+)\s*(B|K|KB|M|MB|G|GB|T|TB)?$",
            RegexOptions.IgnoreCase, RegexTimeout);
        if (!match.Success)
            return false;

        if (!long.TryParse(match.Groups[1].Value, out var number))
            return false;

        var unit = match.Groups[2].Value.ToUpperInvariant();
        bytes = unit switch
        {
            "B" or "" => number,
            "K" or "KB" => number * 1024,
            "M" or "MB" => number * 1024 * 1024,
            "G" or "GB" => number * 1024 * 1024 * 1024,
            "T" or "TB" => number * 1024L * 1024 * 1024 * 1024,
            _ => number
        };

        return true;
    }
}
