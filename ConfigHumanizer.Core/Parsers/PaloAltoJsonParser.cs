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

using System.Text;
using System.Text.Json;
using ConfigHumanizer.Core.Models;
using ConfigHumanizer.Core.Services;

namespace ConfigHumanizer.Core.Parsers;

/// <summary>
/// Parser for Palo Alto firewall rules in JSON format.
/// Handles North-South and West-East firewall rule definitions.
/// </summary>
public class PaloAltoJsonParser : BaseConfigParser
{
    // Mapping des ports vers des noms lisibles
    private static readonly Dictionary<string, (string Name, string Description)> PortMappings = new()
    {
        { "TCP_21", ("FTP", "Transfert de fichiers (non chiffré ⚠️)") },
        { "TCP_22", ("SSH", "Accès shell sécurisé") },
        { "TCP_23", ("Telnet", "Accès terminal (non chiffré 🔴)") },
        { "TCP_25", ("SMTP", "Envoi d'emails") },
        { "TCP_53", ("DNS", "Résolution de noms") },
        { "UDP_53", ("DNS", "Résolution de noms") },
        { "TCP_80", ("HTTP", "Web non chiffré") },
        { "TCP_88", ("Kerberos", "Authentification AD") },
        { "UDP_88", ("Kerberos", "Authentification AD") },
        { "UDP_123", ("NTP", "Synchronisation horaire") },
        { "TCP_135", ("RPC", "Appels de procédures distantes Windows") },
        { "TCP_139", ("NetBIOS", "Partage réseau Windows legacy") },
        { "UDP_137", ("NetBIOS", "Service de noms NetBIOS") },
        { "UDP_138", ("NetBIOS", "Service de datagrammes NetBIOS") },
        { "TCP_389", ("LDAP", "Annuaire (non chiffré)") },
        { "UDP_389", ("LDAP", "Annuaire (non chiffré)") },
        { "TCP_443", ("HTTPS", "Web sécurisé TLS") },
        { "TCP_445", ("SMB", "Partage de fichiers Windows") },
        { "UDP_445", ("SMB", "Partage de fichiers Windows") },
        { "TCP_464", ("Kpasswd", "Changement mot de passe Kerberos") },
        { "UDP_464", ("Kpasswd", "Changement mot de passe Kerberos") },
        { "TCP_636", ("LDAPS", "Annuaire sécurisé TLS ✓") },
        { "UDP_636", ("LDAPS", "Annuaire sécurisé TLS ✓") },
        { "TCP_1433", ("SQL Server", "Base de données Microsoft") },
        { "TCP_1545", ("SOC", "Communication vers SOC") },
        { "TCP_1688", ("KMS", "Activation licences Microsoft") },
        { "TCP_3268", ("GC", "Global Catalog AD") },
        { "TCP_3269", ("GC-SSL", "Global Catalog AD sécurisé") },
        { "TCP_3306", ("MySQL", "Base de données MySQL") },
        { "TCP_3389", ("RDP", "Bureau à distance Windows") },
        { "TCP_5432", ("PostgreSQL", "Base de données PostgreSQL") },
        { "TCP_5985", ("WinRM-HTTP", "Administration Windows à distance") },
        { "TCP_5986", ("WinRM-HTTPS", "Administration Windows sécurisée") },
        { "TCP_6379", ("Redis", "Cache/base NoSQL") },
        { "TCP_6514", ("Syslog-TLS", "Logs sécurisés ✓") },
        { "TCP_8530", ("WSUS-HTTP", "Mises à jour Windows") },
        { "TCP_8531", ("WSUS-HTTPS", "Mises à jour Windows sécurisées") },
        { "TCP_9389", ("ADWS", "Services Web AD") },
        { "TCP_27017", ("MongoDB", "Base de données NoSQL") },
    };

    public PaloAltoJsonParser() : base()
    {
    }

    public PaloAltoJsonParser(RuleEngine ruleEngine, string? formatName = null) : base(ruleEngine, formatName)
    {
    }

