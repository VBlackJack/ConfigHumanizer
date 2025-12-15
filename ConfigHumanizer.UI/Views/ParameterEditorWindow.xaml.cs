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

using System.Windows;
using System.Windows.Controls;
using ConfigHumanizer.UI.ViewModels;

namespace ConfigHumanizer.UI.Views;

/// <summary>
/// Fenêtre d'édition de paramètres WYSIWYG.
/// </summary>
public partial class ParameterEditorWindow : Window
{
    private ParameterEditorViewModel? _viewModel;

    /// <summary>
    /// Ligne de configuration générée.
    /// </summary>
    public string? GeneratedLine { get; private set; }

    public ParameterEditorWindow()
    {
        InitializeComponent();
        Loaded += ParameterEditorWindow_Loaded;
    }

    private void ParameterEditorWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _viewModel = DataContext as ParameterEditorViewModel;
        if (_viewModel != null)
        {
            _viewModel.ParameterAdded += ViewModel_ParameterAdded;
            _viewModel.Cancelled += ViewModel_Cancelled;
        }
    }

    private void ViewModel_ParameterAdded(object? sender, ParameterAddedEventArgs e)
    {
        GeneratedLine = e.GeneratedLine;
        DialogResult = true;
        Close();
    }

    private void ViewModel_Cancelled(object? sender, EventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void EnumRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string value && _viewModel?.CurrentValue != null)
        {
            _viewModel.CurrentValue.Value = value;
        }
    }

    private void BoolRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string value && _viewModel?.CurrentValue != null)
        {
            _viewModel.CurrentValue.Value = value == "true";
        }
    }

    private void IntegerTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb && _viewModel?.CurrentValue != null)
        {
            if (long.TryParse(tb.Text, out var value))
            {
                _viewModel.CurrentValue.Value = value;
            }
            else if (string.IsNullOrEmpty(tb.Text))
            {
                _viewModel.CurrentValue.Value = null;
            }
        }
    }

    private void StringTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb && _viewModel?.CurrentValue != null)
        {
            _viewModel.CurrentValue.Value = tb.Text;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        if (_viewModel != null)
        {
            _viewModel.ParameterAdded -= ViewModel_ParameterAdded;
            _viewModel.Cancelled -= ViewModel_Cancelled;
        }
    }
}
