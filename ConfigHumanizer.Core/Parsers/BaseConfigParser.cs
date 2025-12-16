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

using ConfigHumanizer.Core.Interfaces;
using ConfigHumanizer.Core.Models;
using ConfigHumanizer.Core.Rules;
using ConfigHumanizer.Core.Services;

namespace ConfigHumanizer.Core.Parsers;

/// <summary>
/// Base class for configuration parsers with rule engine support.
/// </summary>
public abstract class BaseConfigParser : IConfigParser
{
    protected readonly RuleEngine? RuleEngine;
    protected readonly string? FormatName;

    protected BaseConfigParser()
    {
    }

    protected BaseConfigParser(RuleEngine ruleEngine, string? formatName = null)
    {
        RuleEngine = ruleEngine;
        FormatName = formatName;
    }

    public abstract List<HumanizedRule> Parse(string fileContent);

    /// <summary>
    /// Creates a HumanizedRule from a matched AnalysisRule.
    /// </summary>
    protected HumanizedRule CreateRuleFromAnalysis(string rawLine, string key, string value, AnalysisRule rule, int lineIndex = -1)
    {
        return new HumanizedRule
        {
            RawLine = rawLine,
            Key = key,
            Value = value,
            HumanDescription = rule.HumanDescription,
            Severity = ParseSeverity(rule.Severity),
            SuggestedFix = rule.SuggestedFix,
            FixReason = rule.FixReason,
            LineIndex = lineIndex,
            EducationalContent = rule.EducationalContent
        };
    }

    /// <summary>
    /// Creates a default Info-level HumanizedRule when no rule matches.
    /// </summary>
    protected HumanizedRule CreateDefaultRule(string rawLine, string key, string value, int lineIndex = -1)
    {
        return new HumanizedRule
        {
            RawLine = rawLine,
            Key = key,
            Value = value,
            HumanDescription = $"Configuration option '{key}' is set to '{value}'.",
            Severity = Severity.Info,
            SuggestedFix = string.Empty,
            FixReason = string.Empty,
            LineIndex = lineIndex
        };
    }

    /// <summary>
    /// Parses a severity string to the Severity enum.
    /// </summary>
    protected static Severity ParseSeverity(string severity)
    {
        return severity.ToLowerInvariant() switch
        {
            "criticalsecurity" or "critical" => Severity.CriticalSecurity,
            "warning" => Severity.Warning,
            "goodpractice" or "good" => Severity.GoodPractice,
            "info" or _ => Severity.Info
        };
    }

    /// <summary>
    /// Tries to match a rule and create a HumanizedRule.
    /// </summary>
    /// <param name="rawLine">The raw line from the configuration file.</param>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value.</param>
    /// <param name="lineIndex">The 0-based line index in the original file (-1 if unknown).</param>
    protected HumanizedRule MatchAndCreateRule(string rawLine, string key, string value, int lineIndex = -1)
    {
        if (RuleEngine != null)
        {
            var matchedRule = RuleEngine.MatchRule(key, value, FormatName);
            if (matchedRule != null)
            {
                return CreateRuleFromAnalysis(rawLine, key, value, matchedRule, lineIndex);
            }
        }

        return CreateDefaultRule(rawLine, key, value, lineIndex);
    }
}
