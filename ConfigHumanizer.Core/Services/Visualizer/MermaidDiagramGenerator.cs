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
using ConfigHumanizer.Core.Models;

namespace ConfigHumanizer.Core.Services.Visualizer;

/// <summary>
/// Generates Mermaid.js diagrams from configuration rules.
/// </summary>
public class MermaidDiagramGenerator : IDiagramGenerator
{
    /// <inheritdoc />
    public string GenerateMermaid(List<HumanizedRule> rules, string formatName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("graph LR");

        return formatName?.ToLowerInvariant() switch
        {
            "squid" => GenerateSquidDiagram(sb, rules),
            "sssd" => GenerateSssdDiagram(sb, rules),
            "openssh" => GenerateOpenSshDiagram(sb, rules),
            "systemd" => GenerateSystemdDiagram(sb, rules),
            "sysctl" => GenerateSysctlDiagram(sb, rules),
            "crontab" => GenerateCrontabDiagram(sb, rules),
            "fstab" => GenerateFstabDiagram(sb, rules),
            "hosts" => GenerateHostsDiagram(sb, rules),
            "resolv" => GenerateResolvDiagram(sb, rules),
            "nginx" => GenerateNginxDiagram(sb, rules),
            "dockercompose" => GenerateDockerComposeDiagram(sb, rules),
            "haproxy" => GenerateHAProxyDiagram(sb, rules),
            "mysql" or "postgresql" or "redis" or "mongodb" => GenerateDatabaseDiagram(sb, rules, formatName),
            "fail2ban" => GenerateFail2banDiagram(sb, rules),
            "iptables" => GenerateFirewallDiagram(sb, rules),
            "postfix" => GeneratePostfixDiagram(sb, rules),
            "terraform" => GenerateTerraformDiagram(sb, rules),
            "paloalto" => GeneratePaloAltoDiagram(sb, rules),
            "passwd" => GeneratePasswdDiagram(sb, rules),
            "group" => GenerateGroupDiagram(sb, rules),
            "shadow" => GeneratePasswdDiagram(sb, rules),
            "sudoers" => GenerateSudoersDiagram(sb, rules),
            "rsyslog" => GenerateRsyslogDiagram(sb, rules),
            _ => GenerateGenericDiagram(sb, rules, formatName)
        };
    }

