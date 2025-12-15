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

using System.Text.RegularExpressions;
using ConfigHumanizer.Core.Models;
using ConfigHumanizer.Core.Services;

namespace ConfigHumanizer.Core.Parsers;

/// <summary>
/// Parser for HashiCorp Configuration Language (HCL) files like Terraform.
/// Handles modules, resources, locals, variables, and infrastructure definitions.
/// </summary>
public class HclConfigParser : BaseConfigParser
{
    public HclConfigParser() : base()
    {
    }

    public HclConfigParser(RuleEngine ruleEngine, string? formatName = null) : base(ruleEngine, formatName)
    {
    }

    public override List<HumanizedRule> Parse(string fileContent)
    {
        var rules = new List<HumanizedRule>();

        if (string.IsNullOrWhiteSpace(fileContent))
            return rules;

        var contextStack = new Stack<string>();
        var lines = fileContent.Split('\n', StringSplitOptions.None);
        var braceDepth = 0;
        var currentBlockType = string.Empty;
        var currentBlockName = string.Empty;

        for (int i = 0; i < lines.Length; i++)
        {
            var rawLine = lines[i];
            var line = rawLine.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#') || line.StartsWith("//"))
                continue;

            // Track brace depth for context
            var openBraces = line.Count(c => c == '{');
            var closeBraces = line.Count(c => c == '}');

            // Check for block start
            if (openBraces > 0)
            {
                var blockInfo = ExtractBlockInfo(line);
                if (blockInfo.HasValue)
                {
                    currentBlockType = blockInfo.Value.blockType;
                    currentBlockName = blockInfo.Value.blockName;
                    contextStack.Push($"{currentBlockType}:{currentBlockName}");

                    // Add rule for the block definition itself
                    var blockRule = MatchAndCreateRule(rawLine.Trim(), $"{currentBlockType}.definition", currentBlockName);
                    rules.Add(blockRule);
                }
                braceDepth += openBraces;
            }

            // Process assignments (key = value)
            ProcessAssignment(line, rawLine, contextStack, rules);

            // Process infrastructure-specific patterns
            ProcessInfrastructurePatterns(line, rawLine, contextStack, rules);

            // Check for block end
            if (closeBraces > 0)
            {
                braceDepth -= closeBraces;
                for (int j = 0; j < closeBraces && contextStack.Count > 0; j++)
                {
                    contextStack.Pop();
                }
            }
        }

        return rules;
    }

    private static (string blockType, string blockName)? ExtractBlockInfo(string line)
    {
        // Match patterns like:
        // module "Name" {
        // resource "type" "name" {
        // locals {
        // variable "name" {
        // WORKSPACE_NAME = {

        var moduleMatch = Regex.Match(line, @"^\s*(module|resource|data|provider|variable|output|terraform)\s+""([^""]+)""");
        if (moduleMatch.Success)
        {
            return (moduleMatch.Groups[1].Value, moduleMatch.Groups[2].Value);
        }

        var localsMatch = Regex.Match(line, @"^\s*(locals)\s*\{");
        if (localsMatch.Success)
        {
            return ("locals", "definitions");
        }

        // Match workspace or named block like: PROD_DC1 = {
        var namedBlockMatch = Regex.Match(line, @"^\s*(\w+)\s*=\s*\{");
        if (namedBlockMatch.Success)
        {
            return ("workspace", namedBlockMatch.Groups[1].Value);
        }

        // Match infrastructure component like: gw-win-01p = {
        var componentMatch = Regex.Match(line, @"^\s*([\w-]+)\s*=\s*\{");
        if (componentMatch.Success)
        {
            return ("component", componentMatch.Groups[1].Value);
        }

        return null;
    }

    private void ProcessAssignment(string line, string rawLine, Stack<string> contextStack, List<HumanizedRule> rules)
    {
        // Match simple assignments: key = value
        // But not block starts: key = {
        var assignmentMatch = Regex.Match(line, @"^\s*([\w_]+)\s*=\s*(.+?)\s*,?\s*$");
        if (assignmentMatch.Success && !line.TrimEnd().EndsWith("{"))
        {
            var key = assignmentMatch.Groups[1].Value;
            var value = assignmentMatch.Groups[2].Value.Trim().TrimEnd(',');

            // Clean up value (remove quotes)
            value = value.Trim('"');

            // Build context path
            var contextPath = contextStack.Count > 0
                ? string.Join(".", contextStack.Reverse()) + "."
                : string.Empty;

            var fullKey = contextPath + key;
            var rule = MatchAndCreateRule(rawLine.Trim(), fullKey, value);
            rules.Add(rule);
        }
    }

