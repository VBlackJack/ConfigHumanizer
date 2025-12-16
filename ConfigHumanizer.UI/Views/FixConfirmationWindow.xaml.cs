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
using ICSharpCode.AvalonEdit.Highlighting;

namespace ConfigHumanizer.UI.Views;

/// <summary>
/// Confirmation dialog for applying fixes with visual diff.
/// </summary>
public partial class FixConfirmationWindow : Window
{
    public FixConfirmationWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Sets up the dialog with the original and proposed content, and the reason for the fix.
    /// </summary>
    /// <param name="original">The original line of configuration.</param>
    /// <param name="proposed">The proposed fix.</param>
    /// <param name="reason">The reason for the fix.</param>
    /// <param name="syntaxHighlighting">Optional syntax highlighting name (default: "XML").</param>
    public void Setup(string original, string proposed, string reason, string syntaxHighlighting = "XML")
    {
        FixReasonText.Text = reason;
        OriginalEditor.Text = original;
        ProposedEditor.Text = proposed;

        // Set syntax highlighting for both editors
        var highlighting = HighlightingManager.Instance.GetDefinition(syntaxHighlighting);
        if (highlighting != null)
        {
            OriginalEditor.SyntaxHighlighting = highlighting;
            ProposedEditor.SyntaxHighlighting = highlighting;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
