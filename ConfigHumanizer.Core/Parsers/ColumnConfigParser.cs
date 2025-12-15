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
/// Parser for column-based configuration files (crontab, fstab, hosts, resolv.conf).
/// </summary>
public class ColumnConfigParser : BaseConfigParser
{
    public ColumnConfigParser() : base()
    {
    }

    public ColumnConfigParser(RuleEngine ruleEngine, string? formatName = null) : base(ruleEngine, formatName)
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
        // Split by whitespace (spaces and tabs)
        var columns = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        if (columns.Length == 0)
            return null;

        var (key, value) = FormatName?.ToLowerInvariant() switch
        {
            "fstab" => ParseFstabLine(columns),
            "hosts" => ParseHostsLine(columns),
            "resolv" => ParseResolvLine(columns),
            "crontab" => ParseCrontabLine(line, columns),
            _ => (columns[0], string.Join(" ", columns.Skip(1)))
        };

        if (string.IsNullOrEmpty(key))
            return null;

        return MatchAndCreateRule(line, key, value);
    }

    /// <summary>
    /// Parses fstab line: device mountpoint fstype options dump pass
    /// Key = MountPoint (column 1), Value = Options (column 3)
    /// </summary>
    private static (string key, string value) ParseFstabLine(string[] columns)
    {
        if (columns.Length < 4)
            return (columns.Length > 1 ? columns[1] : columns[0], "");

        var mountPoint = columns[1];
        var options = columns[3];

        return (mountPoint, options);
    }

    /// <summary>
    /// Parses hosts line: IP hostname [alias...]
    /// Key = Hostname (column 1+), Value = IP (column 0)
    /// </summary>
    private static (string key, string value) ParseHostsLine(string[] columns)
    {
        if (columns.Length < 2)
            return (columns[0], "");

        var ip = columns[0];
        var hostname = string.Join(" ", columns.Skip(1));

        return (hostname, ip);
    }

    /// <summary>
    /// Parses resolv.conf line: keyword value
    /// Key = Keyword (column 0), Value = Rest of line (column 1+)
    /// </summary>
    private static (string key, string value) ParseResolvLine(string[] columns)
    {
        if (columns.Length < 2)
            return (columns[0], "");

        var keyword = columns[0];
        var value = string.Join(" ", columns.Skip(1));

        return (keyword, value);
    }

    /// <summary>
    /// Parses crontab line:
    /// - @keyword command... -> Key = command, Value = @keyword
    /// - min hour day month dow command... -> Key = command (cols 5+), Value = schedule (cols 0-4)
    /// </summary>
    private static (string key, string value) ParseCrontabLine(string line, string[] columns)
    {
        // Handle special time specifications (@reboot, @daily, etc.)
        if (line.StartsWith('@'))
        {
            if (columns.Length < 2)
                return (columns[0], "");

            var keyword = columns[0]; // @reboot, @daily, etc.
            var command = string.Join(" ", columns.Skip(1));

            return (command, keyword);
        }

        // Standard crontab: min hour day month dow command
        if (columns.Length < 6)
            return (string.Join(" ", columns), "");

        var schedule = string.Join(" ", columns.Take(5));
        var cmd = string.Join(" ", columns.Skip(5));

        return (cmd, schedule);
    }
}
