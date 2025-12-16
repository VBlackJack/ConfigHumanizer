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
/// Parser for block-based configuration files (Nginx, Apache, BIND, DHCP).
/// Handles nested blocks with { } delimiters.
/// </summary>
public class BlockConfigParser : BaseConfigParser
{
    public BlockConfigParser() : base()
    {
    }

    public BlockConfigParser(RuleEngine ruleEngine, string? formatName = null) : base(ruleEngine, formatName)
    {
    }

    public override List<HumanizedRule> Parse(string fileContent)
    {
        var rules = new List<HumanizedRule>();

        if (string.IsNullOrWhiteSpace(fileContent))
            return rules;

        var contextStack = new Stack<string>();
        var lines = fileContent.Split('\n', StringSplitOptions.None);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            // Process the line for blocks and directives
            ProcessLine(line, lines[i], contextStack, rules, i);
        }

        return rules;
    }

    private void ProcessLine(string line, string rawLine, Stack<string> contextStack, List<HumanizedRule> rules, int lineIndex)
    {
        // Check for block start: "server {" or "location / {"
        if (line.Contains('{'))
        {
            var blockName = ExtractBlockName(line);
            if (!string.IsNullOrEmpty(blockName))
            {
                contextStack.Push(blockName);
            }

            // Also check if there's a directive before the brace
            var beforeBrace = line.Split('{')[0].Trim();
            if (!string.IsNullOrEmpty(beforeBrace) && !IsBlockKeyword(beforeBrace))
            {
                // It's a directive with a block, extract key-value
                var directive = ParseDirective(beforeBrace);
                if (directive.HasValue)
                {
                    AddRule(rules, rawLine, contextStack, directive.Value.key, directive.Value.value, lineIndex);
                }
            }
        }

        // Check for block end
        if (line.Contains('}'))
        {
            if (contextStack.Count > 0)
            {
                contextStack.Pop();
            }
        }

        // Check for directive (key value;)
        if (line.EndsWith(';') && !line.Contains('{'))
        {
            var directiveLine = line.TrimEnd(';').Trim();
            var directive = ParseDirective(directiveLine);
            if (directive.HasValue)
            {
                AddRule(rules, rawLine, contextStack, directive.Value.key, directive.Value.value, lineIndex);
            }
        }
    }

    private static string ExtractBlockName(string line)
    {
        // Extract block name from lines like "server {", "location / {", "http {"
        var beforeBrace = line.Split('{')[0].Trim();

        // Split by whitespace and get the block identifier
        var parts = beforeBrace.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return string.Empty;

        // For "location /path", return "location:/path"
        // For "server", return "server"
        if (parts.Length >= 2 && IsBlockKeyword(parts[0]))
        {
            return $"{parts[0]}:{string.Join("_", parts.Skip(1))}";
        }

        return parts[0];
    }

    private static bool IsBlockKeyword(string word)
    {
        var blockKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "http", "server", "location", "upstream", "events", "stream", "mail",
            "if", "map", "geo", "types", "limit_except",
            // Apache
            "VirtualHost", "Directory", "Location", "Files", "FilesMatch",
            // BIND
            "zone", "options", "logging", "channel", "category", "view",
            // DHCP
            "subnet", "host", "group", "pool", "class"
        };

        return blockKeywords.Contains(word);
    }

    private static (string key, string value)? ParseDirective(string line)
    {
        var parts = line.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return null;

        var key = parts[0];
        var value = parts.Length > 1 ? parts[1].Trim() : string.Empty;

        return (key, value);
    }

    private void AddRule(List<HumanizedRule> rules, string rawLine, Stack<string> contextStack, string key, string value, int lineIndex)
    {
        // Build full context path
        var contextPath = contextStack.Count > 0
            ? string.Join(":", contextStack.Reverse()) + ":"
            : string.Empty;

        var fullKey = contextPath + key;

        var rule = MatchAndCreateRule(rawLine.Trim(), fullKey, value, lineIndex);
        rules.Add(rule);
    }
}
