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
using ConfigHumanizer.Core.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ConfigHumanizer.Core.Parsers;

/// <summary>
/// Parser for YAML configuration files (Docker Compose, Kubernetes, Ansible, etc.).
/// Flattens the YAML tree to dot-notation keys for rule matching.
/// </summary>
public class YamlConfigParser : BaseConfigParser
{
    private readonly IDeserializer _deserializer;

    public YamlConfigParser() : base()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
    }

    public YamlConfigParser(RuleEngine ruleEngine, string? formatName = null) : base(ruleEngine, formatName)
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
    }

    public override List<HumanizedRule> Parse(string fileContent)
    {
        var rules = new List<HumanizedRule>();

        if (string.IsNullOrWhiteSpace(fileContent))
            return rules;

        try
        {
            var yamlObject = _deserializer.Deserialize<object>(fileContent);
            var lines = fileContent.Split('\n');

            if (yamlObject != null)
            {
                FlattenYaml(yamlObject, string.Empty, rules, lines);
            }
        }
        catch (Exception)
        {
            // Invalid YAML, return empty rules
        }

        return rules;
    }

    private void FlattenYaml(object obj, string prefix, List<HumanizedRule> rules, string[] lines)
    {
        switch (obj)
        {
            case Dictionary<object, object> dict:
                foreach (var kvp in dict)
                {
                    var key = kvp.Key?.ToString() ?? string.Empty;
                    var newPrefix = string.IsNullOrEmpty(prefix) ? key : $"{prefix}.{key}";

                    if (kvp.Value is Dictionary<object, object> || kvp.Value is List<object>)
                    {
                        FlattenYaml(kvp.Value, newPrefix, rules, lines);
                    }
                    else
                    {
                        var value = kvp.Value?.ToString() ?? string.Empty;
                        var (rawLine, lineIndex) = FindRawLine(lines, key, value);
                        var rule = MatchAndCreateRule(rawLine, newPrefix, value, lineIndex);
                        rules.Add(rule);
                    }
                }
                break;

            case List<object> list:
                for (var i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    var newPrefix = $"{prefix}[{i}]";

                    if (item is Dictionary<object, object> || item is List<object>)
                    {
                        FlattenYaml(item, newPrefix, rules, lines);
                    }
                    else
                    {
                        var value = item?.ToString() ?? string.Empty;
                        var (rawLine, lineIndex) = FindRawLineByValue(lines, value);
                        var rule = MatchAndCreateRule(rawLine, newPrefix, value, lineIndex);
                        rules.Add(rule);
                    }
                }
                break;

            default:
                // Scalar value at root level
                var scalarValue = obj?.ToString() ?? string.Empty;
                var (scalarRawLine, scalarLineIndex) = FindRawLineByValue(lines, scalarValue);
                var scalarRule = MatchAndCreateRule(scalarRawLine, prefix, scalarValue, scalarLineIndex);
                rules.Add(scalarRule);
                break;
        }
    }

    private static (string rawLine, int lineIndex) FindRawLine(string[] lines, string key, string value)
    {
        // Try to find the original line in the YAML content
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith($"{key}:") || trimmed.StartsWith($"- {key}:"))
            {
                return (trimmed, i);
            }
        }

        // Fallback to constructed line with unknown index
        return ($"{key}: {value}", -1);
    }

    private static (string rawLine, int lineIndex) FindRawLineByValue(string[] lines, string value)
    {
        // Try to find a line containing this value
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.Contains(value))
            {
                return (trimmed, i);
            }
        }

        // Fallback with unknown index
        return (value, -1);
    }
}
