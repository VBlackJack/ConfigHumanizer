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

/// <summary>
/// Summary of a configuration file analysis for dashboard display.
/// </summary>
public class FileAnalysisSummary
{
    /// <summary>
    /// Full path to the analyzed file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// File name without path.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Detected configuration format (e.g., "OpenSSH", "Nginx", "DockerCompose").
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Health score from 0 to 100.
    /// </summary>
    public double HealthScore { get; set; }

    /// <summary>
    /// Number of critical security issues found.
    /// </summary>
    public int CriticalCount { get; set; }

    /// <summary>
    /// Number of warnings found.
    /// </summary>
    public int WarningCount { get; set; }

    /// <summary>
    /// Number of informational findings.
    /// </summary>
    public int InfoCount { get; set; }

    /// <summary>
    /// Number of good practices detected.
    /// </summary>
    public int GoodCount { get; set; }

    /// <summary>
    /// Total number of rules analyzed.
    /// </summary>
    public int TotalRules { get; set; }

    /// <summary>
    /// Returns a color string based on the HealthScore.
    /// Green (>80), Orange (>50), Red (otherwise).
    /// </summary>
    public string StatusColor => HealthScore > 80 ? "#4CAF50" :
                                  HealthScore > 50 ? "#F39C12" : "#E74C3C";

    /// <summary>
    /// Returns a status text based on the HealthScore.
    /// </summary>
    public string StatusText => HealthScore > 80 ? "Bon" :
                                 HealthScore > 50 ? "Attention" : "Critique";
}
