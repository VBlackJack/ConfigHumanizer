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

using System.IO;
using ConfigHumanizer.UI.Services.Interfaces;
using Microsoft.Win32;

namespace ConfigHumanizer.UI.Services;

public class FileService : IFileService
{
    /// <summary>
    /// Maximum file size allowed (10 MB) to prevent DoS attacks and memory issues.
    /// </summary>
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    public string? OpenFile()
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Open Configuration File",
            Filter = "Config Files (*.conf;*_config;*.cfg)|*.conf;*_config;*.cfg|All Files (*.*)|*.*",
            FilterIndex = 1,
            CheckFileExists = true,
            CheckPathExists = true
        };

        return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
    }

    public string ReadFile(string path)
    {
        // Validate path
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("File path cannot be empty.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"File not found: {path}", path);
        }

        // Check file size to prevent DoS
        var fileInfo = new FileInfo(path);
        if (fileInfo.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"File too large ({fileInfo.Length / 1024.0 / 1024.0:F2} MB). Maximum allowed size is {MaxFileSizeBytes / 1024.0 / 1024.0:F0} MB.");
        }

        return File.ReadAllText(path);
    }

    public void SaveFile(string path, string content)
    {
        // Validate path
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("File path cannot be empty.", nameof(path));
        }

        // Validate content size
        if (content != null && content.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"Content too large. Maximum allowed size is {MaxFileSizeBytes / 1024.0 / 1024.0:F0} MB.");
        }

        // Create backup if file exists
        if (File.Exists(path))
        {
            var backupPath = path + ".bak";
            File.Copy(path, backupPath, overwrite: true);
        }

        // Write the new content
        File.WriteAllText(path, content ?? string.Empty);
    }
}
