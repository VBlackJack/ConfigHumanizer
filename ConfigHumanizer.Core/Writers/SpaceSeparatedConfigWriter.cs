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

using ConfigHumanizer.Core.Models;

namespace ConfigHumanizer.Core.Writers;

/// <summary>
/// Writer pour les formats clé-valeur séparés par espace (SSH, Squid, PostgreSQL, etc.).
/// </summary>
public class SpaceSeparatedConfigWriter : BaseConfigWriter
{
    /// <inheritdoc/>
    public override string GenerateLine(ParameterSchema schema, ParameterDefinition definition, object? value)
    {
        if (value == null)
            return string.Empty;

        var key = FormatKey(schema, definition);
        var formattedValue = FormatValue(definition, value);
        var separator = schema.KeyValueSeparator;

        // Cas des valeurs multiples
        if (definition.MultiValue && value is IEnumerable<object> values)
        {
            var valueSep = definition.MultiValueSeparator ?? " ";
            formattedValue = string.Join(valueSep, values.Select(v => FormatValue(definition, v)));
        }

        return $"{key}{separator}{formattedValue}";
    }
}
