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

using System.Text.Json;
using ConfigHumanizer.Core.Models;
using ConfigHumanizer.Core.Services;

namespace ConfigHumanizer.Core.Parsers;

/// <summary>
/// Parser for JSON configuration files (appsettings.json, package.json, etc.).
/// Flattens the JSON tree to dot-notation keys for rule matching.
/// </summary>
public class JsonConfigParser : BaseConfigParser
{
    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public JsonConfigParser() : base()
    {
    }

    public JsonConfigParser(RuleEngine ruleEngine, string? formatName = null) : base(ruleEngine, formatName)
    {
    }

    private string[] _lines = [];

    public override List<HumanizedRule> Parse(string fileContent)
    {
        var rules = new List<HumanizedRule>();

        if (string.IsNullOrWhiteSpace(fileContent))
            return rules;

        try
        {
            _lines = fileContent.Split('\n');
            using var document = JsonDocument.Parse(fileContent, JsonOptions);
            FlattenJson(document.RootElement, string.Empty, rules);
        }
        catch (Exception)
        {
            // Invalid JSON, return empty rules
        }

        return rules;
    }

    private void FlattenJson(JsonElement element, string prefix, List<HumanizedRule> rules)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var newPrefix = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                    FlattenJson(property.Value, newPrefix, rules);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var newPrefix = $"{prefix}[{index}]";
                    FlattenJson(item, newPrefix, rules);
                    index++;
                }
                break;

            case JsonValueKind.String:
                var stringValue = element.GetString() ?? string.Empty;
                var (stringLine, stringLineIndex) = FindJsonLine(prefix, stringValue);
                var stringRule = MatchAndCreateRule(stringLine, prefix, stringValue, stringLineIndex);
                rules.Add(stringRule);
                break;

            case JsonValueKind.Number:
                var numValue = element.GetRawText();
                var (numLine, numLineIndex) = FindJsonLine(prefix, numValue);
                var numRule = MatchAndCreateRule(numLine, prefix, numValue, numLineIndex);
                rules.Add(numRule);
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                var boolValue = element.GetBoolean().ToString().ToLowerInvariant();
                var (boolLine, boolLineIndex) = FindJsonLine(prefix, boolValue);
                var boolRule = MatchAndCreateRule(boolLine, prefix, boolValue, boolLineIndex);
                rules.Add(boolRule);
                break;

            case JsonValueKind.Null:
                var (nullLine, nullLineIndex) = FindJsonLine(prefix, "null");
                var nullRule = MatchAndCreateRule(nullLine, prefix, "null", nullLineIndex);
                rules.Add(nullRule);
                break;
        }
    }

    private (string rawLine, int lineIndex) FindJsonLine(string key, string value)
    {
        // Extract the last part of the key for searching
        var keyPart = key.Contains('.') ? key.Split('.').Last() : key;
        keyPart = keyPart.TrimEnd(']').Split('[').First(); // Handle array notation

        // Try to find a line containing this key and value
        for (var i = 0; i < _lines.Length; i++)
        {
            var line = _lines[i];
            if (line.Contains($"\"{keyPart}\"") && (line.Contains(value) || line.Contains($"\"{value}\"")))
            {
                return (line.Trim(), i);
            }
        }

        // Fallback to constructed line with unknown index
        return ($"\"{key}\": {(value == "null" || value == "true" || value == "false" || double.TryParse(value, out _) ? value : $"\"{value}\"")}", -1);
    }
}