    private void ProcessInfrastructurePatterns(string line, string rawLine, Stack<string> contextStack, List<HumanizedRule> rules)
    {
        // Detect module source (Git URLs)
        var sourceMatch = Regex.Match(line, @"^\s*source\s*=\s*""([^""]+)""");
        if (sourceMatch.Success)
        {
            var sourceUrl = sourceMatch.Groups[1].Value;
            var rule = MatchAndCreateRule(rawLine.Trim(), "module.source", sourceUrl);
            rules.Add(rule);
        }

        // Detect security group assignments
        var securityGroupMatch = Regex.Match(line, @"^\s*securityGroup\s*=\s*""?([^"",]+)""?");
        if (securityGroupMatch.Success)
        {
            var sgName = securityGroupMatch.Groups[1].Value;
            var rule = MatchAndCreateRule(rawLine.Trim(), "vm.securityGroup", sgName);
            rules.Add(rule);
        }

        // Detect EPG definitions
        var epgMatch = Regex.Match(line, @"^\s*epg_(?:source|destination)\s*=\s*""?([^"",]+)""?");
        if (epgMatch.Success)
        {
            var epgName = epgMatch.Groups[1].Value;
            var rule = MatchAndCreateRule(rawLine.Trim(), "contract.epg", epgName);
            rules.Add(rule);
        }

        // Detect port definitions in firewall rules
        var portsMatch = Regex.Match(line, @"^\s*ports\s*=\s*jsonencode\(\[(.+)\]\)");
        if (portsMatch.Success)
        {
            var ports = portsMatch.Groups[1].Value;
            var rule = MatchAndCreateRule(rawLine.Trim(), "firewall.ports", ports);
            rules.Add(rule);
        }

        // Detect fabric/zone assignments
        var fabricMatch = Regex.Match(line, @"^\s*fabric\s*=\s*""([^""]+)""");
        if (fabricMatch.Success)
        {
            var fabric = fabricMatch.Groups[1].Value;
            var rule = MatchAndCreateRule(rawLine.Trim(), "aci.fabric", fabric);
            rules.Add(rule);
        }

        // Detect tenant references
        var tenantMatch = Regex.Match(line, @"^\s*(?:dst_tenant|src_tenant)\s*=\s*""([^""]+)""");
        if (tenantMatch.Success)
        {
            var tenant = tenantMatch.Groups[1].Value;
            var rule = MatchAndCreateRule(rawLine.Trim(), "contract.tenant", tenant);
            rules.Add(rule);
        }

        // Detect VM OS definitions
        var osMatch = Regex.Match(line, @"^\s*os\s*=\s*""([^""]+)""");
        if (osMatch.Success)
        {
            var os = osMatch.Groups[1].Value;
            var rule = MatchAndCreateRule(rawLine.Trim(), "vm.os", os);
            rules.Add(rule);
        }

        // Detect network class
        var netClassMatch = Regex.Match(line, @"^\s*netClass\s*=\s*""([^""]+)""");
        if (netClassMatch.Success)
        {
            var netClass = netClassMatch.Groups[1].Value;
            var rule = MatchAndCreateRule(rawLine.Trim(), "vm.netClass", netClass);
            rules.Add(rule);
        }

        // Detect storage class
        var storClassMatch = Regex.Match(line, @"^\s*storClass\s*=\s*""([^""]+)""");
        if (storClassMatch.Success)
        {
            var storClass = storClassMatch.Groups[1].Value;
            var rule = MatchAndCreateRule(rawLine.Trim(), "vm.storClass", storClass);
            rules.Add(rule);
        }

        // Detect JSON file references (for firewall rules)
        var jsonFileMatch = Regex.Match(line, @"^\s*json_file_name\s*=\s*""([^""]+)""");
        if (jsonFileMatch.Success)
        {
            var jsonFile = jsonFileMatch.Groups[1].Value;
            var rule = MatchAndCreateRule(rawLine.Trim(), "firewall.json_file", jsonFile);
            rules.Add(rule);
        }
    }
}
