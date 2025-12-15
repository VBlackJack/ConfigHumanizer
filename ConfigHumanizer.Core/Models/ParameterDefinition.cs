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
/// Types de données supportés pour les paramètres de configuration.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ParameterDataType
{
    String,
    Integer,
    Boolean,
    Enum,
    Path,
    IpAddress,
    Port,
    Duration,
    Size
}

/// <summary>
/// Option d'énumération avec sévérité associée.
/// </summary>
public class EnumOption
{
    /// <summary>
    /// Valeur réelle dans le fichier de configuration.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Nom affiché dans l'interface.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description détaillée de cette option.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Sévérité associée à cette valeur (pour indicateurs visuels).
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Severity? Severity { get; set; }
}

/// <summary>
/// Règles de validation pour un paramètre.
/// </summary>
public class ParameterValidation
{
    /// <summary>
    /// Valeur minimale (pour Integer, Port, Duration, Size).
    /// </summary>
    public long? Min { get; set; }

    /// <summary>
    /// Valeur maximale (pour Integer, Port, Duration, Size).
    /// </summary>
    public long? Max { get; set; }

    /// <summary>
    /// Pattern regex pour validation (pour String, Path, IpAddress).
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Message d'erreur personnalisé.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Le paramètre est-il obligatoire ?
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Valeurs interdites.
    /// </summary>
    public List<string>? ForbiddenValues { get; set; }
}

/// <summary>
/// Définition complète d'un paramètre de configuration.
/// </summary>
public class ParameterDefinition
{
    /// <summary>
    /// Clé du paramètre (nom dans le fichier de config).
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Nom affiché dans l'interface utilisateur.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description détaillée du paramètre.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type de données du paramètre.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ParameterDataType DataType { get; set; } = ParameterDataType.String;

    /// <summary>
    /// Valeur par défaut.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Options pour les paramètres de type Enum.
    /// </summary>
    public List<EnumOption>? EnumValues { get; set; }

    /// <summary>
    /// Règles de validation.
    /// </summary>
    public ParameterValidation? Validation { get; set; }

    /// <summary>
    /// Format booléen ("yes/no", "true/false", "on/off", "1/0").
    /// </summary>
    public string? BooleanFormat { get; set; }

    /// <summary>
    /// Indique si le paramètre peut avoir plusieurs valeurs.
    /// </summary>
    public bool MultiValue { get; set; }

    /// <summary>
    /// Séparateur pour les valeurs multiples.
    /// </summary>
    public string? MultiValueSeparator { get; set; }

    /// <summary>
    /// Unité pour l'affichage (ex: "ms", "MB", "connections").
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Exemple de valeur pour l'aide.
    /// </summary>
    public string? Example { get; set; }

    /// <summary>
    /// Lien vers la documentation officielle.
    /// </summary>
    public string? DocumentationUrl { get; set; }

    /// <summary>
    /// Tags pour recherche/filtrage.
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Niveau de sécurité associé au paramètre.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Severity? SecurityLevel { get; set; }
}
