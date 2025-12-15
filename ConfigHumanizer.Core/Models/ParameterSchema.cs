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

namespace ConfigHumanizer.Core.Models;

/// <summary>
/// Format de syntaxe pour l'écriture de configuration.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConfigSyntax
{
    /// <summary>
    /// Clé valeur séparés par espace (SSH, Squid, etc.)
    /// </summary>
    SpaceSeparated,

    /// <summary>
    /// Format INI avec sections [section]
    /// </summary>
    Ini,

    /// <summary>
    /// Format YAML
    /// </summary>
    Yaml,

    /// <summary>
    /// Format JSON
    /// </summary>
    Json,

    /// <summary>
    /// Format bloc avec accolades (Nginx, Apache)
    /// </summary>
    Block,

    /// <summary>
    /// Format HCL (Terraform)
    /// </summary>
    Hcl,

    /// <summary>
    /// Format colonne (passwd, fstab)
    /// </summary>
    Column,

    /// <summary>
    /// Format XML
    /// </summary>
    Xml,

    /// <summary>
    /// Format TOML
    /// </summary>
    Toml
}

/// <summary>
/// Schéma complet définissant les paramètres d'un format de configuration.
/// </summary>
public class ParameterSchema
{
    /// <summary>
    /// Nom du format (ex: "OpenSSH", "Nginx").
    /// </summary>
    public string FormatName { get; set; } = string.Empty;

    /// <summary>
    /// Description du format.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Version du schéma.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Patterns de noms de fichiers associés (ex: "*sshd_config", "nginx.conf").
    /// </summary>
    public List<string> FilePatterns { get; set; } = new();

    /// <summary>
    /// Extensions de fichiers associées (ex: ".conf", ".yaml").
    /// </summary>
    public List<string>? FileExtensions { get; set; }

    /// <summary>
    /// Syntaxe du fichier de configuration.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ConfigSyntax Syntax { get; set; } = ConfigSyntax.SpaceSeparated;

    /// <summary>
    /// Caractère de commentaire (ex: "#", "//", ";").
    /// </summary>
    public string CommentChar { get; set; } = "#";

    /// <summary>
    /// Séparateur clé-valeur (ex: " ", "=", ":").
    /// </summary>
    public string KeyValueSeparator { get; set; } = " ";

    /// <summary>
    /// La clé doit-elle être en majuscules ?
    /// </summary>
    public bool KeyUpperCase { get; set; }

    /// <summary>
    /// La clé doit-elle être en minuscules ?
    /// </summary>
    public bool KeyLowerCase { get; set; }

    /// <summary>
    /// Indentation par niveau (pour formats imbriqués).
    /// </summary>
    public string Indentation { get; set; } = "  ";

    /// <summary>
    /// Catégories de paramètres.
    /// </summary>
    public List<ParameterCategory> ParameterCategories { get; set; } = new();

    /// <summary>
    /// URL de la documentation officielle.
    /// </summary>
    public string? DocumentationUrl { get; set; }

    /// <summary>
    /// Obtient tous les paramètres de toutes les catégories.
    /// </summary>
    public IEnumerable<ParameterDefinition> GetAllParameters()
    {
        return ParameterCategories.SelectMany(c => c.Parameters);
    }

    /// <summary>
    /// Recherche un paramètre par sa clé.
    /// </summary>
    public ParameterDefinition? FindParameter(string key)
    {
        return GetAllParameters()
            .FirstOrDefault(p => p.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    }
}
