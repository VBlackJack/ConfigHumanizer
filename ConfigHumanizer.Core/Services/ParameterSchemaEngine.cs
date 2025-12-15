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
using ConfigHumanizer.Core.Models;

namespace ConfigHumanizer.Core.Services;

/// <summary>
/// Engine pour charger et gérer les schémas de paramètres de configuration.
/// </summary>
public class ParameterSchemaEngine
{
    private readonly List<ParameterSchema> _schemas = new();
    private readonly List<string> _loadErrors = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Erreurs survenues lors du chargement des schémas.
    /// </summary>
    public IReadOnlyList<string> LoadErrors => _loadErrors.AsReadOnly();

    /// <summary>
    /// Tous les schémas chargés.
    /// </summary>
    public IReadOnlyList<ParameterSchema> Schemas => _schemas.AsReadOnly();

    /// <summary>
    /// Charge tous les fichiers *.params.json du répertoire spécifié.
    /// </summary>
    /// <param name="schemasPath">Chemin du répertoire contenant les schémas.</param>
    public void LoadSchemas(string schemasPath)
    {
        if (!Directory.Exists(schemasPath))
        {
            _loadErrors.Add($"Le répertoire de schémas n'existe pas: {schemasPath}");
            return;
        }

        var schemaFiles = Directory.GetFiles(schemasPath, "*.params.json", SearchOption.TopDirectoryOnly);

        foreach (var file in schemaFiles)
        {
            LoadSchemaFile(file);
        }
    }

    /// <summary>
    /// Charge un fichier de schéma spécifique.
    /// </summary>
    /// <param name="filePath">Chemin du fichier de schéma.</param>
    /// <returns>True si le chargement a réussi.</returns>
    public bool LoadSchemaFile(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            return LoadSchemaFromJson(json, Path.GetFileName(filePath));
        }
        catch (IOException ex)
        {
            var errorMsg = $"Erreur IO lors de la lecture de {Path.GetFileName(filePath)}: {ex.Message}";
            _loadErrors.Add(errorMsg);
            Debug.WriteLine(errorMsg);
            return false;
        }
    }

    /// <summary>
    /// Charge un schéma depuis une chaîne JSON.
    /// </summary>
    /// <param name="json">Contenu JSON du schéma.</param>
    /// <param name="sourceName">Nom de la source (pour les messages d'erreur).</param>
    /// <returns>True si le chargement a réussi.</returns>
    public bool LoadSchemaFromJson(string json, string sourceName = "JSON")
    {
        try
        {
            var schema = JsonSerializer.Deserialize<ParameterSchema>(json, JsonOptions);
            if (schema != null)
            {
                // Valider le schéma
                var validationErrors = ValidateSchema(schema);
                if (validationErrors.Count > 0)
                {
                    foreach (var error in validationErrors)
                    {
                        _loadErrors.Add($"{sourceName}: {error}");
                    }
                }

                _schemas.Add(schema);
                return true;
            }

            _loadErrors.Add($"{sourceName}: Échec de la désérialisation (résultat null)");
            return false;
        }
        catch (JsonException ex)
        {
            var errorMsg = $"{sourceName}: Erreur de parsing JSON: {ex.Message}";
            _loadErrors.Add(errorMsg);
            Debug.WriteLine(errorMsg);
            return false;
        }
    }

    /// <summary>
    /// Valide la structure d'un schéma.
    /// </summary>
    private static List<string> ValidateSchema(ParameterSchema schema)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(schema.FormatName))
        {
            errors.Add("FormatName est requis");
        }

        if (schema.ParameterCategories.Count == 0)
        {
            errors.Add("Au moins une catégorie de paramètres est requise");
        }

        foreach (var category in schema.ParameterCategories)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                errors.Add("Le nom de catégorie est requis");
            }

            foreach (var param in category.Parameters)
            {
                if (string.IsNullOrWhiteSpace(param.Key))
                {
                    errors.Add($"Catégorie '{category.Name}': La clé de paramètre est requise");
                }

                if (param.DataType == ParameterDataType.Enum &&
                    (param.EnumValues == null || param.EnumValues.Count == 0))
                {
                    errors.Add($"Paramètre '{param.Key}': Les valeurs Enum sont requises pour le type Enum");
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// Obtient le schéma correspondant à un fichier.
    /// </summary>
    /// <param name="filePath">Chemin du fichier de configuration.</param>
    /// <returns>Le schéma correspondant ou null.</returns>
    public ParameterSchema? GetSchemaForFile(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        var fileName = Path.GetFileName(filePath).ToLowerInvariant();
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        foreach (var schema in _schemas)
        {
            // Vérifier les patterns de fichiers
            foreach (var pattern in schema.FilePatterns)
            {
                if (MatchesPattern(fileName, pattern.ToLowerInvariant()))
                {
                    return schema;
                }
            }

            // Vérifier les extensions
            if (schema.FileExtensions != null)
            {
                foreach (var ext in schema.FileExtensions)
                {
                    if (string.Equals(extension, ext, StringComparison.OrdinalIgnoreCase))
                    {
                        return schema;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Obtient un schéma par son nom de format.
    /// </summary>
    /// <param name="formatName">Nom du format (ex: "OpenSSH").</param>
    /// <returns>Le schéma correspondant ou null.</returns>
    public ParameterSchema? GetSchemaByName(string formatName)
    {
        return _schemas.FirstOrDefault(s =>
            string.Equals(s.FormatName, formatName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Recherche des paramètres par mot-clé.
    /// </summary>
    /// <param name="keyword">Mot-clé à rechercher.</param>
    /// <param name="schema">Schéma dans lequel chercher (null = tous).</param>
    /// <returns>Liste de paramètres correspondants.</returns>
    public IEnumerable<(ParameterSchema Schema, ParameterCategory Category, ParameterDefinition Parameter)>
        SearchParameters(string keyword, ParameterSchema? schema = null)
    {
        IEnumerable<ParameterSchema> schemasToSearch = schema != null ? new[] { schema } : _schemas;
        var lowerKeyword = keyword.ToLowerInvariant();

        foreach (var s in schemasToSearch)
        {
            foreach (var category in s.ParameterCategories)
            {
                foreach (var param in category.Parameters)
                {
                    if (MatchesKeyword(param, lowerKeyword))
                    {
                        yield return (s, category, param);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Vérifie si un paramètre correspond à un mot-clé.
    /// </summary>
    private static bool MatchesKeyword(ParameterDefinition param, string lowerKeyword)
    {
        if (param.Key.ToLowerInvariant().Contains(lowerKeyword))
            return true;

        if (param.DisplayName.ToLowerInvariant().Contains(lowerKeyword))
            return true;

        if (param.Description?.ToLowerInvariant().Contains(lowerKeyword) == true)
            return true;

        if (param.Tags?.Any(t => t.ToLowerInvariant().Contains(lowerKeyword)) == true)
            return true;

        return false;
    }

    /// <summary>
    /// Vérifie si un nom de fichier correspond à un pattern glob.
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
