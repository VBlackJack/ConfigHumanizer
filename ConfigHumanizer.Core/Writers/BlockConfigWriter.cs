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
/// Writer pour les formats bloc avec accolades ou directives (Nginx, Apache, BIND, DHCP).
/// </summary>
public class BlockConfigWriter : BaseConfigWriter
{
    /// <summary>
    /// Style de syntaxe du bloc.
    /// </summary>
    public enum BlockStyle
    {
        /// <summary>
        /// Style Nginx: directive value;
        /// </summary>
        Nginx,

        /// <summary>
        /// Style Apache: Directive value
        /// </summary>
        Apache,

        /// <summary>
        /// Style BIND: option value;
        /// </summary>
        Bind,

        /// <summary>
        /// Style DHCP: option value;
        /// </summary>
        Dhcp
    }

    /// <summary>
    /// Style de bloc à utiliser.
    /// </summary>
    public BlockStyle Style { get; set; } = BlockStyle.Nginx;

    /// <inheritdoc/>
    public override string GenerateLine(ParameterSchema schema, ParameterDefinition definition, object? value)
    {
        return GenerateLineWithIndent(schema, definition, value, 0);
    }

    /// <summary>
    /// Génère une directive avec indentation.
    /// </summary>
    public string GenerateLineWithIndent(ParameterSchema schema, ParameterDefinition definition,
        object? value, int indentLevel)
    {
        if (value == null)
            return string.Empty;

        var indent = new string(' ', indentLevel * 4);
        var key = FormatKey(schema, definition);
        var formattedValue = FormatValue(definition, value);

        return Style switch
        {
            BlockStyle.Nginx => $"{indent}{key} {formattedValue};",
            BlockStyle.Apache => $"{indent}{key} {formattedValue}",
            BlockStyle.Bind => $"{indent}{key} {formattedValue};",
            BlockStyle.Dhcp => $"{indent}option {key} {formattedValue};",
            _ => $"{indent}{key} {formattedValue};"
        };
    }

    /// <summary>
    /// Génère un bloc complet avec contexte.
    /// </summary>
    public string GenerateBlockWithContext(ParameterSchema schema,
        IEnumerable<(ParameterDefinition Definition, object? Value)> parameters,
        string contextName, string? contextValue = null)
    {
        var sb = new StringBuilder();

        // En-tête du bloc
        if (!string.IsNullOrEmpty(contextValue))
        {
            sb.AppendLine($"{contextName} {contextValue} {{");
        }
        else
        {
            sb.AppendLine($"{contextName} {{");
        }

        // Contenu
        foreach (var (definition, value) in parameters)
        {
            var line = GenerateLineWithIndent(schema, definition, value, 1);
            if (!string.IsNullOrEmpty(line))
            {
                sb.AppendLine(line);
            }
        }

        // Fermeture du bloc
        sb.Append("}");

        return sb.ToString();
    }

    /// <inheritdoc/>
    public override string GenerateBlock(ParameterSchema schema,
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
    protected override string FormatString(object value)
    {
        var stringValue = value.ToString() ?? string.Empty;

        // Nginx et autres: guillemets si espaces ou caractères spéciaux
        var needsQuotes = stringValue.Contains(' ') ||
                          stringValue.Contains(';') ||
                          stringValue.Contains('{') ||
                          stringValue.Contains('}');

        if (needsQuotes && !stringValue.StartsWith('"'))
        {
            return $"\"{stringValue}\"";
        }

        return stringValue;
    }
}