    public override List<HumanizedRule> Parse(string fileContent)
    {
        var rules = new List<HumanizedRule>();

        if (string.IsNullOrWhiteSpace(fileContent))
            return rules;

        try
        {
            using var doc = JsonDocument.Parse(fileContent);
            var root = doc.RootElement;

            // Parse tag_color if present - add summary header
            string tagColorValue = "unknown";
            string tagStatus = "";
            if (root.TryGetProperty("tag_color", out var tagColor))
            {
                tagColorValue = tagColor.GetString() ?? "unknown";
                tagStatus = tagColorValue switch
                {
                    "green" => "✅ Règles validées par l'équipe sécurité",
                    "red" => "🔴 Règles en attente de validation",
                    "yellow" => "🟡 Règles en cours de révision",
                    _ => ""
                };
            }

            // Count rules for summary
            int ruleCount = 0;
            if (root.TryGetProperty("rules", out var rulesArrayCount))
            {
                ruleCount = rulesArrayCount.GetArrayLength();
            }

            // Add file summary as first rule
            var summaryRule = new HumanizedRule
            {
                RawLine = "=== RÉSUMÉ DU FICHIER ===",
                Key = "file.summary",
                Value = $"{ruleCount} règles",
                HumanDescription = $"📋 **Fichier de règles Palo Alto**\n\n" +
                    $"Ce fichier contient **{ruleCount} règles firewall** North-South.\n" +
                    $"Ces règles contrôlent le trafic entre votre infrastructure et l'extérieur.\n\n" +
                    $"Statut : {tagStatus}",
                Severity = tagColorValue == "green" ? Severity.GoodPractice :
                          tagColorValue == "red" ? Severity.Warning : Severity.Info
            };
            rules.Add(summaryRule);

            // Parse rules array
            if (root.TryGetProperty("rules", out var rulesArray))
            {
                int ruleIndex = 0;
                foreach (var fwRule in rulesArray.EnumerateArray())
                {
                    ruleIndex++;
                    ParseFirewallRule(fwRule, rules, ruleIndex);
                }
            }
        }
        catch (JsonException)
        {
            return rules;
        }

        return rules;
    }

    private void ParseFirewallRule(JsonElement fwRule, List<HumanizedRule> rules, int ruleIndex)
    {
        var ruleName = GetStringProperty(fwRule, "name") ?? "unnamed";
        var source = GetStringProperty(fwRule, "source") ?? "any";
        var sourceZone = GetStringProperty(fwRule, "source_zone") ?? "";
        var dest = GetStringProperty(fwRule, "dest") ?? "any";
        var destZone = GetStringProperty(fwRule, "dest_zone") ?? "";
        var service = GetStringProperty(fwRule, "service") ?? "";
        var description = GetStringProperty(fwRule, "description") ?? "";

        // Build comprehensive human description
        var sb = new StringBuilder();

        // Rule header with name
        sb.AppendLine($"### 📌 Règle #{ruleIndex}: {ruleName}");
        sb.AppendLine();

        // Description if available
        if (!string.IsNullOrEmpty(description))
        {
            sb.AppendLine($"**Description:** {description}");
            sb.AppendLine();
        }

        // Source section
        sb.AppendLine("**🔹 Source (qui peut initier la connexion):**");
        if (source == "any")
        {
            sb.AppendLine("   ⚠️ **ANY** - N'importe quelle adresse IP peut utiliser cette règle");
        }
        else if (source.Contains(","))
        {
            sb.AppendLine($"   Liste d'adresses IP autorisées:");
            foreach (var ip in source.Split(','))
            {
                sb.AppendLine($"   • {ip.Trim()}");
            }
        }
        else if (source.Contains("/"))
        {
            var (network, hosts) = ParseCidr(source);
            sb.AppendLine($"   Réseau: **{source}** (~{hosts} machines potentielles)");
        }
        else
        {
            sb.AppendLine($"   IP unique: **{source}** ✓");
        }
        if (!string.IsNullOrEmpty(sourceZone) && sourceZone != "any")
        {
            sb.AppendLine($"   Zone de sécurité: {sourceZone}");
        }
        sb.AppendLine();

        // Destination section
        sb.AppendLine("**🔸 Destination (vers où le trafic est autorisé):**");
        if (dest == "any")
        {
            sb.AppendLine("   🔴 **ANY** - Peut communiquer avec n'importe quelle destination (risque d'exfiltration)");
        }
        else if (dest.Contains(","))
        {
            sb.AppendLine($"   Liste de serveurs cibles:");
            foreach (var ip in dest.Split(','))
            {
                sb.AppendLine($"   • {ip.Trim()}");
            }
        }
        else if (dest.Contains("/"))
        {
            var (network, hosts) = ParseCidr(dest);
            sb.AppendLine($"   Réseau: **{dest}** (~{hosts} machines potentielles)");
        }
        else
        {
            sb.AppendLine($"   IP unique: **{dest}** ✓");
        }
        if (!string.IsNullOrEmpty(destZone) && destZone != "any")
        {
            sb.AppendLine($"   Zone de sécurité: {destZone}");
        }
        sb.AppendLine();

        // Ports/Services section
        sb.AppendLine("**🔌 Ports autorisés:**");
        if (string.IsNullOrEmpty(service))
        {
            sb.AppendLine("   Tous les ports (⚠️ très permissif)");
        }
        else
        {
            var ports = service.Split(',');
            foreach (var port in ports)
            {
                var trimmedPort = port.Trim();
                var portInfo = GetPortDescription(trimmedPort);
                sb.AppendLine($"   • **{portInfo.name}** ({trimmedPort}) - {portInfo.description}");
            }
        }

        // Determine severity
        var severity = Severity.Info;
        if (dest == "any")
        {
            severity = Severity.CriticalSecurity;
        }
        else if (source == "any" || service.Contains("TCP_23") || service.Contains("TCP_21"))
        {
            severity = Severity.Warning;
        }
        else if (source.Contains("/32") || (!source.Contains("/") && !source.Contains(",") && source != "any"))
        {
            severity = Severity.GoodPractice;
        }

        // Create the rule
        var rule = new HumanizedRule
        {
            RawLine = $"Rule: {ruleName}",
            Key = $"fw_rule.{ruleIndex}",
            Value = ruleName,
            HumanDescription = sb.ToString(),
            Severity = severity,
            SuggestedFix = GetSuggestedFix(source, dest, service),
            FixReason = GetFixReason(source, dest, service)
        };

        rules.Add(rule);
    }

