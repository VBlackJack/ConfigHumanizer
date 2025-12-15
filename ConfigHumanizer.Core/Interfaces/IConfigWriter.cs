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

namespace ConfigHumanizer.Core.Interfaces;

/// <summary>
/// Interface pour l'écriture de lignes de configuration.
/// </summary>
public interface IConfigWriter
{
    /// <summary>
    /// Génère une ligne de configuration pour un paramètre et sa valeur.
    /// </summary>
    /// <param name="schema">Schéma du format de configuration.</param>
    /// <param name="definition">Définition du paramètre.</param>
    /// <param name="value">Valeur du paramètre.</param>
    /// <returns>La ligne de configuration formatée.</returns>
    string GenerateLine(ParameterSchema schema, ParameterDefinition definition, object? value);

    /// <summary>
    /// Génère un bloc de configuration pour plusieurs paramètres.
    /// </summary>
    /// <param name="schema">Schéma du format de configuration.</param>
    /// <param name="parameters">Liste de tuples (définition, valeur).</param>
    /// <returns>Le bloc de configuration formaté.</returns>
    string GenerateBlock(ParameterSchema schema, IEnumerable<(ParameterDefinition Definition, object? Value)> parameters);

    /// <summary>
    /// Formate une valeur selon le type de données.
    /// </summary>
    /// <param name="definition">Définition du paramètre.</param>
    /// <param name="value">Valeur à formater.</param>
    /// <returns>La valeur formatée.</returns>
    string FormatValue(ParameterDefinition definition, object? value);
}
