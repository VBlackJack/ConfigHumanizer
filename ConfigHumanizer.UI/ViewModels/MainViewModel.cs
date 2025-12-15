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
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using ConfigHumanizer.Core.Factories;
using ConfigHumanizer.Core.Models;
using ConfigHumanizer.Core.Services;
using ConfigHumanizer.Core.Services.Visualizer;
using ConfigHumanizer.UI.Helpers;
using ConfigHumanizer.UI.Services;
using ConfigHumanizer.UI.Services.Interfaces;
using ConfigHumanizer.UI.Views;
using Microsoft.Win32;

namespace ConfigHumanizer.UI.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IFileService _fileService;
    private readonly RuleEngine _ruleEngine;
    private readonly IDiagramGenerator _diagramGenerator;
    private readonly ParameterSchemaEngine _schemaEngine;

    // Core data
    private string _fileContent = string.Empty;
    private string _currentFilePath = "sshd_config";
    private string _currentFormatName = "OpenSSH";
    private string _statusMessage = string.Empty;
    private string _mermaidHtml = string.Empty;
    private ObservableCollection<HumanizedRule> _rules = new();
    private ObservableCollection<HumanizedRule> _filteredRules = new();

    // Search & Filter
    private string _searchText = string.Empty;
    private bool _filterCritical = true;
    private bool _filterWarning = true;
    private bool _filterInfo = true;
    private bool _filterGood = true;

    // Stats
    private int _criticalCount;
    private int _warningCount;
    private int _infoCount;
    private int _goodCount;
    private int _totalCount;
    private double _healthScore;

    // UI State
    private bool _isDarkMode;
    private bool _isLoading;
    private ObservableCollection<RecentFile> _recentFiles = new();

    // Debounce for parsing
    private CancellationTokenSource? _parseDebounceTokenSource;
    private const int ParseDebounceDelayMs = 300;

    // Undo history
    private readonly Stack<string> _undoHistory = new();
    private const int MaxUndoHistory = 50;

    // Selection
    private HumanizedRule? _selectedRule;

    public MainViewModel() : this(new FileService())
    {
    }

    public MainViewModel(IFileService fileService)
    {
        _fileService = fileService;
        _ruleEngine = new RuleEngine();
        _diagramGenerator = new MermaidDiagramGenerator();
        _schemaEngine = new ParameterSchemaEngine();

        // Load rules from the Rules directory
        var rulesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Rules");
        _ruleEngine.LoadRules(rulesPath);

        // Load parameter schemas
        var schemasPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas");
        _schemaEngine.LoadSchemas(schemasPath);

        // Initialize commands
        FixCommand = new RelayCommand(ApplyFixWithConfirmation);
        OpenConfigCommand = new RelayCommand(OpenConfig);
        SaveConfigCommand = new RelayCommand(SaveConfig);
        CopyRuleCommand = new RelayCommand(CopyRule);
        ExportHtmlCommand = new RelayCommand(ExportHtml);
        ToggleDarkModeCommand = new RelayCommand(ToggleDarkMode);
        ClearSearchCommand = new RelayCommand(ClearSearch);
        JumpToProblemsCommand = new RelayCommand(JumpToProblems);
        OpenRecentFileCommand = new RelayCommand(OpenRecentFile);
        UndoCommand = new RelayCommand(Undo, CanUndo);
        FocusSearchCommand = new RelayCommand(FocusSearch);
        ResetFiltersCommand = new RelayCommand(ResetFilters);
        OpenParameterEditorCommand = new RelayCommand(OpenParameterEditor, CanOpenParameterEditor);

        // Load recent files
        LoadRecentFiles();

        // Initialize with dummy SSH config
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

    #region Commands
    public ICommand FixCommand { get; }
    public ICommand OpenConfigCommand { get; }
    public ICommand SaveConfigCommand { get; }
    public ICommand CopyRuleCommand { get; }
    public ICommand ExportHtmlCommand { get; }
    public ICommand ToggleDarkModeCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand JumpToProblemsCommand { get; }
    public ICommand OpenRecentFileCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand FocusSearchCommand { get; }
    public ICommand ResetFiltersCommand { get; }
    public ICommand OpenParameterEditorCommand { get; }
    #endregion

    #region Events
    /// <summary>
    /// Raised when the search box should receive focus.
    /// </summary>
    public event EventHandler? FocusSearchRequested;

    /// <summary>
    /// Raised when a rule is selected and its line should be highlighted in the editor.
    /// </summary>
    public event EventHandler<HighlightLineEventArgs>? HighlightLineRequested;
    #endregion

    #region Selection Properties
    public HumanizedRule? SelectedRule
    {
        get => _selectedRule;
        set
        {
            if (SetProperty(ref _selectedRule, value) && value != null)
            {
                HighlightRuleInEditor(value);
            }
        }
    }

    private void HighlightRuleInEditor(HumanizedRule rule)
    {
        if (string.IsNullOrEmpty(_fileContent) || string.IsNullOrEmpty(rule.RawLine))
            return;

        // Find the position of the raw line in the content
        var index = _fileContent.IndexOf(rule.RawLine, StringComparison.Ordinal);
        if (index >= 0)
        {
            HighlightLineRequested?.Invoke(this, new HighlightLineEventArgs(index, rule.RawLine.Length));
        }
    }
    #endregion

    #region Core Properties
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string CurrentFilePath
    {
        get => _currentFilePath;
        private set => SetProperty(ref _currentFilePath, value);
    }

    public string CurrentFileName => Path.GetFileName(_currentFilePath);

    public string FileContent
    {
        get => _fileContent;
        set
        {
            if (SetProperty(ref _fileContent, value))
            {
                // Debounce parsing to avoid excessive re-parsing on rapid changes
                DebouncedParseConfig();
            }
        }
    }

    private async void DebouncedParseConfig()
    {
        // Cancel previous debounce if still pending
        _parseDebounceTokenSource?.Cancel();
        _parseDebounceTokenSource = new CancellationTokenSource();

        try
        {
            await Task.Delay(ParseDebounceDelayMs, _parseDebounceTokenSource.Token);
            ParseConfig();
        }
        catch (TaskCanceledException)
        {
            // Debounce was cancelled by a new change - this is expected
        }
    }

    public ObservableCollection<HumanizedRule> Rules
    {
        get => _rules;
        set => SetProperty(ref _rules, value);
    }

    public ObservableCollection<HumanizedRule> FilteredRules
    {
        get => _filteredRules;
        set => SetProperty(ref _filteredRules, value);
    }

    public string MermaidHtml
    {
        get => _mermaidHtml;
        private set => SetProperty(ref _mermaidHtml, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }
    #endregion

    #region Search & Filter Properties
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilters();
            }
        }
    }

    public bool FilterCritical
    {
        get => _filterCritical;
        set
        {
            if (SetProperty(ref _filterCritical, value))
            {
                ApplyFilters();
            }
        }
    }

    public bool FilterWarning
    {
        get => _filterWarning;
        set
        {
            if (SetProperty(ref _filterWarning, value))
            {
                ApplyFilters();
            }
        }
    }

    public bool FilterInfo
    {
        get => _filterInfo;
        set
        {
            if (SetProperty(ref _filterInfo, value))
            {
                ApplyFilters();
            }
        }
    }

    public bool FilterGood
    {
        get => _filterGood;
        set
        {
            if (SetProperty(ref _filterGood, value))
            {
                ApplyFilters();
            }
        }
    }
    #endregion

    #region Stats Properties
    public int CriticalCount
    {
        get => _criticalCount;
        private set => SetProperty(ref _criticalCount, value);
    }

    public int WarningCount
    {
        get => _warningCount;
        private set => SetProperty(ref _warningCount, value);
    }

    public int InfoCount
    {
        get => _infoCount;
        private set => SetProperty(ref _infoCount, value);
    }

    public int GoodCount
    {
        get => _goodCount;
        private set => SetProperty(ref _goodCount, value);
    }

    public int TotalCount
    {
        get => _totalCount;
        private set => SetProperty(ref _totalCount, value);
    }

    public double HealthScore
    {
        get => _healthScore;
        private set => SetProperty(ref _healthScore, value);
    }

    public string HealthScoreText => $"{HealthScore:F0}%";

    public string HealthScoreColor => HealthScore >= 80 ? "#4CAF50" :
                                       HealthScore >= 50 ? "#F39C12" : "#E74C3C";

    /// <summary>
    /// Returns true when filters/search hide all results but there are rules loaded.
    /// </summary>
    public bool ShowFilterEmptyState => TotalCount > 0 && FilteredRules.Count == 0;
    #endregion

    #region UI State Properties
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set => SetProperty(ref _isDarkMode, value);
    }

    public ObservableCollection<RecentFile> RecentFiles
    {
        get => _recentFiles;
        set => SetProperty(ref _recentFiles, value);
    }

    public bool HasRecentFiles => RecentFiles.Count > 0;
    #endregion

    #region Core Methods
    private void ParseConfig()
    {
        _currentFormatName = DetectFormatName(_currentFilePath, _fileContent);
        OnPropertyChanged(nameof(CurrentFileName));

        var parser = ParserFactory.GetParser(_currentFilePath, _fileContent, _ruleEngine);
        var parsedRules = parser.Parse(_fileContent);
        Rules = new ObservableCollection<HumanizedRule>(parsedRules);

        UpdateStats();
        ApplyFilters();
        GenerateDiagram();
    }

    private void UpdateStats()
    {
        // Optimized: single iteration instead of 4 separate Count() calls
        int critical = 0, warning = 0, info = 0, good = 0;
        foreach (var rule in Rules)
        {
            switch (rule.Severity)
            {
                case Severity.CriticalSecurity: critical++; break;
                case Severity.Warning: warning++; break;
                case Severity.Info: info++; break;
                case Severity.GoodPractice: good++; break;
            }
        }

        CriticalCount = critical;
        WarningCount = warning;
        InfoCount = info;
        GoodCount = good;
        TotalCount = Rules.Count;

        // Calculate health score (100% = all good, 0% = all critical)
        if (TotalCount > 0)
        {
            var weightedScore = (good * 100) + (info * 75) + (warning * 25) + (critical * 0);
            HealthScore = weightedScore / (double)TotalCount;
        }
        else
        {
            HealthScore = 100;
        }

        OnPropertyChanged(nameof(HealthScoreText));
        OnPropertyChanged(nameof(HealthScoreColor));
    }

    private void ApplyFilters()
    {
        var filtered = Rules.AsEnumerable();

        // Apply severity filters
        filtered = filtered.Where(r =>
            (FilterCritical && r.Severity == Severity.CriticalSecurity) ||
            (FilterWarning && r.Severity == Severity.Warning) ||
            (FilterInfo && r.Severity == Severity.Info) ||
            (FilterGood && r.Severity == Severity.GoodPractice));

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            filtered = filtered.Where(r =>
                (r.Key?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                (r.Value?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                (r.HumanDescription?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                (r.RawLine?.ToLowerInvariant().Contains(searchLower) ?? false));
        }

        FilteredRules = new ObservableCollection<HumanizedRule>(filtered);
        OnPropertyChanged(nameof(ShowFilterEmptyState));
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
        if (filePath.Contains("pam.d") || fileName == "pam.conf")
            return "PAM";
        if (fileName.Contains("iptables") || fileName.Contains("ip6tables") || fileName == "nftables.conf")
            return "Iptables";

        // Proxy & Load Balancing
        if (fileName.Contains("squid")) return "Squid";
        if (fileName.Contains("haproxy")) return "HAProxy";

        // Authentication
        if (fileName.Contains("sssd")) return "SSSD";
        if (fileName.Contains("fail2ban") || fileName == "jail.conf" || fileName == "jail.local")
            return "Fail2ban";

        // System Services
        if (extension == ".service" || extension == ".socket" || extension == ".timer")
            return "Systemd";
        if (fileName.Contains("sysctl")) return "Sysctl";
        if (fileName == "logrotate.conf" || filePath.Contains("logrotate.d"))
            return "Logrotate";
        if (fileName.Contains("rsyslog")) return "Rsyslog";

        // Databases
        if (fileName == "my.cnf" || fileName == "my.ini" || fileName == "mysql.cnf")
            return "MySQL";
        if (fileName == "postgresql.conf" || fileName == "pg_hba.conf")
            return "PostgreSQL";
        if (fileName == "redis.conf") return "Redis";

        // Network Services
        if (fileName == "main.cf" || fileName == "master.cf") return "Postfix";
        if (fileName == "smb.conf") return "Samba";
        if (fileName == "exports") return "NFS";

        // Column-based
        if (fileName.Contains("crontab") || fileName.StartsWith("cron")) return "Crontab";
        if (fileName.Contains("fstab")) return "Fstab";
        if (fileName == "hosts") return "Hosts";
        if (fileName.Contains("resolv")) return "Resolv";

        // Block-based
        if (fileName.Contains("nginx")) return "Nginx";
        if (fileName.Contains("apache") || fileName.Contains("httpd")) return "Apache";

        // IaC
        if (extension == ".tf" || extension == ".tfvars") return "Terraform";

        // YAML files
        if (extension == ".yaml" || extension == ".yml")
        {
            if (fileName.Contains("docker-compose") || fileName.Contains("compose")) return "DockerCompose";
            if (fileName == ".gitlab-ci.yml") return "GitLabCI";
            if (filePath.Contains(".github") && filePath.Contains("workflows")) return "GitHubActions";
            if (fileName == "playbook.yml" || fileName == "site.yml") return "Ansible";
            if (fileName.Contains("prometheus")) return "Prometheus";
            if (!string.IsNullOrEmpty(fileContent) && fileContent.Contains("apiVersion:") && fileContent.Contains("kind:"))
                return "Kubernetes";
            return "YAML";
        }

        // JSON files
        if (extension == ".json")
        {
            if (fileName == "package.json") return "NPM";
            if (fileName.Contains("appsettings")) return "AppSettings";
            if (fileName.Contains("fw_rules") || fileName.Contains("nat_rules")) return "PaloAlto";
            return "JSON";
        }

        // INI/CFG files
        if (extension == ".ini" || extension == ".cfg" || extension == ".conf")
            return "INI";

        return "Generic";
    }

    private void GenerateDiagram()
    {
        var mermaidCode = _diagramGenerator.GenerateMermaid(Rules.ToList(), _currentFormatName);
        MermaidHtml = GenerateMermaidHtml(mermaidCode, _isDarkMode);
    }

    private static string GenerateMermaidHtml(string mermaidCode, bool darkMode)
    {
        var bgColor = darkMode ? "#1e1e1e" : "#f5f5f5";
        var cardBg = darkMode ? "#2d2d30" : "white";
        var textColor = darkMode ? "#cccccc" : "#333333";
        var theme = darkMode ? "dark" : "default";

        // Mermaid 11.4.0 with SRI (Subresource Integrity) for security
        const string mermaidVersion = "11.4.0";
        const string mermaidSri = "sha384-sPe5cvleqMFiPNELO0mSy9nYv9bRKuqICe9LwJp8LZkEiwWnJVaJcNcwYmpb8RCU";

        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="UTF-8">
                <meta http-equiv="Content-Security-Policy" content="default-src 'self'; script-src 'self' https://cdn.jsdelivr.net 'unsafe-inline'; style-src 'self' 'unsafe-inline';">
                <style>
                    body {
                        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                        background-color: {{bgColor}};
                        color: {{textColor}};
                        margin: 0;
                        padding: 20px;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        min-height: 100vh;
                    }
                    .mermaid {
                        background-color: {{cardBg}};
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
                <script src="https://cdn.jsdelivr.net/npm/mermaid@{{mermaidVersion}}/dist/mermaid.min.js"
                        integrity="{{mermaidSri}}"
                        crossorigin="anonymous"></script>
                <script>
                    mermaid.initialize({
                        startOnLoad: true,
                        theme: '{{theme}}',
                        securityLevel: 'strict',
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
    #endregion

    #region Command Handlers

    private void ApplyFixWithConfirmation(object? parameter)
    {
        if (parameter is not HumanizedRule rule || !rule.HasFix)
            return;

        var result = MessageBox.Show(
            $"Voulez-vous appliquer ce correctif ?\n\n" +
            $"Avant : {rule.RawLine}\n" +
            $"Après : {rule.SuggestedFix}\n\n" +
            $"Raison : {rule.FixReason}",
            "Confirmer le correctif",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            ApplyFix(rule);
        }
    }

    private void ApplyFix(HumanizedRule rule)
    {
        // Save current state for undo
        SaveToUndoHistory();

        // Apply fix without triggering debounce
        _fileContent = _fileContent.Replace(rule.RawLine, rule.SuggestedFix);
        OnPropertyChanged(nameof(FileContent));
        ParseConfig();

        StatusMessage = $"✅ Correctif appliqué : {rule.Key} → {rule.SuggestedFix}";
    }

    private void SaveToUndoHistory()
    {
        _undoHistory.Push(_fileContent);

        // Limit history size
        if (_undoHistory.Count > MaxUndoHistory)
        {
            var tempStack = new Stack<string>(_undoHistory.Take(MaxUndoHistory).Reverse());
            _undoHistory.Clear();
            foreach (var item in tempStack.Reverse())
                _undoHistory.Push(item);
        }
    }

    private void Undo()
    {
        if (_undoHistory.Count > 0)
        {
            _fileContent = _undoHistory.Pop();
            OnPropertyChanged(nameof(FileContent));
            ParseConfig();
            StatusMessage = "↩️ Annulation effectuée";
        }
    }

    private bool CanUndo() => _undoHistory.Count > 0;

    private void FocusSearch()
    {
        FocusSearchRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ResetFilters()
    {
        FilterCritical = true;
        FilterWarning = true;
        FilterInfo = true;
        FilterGood = true;
        SearchText = string.Empty;
        StatusMessage = "🔄 Filtres réinitialisés";
    }

    private bool CanOpenParameterEditor() => _schemaEngine.Schemas.Count > 0;

    private void OpenParameterEditor()
    {
        var schema = _schemaEngine.GetSchemaForFile(_currentFilePath);
        if (schema == null && _schemaEngine.Schemas.Count > 0)
        {
            // Utiliser le premier schéma disponible
            schema = _schemaEngine.Schemas[0];
        }

        if (schema == null)
        {
            StatusMessage = "❌ Aucun schéma de paramètres disponible";
            return;
        }

        var viewModel = new ParameterEditorViewModel(_schemaEngine);
        viewModel.Initialize(schema);

        var window = new ParameterEditorWindow
        {
            DataContext = viewModel,
            Owner = Application.Current.MainWindow
        };

        if (window.ShowDialog() == true && !string.IsNullOrEmpty(window.GeneratedLine))
        {
            // Sauvegarder pour undo
            SaveToUndoHistory();

            // Ajouter la ligne à la fin du fichier
            var newContent = _fileContent.TrimEnd();
            if (!string.IsNullOrEmpty(newContent))
            {
                newContent += Environment.NewLine;
            }
            newContent += window.GeneratedLine;

            _fileContent = newContent;
            OnPropertyChanged(nameof(FileContent));
            ParseConfig();

            StatusMessage = $"✅ Paramètre ajouté : {window.GeneratedLine}";
        }
    }

    private void OpenConfig()
    {
        var path = _fileService.OpenFile();
        if (path != null)
        {
            LoadFile(path);
        }
    }

    public void LoadFile(string path)
    {
        if (!File.Exists(path)) return;

        CurrentFilePath = path;
        FileContent = _fileService.ReadFile(path);
        AddToRecentFiles(path);
        StatusMessage = $"📄 Fichier chargé : {Path.GetFileName(path)}";
    }

    private async Task SaveConfigAsync()
    {
        if (string.IsNullOrEmpty(_currentFilePath) || !Path.IsPathRooted(_currentFilePath))
        {
            StatusMessage = "❌ Erreur : Aucun fichier ouvert. Ouvrez d'abord un fichier.";
            return;
        }

        try
        {
            _fileService.SaveFile(_currentFilePath, _fileContent);
            StatusMessage = $"💾 Sauvegardé à {DateTime.Now:HH:mm:ss}";
            await Task.Delay(3000);
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erreur : {ex.Message}";
        }
    }

    private void SaveConfig()
    {
        // Fire-and-forget with proper exception handling
        _ = SaveConfigAsync().ContinueWith(t =>
        {
            if (t.Exception != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    StatusMessage = $"Error: {t.Exception.InnerException?.Message ?? t.Exception.Message}");
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    private void CopyRule(object? parameter)
    {
        if (parameter is HumanizedRule rule)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"## {rule.Key}");
            sb.AppendLine($"**Value:** {rule.Value}");
            sb.AppendLine();
            sb.AppendLine(rule.HumanDescription);

            if (rule.HasFix)
            {
                sb.AppendLine();
                sb.AppendLine($"**Suggested Fix:** {rule.SuggestedFix}");
                sb.AppendLine($"**Reason:** {rule.FixReason}");
            }

            Clipboard.SetText(sb.ToString());
            StatusMessage = "📋 Copié dans le presse-papiers";
        }
    }

    private void ExportHtml()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "HTML files (*.html)|*.html|All files (*.*)|*.*",
            DefaultExt = ".html",
            FileName = $"ConfigAnalysis_{Path.GetFileNameWithoutExtension(_currentFilePath)}_{DateTime.Now:yyyyMMdd}"
        };

        if (dialog.ShowDialog() == true)
        {
            var html = GenerateExportHtml();
            File.WriteAllText(dialog.FileName, html);
            StatusMessage = $"Exported to {dialog.FileName}";
        }
    }

    private string GenerateExportHtml()
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><meta charset='UTF-8'>");
        sb.AppendLine("<title>ConfigHumanizer Analysis Report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: 'Segoe UI', sans-serif; max-width: 1200px; margin: 0 auto; padding: 20px; background: #f5f5f5; }");
        sb.AppendLine(".header { background: #007ACC; color: white; padding: 20px; border-radius: 8px; margin-bottom: 20px; }");
        sb.AppendLine(".stats { display: flex; gap: 10px; margin-bottom: 20px; }");
        sb.AppendLine(".stat { background: white; padding: 15px; border-radius: 8px; text-align: center; flex: 1; }");
        sb.AppendLine(".stat-value { font-size: 24px; font-weight: bold; }");
        sb.AppendLine(".critical { border-left: 4px solid #E74C3C; }");
        sb.AppendLine(".warning { border-left: 4px solid #F39C12; }");
        sb.AppendLine(".info { border-left: 4px solid #3498DB; }");
        sb.AppendLine(".good { border-left: 4px solid #4CAF50; }");
        sb.AppendLine(".rule { background: white; padding: 16px; margin-bottom: 12px; border-radius: 8px; }");
        sb.AppendLine(".rule-key { font-weight: bold; font-size: 16px; }");
        sb.AppendLine(".rule-value { background: #333; color: white; padding: 4px 8px; border-radius: 4px; font-family: monospace; }");
        sb.AppendLine(".rule-desc { margin-top: 10px; color: #666; white-space: pre-wrap; }");
        sb.AppendLine("</style></head><body>");

        // Header
        sb.AppendLine($"<div class='header'>");
        sb.AppendLine($"<h1>Configuration Analysis Report</h1>");
        sb.AppendLine($"<p>File: {CurrentFileName} | Generated: {DateTime.Now:yyyy-MM-dd HH:mm}</p>");
        sb.AppendLine("</div>");

        // Stats
        sb.AppendLine("<div class='stats'>");
        sb.AppendLine($"<div class='stat'><div class='stat-value' style='color:#E74C3C'>{CriticalCount}</div><div>Critical</div></div>");
        sb.AppendLine($"<div class='stat'><div class='stat-value' style='color:#F39C12'>{WarningCount}</div><div>Warnings</div></div>");
        sb.AppendLine($"<div class='stat'><div class='stat-value' style='color:#3498DB'>{InfoCount}</div><div>Info</div></div>");
        sb.AppendLine($"<div class='stat'><div class='stat-value' style='color:#4CAF50'>{GoodCount}</div><div>Good</div></div>");
        sb.AppendLine($"<div class='stat'><div class='stat-value' style='color:#007ACC'>{HealthScore:F0}%</div><div>Health</div></div>");
        sb.AppendLine("</div>");

        // Rules
        foreach (var rule in Rules)
        {
            var severityClass = rule.Severity switch
            {
                Severity.CriticalSecurity => "critical",
                Severity.Warning => "warning",
                Severity.Info => "info",
                Severity.GoodPractice => "good",
                _ => ""
            };

            sb.AppendLine($"<div class='rule {severityClass}'>");
            sb.AppendLine($"<span class='rule-key'>{rule.Key}</span> ");
            sb.AppendLine($"<span class='rule-value'>{rule.Value}</span>");
            sb.AppendLine($"<div class='rule-desc'>{rule.HumanDescription}</div>");
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private void ToggleDarkMode()
    {
        IsDarkMode = !IsDarkMode;
        GenerateDiagram(); // Regenerate with new theme
    }

    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    private void JumpToProblems()
    {
        // Enable only critical and warning filters
        FilterCritical = true;
        FilterWarning = true;
        FilterInfo = false;
        FilterGood = false;
        SearchText = string.Empty;
    }

    private void OpenRecentFile(object? parameter)
    {
        if (parameter is RecentFile recent && File.Exists(recent.Path))
        {
            LoadFile(recent.Path);
        }
    }
    #endregion

    #region Recent Files
    private void LoadRecentFiles()
    {
        try
        {
            var recentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recent.txt");
            if (File.Exists(recentPath))
            {
                var lines = File.ReadAllLines(recentPath).Take(10);
                RecentFiles = new ObservableCollection<RecentFile>(
                    lines.Where(File.Exists).Select(p => new RecentFile(p)));
            }
        }
        catch { }
        OnPropertyChanged(nameof(HasRecentFiles));
    }

    private void SaveRecentFiles()
    {
        try
        {
            var recentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recent.txt");
            File.WriteAllLines(recentPath, RecentFiles.Select(r => r.Path));
        }
        catch { }
    }

    private void AddToRecentFiles(string path)
    {
        var existing = RecentFiles.FirstOrDefault(r => r.Path == path);
        if (existing != null)
        {
            RecentFiles.Remove(existing);
        }

        RecentFiles.Insert(0, new RecentFile(path));

        while (RecentFiles.Count > 10)
        {
            RecentFiles.RemoveAt(RecentFiles.Count - 1);
        }

        SaveRecentFiles();
        OnPropertyChanged(nameof(HasRecentFiles));
    }
    #endregion

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    #endregion
}

public class RecentFile
{
    public string Path { get; }
    public string Name => System.IO.Path.GetFileName(Path);
    public string Directory => System.IO.Path.GetDirectoryName(Path) ?? "";

    public RecentFile(string path)
    {
        Path = path;
    }
}

/// <summary>
/// Event args for highlighting a line in the code editor.
/// </summary>
public class HighlightLineEventArgs : EventArgs
{
    public int StartIndex { get; }
    public int Length { get; }

    public HighlightLineEventArgs(int startIndex, int length)
    {
        StartIndex = startIndex;
        Length = length;
    }
}
