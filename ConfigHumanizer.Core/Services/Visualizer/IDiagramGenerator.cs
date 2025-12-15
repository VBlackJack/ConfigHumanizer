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

namespace ConfigHumanizer.Core.Services.Visualizer;

/// <summary>
/// Interface for generating visual diagrams from configuration rules.
/// </summary>
public interface IDiagramGenerator
{
    /// <summary>
    /// Generates a Mermaid.js diagram from parsed configuration rules.
    /// </summary>
    /// <param name="rules">The parsed configuration rules.</param>
    /// <param name="formatName">The format name (e.g., "OpenSSH", "Squid", "SSSD").</param>
    /// <returns>A Mermaid.js diagram string.</returns>
    string GenerateMermaid(List<HumanizedRule> rules, string formatName);
}
