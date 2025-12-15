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
/// Writer pour les formats INI avec sections (SSSD, Fail2ban, MySQL, PHP, Systemd).
/// </summary>
public class IniConfigWriter : BaseConfigWriter
{
    /// <inheritdoc/>
    public override string GenerateLine(ParameterSchema schema, ParameterDefinition definition, object? value)
    {
        if (value == null)
            return string.Empty;

        var key = FormatKey(schema, definition);
        var formattedValue = FormatValue(definition, value);

        // Format INI standard: key = value
        return $"{key} = {formattedValue}";
    }

    /// <summary>
    /// Génère une ligne avec section.
    /// </summary>
    public string GenerateLineWithSection(ParameterSchema schema, ParameterDefinition definition,
        object? value, string section)
    {
        var line = GenerateLine(schema, definition, value);
        if (string.IsNullOrEmpty(line))
            return string.Empty;

        return $"[{section}]\n{line}";
    }

    /// <inheritdoc/>
    public override string GenerateBlock(ParameterSchema schema,
        IEnumerable<(ParameterDefinition Definition, object? Value)> parameters)
    {
        var sb = new StringBuilder();
        string? currentSection = null;

        // Grouper par catégorie (utilisée comme section)
        var parametersList = parameters.ToList();

        foreach (var category in schema.ParameterCategories)
        {
            var categoryParams = parametersList
                .Where(p => category.Parameters.Any(cp => cp.Key == p.Definition.Key))
                .ToList();

            if (categoryParams.Count == 0)
                continue;

            // Écrire la section si différente
            var sectionName = category.Name;
            if (sectionName != currentSection)
            {
                if (currentSection != null)
                    sb.AppendLine();

                sb.AppendLine($"[{sectionName}]");
                currentSection = sectionName;
            }

            // Écrire les paramètres de cette section
            foreach (var (definition, value) in categoryParams)
            {
                var line = GenerateLine(schema, definition, value);
                if (!string.IsNullOrEmpty(line))
                {
                    sb.AppendLine(line);
                }
            }
        }

        return sb.ToString().TrimEnd();
    }
}
