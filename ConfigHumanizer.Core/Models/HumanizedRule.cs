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

namespace ConfigHumanizer.Core.Models;

public enum Severity
{
    Info,
    GoodPractice,
    Warning,
    CriticalSecurity
}

public class HumanizedRule
{
    public string RawLine { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string HumanDescription { get; set; } = string.Empty;
    public Severity Severity { get; set; }
    public string SuggestedFix { get; set; } = string.Empty;
    public string FixReason { get; set; } = string.Empty;

    /// <summary>
    /// The 0-based line index in the original file where this rule was found.
    /// -1 indicates the line index is unknown or not applicable.
    /// </summary>
    public int LineIndex { get; set; } = -1;

    /// <summary>
    /// Detailed educational content explaining the configuration concept for junior developers.
    /// </summary>
    public string EducationalContent { get; set; } = string.Empty;

    public bool HasFix => !string.IsNullOrEmpty(SuggestedFix);
    public bool HasEducationalContent => !string.IsNullOrEmpty(EducationalContent);
}
