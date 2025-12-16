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

using System.Text.Json.Serialization;

namespace ConfigHumanizer.Core.Rules;

/// <summary>
/// Represents a single analysis rule for configuration validation.
/// </summary>
public class AnalysisRule
{
    /// <summary>
    /// The configuration key to watch (e.g., "PermitRootLogin").
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Regex pattern or exact match value to check against.
    /// </summary>
    [JsonPropertyName("valuePattern")]
    public string ValuePattern { get; set; } = string.Empty;

    /// <summary>
    /// Severity level: "CriticalSecurity", "Warning", "GoodPractice", "Info".
    /// </summary>
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "Info";

    /// <summary>
    /// Human-readable explanation of the rule.
    /// </summary>
    [JsonPropertyName("humanDescription")]
    public string HumanDescription { get; set; } = string.Empty;

    /// <summary>
    /// Suggested fix value (e.g., "PermitRootLogin no").
    /// </summary>
    [JsonPropertyName("suggestedFix")]
    public string SuggestedFix { get; set; } = string.Empty;

    /// <summary>
    /// Explanation of why the fix is recommended.
    /// </summary>
    [JsonPropertyName("fixReason")]
    public string FixReason { get; set; } = string.Empty;

    /// <summary>
    /// Detailed educational content explaining the configuration concept for junior developers.
    /// </summary>
    [JsonPropertyName("educationalContent")]
    public string EducationalContent { get; set; } = string.Empty;
}
