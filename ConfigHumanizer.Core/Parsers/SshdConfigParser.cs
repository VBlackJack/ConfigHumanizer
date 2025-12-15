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
/// Parser for space-separated config files (OpenSSH, Squid, etc.) using the rule engine.
/// </summary>
public class SshdConfigParser : BaseConfigParser
{
    public SshdConfigParser() : base()
    {
    }

    public SshdConfigParser(RuleEngine ruleEngine, string? formatName = null) : base(ruleEngine, formatName)
    {
    }

    public override List<HumanizedRule> Parse(string fileContent)
    {
        var rules = new List<HumanizedRule>();

        if (string.IsNullOrWhiteSpace(fileContent))
            return rules;

        var lines = fileContent.Split('\n', StringSplitOptions.None);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith('#'))
                continue;

            var rule = ParseLine(trimmedLine);
            if (rule != null)
            {
                rules.Add(rule);
            }
        }

        return rules;
    }

    private HumanizedRule? ParseLine(string line)
    {
        // Split by whitespace to get key and value
        var parts = line.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
            return null;

        var key = parts[0];
        var value = parts[1].Trim();

        // Use rule engine if available
        return MatchAndCreateRule(line, key, value);
    }
}
