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
using ConfigHumanizer.Core.Parsers;
using ConfigHumanizer.Core.Services;

namespace ConfigHumanizer.Core.Factories;

public static class ParserFactory
{
    /// <summary>
    /// Returns the appropriate parser based on the file path and content.
    /// </summary>
    /// <param name="filePath">The path of the configuration file.</param>
    /// <param name="fileContent">The content of the configuration file.</param>
    /// <param name="ruleEngine">Optional rule engine for data-driven analysis.</param>
    /// <returns>An IConfigParser instance suitable for the file type.</returns>
    public static IConfigParser GetParser(string? filePath, string fileContent, RuleEngine? ruleEngine = null)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return ruleEngine != null ? new SshdConfigParser(ruleEngine, "OpenSSH") : new SshdConfigParser();
        }

        var fileName = Path.GetFileName(filePath).ToLowerInvariant();
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        // ==================== SSH & Security ====================

        // Check for SSHD config files
        if (fileName.EndsWith("sshd_config", StringComparison.OrdinalIgnoreCase) ||
            fileName == "sshd_config")
        {
            return ruleEngine != null ? new SshdConfigParser(ruleEngine, "OpenSSH") : new SshdConfigParser();
        }

        // Check for SSH client config
        if (fileName == "ssh_config" || fileName == "config" && filePath.Contains(".ssh"))
        {
            return ruleEngine != null ? new SshdConfigParser(ruleEngine, "SSHClient") : new SshdConfigParser();
        }

        // Check for Sudoers files
        if (fileName == "sudoers" || filePath.Contains("sudoers.d"))
        {
            return ruleEngine != null ? new SshdConfigParser(ruleEngine, "Sudoers") : new SshdConfigParser();
        }

        // Check for PAM config files
        if (filePath.Contains("pam.d") || fileName == "pam.conf" ||
            fileName == "system-auth" || fileName == "password-auth" || fileName == "common-auth")
        {
            return ruleEngine != null ? new SshdConfigParser(ruleEngine, "PAM") : new SshdConfigParser();
        }

        // Check for iptables/nftables rules
        if (fileName.Contains("iptables") || fileName.Contains("ip6tables") ||
            fileName == "nftables.conf" || fileName.Contains("firewall"))
        {
            return ruleEngine != null ? new SshdConfigParser(ruleEngine, "Iptables") : new SshdConfigParser();
        }

        // ==================== Proxy & Load Balancing ====================

        // Check for Squid proxy config files
        if (fileName.Contains("squid"))
        {
            return ruleEngine != null ? new SshdConfigParser(ruleEngine, "Squid") : new SshdConfigParser();
        }

        // Check for HAProxy config files
        if (fileName.Contains("haproxy"))
        {
            return ruleEngine != null ? new SshdConfigParser(ruleEngine, "HAProxy") : new SshdConfigParser();
        }

        // ==================== Authentication ====================

        // Check for SSSD config files (INI format)
        if (fileName.Contains("sssd"))
        {
            return ruleEngine != null ? new IniConfigParser(ruleEngine, "SSSD") : new IniConfigParser();
        }

        // Check for Fail2ban config files (INI format)
        if (fileName.Contains("fail2ban") || fileName == "jail.conf" || fileName == "jail.local" || filePath.Contains("jail.d"))
        {
            return ruleEngine != null ? new IniConfigParser(ruleEngine, "Fail2ban") : new IniConfigParser();
        }

        // ==================== System Services ====================

        // Check for Systemd unit files (.service, .socket, .timer, .mount)
        if (extension == ".service" || extension == ".socket" || extension == ".timer" || extension == ".mount")
        {
            return ruleEngine != null ? new IniConfigParser(ruleEngine, "Systemd") : new IniConfigParser();
        }

        // Check for Sysctl config files
        if (fileName.Contains("sysctl"))
        {
            return ruleEngine != null ? new IniConfigParser(ruleEngine, "Sysctl") : new IniConfigParser();
        }

        // Check for Logrotate config files
        if (fileName == "logrotate.conf" || filePath.Contains("logrotate.d"))
        {
            return ruleEngine != null ? new SshdConfigParser(ruleEngine, "Logrotate") : new SshdConfigParser();
        }

        // Check for Rsyslog config files
        if (fileName.Contains("rsyslog") || filePath.Contains("rsyslog.d"))
        {
            return ruleEngine != null ? new SshdConfigParser(ruleEngine, "Rsyslog") : new SshdConfigParser();
        }

        // ==================== Databases ====================

        // Check for MySQL/MariaDB config files
        if (fileName == "my.cnf" || fileName == "my.ini" || fileName == "mysql.cnf" ||
            fileName == "mariadb.cnf" || filePath.Contains("mysql"))
        {
            return ruleEngine != null ? new IniConfigParser(ruleEngine, "MySQL") : new IniConfigParser();
        }

        // Check for PostgreSQL config files
        if (fileName == "postgresql.conf" || fileName == "pg_hba.conf" || filePath.Contains("postgresql"))
        {
            return ruleEngine != null ? new SshdConfigParser(ruleEngine, "PostgreSQL") : new SshdConfigParser();
        }

        // Check for Redis config files
        if (fileName == "redis.conf" || filePath.Contains("redis"))
        {
            return ruleEngine != null ? new SshdConfigParser(ruleEngine, "Redis") : new SshdConfigParser();
        }

        // ==================== Network Services ====================

        // Check for Postfix config files
        if (fileName == "main.cf" || fileName == "master.cf" || filePath.Contains("postfix"))
        {
            return ruleEngine != null ? new SshdConfigParser(ruleEngine, "Postfix") : new SshdConfigParser();
        }

        // Check for Samba config files
        if (fileName == "smb.conf" || filePath.Contains("samba"))
        {
            return ruleEngine != null ? new IniConfigParser(ruleEngine, "Samba") : new IniConfigParser();
        }

        // Check for NFS exports
        if (fileName == "exports")
        {
            return ruleEngine != null ? new SshdConfigParser(ruleEngine, "NFS") : new SshdConfigParser();
        }

        // ==================== Column-based Files ====================

        // Check for Crontab files
        if (fileName.Contains("crontab") || fileName.Contains("cron.d") || fileName.StartsWith("cron"))
        {
            return ruleEngine != null ? new ColumnConfigParser(ruleEngine, "Crontab") : new ColumnConfigParser();
        }

        // Check for Fstab files
        if (fileName.Contains("fstab"))
        {
            return ruleEngine != null ? new ColumnConfigParser(ruleEngine, "Fstab") : new ColumnConfigParser();
        }

        // Check for Hosts file
        if (fileName == "hosts")
        {
            return ruleEngine != null ? new ColumnConfigParser(ruleEngine, "Hosts") : new ColumnConfigParser();
        }

        // Check for Resolv.conf
        if (fileName.Contains("resolv"))
        {
            return ruleEngine != null ? new ColumnConfigParser(ruleEngine, "Resolv") : new ColumnConfigParser();
        }

        // ==================== Block-based Files ====================

        // Check for Nginx config files
        if (fileName.Contains("nginx") || fileName == "nginx.conf")
        {
            return ruleEngine != null ? new BlockConfigParser(ruleEngine, "Nginx") : new BlockConfigParser();
        }

        // Check for Apache config files
        if (fileName.Contains("apache") || fileName.Contains("httpd") ||
            fileName == "apache2.conf" || fileName == "httpd.conf")
        {
            return ruleEngine != null ? new BlockConfigParser(ruleEngine, "Apache") : new BlockConfigParser();
        }

        // Check for BIND/Named config files
        if (fileName.Contains("named") || fileName == "named.conf")
        {
            return ruleEngine != null ? new BlockConfigParser(ruleEngine, "BIND") : new BlockConfigParser();
        }

        // Check for DHCP config files
        if (fileName.Contains("dhcp") || fileName == "dhcpd.conf")
        {
            return ruleEngine != null ? new BlockConfigParser(ruleEngine, "DHCP") : new BlockConfigParser();
        }

        // ==================== Container & IaC ====================

        // Check for Dockerfile
        if (fileName == "dockerfile" || fileName.StartsWith("dockerfile."))
        {
            return ruleEngine != null ? new ColumnConfigParser(ruleEngine, "Dockerfile") : new ColumnConfigParser();
        }

        // Check for Terraform files
        if (extension == ".tf" || extension == ".tfvars")
        {
            return ruleEngine != null ? new SshdConfigParser(ruleEngine, "Terraform") : new SshdConfigParser();
        }

        // ==================== YAML Files ====================

        if (extension == ".yaml" || extension == ".yml")
        {
            // Docker Compose
            if (fileName.Contains("docker-compose") || fileName.Contains("compose"))
            {
                return ruleEngine != null ? new YamlConfigParser(ruleEngine, "DockerCompose") : new YamlConfigParser();
            }

            // GitLab CI
            if (fileName == ".gitlab-ci.yml" || fileName == ".gitlab-ci.yaml")
            {
                return ruleEngine != null ? new YamlConfigParser(ruleEngine, "GitLabCI") : new YamlConfigParser();
            }

            // GitHub Actions
            if (filePath.Contains(".github") && filePath.Contains("workflows"))
            {
                return ruleEngine != null ? new YamlConfigParser(ruleEngine, "GitHubActions") : new YamlConfigParser();
            }

            // Ansible playbooks
            if (fileName == "playbook.yml" || fileName == "playbook.yaml" || fileName == "site.yml" ||
                filePath.Contains("playbooks") || filePath.Contains("tasks") || filePath.Contains("handlers"))
            {
                return ruleEngine != null ? new YamlConfigParser(ruleEngine, "Ansible") : new YamlConfigParser();
            }

            // Prometheus
            if (fileName.Contains("prometheus"))
            {
                return ruleEngine != null ? new YamlConfigParser(ruleEngine, "Prometheus") : new YamlConfigParser();
            }

            // Traefik
            if (fileName.Contains("traefik"))
            {
                return ruleEngine != null ? new YamlConfigParser(ruleEngine, "Traefik") : new YamlConfigParser();
            }

            // Envoy
            if (fileName.Contains("envoy"))
            {
                return ruleEngine != null ? new YamlConfigParser(ruleEngine, "Envoy") : new YamlConfigParser();
            }

            // MongoDB (YAML format)
            if (fileName == "mongod.conf" || fileName == "mongodb.conf")
            {
                return ruleEngine != null ? new YamlConfigParser(ruleEngine, "MongoDB") : new YamlConfigParser();
            }

            // Kubernetes manifests
            if (fileContent.Contains("apiVersion:") && fileContent.Contains("kind:"))
            {
                return ruleEngine != null ? new YamlConfigParser(ruleEngine, "Kubernetes") : new YamlConfigParser();
            }

            // Generic YAML
            return ruleEngine != null ? new YamlConfigParser(ruleEngine, "YAML") : new YamlConfigParser();
        }

        // ==================== JSON Files ====================

        if (extension == ".json")
        {
            if (fileName == "package.json")
            {
                return ruleEngine != null ? new JsonConfigParser(ruleEngine, "NPM") : new JsonConfigParser();
            }

            if (fileName.Contains("appsettings"))
            {
                return ruleEngine != null ? new JsonConfigParser(ruleEngine, "AppSettings") : new JsonConfigParser();
            }

            return ruleEngine != null ? new JsonConfigParser(ruleEngine, "JSON") : new JsonConfigParser();
        }

        // ==================== INI-style Files ====================

        // Check for PHP INI files
        if (fileName == "php.ini" || (fileName.Contains("php") && extension == ".ini"))
        {
            return ruleEngine != null ? new IniConfigParser(ruleEngine, "PHP") : new IniConfigParser();
        }

        // Generic INI/CFG files
        if (extension == ".ini" || extension == ".cfg" || extension == ".conf")
        {
            return ruleEngine != null ? new IniConfigParser(ruleEngine) : new IniConfigParser();
        }

        // Default fallback to SSHD parser (space-separated key-value)
        return ruleEngine != null ? new SshdConfigParser(ruleEngine) : new SshdConfigParser();
    }
}