    private static string GenerateSquidDiagram(StringBuilder sb, List<HumanizedRule> rules)
    {
        var httpPort = FindRuleValue(rules, "http_port") ?? "3128";
        var cacheDir = FindRuleValue(rules, "cache_dir");
        var httpAccess = FindRuleValue(rules, "http_access");

        // Client to Proxy connection
        sb.AppendLine($"    Client((Client)) -->|Port {httpPort}| Proxy[Squid Proxy]");

        // Cache storage
        if (!string.IsNullOrEmpty(cacheDir) && !cacheDir.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("    Proxy -->|Cache| Disk[(Storage)]");
        }
        else
        {
            sb.AppendLine("    Proxy -.->|No Cache| Disk[(Storage)]");
        }

        // Internet connection
        sb.AppendLine("    Proxy -->|Forward| Internet((Internet))");

        // ACL warning
        if (!string.IsNullOrEmpty(httpAccess) && httpAccess.Contains("allow all", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("    style Proxy fill:#ff6b6b,stroke:#c92a2a");
        }

        return sb.ToString();
    }

    private static string GenerateSssdDiagram(StringBuilder sb, List<HumanizedRule> rules)
    {
        var idProvider = FindRuleValue(rules, "id_provider") ?? "ldap";
        var authProvider = FindRuleValue(rules, "auth_provider");
        var cacheCredentials = FindRuleValue(rules, "cache_credentials");
        var enumerate = FindRuleValue(rules, "enumerate");

        // Core SSSD flow
        sb.AppendLine("    System[Linux OS] --> SSSD{SSSD Daemon}");
        sb.AppendLine($"    SSSD -->|Provider| Backend[({idProvider.ToUpperInvariant()})]");

        // Auth provider if different
        if (!string.IsNullOrEmpty(authProvider) && !authProvider.Equals(idProvider, StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine($"    SSSD -->|Auth| AuthBackend[({authProvider.ToUpperInvariant()})]");
        }

        // Credential cache
        if (string.Equals(cacheCredentials, "true", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("    SSSD -->|Cache| CredCache[(Credential Cache)]");
        }
        else
        {
            sb.AppendLine("    SSSD -.->|No Cache| CredCache[(Credential Cache)]");
            sb.AppendLine("    style CredCache fill:#ff6b6b,stroke:#c92a2a");
        }

        // Enumeration warning
        if (string.Equals(enumerate, "true", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("    style Backend fill:#ffd43b,stroke:#fab005");
        }

        return sb.ToString();
    }

    private static string GenerateOpenSshDiagram(StringBuilder sb, List<HumanizedRule> rules)
    {
        var port = FindRuleValue(rules, "Port") ?? "22";
        var permitRootLogin = FindRuleValue(rules, "PermitRootLogin");
        var passwordAuth = FindRuleValue(rules, "PasswordAuthentication");
        var pubkeyAuth = FindRuleValue(rules, "PubkeyAuthentication");

        // User to Server connection
        sb.AppendLine($"    User((User)) -->|SSH:{port}| Server[SSH Server]");

        // Authentication methods
        if (string.Equals(pubkeyAuth, "yes", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("    User -->|Key Auth| Server");
        }

        if (string.Equals(passwordAuth, "yes", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("    User -->|Password| Server");
            sb.AppendLine("    style Server fill:#ffd43b,stroke:#fab005");
        }

        // Root login warning
        if (string.Equals(permitRootLogin, "yes", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("    Root((Root)) -->|Direct Login| Server");
            sb.AppendLine("    linkStyle 0 stroke:red,stroke-width:3px");
            sb.AppendLine("    style Root fill:#ff6b6b,stroke:#c92a2a");
        }

        // Server to system
        sb.AppendLine("    Server --> System[System Shell]");

        return sb.ToString();
    }

    private static string GenerateSystemdDiagram(StringBuilder sb, List<HumanizedRule> rules)
    {
        var user = FindRuleValue(rules, "User");
        var execStart = FindRuleValue(rules, "ExecStart");
        var restart = FindRuleValue(rules, "Restart");
        var privateTmp = FindRuleValue(rules, "PrivateTmp");
        var protectSystem = FindRuleValue(rules, "ProtectSystem");

        // Service node
        sb.AppendLine("    Service[Systemd Service]");

        // Dependencies (After, Requires, Wants)
        var afterTargets = rules.Where(r => string.Equals(r.Key, "After", StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Value).ToList();
        var requiresTargets = rules.Where(r => string.Equals(r.Key, "Requires", StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Value).ToList();

        foreach (var target in afterTargets)
        {
            var sanitized = SanitizeMermaidId(target);
            sb.AppendLine($"    {sanitized}(({target})) -->|After| Service");
        }

        foreach (var target in requiresTargets)
        {
            var sanitized = SanitizeMermaidId(target);
            sb.AppendLine($"    {sanitized}(({target})) ==>|Requires| Service");
        }

        // User running the service
        if (!string.IsNullOrEmpty(user))
        {
            sb.AppendLine($"    User((User: {user})) -.-> Service");
            if (string.Equals(user, "root", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("    style User fill:#ff6b6b,stroke:#c92a2a");
            }
            else
            {
                sb.AppendLine("    style User fill:#69db7c,stroke:#37b24d");
            }
        }

        // Execution
        if (!string.IsNullOrEmpty(execStart))
        {
            var shortExec = execStart.Length > 30 ? execStart[..27] + "..." : execStart;
            sb.AppendLine($"    Service -->|ExecStart| Process[{shortExec}]");
        }

        // Security features
        var securityFeatures = new List<string>();
        if (string.Equals(privateTmp, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(privateTmp, "yes", StringComparison.OrdinalIgnoreCase))
        {
            securityFeatures.Add("PrivateTmp");
        }
        if (!string.IsNullOrEmpty(protectSystem) &&
            !string.Equals(protectSystem, "false", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(protectSystem, "no", StringComparison.OrdinalIgnoreCase))
        {
            securityFeatures.Add("ProtectSystem");
        }

        if (securityFeatures.Count > 0)
        {
            sb.AppendLine($"    Security{{Security: {string.Join(", ", securityFeatures)}}} --> Service");
            sb.AppendLine("    style Security fill:#69db7c,stroke:#37b24d");
        }

        // Restart policy
        if (!string.IsNullOrEmpty(restart))
        {
            sb.AppendLine($"    Service -->|Restart: {restart}| Service");
        }

        return sb.ToString();
    }

    private static string GenerateSysctlDiagram(StringBuilder sb, List<HumanizedRule> rules)
    {
        // Kernel node
        sb.AppendLine("    Kernel[Linux Kernel]");

        // Track which subsystems we've added
        var hasNetwork = false;
        var hasFilesystem = false;
        var hasKernel = false;

        foreach (var rule in rules)
        {
            if (rule.Key.StartsWith("net.", StringComparison.OrdinalIgnoreCase) && !hasNetwork)
            {
                sb.AppendLine("    Network((Network Stack)) --> Kernel");
                hasNetwork = true;
            }
            else if (rule.Key.StartsWith("fs.", StringComparison.OrdinalIgnoreCase) && !hasFilesystem)
            {
                sb.AppendLine("    FS[(Filesystem)] --> Kernel");
                hasFilesystem = true;
            }
            else if (rule.Key.StartsWith("kernel.", StringComparison.OrdinalIgnoreCase) && !hasKernel)
            {
                sb.AppendLine("    KernelParams{{Kernel Parameters}} --> Kernel");
                hasKernel = true;
            }
        }

        // Highlight specific security settings
        var ipForward = FindRuleValue(rules, "net.ipv4.ip_forward");
        var sysrq = FindRuleValue(rules, "kernel.sysrq");
        var aslr = FindRuleValue(rules, "kernel.randomize_va_space");

        if (string.Equals(ipForward, "1", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("    Kernel -->|IP Forward: ON| Router((Router Mode))");
            sb.AppendLine("    style Router fill:#ffd43b,stroke:#fab005");
        }

        if (string.Equals(sysrq, "1", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("    Kernel -->|SysRq: ON| Console[Physical Console]");
            sb.AppendLine("    style Console fill:#ff6b6b,stroke:#c92a2a");
        }

        if (!string.IsNullOrEmpty(aslr) && !string.Equals(aslr, "2", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("    Kernel -->|ASLR Weak| Memory[Memory Protection]");
            sb.AppendLine("    style Memory fill:#ff6b6b,stroke:#c92a2a");
        }
        else if (string.Equals(aslr, "2", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("    Kernel -->|ASLR Full| Memory[Memory Protection]");
            sb.AppendLine("    style Memory fill:#69db7c,stroke:#37b24d");
        }

        return sb.ToString();
    }

    private static string GenerateCrontabDiagram(StringBuilder sb, List<HumanizedRule> rules)
    {
        sb.AppendLine("    Scheduler((Cron Scheduler))");

        var taskIndex = 0;
        foreach (var rule in rules)
        {
            taskIndex++;
            var taskId = $"Task{taskIndex}";
            var command = rule.Key.Length > 25 ? rule.Key[..22] + "..." : rule.Key;
            var schedule = rule.Value;

            // Sanitize for Mermaid
            command = command.Replace("\"", "'").Replace("|", " ");

            sb.AppendLine($"    Scheduler -->|\"{schedule}\"| {taskId}[\"{command}\"]");

            // Style based on content
            if (rule.Key.Contains("backup", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine($"    style {taskId} fill:#69db7c,stroke:#37b24d");
            }
            else if (rule.Severity == Severity.CriticalSecurity)
            {
                sb.AppendLine($"    style {taskId} fill:#ff6b6b,stroke:#c92a2a");
            }
            else if (rule.Key.Contains("curl", StringComparison.OrdinalIgnoreCase) ||
                     rule.Key.Contains("wget", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine($"    style {taskId} fill:#ffd43b,stroke:#fab005");
            }
        }

        return sb.ToString();
    }

    private static string GenerateFstabDiagram(StringBuilder sb, List<HumanizedRule> rules)
    {
        sb.AppendLine("    Disk[(Disk Storage)]");
        sb.AppendLine("    System[Linux OS]");

        var mountIndex = 0;
        foreach (var rule in rules)
        {
            mountIndex++;
            var mountId = $"Mount{mountIndex}";
            var mountPoint = rule.Key;
            var options = rule.Value.Length > 20 ? rule.Value[..17] + "..." : rule.Value;

            sb.AppendLine($"    Disk -->|Mount: {mountPoint}| {mountId}[{mountPoint}]");
            sb.AppendLine($"    {mountId} --> System");

            // Style based on security
            if (rule.Severity == Severity.Warning)
            {
                sb.AppendLine($"    style {mountId} fill:#ffd43b,stroke:#fab005");
            }
            else if (rule.Severity == Severity.GoodPractice)
            {
                sb.AppendLine($"    style {mountId} fill:#69db7c,stroke:#37b24d");
            }
        }

        return sb.ToString();
    }

    private static string GenerateHostsDiagram(StringBuilder sb, List<HumanizedRule> rules)
    {
        sb.AppendLine("    Computer[This PC]");

        var hostIndex = 0;
        foreach (var rule in rules)
        {
            hostIndex++;
            var hostId = $"Host{hostIndex}";
            var hostname = rule.Key.Length > 20 ? rule.Key[..17] + "..." : rule.Key;
            var ip = rule.Value;

            sb.AppendLine($"    Computer -->|Resolves| {hostId}((\"{hostname}\"))");
            sb.AppendLine($"    {hostId} -.-> IP{hostIndex}[\"{ip}\"]");

            // Style loopback differently
            if (ip.StartsWith("127.") || ip == "::1")
            {
                sb.AppendLine($"    style {hostId} fill:#74c0fc,stroke:#339af0");
            }
            else if (ip == "0.0.0.0")
            {
                sb.AppendLine($"    style {hostId} fill:#ff6b6b,stroke:#c92a2a");
            }
        }

        return sb.ToString();
    }

    private static string GenerateResolvDiagram(StringBuilder sb, List<HumanizedRule> rules)
    {
        sb.AppendLine("    System[Linux OS]");
        sb.AppendLine("    Resolver{{DNS Resolver}}");
        sb.AppendLine("    System --> Resolver");

        var nsIndex = 0;
        foreach (var rule in rules)
        {
            if (string.Equals(rule.Key, "nameserver", StringComparison.OrdinalIgnoreCase))
            {
                nsIndex++;
                var nsId = $"NS{nsIndex}";
                var server = rule.Value;

                sb.AppendLine($"    Resolver -->|Query| {nsId}(({server}))");

                // Style based on DNS type
                if (server.StartsWith("8.8.") || server.StartsWith("8.8."))
                {
                    sb.AppendLine($"    style {nsId} fill:#74c0fc,stroke:#339af0");
                }
                else if (server.StartsWith("1.1.") || server.StartsWith("1.0."))
                {
                    sb.AppendLine($"    style {nsId} fill:#ffd43b,stroke:#fab005");
                }
                else if (server == "9.9.9.9")
                {
                    sb.AppendLine($"    style {nsId} fill:#69db7c,stroke:#37b24d");
                }
            }
            else if (string.Equals(rule.Key, "search", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(rule.Key, "domain", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine($"    Resolver -.->|{rule.Key}| Domain[{rule.Value}]");
            }
        }

        return sb.ToString();
    }

    private static string GenerateNginxDiagram(StringBuilder sb, List<HumanizedRule> rules)
    {
        // Internet/Client entry point
        sb.AppendLine("    Internet((Internet)) -->|HTTP/HTTPS| Nginx[Nginx Server]");

        // Check for listen ports
        var listenPorts = rules.Where(r => r.Key.Contains("listen", StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Value).ToList();

        if (listenPorts.Any(p => p.Contains("443") || p.Contains("ssl")))
        {
            sb.AppendLine("    Nginx -->|TLS| SSL{{SSL/TLS}}");
            sb.AppendLine("    style SSL fill:#69db7c,stroke:#37b24d");
        }

        // Check for proxy_pass (reverse proxy)
        var proxyTargets = rules.Where(r => r.Key.Contains("proxy_pass", StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Value).ToList();

        var backendIndex = 0;
        foreach (var target in proxyTargets.Distinct())
        {
            backendIndex++;
            var backendId = $"Backend{backendIndex}";
            var shortTarget = target.Length > 30 ? target[..27] + "..." : target;
            sb.AppendLine($"    Nginx -->|Proxy| {backendId}[{shortTarget}]");
        }

        // Check for upstream (load balancing)
        var upstreams = rules.Where(r => r.Key.Contains("upstream", StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Value).ToList();

        if (upstreams.Count > 0)
        {
            sb.AppendLine("    Nginx -->|Load Balance| LB{{Upstream Pool}}");
            sb.AppendLine("    style LB fill:#74c0fc,stroke:#339af0");
        }

        // Check for static file serving (root directive)
        var roots = rules.Where(r => r.Key.Contains("root", StringComparison.OrdinalIgnoreCase) && !r.Key.Contains("proxy"))
            .Select(r => r.Value).FirstOrDefault();

        if (!string.IsNullOrEmpty(roots))
        {
            sb.AppendLine("    Nginx -->|Static Files| Static[(Static Content)]");
        }

        // Security warnings
        var serverTokens = FindRuleValue(rules, "server_tokens");
        if (string.Equals(serverTokens, "on", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("    style Nginx fill:#ffd43b,stroke:#fab005");
        }

        var sslProtocols = rules.FirstOrDefault(r => r.Key.Contains("ssl_protocols"))?.Value ?? "";
        if (sslProtocols.Contains("TLSv1.0") || sslProtocols.Contains("SSLv"))
        {
            sb.AppendLine("    SSL -.->|Weak Protocol| Warning[Insecure!]");
            sb.AppendLine("    style Warning fill:#ff6b6b,stroke:#c92a2a");
        }

        var autoindex = FindRuleValue(rules, "autoindex");
        if (string.Equals(autoindex, "on", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("    Nginx -->|Directory Listing| DirList[Directory Exposed]");
            sb.AppendLine("    style DirList fill:#ff6b6b,stroke:#c92a2a");
        }

        return sb.ToString();
    }

    private static string GenerateDockerComposeDiagram(StringBuilder sb, List<HumanizedRule> rules)
    {
        sb.AppendLine("    Docker{{Docker Compose}}");

        // Extract service names from keys like "services.web.image"
        var serviceNames = rules
            .Where(r => r.Key.StartsWith("services.", StringComparison.OrdinalIgnoreCase))
            .Select(r =>
            {
                var parts = r.Key.Split('.');
                return parts.Length >= 2 ? parts[1] : null;
            })
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct()
            .ToList();

        var serviceStyles = new Dictionary<string, string>();

        foreach (var serviceName in serviceNames)
        {
            var serviceId = SanitizeMermaidId(serviceName!);
            sb.AppendLine($"    Docker --> {serviceId}[{serviceName}]");
            sb.AppendLine($"    click {serviceId} call onNodeClick(\"{serviceName}:\")");

            // Check image for database styling
            var imageRule = rules.FirstOrDefault(r =>
                r.Key.Equals($"services.{serviceName}.image", StringComparison.OrdinalIgnoreCase));

            if (imageRule != null)
            {
                var image = imageRule.Value.ToLowerInvariant();
                if (image.Contains("postgres") || image.Contains("mysql") ||
                    image.Contains("mariadb") || image.Contains("mongo") ||
                    image.Contains("redis") || image.Contains("elasticsearch"))
                {
                    serviceStyles[serviceId] = "fill:#74c0fc,stroke:#339af0";
                }

                // Check for :latest tag
                if (image.EndsWith(":latest"))
                {
                    serviceStyles[serviceId] = "fill:#ffd43b,stroke:#fab005";
                }
            }

            // Check for privileged mode
            var privilegedRule = rules.FirstOrDefault(r =>
                r.Key.Contains($"services.{serviceName}") && r.Key.Contains("privileged"));

            if (privilegedRule != null && string.Equals(privilegedRule.Value, "true", StringComparison.OrdinalIgnoreCase))
            {
                serviceStyles[serviceId] = "fill:#ff6b6b,stroke:#c92a2a";
            }

            // Check for depends_on
            var dependsOnRules = rules.Where(r =>
                r.Key.StartsWith($"services.{serviceName}.depends_on", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var dep in dependsOnRules)
            {
                var depService = dep.Value;
                if (!string.IsNullOrEmpty(depService))
                {
                    var depId = SanitizeMermaidId(depService);
                    sb.AppendLine($"    {serviceId} -->|depends_on| {depId}");
                }
            }

            // Check for ports
            var portRules = rules.Where(r =>
                r.Key.StartsWith($"services.{serviceName}.ports", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var portRule in portRules)
            {
                if (portRule.Value.StartsWith("0.0.0.0") || !portRule.Value.Contains("127.0.0.1"))
                {
                    var portId = $"{serviceId}Port";
                    var shortPort = portRule.Value.Length > 15 ? portRule.Value[..12] + "..." : portRule.Value;
                    sb.AppendLine($"    Internet((Internet)) -->|\"{shortPort}\"| {serviceId}");
                }
            }

            // Check for Docker socket mount (security issue)
            var volumeRules = rules.Where(r =>
                r.Key.StartsWith($"services.{serviceName}.volumes", StringComparison.OrdinalIgnoreCase) &&
                r.Value.Contains("docker.sock"))
                .ToList();

            if (volumeRules.Count > 0)
            {
                serviceStyles[serviceId] = "fill:#ff6b6b,stroke:#c92a2a";
                sb.AppendLine($"    {serviceId} -->|Docker Socket| DockerDaemon((Docker Daemon))");
            }
        }

        // Apply styles
        foreach (var style in serviceStyles)
        {
            sb.AppendLine($"    style {style.Key} {style.Value}");
        }

        // If no services found, show generic view
        if (serviceNames.Count == 0)
        {
            return GenerateGenericDiagram(sb, rules, "DockerCompose");
        }

        return sb.ToString();
    }

    private static string GenerateHAProxyDiagram(StringBuilder sb, List<HumanizedRule> rules)
    {
        sb.AppendLine("    Internet((Internet)) -->|HTTP/HTTPS| HAProxy[HAProxy LB]");

        // Check for backends/servers
        var servers = rules.Where(r => r.Key.Contains("server", StringComparison.OrdinalIgnoreCase) &&
            !r.Key.Contains("timeout"))
            .ToList();

        var backendIndex = 0;
        foreach (var server in servers.Take(5)) // Limit to 5 for readability
        {
            backendIndex++;
            var backendId = $"Backend{backendIndex}";
            var serverInfo = server.Value.Length > 20 ? server.Value[..17] + "..." : server.Value;
            sb.AppendLine($"    HAProxy -->|Balance| {backendId}[{serverInfo}]");

            if (server.Value.Contains("check"))
            {
                sb.AppendLine($"    style {backendId} fill:#69db7c,stroke:#37b24d");
            }
        }

        // Check for stats
        var stats = rules.Any(r => r.Key.Contains("stats", StringComparison.OrdinalIgnoreCase) &&
            r.Key.Contains("enable"));
        if (stats)
        {
            sb.AppendLine("    HAProxy -->|Stats| Dashboard[Stats Page]");
            sb.AppendLine("    style Dashboard fill:#74c0fc,stroke:#339af0");
        }

        // Check for SSL
        var ssl = rules.Any(r => r.Key.Contains("ssl", StringComparison.OrdinalIgnoreCase));
        if (ssl)
        {
            sb.AppendLine("    HAProxy -->|TLS| SSL{{SSL/TLS}}");
            sb.AppendLine("    style SSL fill:#69db7c,stroke:#37b24d");
        }

        // Balance algorithm
        var balance = FindRuleValue(rules, "balance");
        if (!string.IsNullOrEmpty(balance))
        {
            sb.AppendLine($"    Balance{{Algorithm: {balance}}} -.-> HAProxy");
        }

        return sb.ToString();
    }

    private static string GenerateDatabaseDiagram(StringBuilder sb, List<HumanizedRule> rules, string dbType)
    {
        var dbName = dbType.ToUpperInvariant();
        sb.AppendLine($"    App((Application)) --> DB[({dbName})]");

        // Check bind address
        var bindAddress = FindRuleValue(rules, "bind-address") ??
                         FindRuleValue(rules, "bind_address") ??
                         FindRuleValue(rules, "listen_addresses") ??
                         FindRuleValue(rules, "bind") ??
                         FindRuleValue(rules, "bindIp");

        if (!string.IsNullOrEmpty(bindAddress))
        {
            if (bindAddress == "127.0.0.1" || bindAddress.Contains("localhost"))
            {
                sb.AppendLine("    DB -->|Local Only| Localhost((localhost))");
                sb.AppendLine("    style Localhost fill:#69db7c,stroke:#37b24d");
            }
            else if (bindAddress == "0.0.0.0" || bindAddress == "*")
            {
                sb.AppendLine("    Internet((Internet)) -->|Open!| DB");
                sb.AppendLine("    style Internet fill:#ff6b6b,stroke:#c92a2a");
            }
        }

        // Check SSL/TLS
        var ssl = rules.Any(r => r.Key.Contains("ssl", StringComparison.OrdinalIgnoreCase) ||
                                  r.Key.Contains("tls", StringComparison.OrdinalIgnoreCase));
        if (ssl)
        {
            sb.AppendLine("    DB -->|Encrypted| TLS{{TLS}}");
            sb.AppendLine("    style TLS fill:#69db7c,stroke:#37b24d");
        }

        // Check authentication
        var authEnabled = rules.Any(r =>
            (r.Key.Contains("auth", StringComparison.OrdinalIgnoreCase) &&
             (r.Value.Contains("enabled", StringComparison.OrdinalIgnoreCase) ||
              r.Value.Equals("on", StringComparison.OrdinalIgnoreCase))) ||
            r.Key.Contains("requirepass"));

        if (authEnabled)
        {
            sb.AppendLine("    Auth{{Auth Required}} --> DB");
            sb.AppendLine("    style Auth fill:#69db7c,stroke:#37b24d");
        }

        // Storage
        sb.AppendLine("    DB --> Storage[(Data Storage)]");

        return sb.ToString();
    }

    private static string GenerateFail2banDiagram(StringBuilder sb, List<HumanizedRule> rules)
    {
        sb.AppendLine("    Attacker((Attacker)) -->|Failed Auth| Services[Protected Services]");
        sb.AppendLine("    Services --> Fail2ban{{Fail2ban}}");
        sb.AppendLine("    Fail2ban -->|Monitor| Logs[(Log Files)]");

        // Check for enabled jails
        var enabledJails = rules.Where(r => r.Key.Contains("enabled") &&
            (r.Value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
             r.Value.Equals("yes", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (enabledJails.Count > 0)
        {
            sb.AppendLine($"    Jails[{enabledJails.Count} Jails Active] --> Fail2ban");
            sb.AppendLine("    style Jails fill:#69db7c,stroke:#37b24d");
        }

        // Ban action
        sb.AppendLine("    Fail2ban -->|Ban| Firewall[Firewall]");
        sb.AppendLine("    Firewall -->|Block| Attacker");
        sb.AppendLine("    style Attacker fill:#ff6b6b,stroke:#c92a2a");

        // Check bantime
        var bantime = FindRuleValue(rules, "bantime");
        if (!string.IsNullOrEmpty(bantime))
        {
            sb.AppendLine($"    BanTime{{Ban: {bantime}}} -.-> Fail2ban");
        }

        return sb.ToString();
    }

    private static string GenerateFirewallDiagram(StringBuilder sb, List<HumanizedRule> rules)
    {
        sb.AppendLine("    Internet((Internet)) --> Firewall{{Firewall}}");

        // Check INPUT policy
        var inputDrop = rules.Any(r => r.Key.Contains("INPUT") &&
            (r.Key.Contains("DROP") || r.Key.Contains("REJECT")));

        if (inputDrop)
        {
            sb.AppendLine("    Firewall -->|Default: DROP| System[System]");
            sb.AppendLine("    style Firewall fill:#69db7c,stroke:#37b24d");
        }
        else
        {
            sb.AppendLine("    Firewall -->|Default: ACCEPT| System[System]");
            sb.AppendLine("    style Firewall fill:#ffd43b,stroke:#fab005");
        }

        // Check for SSH access
        var sshAllowed = rules.Any(r => r.Key.Contains("22") && r.Key.Contains("ACCEPT"));
        if (sshAllowed)
        {
            sb.AppendLine("    Firewall -->|:22| SSH[SSH]");
        }

        // Check for HTTP/HTTPS
        var httpAllowed = rules.Any(r => (r.Key.Contains("80") || r.Key.Contains("443")) &&
            r.Key.Contains("ACCEPT"));
        if (httpAllowed)
        {
            sb.AppendLine("    Firewall -->|:80/443| Web[Web Server]");
        }

        // Check for logging
        var logging = rules.Any(r => r.Key.Contains("LOG"));
        if (logging)
        {
            sb.AppendLine("    Firewall -->|Log| Logs[(Logs)]");
        }

        return sb.ToString();
    }

    private static string GeneratePostfixDiagram(StringBuilder sb, List<HumanizedRule> rules)
    {
        sb.AppendLine("    Sender((Sender)) -->|SMTP| Postfix[Postfix MTA]");

        // Check interfaces
        var interfaces = FindRuleValue(rules, "inet_interfaces");
        if (!string.IsNullOrEmpty(interfaces))
        {
            if (interfaces.Contains("loopback") || interfaces.Contains("localhost"))
            {
                sb.AppendLine("    Postfix -->|Local Only| Local[Local Delivery]");
            }
            else if (interfaces == "all")
            {
                sb.AppendLine("    Internet((Internet)) -->|Open| Postfix");
                sb.AppendLine("    style Internet fill:#ffd43b,stroke:#fab005");
            }
        }

        // Check TLS
        var tls = rules.Any(r => r.Key.Contains("tls", StringComparison.OrdinalIgnoreCase) &&
            (r.Value.Contains("yes", StringComparison.OrdinalIgnoreCase) ||
             r.Value.Contains("may", StringComparison.OrdinalIgnoreCase) ||
             r.Value.Contains("encrypt", StringComparison.OrdinalIgnoreCase)));

        if (tls)
        {
            sb.AppendLine("    TLS{{TLS Enabled}} --> Postfix");
            sb.AppendLine("    style TLS fill:#69db7c,stroke:#37b24d");
        }

        // Check relay restrictions
        var openRelay = rules.Any(r => r.Key.Contains("relay") &&
            r.Value.Contains("permit", StringComparison.OrdinalIgnoreCase) &&
            !r.Value.Contains("reject", StringComparison.OrdinalIgnoreCase));

        if (openRelay)
        {
            sb.AppendLine("    style Postfix fill:#ff6b6b,stroke:#c92a2a");
            sb.AppendLine("    Warning[OPEN RELAY!] -.-> Postfix");
        }

        // Outgoing mail
        sb.AppendLine("    Postfix -->|Deliver| Recipient((Recipient))");
        sb.AppendLine("    Postfix -->|Queue| Queue[(Mail Queue)]");

        return sb.ToString();
    }

    private static string GenerateTerraformDiagram(StringBuilder sb, List<HumanizedRule> rules)
    {
        sb.AppendLine("    TF{{Terraform}}");

        // Extract modules
        var modules = rules.Where(r => r.Key.Contains("module.definition"))
            .Select(r => r.Value)
            .Distinct()
            .ToList();

        // Extract EPGs/security groups
        var epgs = rules.Where(r => r.Key.Contains("vm.securityGroup") || r.Key.Contains("contract.epg"))
            .Select(r => r.Value)
            .Distinct()
            .ToList();

        // Extract VMs by OS type
        var windowsVms = rules.Count(r => r.Key.Contains("vm.os") && r.Value.ToLowerInvariant().Contains("windows"));
        var linuxVms = rules.Count(r => r.Key.Contains("vm.os") &&
            (r.Value.ToLowerInvariant().Contains("rhel") || r.Value.ToLowerInvariant().Contains("centos")));

        // Draw modules
        foreach (var module in modules.Take(6))
        {
            var moduleId = SanitizeMermaidId(module);
            sb.AppendLine($"    TF --> {moduleId}[Module: {module}]");
            sb.AppendLine($"    style {moduleId} fill:#74c0fc,stroke:#339af0");
        }

        // Draw ACI tenant structure if present
        var hasTenant = rules.Any(r => r.Key.Contains("aci.fabric"));
        if (hasTenant)
        {
            sb.AppendLine("    TF --> ACI{{Cisco ACI Fabric}}");
            sb.AppendLine("    style ACI fill:#00bceb,stroke:#005073");
        }

        // Draw EPG groups
        var epgIndex = 0;
        foreach (var epg in epgs.Distinct().Take(6))
        {
            epgIndex++;
            var epgId = $"EPG{epgIndex}";
            var shortName = epg.Length > 20 ? epg[..17] + "..." : epg;

            if (hasTenant)
            {
                sb.AppendLine($"    ACI --> {epgId}[/{shortName}/]");
            }
            else
            {
                sb.AppendLine($"    TF --> {epgId}[/{shortName}/]");
            }

            // Color based on EPG type
            if (epg.Contains("_GW"))
            {
                sb.AppendLine($"    style {epgId} fill:#ffd43b,stroke:#fab005");
            }
            else if (epg.Contains("_SECU"))
            {
                sb.AppendLine($"    style {epgId} fill:#69db7c,stroke:#37b24d");
            }
            else if (epg.Contains("_AD") || epg.Contains("_LDAP"))
            {
                sb.AppendLine($"    style {epgId} fill:#da77f2,stroke:#ae3ec9");
            }
        }

        // Draw VM counts
        if (windowsVms > 0)
        {
            sb.AppendLine($"    WinVMs[Windows VMs: {windowsVms}] --> TF");
            sb.AppendLine("    style WinVMs fill:#00adef,stroke:#0078d4");
        }

        if (linuxVms > 0)
        {
            sb.AppendLine($"    LinuxVMs[Linux VMs: {linuxVms}] --> TF");
            sb.AppendLine("    style LinuxVMs fill:#e95420,stroke:#c34113");
        }

        // Check for Palo Alto firewall rules
        var hasPaloAlto = rules.Any(r => r.Key.Contains("firewall.json_file") || r.Key.Contains("firewall.ports"));
        if (hasPaloAlto)
        {
            sb.AppendLine("    TF --> PaloAlto{{Palo Alto Firewall}}");
            sb.AppendLine("    style PaloAlto fill:#ff6b6b,stroke:#c92a2a");
        }

        // Check for cross-tenant contracts
        var hasCrossTenant = rules.Any(r => r.Key.Contains("contract.tenant"));
        if (hasCrossTenant)
        {
            sb.AppendLine("    TF -.->|Cross-Tenant| External((External Tenants))");
        }

        return sb.ToString();
    }

    private static string GeneratePaloAltoDiagram(StringBuilder sb, List<HumanizedRule> rules)
    {
        // Count rules by severity for summary
        var totalRules = rules.Count(r => r.Key.StartsWith("fw_rule."));
        var criticalRules = rules.Count(r => r.Key.StartsWith("fw_rule.") && r.Severity == Severity.CriticalSecurity);
        var warningRules = rules.Count(r => r.Key.StartsWith("fw_rule.") && r.Severity == Severity.Warning);
        var goodRules = rules.Count(r => r.Key.StartsWith("fw_rule.") && r.Severity == Severity.GoodPractice);

        // Simple summary diagram
        sb.AppendLine("    subgraph FW[\"🔥 Résumé des règles Palo Alto\"]");
        sb.AppendLine("    direction TB");
        sb.AppendLine($"    Total[\"📋 Total: {totalRules} règles\"]");

        if (criticalRules > 0)
        {
            sb.AppendLine($"    Critical[\"🔴 Critiques: {criticalRules}\"]");
            sb.AppendLine("    style Critical fill:#ffebee,stroke:#c62828");
        }
        if (warningRules > 0)
        {
            sb.AppendLine($"    Warning[\"⚠️ Attention: {warningRules}\"]");
            sb.AppendLine("    style Warning fill:#fff3e0,stroke:#f57c00");
        }
        if (goodRules > 0)
        {
            sb.AppendLine($"    Good[\"✅ Bonnes pratiques: {goodRules}\"]");
            sb.AppendLine("    style Good fill:#e8f5e9,stroke:#388e3c");
        }

        sb.AppendLine("    end");
        sb.AppendLine();

        // Flow diagram
        sb.AppendLine("    Internet((Internet)) --> FW");
        sb.AppendLine("    FW --> Internal((Réseau Interne))");

        return sb.ToString();
    }

    private static string GeneratePasswdDiagram(StringBuilder sb, List<HumanizedRule> rules)
    {
        // Count account types
        var rootAccounts = rules.Count(r => r.Value.Contains("UID:0 "));
        var serviceAccounts = rules.Count(r => r.Value.Contains("nologin") || r.Value.Contains("/bin/false"));
        var interactiveAccounts = rules.Count(r => r.Value.Contains("/bin/bash") || r.Value.Contains("/bin/zsh"));
        var ldapAccounts = rules.Count(r => r.Value.Contains("UID:") &&
            int.TryParse(r.Value.Split("UID:")[1].Split(' ')[0], out var uid) && uid >= 10000000);

        sb.AppendLine("    subgraph Users[\"👥 Comptes Utilisateurs\"]");
        sb.AppendLine("    direction TB");
        sb.AppendLine($"    Total[\"📋 Total: {rules.Count} comptes\"]");
        sb.AppendLine("    end");
        sb.AppendLine();

        // Root accounts
        if (rootAccounts > 0)
        {
            sb.AppendLine($"    Root[\"🔴 Root (UID 0): {rootAccounts}\"]");
            sb.AppendLine("    Users --> Root");
            sb.AppendLine("    style Root fill:#ffebee,stroke:#c62828");
        }

        // Interactive accounts
        if (interactiveAccounts > 0)
        {
            sb.AppendLine($"    Interactive[\"👤 Interactifs: {interactiveAccounts}\"]");
            sb.AppendLine("    Users --> Interactive");
            sb.AppendLine("    Interactive --> Shell[\"🖥️ Shell bash/zsh\"]");
            sb.AppendLine("    style Interactive fill:#e3f2fd,stroke:#1565c0");
        }

        // Service accounts
        if (serviceAccounts > 0)
        {
            sb.AppendLine($"    Service[\"⚙️ Services: {serviceAccounts}\"]");
            sb.AppendLine("    Users --> Service");
            sb.AppendLine("    Service --> NoLogin[\"🔒 nologin/false\"]");
            sb.AppendLine("    style Service fill:#e8f5e9,stroke:#2e7d32");
            sb.AppendLine("    style NoLogin fill:#e8f5e9,stroke:#2e7d32");
        }

        // LDAP/AD accounts
        if (ldapAccounts > 0)
        {
            sb.AppendLine($"    LDAP[\"🌐 LDAP/AD: {ldapAccounts}\"]");
            sb.AppendLine("    Users --> LDAP");
            sb.AppendLine("    LDAP --> Directory[(Annuaire)]");
            sb.AppendLine("    style LDAP fill:#fff3e0,stroke:#ef6c00");
        }

        // Security concerns
        var criticalCount = rules.Count(r => r.Severity == Severity.CriticalSecurity);
        var warningCount = rules.Count(r => r.Severity == Severity.Warning);

        if (criticalCount > 0)
        {
            sb.AppendLine($"    Security[\"⚠️ Problèmes critiques: {criticalCount}\"]");
            sb.AppendLine("    Users -.-> Security");
            sb.AppendLine("    style Security fill:#ffebee,stroke:#c62828");
        }

        if (warningCount > 0)
        {
            sb.AppendLine($"    Warnings[\"⚡ Avertissements: {warningCount}\"]");
            sb.AppendLine("    Users -.-> Warnings");
            sb.AppendLine("    style Warnings fill:#fff3e0,stroke:#f57c00");
        }

        return sb.ToString();
    }

    private static string GenerateGroupDiagram(StringBuilder sb, List<HumanizedRule> rules)
    {
        // Count group types
        var systemGroups = rules.Count(r => r.Value.Contains("GID:") &&
            int.TryParse(r.Value.Split("GID:")[1].Split(' ')[0], out var gid) && gid < 1000);
        var userGroups = rules.Count(r => r.Value.Contains("GID:") &&
            int.TryParse(r.Value.Split("GID:")[1].Split(' ')[0], out var gid) && gid >= 1000 && gid < 10000000);
        var ldapGroups = rules.Count(r => r.Value.Contains("GID:") &&
            int.TryParse(r.Value.Split("GID:")[1].Split(' ')[0], out var gid) && gid >= 10000000);

        sb.AppendLine("    subgraph Groups[\"👥 Groupes du Système\"]");
        sb.AppendLine("    direction TB");
        sb.AppendLine($"    Total[\"📋 Total: {rules.Count} groupes\"]");
        sb.AppendLine("    end");
        sb.AppendLine();

        // Privileged groups
        var wheelGroup = rules.FirstOrDefault(r => r.Key == "wheel" || r.Key == "sudo");
        var rootGroup = rules.FirstOrDefault(r => r.Key == "root");

        if (rootGroup != null || wheelGroup != null)
        {
            sb.AppendLine("    subgraph Privileged[\"⚡ Groupes Privilégiés\"]");
            if (rootGroup != null)
            {
                sb.AppendLine("    RootGrp[\"🔴 root (GID 0)\"]");
            }
            if (wheelGroup != null)
            {
                var memberCount = wheelGroup.Value.Contains("Membres:") ?
                    wheelGroup.Value.Split("Membres:")[1].Trim() : "0";
                sb.AppendLine($"    Wheel[\"⚡ wheel/sudo ({memberCount})\"]");
            }
            sb.AppendLine("    end");
            sb.AppendLine("    Groups --> Privileged");
            sb.AppendLine("    style Privileged fill:#ffebee,stroke:#c62828");
        }

        // System groups
        if (systemGroups > 0)
        {
            sb.AppendLine($"    System[\"⚙️ Système: {systemGroups}\"]");
            sb.AppendLine("    Groups --> System");
            sb.AppendLine("    style System fill:#e8f5e9,stroke:#2e7d32");
        }

        // User groups
        if (userGroups > 0)
        {
            sb.AppendLine($"    UserGrps[\"👤 Utilisateurs: {userGroups}\"]");
            sb.AppendLine("    Groups --> UserGrps");
            sb.AppendLine("    style UserGrps fill:#e3f2fd,stroke:#1565c0");
        }

        // LDAP groups
        if (ldapGroups > 0)
        {
            sb.AppendLine($"    LDAPGrps[\"🌐 LDAP/AD: {ldapGroups}\"]");
            sb.AppendLine("    Groups --> LDAPGrps");
            sb.AppendLine("    LDAPGrps --> Directory[(Annuaire)]");
            sb.AppendLine("    style LDAPGrps fill:#fff3e0,stroke:#ef6c00");
        }

        return sb.ToString();
    }

    private static string GenerateSudoersDiagram(StringBuilder sb, List<HumanizedRule> rules)
    {
        sb.AppendLine("    subgraph Sudo[\"⚡ Configuration Sudoers\"]");
        sb.AppendLine("    direction TB");
        sb.AppendLine("    Root[\"🔴 root ALL=(ALL) ALL\"]");
        sb.AppendLine("    end");
        sb.AppendLine();

        // Check for NOPASSWD rules
        var nopasswdRules = rules.Where(r => r.Value.Contains("NOPASSWD") ||
            r.RawLine.Contains("NOPASSWD")).ToList();

        if (nopasswdRules.Count > 0)
        {
            sb.AppendLine("    subgraph NoPass[\"⚠️ Sans mot de passe\"]");
            var idx = 0;
            foreach (var rule in nopasswdRules.Take(5))
            {
                idx++;
                var user = rule.Key.Length > 20 ? rule.Key[..17] + "..." : rule.Key;
                sb.AppendLine($"    NP{idx}[\"{user}\"]");
            }
            sb.AppendLine("    end");
            sb.AppendLine("    Sudo --> NoPass");
            sb.AppendLine("    style NoPass fill:#ffebee,stroke:#c62828");
        }

        // Check for wheel group
        var wheelRule = rules.FirstOrDefault(r => r.Key.Contains("wheel") || r.Key.Contains("%wheel"));
        if (wheelRule != null)
        {
            sb.AppendLine("    Wheel[\"👥 %wheel ALL=(ALL) ALL\"]");
            sb.AppendLine("    Sudo --> Wheel");
            sb.AppendLine("    style Wheel fill:#fff3e0,stroke:#f57c00");
        }

        // Security settings
        var visiblepw = rules.FirstOrDefault(r => r.Key.Contains("visiblepw"));
        var envReset = rules.FirstOrDefault(r => r.Key.Contains("env_reset"));

        sb.AppendLine("    subgraph Security[\"🔒 Sécurité\"]");
        if (visiblepw != null && visiblepw.Value.Contains("!"))
        {
            sb.AppendLine("    VP[\"✅ !visiblepw\"]");
        }
        if (envReset != null)
        {
            sb.AppendLine("    ER[\"✅ env_reset\"]");
        }
        sb.AppendLine("    end");
        sb.AppendLine("    style Security fill:#e8f5e9,stroke:#2e7d32");

        return sb.ToString();
    }

    private static string GenerateRsyslogDiagram(StringBuilder sb, List<HumanizedRule> rules)
    {
        sb.AppendLine("    subgraph Rsyslog[\"📝 Configuration Rsyslog\"]");
        sb.AppendLine("    direction TB");
        sb.AppendLine("    end");
        sb.AppendLine();

        sb.AppendLine("    Sources((Sources)) --> Rsyslog");

        // Check for remote logging
        var remoteRules = rules.Where(r => r.RawLine.Contains("@@") || r.RawLine.Contains("@")).ToList();
        if (remoteRules.Count > 0)
        {
            sb.AppendLine("    Rsyslog --> Remote[\"🌐 Serveur distant\"]");
            sb.AppendLine("    style Remote fill:#e3f2fd,stroke:#1565c0");
        }

        // Check for local files
        var fileRules = rules.Where(r => r.RawLine.Contains("/var/log/")).ToList();
        if (fileRules.Count > 0)
        {
            sb.AppendLine($"    Rsyslog --> Local[\"📁 Fichiers locaux ({fileRules.Count})\"]");
            sb.AppendLine("    style Local fill:#e8f5e9,stroke:#2e7d32");
        }

        // Common log destinations
        var authLog = rules.Any(r => r.RawLine.Contains("auth") || r.RawLine.Contains("secure"));
        var kernLog = rules.Any(r => r.RawLine.Contains("kern"));
        var mailLog = rules.Any(r => r.RawLine.Contains("mail"));

        if (authLog)
        {
            sb.AppendLine("    Local --> Auth[\"🔐 Auth/Secure\"]");
        }
        if (kernLog)
        {
            sb.AppendLine("    Local --> Kern[\"⚙️ Kernel\"]");
        }
        if (mailLog)
        {
            sb.AppendLine("    Local --> Mail[\"📧 Mail\"]");
        }

        return sb.ToString();
    }

    private static string SanitizeMermaidId(string input)
    {
        // Remove special characters that break Mermaid syntax
        return input.Replace(".", "_").Replace("-", "_").Replace(" ", "_");
    }

    private static string GenerateGenericDiagram(StringBuilder sb, List<HumanizedRule> rules, string? formatName)
    {
        var displayName = formatName ?? "Config";

        sb.AppendLine($"    Config[{displayName} Configuration]");

        // Group rules by severity
        var criticalCount = rules.Count(r => r.Severity == Severity.CriticalSecurity);
        var warningCount = rules.Count(r => r.Severity == Severity.Warning);
        var goodCount = rules.Count(r => r.Severity == Severity.GoodPractice);
        var infoCount = rules.Count(r => r.Severity == Severity.Info);

        if (criticalCount > 0)
        {
            sb.AppendLine($"    Config --> Critical[Critical: {criticalCount}]");
            sb.AppendLine("    style Critical fill:#ff6b6b,stroke:#c92a2a");
        }

        if (warningCount > 0)
        {
            sb.AppendLine($"    Config --> Warning[Warnings: {warningCount}]");
            sb.AppendLine("    style Warning fill:#ffd43b,stroke:#fab005");
        }

        if (goodCount > 0)
        {
            sb.AppendLine($"    Config --> Good[Good Practices: {goodCount}]");
            sb.AppendLine("    style Good fill:#69db7c,stroke:#37b24d");
        }

        if (infoCount > 0)
        {
            sb.AppendLine($"    Config --> Info[Info: {infoCount}]");
            sb.AppendLine("    style Info fill:#74c0fc,stroke:#339af0");
        }

        return sb.ToString();
    }

    private static string? FindRuleValue(List<HumanizedRule> rules, string key)
    {
        return rules.FirstOrDefault(r =>
            string.Equals(r.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;
    }
}