    private static (string name, string description) GetPortDescription(string port)
    {
        // Check for port ranges
        if (port.Contains("-"))
        {
            if (port.Contains("49152-65535"))
                return ("Ports dynamiques", "Plage RPC Windows (nécessaire pour AD)");
            if (port.Contains("25000-25010"))
                return ("Ports AD", "Communication Active Directory");
            if (port.Contains("1024-1028"))
                return ("Ports RPC", "Ports RPC dynamiques");

            return ("Plage de ports", port);
        }

        // Check known ports
        if (PortMappings.TryGetValue(port, out var mapping))
        {
            return (mapping.Name, mapping.Description);
        }

        // Extract port number for unknown ports
        var portNum = port.Replace("TCP_", "").Replace("UDP_", "");
        var protocol = port.StartsWith("UDP_") ? "UDP" : "TCP";
        return ($"Port {portNum}", $"Port {protocol}/{portNum}");
    }

    private static (string network, int hosts) ParseCidr(string cidr)
    {
        var parts = cidr.Split('/');
        if (parts.Length != 2 || !int.TryParse(parts[1], out var mask))
            return (cidr, 0);

        var hosts = (int)Math.Pow(2, 32 - mask);
        return (parts[0], hosts);
    }

    private static string GetSuggestedFix(string source, string dest, string service)
    {
        var fixes = new List<string>();

        if (dest == "any")
            fixes.Add("Définir des destinations spécifiques au lieu de ANY");
        if (source == "any")
            fixes.Add("Restreindre les sources autorisées si possible");
        if (service.Contains("TCP_23"))
            fixes.Add("Remplacer Telnet (23) par SSH (22)");
        if (service.Contains("TCP_21"))
            fixes.Add("Remplacer FTP (21) par SFTP (22) ou FTPS (990)");

        return string.Join(". ", fixes);
    }

    private static string GetFixReason(string source, string dest, string service)
    {
        if (dest == "any")
            return "Une destination ANY permet potentiellement l'exfiltration de données vers n'importe quel serveur externe.";
        if (source == "any")
            return "Une source ANY augmente la surface d'attaque en permettant des connexions depuis n'importe quelle IP.";
        if (service.Contains("TCP_23") || service.Contains("TCP_21"))
            return "Ces protocoles transmettent les données et mots de passe en clair sur le réseau.";

        return "";
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            return property.GetString();
        }
        return null;
    }
}
