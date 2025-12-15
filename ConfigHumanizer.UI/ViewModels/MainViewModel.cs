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

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ConfigHumanizer.Core.Factories;
using ConfigHumanizer.Core.Models;
using ConfigHumanizer.Core.Services;
using ConfigHumanizer.Core.Services.Visualizer;
using ConfigHumanizer.UI.Helpers;
using ConfigHumanizer.UI.Services;
using ConfigHumanizer.UI.Services.Interfaces;

namespace ConfigHumanizer.UI.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IFileService _fileService;
    private readonly RuleEngine _ruleEngine;
    private readonly IDiagramGenerator _diagramGenerator;
    private string _fileContent = string.Empty;
    private string _currentFilePath = "sshd_config";
    private string _currentFormatName = "OpenSSH";
    private string _statusMessage = string.Empty;
    private string _mermaidHtml = string.Empty;
    private ObservableCollection<HumanizedRule> _rules = new();

    public MainViewModel() : this(new FileService())
    {
    }

    public MainViewModel(IFileService fileService)
    {
        _fileService = fileService;
        _ruleEngine = new RuleEngine();
        _diagramGenerator = new MermaidDiagramGenerator();

        // Load rules from the Rules directory
        var rulesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Rules");
        _ruleEngine.LoadRules(rulesPath);

        FixCommand = new RelayCommand(ApplyFix);
        OpenConfigCommand = new RelayCommand(OpenConfig);
        SaveConfigCommand = new RelayCommand(SaveConfig);

        // Initialize with dummy SSH config containing mixed safe/unsafe settings
        FileContent = """
            # SSH Server Configuration
            Port 22
            PermitRootLogin yes
            PasswordAuthentication yes
            PubkeyAuthentication yes
            MaxAuthTries 6
            X11Forwarding no
            AllowTcpForwarding no
            """;

        ParseConfig();
    }

    public ICommand FixCommand { get; }
    public ICommand OpenConfigCommand { get; }
    public ICommand SaveConfigCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public string CurrentFilePath
    {
        get => _currentFilePath;
        private set
        {
            if (_currentFilePath != value)
            {
                _currentFilePath = value;
                OnPropertyChanged();
            }
        }
    }

    public string FileContent
    {
        get => _fileContent;
        set
        {
            if (_fileContent != value)
            {
                _fileContent = value;
                OnPropertyChanged();
                ParseConfig();
            }
        }
    }

    public ObservableCollection<HumanizedRule> Rules
    {
        get => _rules;
        set
        {
            if (_rules != value)
            {
                _rules = value;
                OnPropertyChanged();
            }
        }
    }

    public string MermaidHtml
    {
        get => _mermaidHtml;
        private set
        {
            if (_mermaidHtml != value)
            {
                _mermaidHtml = value;
                OnPropertyChanged();
            }
        }
    }

    private void ParseConfig()
    {
        // Detect format name from file path and content
        _currentFormatName = DetectFormatName(_currentFilePath, _fileContent);

        var parser = ParserFactory.GetParser(_currentFilePath, _fileContent, _ruleEngine);
        var parsedRules = parser.Parse(_fileContent);
        Rules = new ObservableCollection<HumanizedRule>(parsedRules);

        // Generate Mermaid diagram
        GenerateDiagram();
    }

    private static string DetectFormatName(string filePath, string? fileContent = null)
    {
        if (string.IsNullOrEmpty(filePath))
            return "OpenSSH";

        var fileName = Path.GetFileName(filePath).ToLowerInvariant();
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        // SSH & Security
        if (fileName.EndsWith("sshd_config") || fileName == "sshd_config")
            return "OpenSSH";
        if (fileName == "ssh_config" || (fileName == "config" && filePath.Contains(".ssh")))
            return "SSHClient";
        if (fileName == "sudoers" || filePath.Contains("sudoers.d"))
            return "Sudoers";
        if (filePath.Contains("pam.d") || fileName == "pam.conf" ||
            fileName == "system-auth" || fileName == "password-auth" || fileName == "common-auth")
            return "PAM";
        if (fileName.Contains("iptables") || fileName.Contains("ip6tables") ||
            fileName == "nftables.conf" || fileName.Contains("firewall"))
            return "Iptables";

        // Proxy & Load Balancing
        if (fileName.Contains("squid"))
            return "Squid";
        if (fileName.Contains("haproxy"))
            return "HAProxy";

        // Authentication
        if (fileName.Contains("sssd"))
            return "SSSD";
        if (fileName.Contains("fail2ban") || fileName == "jail.conf" || fileName == "jail.local" || filePath.Contains("jail.d"))
            return "Fail2ban";

        // System Services
        if (extension == ".service" || extension == ".socket" || extension == ".timer" || extension == ".mount")
            return "Systemd";
        if (fileName.Contains("sysctl"))
            return "Sysctl";
        if (fileName == "logrotate.conf" || filePath.Contains("logrotate.d"))
            return "Logrotate";
        if (fileName.Contains("rsyslog") || filePath.Contains("rsyslog.d"))
            return "Rsyslog";

        // Databases
        if (fileName == "my.cnf" || fileName == "my.ini" || fileName == "mysql.cnf" ||
            fileName == "mariadb.cnf" || filePath.Contains("mysql"))
            return "MySQL";
        if (fileName == "postgresql.conf" || fileName == "pg_hba.conf" || filePath.Contains("postgresql"))
            return "PostgreSQL";
        if (fileName == "redis.conf" || filePath.Contains("redis"))
            return "Redis";

        // Network Services
        if (fileName == "main.cf" || fileName == "master.cf" || filePath.Contains("postfix"))
            return "Postfix";
        if (fileName == "smb.conf" || filePath.Contains("samba"))
            return "Samba";
        if (fileName == "exports")
            return "NFS";

        // Column-based
        if (fileName.Contains("crontab") || fileName.Contains("cron.d") || fileName.StartsWith("cron"))
            return "Crontab";
        if (fileName.Contains("fstab"))
            return "Fstab";
        if (fileName == "hosts")
            return "Hosts";
        if (fileName.Contains("resolv"))
            return "Resolv";

        // Block-based
        if (fileName.Contains("nginx") || fileName == "nginx.conf")
            return "Nginx";
        if (fileName.Contains("apache") || fileName.Contains("httpd") ||
            fileName == "apache2.conf" || fileName == "httpd.conf")
            return "Apache";

        // IaC
        if (extension == ".tf" || extension == ".tfvars")
            return "Terraform";

        // YAML files
        if (extension == ".yaml" || extension == ".yml")
        {
            if (fileName.Contains("docker-compose") || fileName.Contains("compose"))
                return "DockerCompose";
            if (fileName == ".gitlab-ci.yml" || fileName == ".gitlab-ci.yaml")
                return "GitLabCI";
            if (filePath.Contains(".github") && filePath.Contains("workflows"))
                return "GitHubActions";
            if (fileName == "playbook.yml" || fileName == "playbook.yaml" || fileName == "site.yml" ||
                filePath.Contains("playbooks") || filePath.Contains("tasks") || filePath.Contains("handlers"))
                return "Ansible";
            if (fileName.Contains("prometheus"))
                return "Prometheus";
            if (fileName.Contains("traefik"))
                return "Traefik";
            if (fileName.Contains("envoy"))
                return "Envoy";
            if (fileName == "mongod.conf" || fileName == "mongodb.conf")
                return "MongoDB";
            if (!string.IsNullOrEmpty(fileContent) &&
                fileContent.Contains("apiVersion:") && fileContent.Contains("kind:"))
                return "Kubernetes";
            return "YAML";
        }

        // JSON files
        if (extension == ".json")
        {
            if (fileName == "package.json")
                return "NPM";
            if (fileName.Contains("appsettings"))
                return "AppSettings";
            return "JSON";
        }

        // PHP INI
        if (fileName == "php.ini" || (fileName.Contains("php") && extension == ".ini"))
            return "PHP";

        // INI/CFG files
        if (extension == ".ini" || extension == ".cfg" || extension == ".conf")
            return "INI";

        return "Generic";
    }

    private void GenerateDiagram()
    {
        var mermaidCode = _diagramGenerator.GenerateMermaid(Rules.ToList(), _currentFormatName);
        MermaidHtml = GenerateMermaidHtml(mermaidCode);
    }

    private static string GenerateMermaidHtml(string mermaidCode)
    {
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="UTF-8">
                <style>
                    body {
                        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                        background-color: #f5f5f5;
                        margin: 0;
                        padding: 20px;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        min-height: 100vh;
                    }
                    .mermaid {
                        background-color: white;
                        padding: 30px;
                        border-radius: 10px;
                        box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                    }
                </style>
            </head>
            <body>
                <div class="mermaid">
            {{mermaidCode}}
                </div>
                <script src="https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js"></script>
                <script>
                    mermaid.initialize({
                        startOnLoad: true,
                        theme: 'default',
                        securityLevel: 'loose',
                        flowchart: {
                            useMaxWidth: true,
                            htmlLabels: true,
                            curve: 'basis'
                        }
                    });
                </script>
            </body>
            </html>
            """;
    }

    private void ApplyFix(object? parameter)
    {
        if (parameter is HumanizedRule rule && rule.HasFix)
        {
            FileContent = FileContent.Replace(rule.RawLine, rule.SuggestedFix);
        }
    }

    private void OpenConfig()
    {
        var path = _fileService.OpenFile();
        if (path != null)
        {
            CurrentFilePath = path;
            FileContent = _fileService.ReadFile(path);
            StatusMessage = string.Empty;
        }
    }

    private async void SaveConfig()
    {
        // Check if we have a valid file path (not the default startup value)
        if (string.IsNullOrEmpty(_currentFilePath) ||
            !System.IO.Path.IsPathRooted(_currentFilePath))
        {
            StatusMessage = "Error: No file path associated. Please open a file first.";
            return;
        }

        try
        {
            _fileService.SaveFile(_currentFilePath, _fileContent);
            StatusMessage = $"Saved successfully with backup at {DateTime.Now:HH:mm:ss}";

            // Clear status message after 3 seconds
            await Task.Delay(3000);
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving file: {ex.Message}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
