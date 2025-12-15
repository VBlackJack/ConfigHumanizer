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

using ConfigHumanizer.Core.Interfaces;
using ConfigHumanizer.Core.Models;
using ConfigHumanizer.Core.Writers;

namespace ConfigHumanizer.Core.Factories;

/// <summary>
/// Factory pour créer le writer approprié selon le schéma de configuration.
/// </summary>
public static class WriterFactory
{
    private static readonly SpaceSeparatedConfigWriter SpaceSeparatedWriter = new();
    private static readonly IniConfigWriter IniWriter = new();
    private static readonly YamlConfigWriter YamlWriter = new();
    private static readonly JsonConfigWriter JsonWriter = new();
    private static readonly BlockConfigWriter NginxWriter = new() { Style = BlockConfigWriter.BlockStyle.Nginx };
    private static readonly BlockConfigWriter ApacheWriter = new() { Style = BlockConfigWriter.BlockStyle.Apache };
    private static readonly BlockConfigWriter BindWriter = new() { Style = BlockConfigWriter.BlockStyle.Bind };
    private static readonly BlockConfigWriter DhcpWriter = new() { Style = BlockConfigWriter.BlockStyle.Dhcp };

    /// <summary>
    /// Obtient le writer approprié pour un schéma donné.
    /// </summary>
    /// <param name="schema">Schéma de configuration.</param>
    /// <returns>Le writer correspondant.</returns>
    public static IConfigWriter GetWriter(ParameterSchema schema)
    {
        return schema.Syntax switch
        {
            ConfigSyntax.SpaceSeparated => SpaceSeparatedWriter,
            ConfigSyntax.Ini => IniWriter,
            ConfigSyntax.Yaml => YamlWriter,
            ConfigSyntax.Json => JsonWriter,
            ConfigSyntax.Block => GetBlockWriter(schema.FormatName),
            ConfigSyntax.Hcl => SpaceSeparatedWriter, // HCL utilise une syntaxe similaire
            ConfigSyntax.Column => SpaceSeparatedWriter,
            ConfigSyntax.Xml => SpaceSeparatedWriter, // TODO: XmlConfigWriter
            ConfigSyntax.Toml => IniWriter, // TOML est similaire à INI
            _ => SpaceSeparatedWriter
        };
    }

    /// <summary>
    /// Obtient le writer de bloc approprié selon le format.
    /// </summary>
    private static IConfigWriter GetBlockWriter(string formatName)
    {
        return formatName.ToLowerInvariant() switch
        {
            "nginx" => NginxWriter,
            "apache" or "httpd" => ApacheWriter,
            "bind" or "named" or "dns" => BindWriter,
            "dhcp" or "dhcpd" => DhcpWriter,
            _ => NginxWriter // Par défaut style Nginx
        };
    }

    /// <summary>
    /// Génère une ligne de configuration pour un paramètre.
    /// </summary>
    /// <param name="schema">Schéma de configuration.</param>
    /// <param name="definition">Définition du paramètre.</param>
    /// <param name="value">Valeur du paramètre.</param>
    /// <returns>La ligne de configuration formatée.</returns>
    public static string GenerateLine(ParameterSchema schema, ParameterDefinition definition, object? value)
    {
        var writer = GetWriter(schema);
        return writer.GenerateLine(schema, definition, value);
    }

    /// <summary>
    /// Génère un bloc de configuration pour plusieurs paramètres.
    /// </summary>
    /// <param name="schema">Schéma de configuration.</param>
    /// <param name="parameters">Liste de paramètres avec leurs valeurs.</param>
    /// <returns>Le bloc de configuration formaté.</returns>
    public static string GenerateBlock(ParameterSchema schema,
        IEnumerable<(ParameterDefinition Definition, object? Value)> parameters)
    {
        var writer = GetWriter(schema);
        return writer.GenerateBlock(schema, parameters);
    }
}
