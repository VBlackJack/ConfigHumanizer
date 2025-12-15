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
/// Represents a collection of analysis rules for a specific configuration format.
/// </summary>
public class RuleSet
{
    /// <summary>
    /// Name of the configuration format (e.g., "OpenSSH", "SSSD", "Squid").
    /// </summary>
    [JsonPropertyName("formatName")]
    public string FormatName { get; set; } = string.Empty;

    /// <summary>
    /// File patterns this ruleset applies to (e.g., ["*sshd_config", "*.ssh"]).
    /// </summary>
    [JsonPropertyName("filePatterns")]
    public List<string> FilePatterns { get; set; } = new();

    /// <summary>
    /// Collection of analysis rules for this format.
    /// </summary>
    [JsonPropertyName("rules")]
    public List<AnalysisRule> Rules { get; set; } = new();
}
