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

namespace ConfigHumanizer.Core.Parsers;

/// <summary>
/// Parser for INI-style configuration files (SSSD, etc.) using the rule engine.
/// </summary>
public class IniConfigParser : BaseConfigParser
{
    public IniConfigParser() : base()
    {
    }

    public IniConfigParser(RuleEngine ruleEngine, string? formatName = null) : base(ruleEngine, formatName)
    {
    }

    public override List<HumanizedRule> Parse(string fileContent)
    {
        var rules = new List<HumanizedRule>();

        if (string.IsNullOrWhiteSpace(fileContent))
            return rules;

        var lines = fileContent.Split('\n', StringSplitOptions.None);

        for (var i = 0; i < lines.Length; i++)
        {
            var trimmedLine = lines[i].Trim();

            // Skip empty lines and comments (starting with ; or #)
            if (string.IsNullOrWhiteSpace(trimmedLine) ||
                trimmedLine.StartsWith(';') ||
                trimmedLine.StartsWith('#'))
                continue;

            // Skip section headers [SectionName]
            if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
                continue;

            var rule = ParseLine(trimmedLine, i);
            if (rule != null)
            {
                rules.Add(rule);
            }
        }

        return rules;
    }

    private HumanizedRule? ParseLine(string line, int lineIndex)
    {
        // Split by '=' to get key and value
        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
            return null;

        var key = line[..separatorIndex].Trim();
        var value = line[(separatorIndex + 1)..].Trim();

        // Use rule engine if available
        return MatchAndCreateRule(line, key, value, lineIndex);
    }
}
