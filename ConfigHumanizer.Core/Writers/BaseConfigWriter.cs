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

using System.Text;
using ConfigHumanizer.Core.Interfaces;
using ConfigHumanizer.Core.Models;

namespace ConfigHumanizer.Core.Writers;

/// <summary>
/// Classe de base abstraite pour les writers de configuration.
/// </summary>
public abstract class BaseConfigWriter : IConfigWriter
{
    /// <inheritdoc/>
    public abstract string GenerateLine(ParameterSchema schema, ParameterDefinition definition, object? value);

    /// <inheritdoc/>
    public virtual string GenerateBlock(ParameterSchema schema,
        IEnumerable<(ParameterDefinition Definition, object? Value)> parameters)
    {
        var sb = new StringBuilder();
        foreach (var (definition, value) in parameters)
        {
            var line = GenerateLine(schema, definition, value);
            if (!string.IsNullOrEmpty(line))
            {
                sb.AppendLine(line);
            }
        }
        return sb.ToString().TrimEnd();
    }

    /// <inheritdoc/>
    public virtual string FormatValue(ParameterDefinition definition, object? value)
    {
        if (value == null)
            return string.Empty;

        return definition.DataType switch
        {
            ParameterDataType.Boolean => FormatBoolean(definition, value),
            ParameterDataType.String => FormatString(value),
            ParameterDataType.Integer => value.ToString() ?? string.Empty,
            ParameterDataType.Port => value.ToString() ?? string.Empty,
            ParameterDataType.Enum => value.ToString() ?? string.Empty,
            ParameterDataType.Path => FormatPath(value),
            ParameterDataType.IpAddress => value.ToString() ?? string.Empty,
            ParameterDataType.Duration => FormatDuration(definition, value),
            ParameterDataType.Size => FormatSize(definition, value),
            _ => value.ToString() ?? string.Empty
        };
    }

    /// <summary>
    /// Formate une valeur booléenne selon le format spécifié.
    /// </summary>
    protected virtual string FormatBoolean(ParameterDefinition definition, object value)
    {
        bool boolValue;

        if (value is bool b)
        {
            boolValue = b;
        }
        else
        {
            var stringValue = value.ToString()?.ToLowerInvariant() ?? string.Empty;
            boolValue = stringValue is "true" or "yes" or "on" or "1";
        }

        var format = definition.BooleanFormat ?? "yes/no";
        var parts = format.Split('/');
        if (parts.Length != 2)
            return boolValue ? "yes" : "no";

        return boolValue ? parts[0] : parts[1];
    }

    /// <summary>
    /// Formate une chaîne de caractères (ajoute des guillemets si nécessaire).
    /// </summary>
    protected virtual string FormatString(object value)
    {
        var stringValue = value.ToString() ?? string.Empty;

        // Ajouter des guillemets si la valeur contient des espaces
        if (stringValue.Contains(' ') && !stringValue.StartsWith('"'))
        {
            return $"\"{stringValue}\"";
        }

        return stringValue;
    }

    /// <summary>
    /// Formate un chemin de fichier.
    /// </summary>
    protected virtual string FormatPath(object value)
    {
        var path = value.ToString() ?? string.Empty;

        // Ajouter des guillemets si le chemin contient des espaces
        if (path.Contains(' ') && !path.StartsWith('"'))
        {
            return $"\"{path}\"";
        }

        return path;
    }

    /// <summary>
    /// Formate une durée (avec unité si applicable).
    /// </summary>
    protected virtual string FormatDuration(ParameterDefinition definition, object value)
    {
        // Par défaut, retourner la valeur telle quelle
        return value.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Formate une taille (avec unité si applicable).
    /// </summary>
    protected virtual string FormatSize(ParameterDefinition definition, object value)
    {
        // Par défaut, retourner la valeur telle quelle
        return value.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Formate la clé selon les règles du schéma.
    /// </summary>
    protected virtual string FormatKey(ParameterSchema schema, ParameterDefinition definition)
    {
        var key = definition.Key;

        if (schema.KeyUpperCase)
            return key.ToUpperInvariant();

        if (schema.KeyLowerCase)
            return key.ToLowerInvariant();

        return key;
    }
}
