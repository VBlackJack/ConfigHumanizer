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

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ConfigHumanizer.Core.Models;
using ConfigHumanizer.UI.ViewModels;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Web.WebView2.Core;

namespace ConfigHumanizer.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;

        // Handle recent files selection
        RecentFilesCombo.SelectionChanged += RecentFilesCombo_SelectionChanged;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Subscribe to ViewModel property changes (outside WebView2 try-catch)
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            viewModel.FocusSearchRequested += ViewModel_FocusSearchRequested;
            viewModel.HighlightLineRequested += ViewModel_HighlightLineRequested;

            // Initialize CodeEditor with current content
            CodeEditor.Text = viewModel.FileContent ?? string.Empty;

            // Apply initial syntax highlighting
            ApplySyntaxHighlighting(viewModel.SyntaxHighlightingName);

            // Subscribe to CodeEditor.TextChanged to update ViewModel
            CodeEditor.TextChanged += CodeEditor_TextChanged;
        }

        // Initialize WebView2 (optional - diagrams may not work without it)
        try
        {
            await DiagramWebView.EnsureCoreWebView2Async();

            if (DataContext is MainViewModel viewModel2)
            {
                // Subscribe to WebView2 messages for diagram click-to-scroll
                DiagramWebView.WebMessageReceived += DiagramWebView_WebMessageReceived;

                // Initial render
                if (!string.IsNullOrEmpty(viewModel2.MermaidHtml))
                {
                    DiagramWebView.NavigateToString(viewModel2.MermaidHtml);
                }
            }
        }
        catch (Exception ex)
        {
            // WebView2 runtime may not be installed - diagrams won't work but editor still functions
            System.Diagnostics.Debug.WriteLine($"WebView2 initialization failed: {ex.Message}");
        }
    }

    private void CodeEditor_TextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.FileContent = CodeEditor.Text;
        }
    }

    private void ViewModel_FocusSearchRequested(object? sender, EventArgs e)
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private void DiagramWebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var searchText = e.TryGetWebMessageAsString();
            if (string.IsNullOrEmpty(searchText))
                return;

            var index = CodeEditor.Text.IndexOf(searchText, StringComparison.Ordinal);
            if (index >= 0)
            {
                // Get the line number from the document offset
                var line = CodeEditor.Document.GetLineByOffset(index);
                if (line != null)
                {
                    CodeEditor.ScrollToLine(line.LineNumber);
                    CodeEditor.Select(index, searchText.Length);
                    CodeEditor.Focus();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebMessageReceived failed: {ex.Message}");
        }
    }

    private void ViewModel_HighlightLineRequested(object? sender, HighlightLineEventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            try
            {
                // Focus the code editor
                CodeEditor.Focus();

                // Select the text using AvalonEdit API
                CodeEditor.Select(e.StartIndex, e.Length);

                // Get the line number from the document offset
                var line = CodeEditor.Document.GetLineByOffset(e.StartIndex);
                if (line != null)
                {
                    CodeEditor.ScrollToLine(line.LineNumber);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Highlight failed: {ex.Message}");
            }
        });
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MainViewModel viewModel)
            return;

        if (e.PropertyName == nameof(MainViewModel.MermaidHtml))
        {
            // Update WebView2 content on UI thread
            Dispatcher.InvokeAsync(() =>
            {
                if (DiagramWebView.CoreWebView2 != null && !string.IsNullOrEmpty(viewModel.MermaidHtml))
                {
                    DiagramWebView.NavigateToString(viewModel.MermaidHtml);
                }
            });
        }
        else if (e.PropertyName == nameof(MainViewModel.FileContent))
        {
            // Update CodeEditor when ViewModel's FileContent changes (e.g., file loaded)
            Dispatcher.InvokeAsync(() =>
            {
                var newContent = viewModel.FileContent ?? string.Empty;
                if (CodeEditor.Text != newContent)
                {
                    CodeEditor.Text = newContent;
                }
            });
        }
        else if (e.PropertyName == nameof(MainViewModel.CurrentFilePath))
        {
            // Update syntax highlighting when a new file is loaded
            Dispatcher.InvokeAsync(() =>
            {
                ApplySyntaxHighlighting(viewModel.SyntaxHighlightingName);
            });
        }
    }

    /// <summary>
    /// Applies syntax highlighting to the code editor based on the highlighting name.
    /// </summary>
    private void ApplySyntaxHighlighting(string highlightingName)
    {
        try
        {
            var highlighting = HighlightingManager.Instance.GetDefinition(highlightingName);
            CodeEditor.SyntaxHighlighting = highlighting;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to apply syntax highlighting '{highlightingName}': {ex.Message}");
            // Fall back to no highlighting
            CodeEditor.SyntaxHighlighting = null;
        }
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                var filePath = files[0]; // Take the first file
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.LoadFile(filePath);
                }
            }
        }
    }

    private void RecentFilesCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RecentFilesCombo.SelectedItem is RecentFile recent && DataContext is MainViewModel viewModel)
        {
            viewModel.LoadFile(recent.Path);
            // Reset selection so user can select the same file again
            RecentFilesCombo.SelectedItem = null;
        }
    }

    private void DashboardRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGridRow row && row.Item is FileAnalysisSummary item)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.OpenDashboardFileCommand.Execute(item);
            }
        }
    }
}
