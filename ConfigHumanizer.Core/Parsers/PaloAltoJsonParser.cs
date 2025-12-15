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
/// Parser for Palo Alto firewall rules in JSON format.
/// Handles North-South and West-East firewall rule definitions.
/// </summary>
public class PaloAltoJsonParser : BaseConfigParser
{
    public PaloAltoJsonParser() : base()
    {
    }

    public PaloAltoJsonParser(RuleEngine ruleEngine, string? formatName = null) : base(ruleEngine, formatName)
    {
    }

    public override List<HumanizedRule> Parse(string fileContent)
    {
        var rules = new List<HumanizedRule>();

        if (string.IsNullOrWhiteSpace(fileContent))
            return rules;

        try
        {
            using var doc = JsonDocument.Parse(fileContent);
            var root = doc.RootElement;

            // Parse tag_color if present
            if (root.TryGetProperty("tag_color", out var tagColor))
            {
                var rule = MatchAndCreateRule($"\"tag_color\": \"{tagColor.GetString()}\"", "firewall.tag_color", tagColor.GetString() ?? "");
                rules.Add(rule);
            }

            // Parse rules array
            if (root.TryGetProperty("rules", out var rulesArray))
            {
                foreach (var fwRule in rulesArray.EnumerateArray())
                {
                    ParseFirewallRule(fwRule, rules);
                }
            }
        }
        catch (JsonException)
        {
            // If JSON parsing fails, return empty rules
            return rules;
        }

        return rules;
    }

    private void ParseFirewallRule(JsonElement fwRule, List<HumanizedRule> rules)
    {
        var ruleName = GetStringProperty(fwRule, "name") ?? "unnamed";

        // Parse source
        var source = GetStringProperty(fwRule, "source") ?? "any";
        var sourceZone = GetStringProperty(fwRule, "source_zone") ?? "";

        // Parse destination
        var dest = GetStringProperty(fwRule, "dest") ?? "any";
        var destZone = GetStringProperty(fwRule, "dest_zone") ?? "";

        // Parse service/ports
        var service = GetStringProperty(fwRule, "service") ?? "";

        // Parse description
        var description = GetStringProperty(fwRule, "description") ?? "";

        // Create rule for the firewall rule definition
        var ruleDefRaw = $"Rule: {ruleName}";
        var ruleDefRule = MatchAndCreateRule(ruleDefRaw, "fw_rule.name", ruleName);
        rules.Add(ruleDefRule);

        // Check source configuration
        if (source == "any")
        {
            var sourceRule = MatchAndCreateRule($"source: {source}", "fw_rule.source_any", source);
            rules.Add(sourceRule);
        }
        else
        {
            var sourceRule = MatchAndCreateRule($"source: {source}", "fw_rule.source", source);
            rules.Add(sourceRule);
        }

        // Check destination configuration
        if (dest == "any")
        {
            var destRule = MatchAndCreateRule($"dest: {dest}", "fw_rule.dest_any", dest);
            rules.Add(destRule);
        }
        else
        {
            var destRule = MatchAndCreateRule($"dest: {dest}", "fw_rule.dest", dest);
            rules.Add(destRule);
        }

        // Check service/port configuration
        if (!string.IsNullOrEmpty(service))
        {
            var serviceRule = MatchAndCreateRule($"service: {service}", "fw_rule.service", service);
            rules.Add(serviceRule);

            // Check for potentially risky ports
            CheckRiskyPorts(service, rules);
        }

        // Check for zone configurations
        if (!string.IsNullOrEmpty(sourceZone))
        {
            var szRule = MatchAndCreateRule($"source_zone: {sourceZone}", "fw_rule.source_zone", sourceZone);
            rules.Add(szRule);
        }

        if (!string.IsNullOrEmpty(destZone))
        {
            var dzRule = MatchAndCreateRule($"dest_zone: {destZone}", "fw_rule.dest_zone", destZone);
            rules.Add(dzRule);
        }
    }

    private void CheckRiskyPorts(string service, List<HumanizedRule> rules)
    {
        var riskyPatterns = new Dictionary<string, string>
        {
            { "TCP_22", "SSH access" },
            { "TCP_23", "Telnet (insecure)" },
            { "TCP_3389", "RDP access" },
            { "TCP_445", "SMB file sharing" },
            { "TCP_1433", "SQL Server" },
            { "TCP_3306", "MySQL" },
            { "TCP_5432", "PostgreSQL" },
            { "TCP_27017", "MongoDB" },
            { "TCP_6379", "Redis" },
            { "TCP_21", "FTP (insecure)" },
            { "TCP_80", "HTTP (unencrypted)" },
        };

        foreach (var pattern in riskyPatterns)
        {
            if (service.Contains(pattern.Key))
            {
                var rule = MatchAndCreateRule($"Port: {pattern.Key}", $"fw_rule.port.{pattern.Key}", pattern.Value);
                rules.Add(rule);
            }
        }

        // Check for wide port ranges
        if (service.Contains("49152-65535"))
        {
            var rule = MatchAndCreateRule("Port range: 49152-65535", "fw_rule.port.wide_range", "Dynamic/ephemeral ports (wide range)");
            rules.Add(rule);
        }
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            return property.GetString();
        }
        return null;
    }
}
