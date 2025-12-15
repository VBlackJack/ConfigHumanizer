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
using ConfigHumanizer.UI.ViewModels;

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
        // Initialize WebView2
        try
        {
            await DiagramWebView.EnsureCoreWebView2Async();

            // Subscribe to ViewModel property changes
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
                viewModel.FocusSearchRequested += ViewModel_FocusSearchRequested;
                viewModel.HighlightLineRequested += ViewModel_HighlightLineRequested;

                // Initial render
                if (!string.IsNullOrEmpty(viewModel.MermaidHtml))
                {
                    DiagramWebView.NavigateToString(viewModel.MermaidHtml);
                }
            }
        }
        catch (Exception ex)
        {
            // WebView2 runtime may not be installed
            System.Diagnostics.Debug.WriteLine($"WebView2 initialization failed: {ex.Message}");
        }
    }

    private void ViewModel_FocusSearchRequested(object? sender, EventArgs e)
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private void ViewModel_HighlightLineRequested(object? sender, HighlightLineEventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            try
            {
                // Focus the code editor
                CodeEditor.Focus();

                // Select the text
                CodeEditor.SelectionStart = e.StartIndex;
                CodeEditor.SelectionLength = e.Length;

                // Scroll to make the selection visible
                var lineIndex = CodeEditor.GetLineIndexFromCharacterIndex(e.StartIndex);
                if (lineIndex >= 0)
                {
                    CodeEditor.ScrollToLine(lineIndex);
                }

                // Flash effect: briefly change selection background
                // This provides visual feedback even if the TextBox loses focus
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Highlight failed: {ex.Message}");
            }
        });
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.MermaidHtml) && sender is MainViewModel viewModel)
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
}
