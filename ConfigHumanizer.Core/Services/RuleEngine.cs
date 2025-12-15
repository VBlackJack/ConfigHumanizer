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

using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using ConfigHumanizer.Core.Rules;

namespace ConfigHumanizer.Core.Services;

/// <summary>
/// Engine for loading and matching configuration rules.
/// </summary>
public class RuleEngine
{
    private readonly List<RuleSet> _ruleSets = new();
    private readonly List<string> _loadErrors = new();

    // Regex timeout to prevent ReDoS attacks
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Gets all errors that occurred during rule loading.
    /// </summary>
    public IReadOnlyList<string> LoadErrors => _loadErrors.AsReadOnly();

    /// <summary>
    /// Gets all loaded rule sets.
    /// </summary>
    public IReadOnlyList<RuleSet> RuleSets => _ruleSets.AsReadOnly();

    /// <summary>
    /// Loads all *.rules.json files from the specified directory.
    /// </summary>
    /// <param name="rulesPath">Path to the directory containing rule files.</param>
    public void LoadRules(string rulesPath)
    {
        if (!Directory.Exists(rulesPath))
        {
            return;
        }

        var ruleFiles = Directory.GetFiles(rulesPath, "*.rules.json", SearchOption.TopDirectoryOnly);

        foreach (var file in ruleFiles)
        {
            try
            {
                var json = File.ReadAllText(file);
                var ruleSet = JsonSerializer.Deserialize<RuleSet>(json, JsonOptions);

                if (ruleSet != null)
                {
                    _ruleSets.Add(ruleSet);
                }
                else
                {
                    var errorMsg = $"Failed to deserialize rule file (null result): {file}";
                    _loadErrors.Add(errorMsg);
                    Debug.WriteLine(errorMsg);
                }
            }
            catch (JsonException ex)
            {
                var errorMsg = $"JSON parsing error in {Path.GetFileName(file)}: {ex.Message}";
                _loadErrors.Add(errorMsg);
                Debug.WriteLine(errorMsg);
            }
            catch (IOException ex)
            {
                var errorMsg = $"IO error reading {Path.GetFileName(file)}: {ex.Message}";
                _loadErrors.Add(errorMsg);
                Debug.WriteLine(errorMsg);
            }
            catch (Exception ex)
            {
                var errorMsg = $"Unexpected error loading {Path.GetFileName(file)}: {ex.Message}";
                _loadErrors.Add(errorMsg);
                Debug.WriteLine(errorMsg);
            }
        }
    }

    /// <summary>
    /// Loads rules from a JSON string.
    /// </summary>
    /// <param name="json">JSON content representing a RuleSet.</param>
    /// <returns>True if rules were loaded successfully, false otherwise.</returns>
    public bool LoadRulesFromJson(string json)
    {
        try
        {
            var ruleSet = JsonSerializer.Deserialize<RuleSet>(json, JsonOptions);
            if (ruleSet != null)
            {
                _ruleSets.Add(ruleSet);
                return true;
            }

            _loadErrors.Add("Failed to deserialize JSON string (null result)");
            return false;
        }
        catch (JsonException ex)
        {
            var errorMsg = $"JSON parsing error: {ex.Message}";
            _loadErrors.Add(errorMsg);
            Debug.WriteLine(errorMsg);
            return false;
        }
    }

    /// <summary>
    /// Finds a matching rule for the given key and value.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value.</param>
    /// <returns>The matching AnalysisRule or null if no match found.</returns>
    public AnalysisRule? MatchRule(string key, string value)
    {
        foreach (var ruleSet in _ruleSets)
        {
            var rule = MatchRuleInSet(ruleSet, key, value);
            if (rule != null)
            {
                return rule;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds a matching rule for the given key and value within a specific format.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value.</param>
    /// <param name="formatName">The format name to filter by (e.g., "OpenSSH", "Squid", "SSSD").</param>
    /// <returns>The matching AnalysisRule or null if no match found.</returns>
    public AnalysisRule? MatchRule(string key, string value, string? formatName)
    {
        if (string.IsNullOrEmpty(formatName))
        {
            return MatchRule(key, value);
        }

        var ruleSet = GetRuleSetByName(formatName);
        if (ruleSet != null)
        {
            return MatchRuleInSet(ruleSet, key, value);
        }

        return null;
    }

    /// <summary>
    /// Gets a ruleset by its format name.
    /// </summary>
    /// <param name="formatName">The format name to search for.</param>
    /// <returns>The matching RuleSet or null.</returns>
    public RuleSet? GetRuleSetByName(string formatName)
    {
        return _ruleSets.FirstOrDefault(rs =>
            string.Equals(rs.FormatName, formatName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds a matching rule for the given key and value within a specific ruleset.
    /// </summary>
    /// <param name="ruleSet">The ruleset to search.</param>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value.</param>
    /// <returns>The matching AnalysisRule or null if no match found.</returns>
    public AnalysisRule? MatchRuleInSet(RuleSet ruleSet, string key, string value)
    {
        foreach (var rule in ruleSet.Rules)
        {
            if (!string.Equals(rule.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsValueMatch(value, rule.ValuePattern))
            {
                return rule;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the appropriate ruleset for a file based on file patterns.
    /// </summary>
    /// <param name="filePath">The file path to match.</param>
    /// <returns>The matching RuleSet or null.</returns>
    public RuleSet? GetRuleSetForFile(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return _ruleSets.FirstOrDefault();
        }

        var fileName = Path.GetFileName(filePath).ToLowerInvariant();

        foreach (var ruleSet in _ruleSets)
        {
            foreach (var pattern in ruleSet.FilePatterns)
            {
                if (MatchesPattern(fileName, pattern.ToLowerInvariant()))
                {
                    return ruleSet;
                }
            }
        }

        return _ruleSets.FirstOrDefault();
    }

    /// <summary>
    /// Checks if a value matches a pattern (exact match or regex).
    /// </summary>
    private bool IsValueMatch(string value, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return true; // Empty pattern matches everything
        }

        // Try exact match first (case-insensitive) - most efficient
        if (string.Equals(value, pattern, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Try regex match with timeout to prevent ReDoS attacks
        try
        {
            return Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase, RegexTimeout);
        }
        catch (RegexMatchTimeoutException)
        {
            // Pattern took too long - log and reject
            Debug.WriteLine($"Regex timeout for pattern: {pattern}");
            return false;
        }
        catch (ArgumentException ex)
        {
            // Invalid regex pattern
            Debug.WriteLine($"Invalid regex pattern '{pattern}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks if a filename matches a glob-like pattern.
    /// </summary>
    private static bool MatchesPattern(string fileName, string pattern)
    {
        // Handle wildcards at both ends (*contains*)
        if (pattern.StartsWith('*') && pattern.EndsWith('*') && pattern.Length > 2)
        {
            var middle = pattern[1..^1];
            return fileName.Contains(middle, StringComparison.OrdinalIgnoreCase);
        }

        // Handle wildcard at start (*suffix)
        if (pattern.StartsWith('*'))
        {
            return fileName.EndsWith(pattern[1..], StringComparison.OrdinalIgnoreCase);
        }

        // Handle wildcard at end (prefix*)
        if (pattern.EndsWith('*'))
        {
            return fileName.StartsWith(pattern[..^1], StringComparison.OrdinalIgnoreCase);
        }

        // Exact match
        return string.Equals(fileName, pattern, StringComparison.OrdinalIgnoreCase);
    }
}
