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
/// Catégorie groupant des paramètres de configuration liés.
/// </summary>
public class ParameterCategory
{
    /// <summary>
    /// Nom de la catégorie (ex: "Authentification", "Réseau").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description de la catégorie.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Icône pour l'affichage (emoji ou code icône).
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Ordre d'affichage dans la liste.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Paramètres appartenant à cette catégorie.
    /// </summary>
    public List<ParameterDefinition> Parameters { get; set; } = new();
}
