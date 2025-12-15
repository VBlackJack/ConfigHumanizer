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
using ConfigHumanizer.Core.Models;

namespace ConfigHumanizer.Core.Writers;

/// <summary>
/// Writer pour les formats YAML (Docker Compose, Kubernetes, Ansible, Prometheus).
/// </summary>
public class YamlConfigWriter : BaseConfigWriter
{
    /// <inheritdoc/>
    public override string GenerateLine(ParameterSchema schema, ParameterDefinition definition, object? value)
    {
        return GenerateLineWithIndent(schema, definition, value, 0);
    }

    /// <summary>
    /// Génère une ligne YAML avec indentation.
    /// </summary>
    public string GenerateLineWithIndent(ParameterSchema schema, ParameterDefinition definition,
        object? value, int indentLevel)
    {
        if (value == null)
            return string.Empty;

        var indent = new string(' ', indentLevel * 2);
        var key = FormatKeyYaml(definition);
        var formattedValue = FormatYamlValue(definition, value);

        // Cas des valeurs multiples (listes)
        if (definition.MultiValue && value is IEnumerable<object> values)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{indent}{key}:");
            foreach (var v in values)
            {
                sb.AppendLine($"{indent}  - {FormatYamlValue(definition, v)}");
            }
            return sb.ToString().TrimEnd();
        }

        return $"{indent}{key}: {formattedValue}";
    }

    /// <summary>
    /// Formate la clé au format YAML (snake_case généralement).
    /// </summary>
    protected virtual string FormatKeyYaml(ParameterDefinition definition)
    {
        return definition.Key;
    }

    /// <summary>
    /// Formate une valeur pour YAML.
    /// </summary>
    protected virtual string FormatYamlValue(ParameterDefinition definition, object? value)
    {
        if (value == null)
            return "null";

        return definition.DataType switch
        {
            ParameterDataType.Boolean => FormatYamlBoolean(value),
            ParameterDataType.String => FormatYamlString(value),
            ParameterDataType.Integer => value.ToString() ?? "0",
            ParameterDataType.Port => value.ToString() ?? "0",
            _ => FormatYamlString(value)
        };
    }

    /// <summary>
    /// Formate un booléen pour YAML (true/false sans guillemets).
    /// </summary>
    protected virtual string FormatYamlBoolean(object value)
    {
        if (value is bool b)
            return b ? "true" : "false";

        var stringValue = value.ToString()?.ToLowerInvariant() ?? "false";
        return stringValue is "true" or "yes" or "on" or "1" ? "true" : "false";
    }

    /// <summary>
    /// Formate une chaîne pour YAML (avec guillemets si nécessaire).
    /// </summary>
    protected virtual string FormatYamlString(object value)
    {
        var stringValue = value.ToString() ?? string.Empty;

        // Guillemets si contient des caractères spéciaux YAML
        var needsQuotes = stringValue.Contains(':') ||
                          stringValue.Contains('#') ||
                          stringValue.Contains('{') ||
                          stringValue.Contains('}') ||
                          stringValue.Contains('[') ||
                          stringValue.Contains(']') ||
                          stringValue.Contains('&') ||
                          stringValue.Contains('*') ||
                          stringValue.Contains('!') ||
                          stringValue.Contains('|') ||
                          stringValue.Contains('>') ||
                          stringValue.Contains('\'') ||
                          stringValue.Contains('"') ||
                          stringValue.Contains('%') ||
                          stringValue.Contains('@') ||
                          stringValue.StartsWith(' ') ||
                          stringValue.EndsWith(' ') ||
                          stringValue == "true" ||
                          stringValue == "false" ||
                          stringValue == "null" ||
                          stringValue == "yes" ||
                          stringValue == "no";

        if (needsQuotes)
        {
            // Échapper les guillemets doubles
            var escaped = stringValue.Replace("\"", "\\\"");
            return $"\"{escaped}\"";
        }

        return stringValue;
    }
}
