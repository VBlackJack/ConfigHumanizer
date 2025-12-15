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
using System.Text.Json;
using ConfigHumanizer.Core.Models;

namespace ConfigHumanizer.Core.Writers;

/// <summary>
/// Writer pour les formats JSON (NPM, AppSettings, PaloAlto).
/// </summary>
public class JsonConfigWriter : BaseConfigWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <inheritdoc/>
    public override string GenerateLine(ParameterSchema schema, ParameterDefinition definition, object? value)
    {
        return GenerateLineWithIndent(schema, definition, value, 1);
    }

    /// <summary>
    /// Génère une propriété JSON avec indentation.
    /// </summary>
    public string GenerateLineWithIndent(ParameterSchema schema, ParameterDefinition definition,
        object? value, int indentLevel)
    {
        if (value == null)
            return string.Empty;

        var indent = new string(' ', indentLevel * 2);
        var key = definition.Key;
        var formattedValue = FormatJsonValue(definition, value);

        return $"{indent}\"{key}\": {formattedValue}";
    }

    /// <summary>
    /// Formate une valeur pour JSON.
    /// </summary>
    protected virtual string FormatJsonValue(ParameterDefinition definition, object? value)
    {
        if (value == null)
            return "null";

        return definition.DataType switch
        {
            ParameterDataType.Boolean => FormatJsonBoolean(value),
            ParameterDataType.Integer => value.ToString() ?? "0",
            ParameterDataType.Port => value.ToString() ?? "0",
            ParameterDataType.String => JsonSerializer.Serialize(value.ToString()),
            ParameterDataType.Path => JsonSerializer.Serialize(value.ToString()),
            ParameterDataType.IpAddress => JsonSerializer.Serialize(value.ToString()),
            ParameterDataType.Duration => FormatJsonDuration(definition, value),
            ParameterDataType.Size => FormatJsonSize(definition, value),
            ParameterDataType.Enum => JsonSerializer.Serialize(value.ToString()),
            _ => JsonSerializer.Serialize(value.ToString())
        };
    }

    /// <summary>
    /// Formate un booléen pour JSON.
    /// </summary>
    protected virtual string FormatJsonBoolean(object value)
    {
        if (value is bool b)
            return b ? "true" : "false";

        var stringValue = value.ToString()?.ToLowerInvariant() ?? "false";
        return stringValue is "true" or "yes" or "on" or "1" ? "true" : "false";
    }

    /// <summary>
    /// Formate une durée pour JSON (nombre ou string selon le format).
    /// </summary>
    protected virtual string FormatJsonDuration(ParameterDefinition definition, object value)
    {
        // Si c'est un nombre, le garder comme nombre
        if (value is int or long or double)
            return value.ToString() ?? "0";

        // Sinon, comme string
        return JsonSerializer.Serialize(value.ToString());
    }

    /// <summary>
    /// Formate une taille pour JSON.
    /// </summary>
    protected virtual string FormatJsonSize(ParameterDefinition definition, object value)
    {
        // Si c'est un nombre, le garder comme nombre
        if (value is int or long or double)
            return value.ToString() ?? "0";

        // Sinon, comme string
        return JsonSerializer.Serialize(value.ToString());
    }

    /// <inheritdoc/>
    public override string GenerateBlock(ParameterSchema schema,
        IEnumerable<(ParameterDefinition Definition, object? Value)> parameters)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");

        var paramList = parameters.Where(p => p.Value != null).ToList();
        for (var i = 0; i < paramList.Count; i++)
        {
            var (definition, value) = paramList[i];
            var line = GenerateLineWithIndent(schema, definition, value, 1);

            if (!string.IsNullOrEmpty(line))
            {
                // Ajouter virgule sauf pour le dernier élément
                if (i < paramList.Count - 1)
                    line += ",";

                sb.AppendLine(line);
            }
        }

        sb.Append("}");
        return sb.ToString();
    }
}
