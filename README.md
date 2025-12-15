# ConfigHumanizer

**A WPF application that analyzes configuration files and provides human-readable security recommendations with visual architecture diagrams.**

![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![WPF](https://img.shields.io/badge/WPF-Windows-0078D6?logo=windows)
![License](https://img.shields.io/badge/License-Apache%202.0-blue)

## Overview

ConfigHumanizer is a security-focused configuration analyzer for SysOps and DevOps professionals. It parses configuration files from 35+ formats, applies security rules, and generates:

- **Human-readable explanations** of each configuration setting
- **Security severity ratings** (Critical, Warning, Good Practice, Info)
- **Suggested fixes** with explanations
- **Mermaid.js architecture diagrams** visualizing the configuration

## Screenshots

*Drop a config file and instantly see security issues with visual diagrams*

## Supported Formats

### Security & Access
| Format | Files | Key Rules |
|--------|-------|-----------|
| **OpenSSH** | `sshd_config` | PermitRootLogin, PasswordAuthentication, Protocol |
| **SSH Client** | `ssh_config`, `~/.ssh/config` | StrictHostKeyChecking, ForwardAgent |
| **Sudoers** | `sudoers`, `sudoers.d/*` | NOPASSWD, env_reset, secure_path |
| **PAM** | `pam.d/*`, `system-auth` | nullok, pam_permit, 2FA modules |
| **Fail2ban** | `jail.conf`, `jail.local` | bantime, maxretry, enabled jails |
| **iptables/nftables** | `iptables.rules`, `nftables.conf` | INPUT policy, open ports, NAT |

### Proxy & Load Balancing
| Format | Files | Key Rules |
|--------|-------|-----------|
| **Nginx** | `nginx.conf` | server_tokens, ssl_protocols, proxy_pass |
| **Apache** | `httpd.conf`, `apache2.conf` | ServerTokens, SSL configuration |
| **HAProxy** | `haproxy.cfg` | stats exposure, SSL, health checks |
| **Traefik** | `traefik.yml` | api.insecure, TLS version, middlewares |
| **Envoy** | `envoy.yaml` | admin interface, circuit breakers |
| **Squid** | `squid.conf` | http_access, cache_dir |

### Databases
| Format | Files | Key Rules |
|--------|-------|-----------|
| **MySQL/MariaDB** | `my.cnf`, `my.ini` | bind-address, local-infile, SSL |
| **PostgreSQL** | `postgresql.conf`, `pg_hba.conf` | listen_addresses, trust auth, SSL |
| **Redis** | `redis.conf` | bind, protected-mode, requirepass |
| **MongoDB** | `mongod.conf` | bindIp, authorization, TLS |

### Containers & IaC
| Format | Files | Key Rules |
|--------|-------|-----------|
| **Docker Compose** | `docker-compose.yml` | privileged, :latest tag, docker.sock |
| **Dockerfile** | `Dockerfile` | USER root, secrets in ENV |
| **Kubernetes** | `*.yaml` (with apiVersion) | securityContext, privileged pods |
| **Terraform** | `*.tf`, `*.tfvars` | hardcoded secrets, state encryption |
| **Ansible** | `playbook.yml` | become, vault, mode 777 |

### CI/CD
| Format | Files | Key Rules |
|--------|-------|-----------|
| **GitLab CI** | `.gitlab-ci.yml` | :latest images, hardcoded secrets |
| **GitHub Actions** | `.github/workflows/*.yml` | SHA pinning, permissions, pull_request_target |

### System Configuration
| Format | Files | Key Rules |
|--------|-------|-----------|
| **Systemd** | `*.service`, `*.socket` | User, PrivateTmp, ProtectSystem |
| **Sysctl** | `sysctl.conf` | ip_forward, ASLR, sysrq |
| **Crontab** | `crontab`, `cron.d/*` | suspicious commands |
| **Fstab** | `fstab` | noexec, nosuid, encryption |
| **Hosts** | `hosts` | suspicious redirects |
| **Resolv.conf** | `resolv.conf` | DNS servers |

### Network Services
| Format | Files | Key Rules |
|--------|-------|-----------|
| **Postfix** | `main.cf`, `master.cf` | open relay, TLS, mynetworks |
| **Samba** | `smb.conf` | SMBv1, guest access, encryption |
| **NFS** | `exports` | no_root_squash, all_squash |
| **BIND** | `named.conf` | recursion, zone transfers |
| **DHCP** | `dhcpd.conf` | ranges, options |

### Monitoring & Logging
| Format | Files | Key Rules |
|--------|-------|-----------|
| **Prometheus** | `prometheus.yml` | insecure_skip_verify, scrape configs |
| **Logrotate** | `logrotate.conf` | rotate count, permissions |
| **Rsyslog** | `rsyslog.conf` | UDP vs TCP, TLS, forwarding |
| **SSSD** | `sssd.conf` | id_provider, cache_credentials |

### Application Config
| Format | Files | Key Rules |
|--------|-------|-----------|
| **PHP** | `php.ini` | expose_php, display_errors, allow_url_include |
| **JSON** | `appsettings.json`, `package.json` | connection strings, dependencies |

## Installation

### Prerequisites
- Windows 10/11
- .NET 8.0 Runtime

### Build from Source
```bash
git clone https://github.com/VBlackJack/ConfigHumanizer.git
cd ConfigHumanizer
dotnet build --configuration Release
```

### Run
```bash
dotnet run --project ConfigHumanizer.UI
```

Or run the executable directly from `ConfigHumanizer.UI/bin/Release/net8.0-windows/`

## Usage

1. **Open a config file** via File > Open or drag & drop
2. **Review the analysis** in the Rules tab:
   - Red = Critical Security Issue
   - Orange = Warning
   - Green = Good Practice
   - Blue = Informational
3. **View the architecture diagram** in the Diagram tab
4. **Apply suggested fixes** based on recommendations

## Architecture

```
ConfigHumanizer/
├── ConfigHumanizer.Core/           # Core library
│   ├── Factories/
│   │   └── ParserFactory.cs        # Auto-detects file format
│   ├── Interfaces/
│   │   └── IConfigParser.cs
│   ├── Models/
│   │   └── HumanizedRule.cs        # Rule result model
│   ├── Parsers/
│   │   ├── BaseConfigParser.cs     # Abstract base
│   │   ├── SshdConfigParser.cs     # Space-separated (SSH, Sudoers...)
│   │   ├── IniConfigParser.cs      # INI format (MySQL, Systemd...)
│   │   ├── BlockConfigParser.cs    # Block-based (Nginx, Apache...)
│   │   ├── YamlConfigParser.cs     # YAML (Docker Compose, K8s...)
│   │   ├── JsonConfigParser.cs     # JSON (appsettings, package.json)
│   │   └── ColumnConfigParser.cs   # Column-based (fstab, crontab...)
│   └── Services/
│       ├── RuleEngine.cs           # Loads and matches JSON rules
│       └── Visualizer/
│           └── MermaidDiagramGenerator.cs
│
├── ConfigHumanizer.UI/             # WPF Application
│   ├── Rules/                      # JSON rule definitions
│   │   ├── openssh.rules.json
│   │   ├── nginx.rules.json
│   │   ├── docker-compose.rules.json
│   │   └── ... (35+ rule files)
│   ├── ViewModels/
│   │   └── MainViewModel.cs
│   └── Views/
│       └── MainWindow.xaml
```

## Rule Format

Rules are defined in JSON files under `ConfigHumanizer.UI/Rules/`:

```json
{
  "formatName": "OpenSSH",
  "filePatterns": ["sshd_config", "*sshd*"],
  "rules": [
    {
      "key": "PermitRootLogin",
      "valuePattern": "(?i)^yes$",
      "severity": "CriticalSecurity",
      "humanDescription": "Root login permitted. Attackers can directly target root account.",
      "suggestedFix": "PermitRootLogin no",
      "fixReason": "Disable direct root login. Use sudo instead."
    }
  ]
}
```

### Severity Levels
- `CriticalSecurity` - Immediate security risk
- `Warning` - Potential issue, review recommended
- `GoodPractice` - Secure configuration
- `Info` - Informational only

## Adding New Rules

1. Create a new JSON file in `ConfigHumanizer.UI/Rules/`
2. Define `formatName`, `filePatterns`, and `rules` array
3. Update `ParserFactory.cs` to detect the new format
4. (Optional) Add a diagram generator in `MermaidDiagramGenerator.cs`

## Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Submit a pull request

### Ideas for Contribution
- Add rules for new configuration formats
- Improve diagram visualizations
- Add export functionality (PDF, HTML reports)
- Add batch scanning for directories

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

## Author

**Julien Bombled** - [VBlackJack](https://github.com/VBlackJack)

---

*ConfigHumanizer - Making configuration security accessible to everyone*
