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
using ConfigHumanizer.Core.Services;
using System.Text;

namespace ConfigHumanizer.Core.Parsers;

/// <summary>
/// Parser for Unix account files (passwd, group, shadow).
/// Format passwd: username:password:uid:gid:gecos:home:shell
/// Format group: groupname:password:gid:members
/// </summary>
public class UnixAccountParser : BaseConfigParser
{
    private readonly Dictionary<int, string> _gidToGroupName = new();
    private readonly Dictionary<string, List<string>> _groupMembers = new();

    public UnixAccountParser() : base()
    {
    }

    public UnixAccountParser(RuleEngine ruleEngine, string? formatName = null) : base(ruleEngine, formatName)
    {
    }

    public override List<HumanizedRule> Parse(string fileContent)
    {
        var rules = new List<HumanizedRule>();

        if (string.IsNullOrWhiteSpace(fileContent))
            return rules;

        var lines = fileContent.Split('\n', StringSplitOptions.None);

        // First pass: determine if this is passwd or group format
        var isPasswd = DetermineFormat(lines);

        // For group files, build GID to name mapping first
        if (!isPasswd)
        {
            BuildGroupMappings(lines);
        }

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith('#'))
                continue;

            var rule = isPasswd ? ParsePasswdLine(trimmedLine) : ParseGroupLine(trimmedLine);
            if (rule != null)
            {
                rules.Add(rule);
            }
        }

        return rules;
    }

    private bool DetermineFormat(string[] lines)
    {
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                continue;

            var parts = trimmed.Split(':');
            // passwd has 7 fields, group has 4 fields
            return parts.Length >= 6;
        }
        return true; // default to passwd
    }

    private void BuildGroupMappings(string[] lines)
    {
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                continue;

            var parts = trimmed.Split(':');
            if (parts.Length >= 3 && int.TryParse(parts[2], out var gid))
            {
                _gidToGroupName[gid] = parts[0];
                if (parts.Length >= 4 && !string.IsNullOrEmpty(parts[3]))
                {
                    _groupMembers[parts[0]] = parts[3].Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                }
            }
        }
    }

    private HumanizedRule? ParsePasswdLine(string line)
    {
        var parts = line.Split(':');
        if (parts.Length < 7)
            return null;

        var username = parts[0];
        var password = parts[1];
        var uid = int.TryParse(parts[2], out var u) ? u : -1;
        var gid = int.TryParse(parts[3], out var g) ? g : -1;
        var gecos = parts[4];
        var home = parts[5];
        var shell = parts[6];

        var sb = new StringBuilder();
        sb.AppendLine($"### 👤 Utilisateur: **{username}**");
        sb.AppendLine();

        // Account type determination
        var accountType = DetermineAccountType(username, uid, shell);
        sb.AppendLine($"**Type de compte:** {accountType}");
        sb.AppendLine();

        // Basic info
        sb.AppendLine("**Identifiants:**");
        sb.AppendLine($"- UID: `{uid}` {GetUidDescription(uid)}");
        sb.AppendLine($"- GID: `{gid}`");
        sb.AppendLine();

        // GECOS field
        if (!string.IsNullOrEmpty(gecos))
        {
            sb.AppendLine($"**Description:** {gecos}");
            sb.AppendLine();
        }

        // Home directory
        sb.AppendLine($"**Répertoire personnel:** `{home}`");
        sb.AppendLine();

        // Shell analysis
        sb.AppendLine($"**Shell:** `{shell}`");
        var shellAnalysis = AnalyzeShell(shell, accountType);
        if (!string.IsNullOrEmpty(shellAnalysis))
        {
            sb.AppendLine($"  - {shellAnalysis}");
        }

        // Security analysis
        var (severity, securityNotes, suggestedFix) = AnalyzePasswdSecurity(username, uid, password, shell, accountType);

        if (securityNotes.Any())
        {
            sb.AppendLine();
            sb.AppendLine("**⚠️ Points d'attention:**");
            foreach (var note in securityNotes)
            {
                sb.AppendLine($"- {note}");
            }
        }

        return new HumanizedRule
        {
            RawLine = line,
            Key = username,
            Value = $"UID:{uid} GID:{gid} Shell:{shell}",
            HumanDescription = sb.ToString(),
            Severity = severity,
            SuggestedFix = suggestedFix,
            FixReason = securityNotes.FirstOrDefault() ?? ""
        };
    }

    private HumanizedRule? ParseGroupLine(string line)
    {
        var parts = line.Split(':');
        if (parts.Length < 3)
            return null;

        var groupName = parts[0];
        var password = parts.Length > 1 ? parts[1] : "";
        var gid = int.TryParse(parts[2], out var g) ? g : -1;
        var members = parts.Length > 3 ? parts[3].Split(',', StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>();

        var sb = new StringBuilder();
        sb.AppendLine($"### 👥 Groupe: **{groupName}**");
        sb.AppendLine();

        // Group type
        var groupType = DetermineGroupType(groupName, gid);
        sb.AppendLine($"**Type:** {groupType}");
        sb.AppendLine();

        // GID info
        sb.AppendLine($"**GID:** `{gid}` {GetGidDescription(gid)}");
        sb.AppendLine();

        // Members
        if (members.Length > 0)
        {
            sb.AppendLine($"**Membres ({members.Length}):**");
            foreach (var member in members)
            {
                sb.AppendLine($"- `{member}`");
            }
        }
        else
        {
            sb.AppendLine("**Membres:** _Aucun membre explicite_");
            sb.AppendLine("  - Les utilisateurs avec ce GID principal appartiennent implicitement à ce groupe");
        }

        // Security analysis
        var (severity, securityNotes, suggestedFix) = AnalyzeGroupSecurity(groupName, gid, password, members);

        if (securityNotes.Any())
        {
            sb.AppendLine();
            sb.AppendLine("**⚠️ Points d'attention:**");
            foreach (var note in securityNotes)
            {
                sb.AppendLine($"- {note}");
            }
        }

        return new HumanizedRule
        {
            RawLine = line,
            Key = groupName,
            Value = $"GID:{gid} Membres:{members.Length}",
            HumanDescription = sb.ToString(),
            Severity = severity,
            SuggestedFix = suggestedFix,
            FixReason = securityNotes.FirstOrDefault() ?? ""
        };
    }

    private string DetermineAccountType(string username, int uid, string shell)
    {
        if (uid == 0)
            return "🔴 Compte superutilisateur (root)";
        if (uid < 1000 && uid > 0)
            return "⚙️ Compte système/service";
        if (shell.Contains("nologin") || shell.Contains("false") || shell == "/bin/sync")
            return "🔒 Compte de service (sans login interactif)";
        if (username.StartsWith("svc_"))
            return "🤖 Compte de service applicatif";
        return "👤 Compte utilisateur standard";
    }

    private string DetermineGroupType(string groupName, int gid)
    {
        if (gid == 0)
            return "🔴 Groupe root (privilèges administrateur)";
        if (groupName == "wheel" || groupName == "sudo")
            return "⚡ Groupe sudo/wheel (peut devenir root)";
        if (gid < 1000)
            return "⚙️ Groupe système";
        return "👥 Groupe utilisateur";
    }

    private string GetUidDescription(int uid)
    {
        return uid switch
        {
            0 => "_(superutilisateur - accès total au système)_",
            < 100 => "_(compte système réservé)_",
            < 1000 => "_(compte système)_",
            < 10000 => "_(utilisateur local)_",
            >= 10000000 => "_(compte LDAP/AD - UID élevé typique d'annuaire)_",
            _ => "_(utilisateur)_"
        };
    }

    private string GetGidDescription(int gid)
    {
        return gid switch
        {
            0 => "_(groupe root)_",
            < 100 => "_(groupe système réservé)_",
            < 1000 => "_(groupe système)_",
            >= 10000000 => "_(groupe LDAP/AD)_",
            _ => ""
        };
    }

    private string AnalyzeShell(string shell, string accountType)
    {
        return shell switch
        {
            "/bin/bash" or "/bin/sh" or "/bin/zsh" => "Shell interactif standard - permet la connexion",
            "/sbin/nologin" => "Connexion interdite - affiche un message et déconnecte",
            "/bin/false" => "Connexion interdite - termine immédiatement",
            "/bin/sync" => "Compte spécial - exécute sync puis déconnecte",
            "/sbin/halt" => "Compte spécial - arrête le système",
            "/sbin/shutdown" => "Compte spécial - éteint le système",
            _ when shell.Contains("nologin") => "Connexion interdite",
            _ => ""
        };
    }

    private (Severity severity, List<string> notes, string suggestedFix) AnalyzePasswdSecurity(
        string username, int uid, string password, string shell, string accountType)
    {
        var notes = new List<string>();
        var severity = Severity.Info;
        var fix = "";

        // UID 0 check (multiple root accounts)
        if (uid == 0 && username != "root")
        {
            notes.Add("⚠️ CRITIQUE: Ce compte a UID 0 (équivalent root) - risque de sécurité majeur!");
            severity = Severity.CriticalSecurity;
            fix = "Vérifier si ce compte UID 0 est légitime. Généralement, seul 'root' devrait avoir UID 0.";
        }

        // Password field check
        if (password != "x" && password != "!")
        {
            if (string.IsNullOrEmpty(password))
            {
                notes.Add("⚠️ CRITIQUE: Pas de mot de passe requis pour ce compte!");
                severity = Severity.CriticalSecurity;
                fix = "Définir un mot de passe ou désactiver le compte: passwd -l " + username;
            }
            else if (password != "*")
            {
                notes.Add("⚠️ Mot de passe potentiellement stocké dans ce fichier (devrait être dans /etc/shadow)");
                severity = severity == Severity.Info ? Severity.Warning : severity;
            }
        }

        // Service account with login shell
        if (uid > 0 && uid < 1000 && !shell.Contains("nologin") && !shell.Contains("false") && shell != "/bin/sync")
        {
            notes.Add("⚠️ Compte système avec shell de connexion - généralement les services ne devraient pas pouvoir se connecter");
            severity = severity == Severity.Info ? Severity.Warning : severity;
            fix = $"usermod -s /sbin/nologin {username}";
        }

        // Good practices
        if (shell.Contains("nologin") && uid > 0 && uid < 1000)
        {
            notes.Add("✅ Bonne pratique: compte service sans shell de connexion");
            if (severity == Severity.Info)
                severity = Severity.GoodPractice;
        }

        return (severity, notes, fix);
    }

    private (Severity severity, List<string> notes, string suggestedFix) AnalyzeGroupSecurity(
        string groupName, int gid, string password, string[] members)
    {
        var notes = new List<string>();
        var severity = Severity.Info;
        var fix = "";

        // Wheel/sudo group analysis
        if (groupName == "wheel" || groupName == "sudo")
        {
            if (members.Length > 5)
            {
                notes.Add($"⚠️ {members.Length} membres dans le groupe sudo - vérifier si tous ont besoin de privilèges root");
                severity = Severity.Warning;
            }
            else if (members.Length > 0)
            {
                notes.Add($"ℹ️ Ces utilisateurs peuvent exécuter des commandes en tant que root via sudo");
            }
        }

        // Root group with members
        if (gid == 0 && members.Length > 0)
        {
            notes.Add("⚠️ Des utilisateurs sont membres du groupe root - accès aux fichiers root");
            severity = Severity.Warning;
        }

        // Group password (rarely used)
        if (!string.IsNullOrEmpty(password) && password != "x" && password != "!")
        {
            notes.Add("⚠️ Ce groupe a un mot de passe défini - fonctionnalité rarement utilisée");
            severity = severity == Severity.Info ? Severity.Warning : severity;
        }

        // Large groups
        if (members.Length > 20)
        {
            notes.Add($"ℹ️ Groupe avec {members.Length} membres - vérifier la pertinence de tous les membres");
        }

        return (severity, notes, fix);
    }
}
