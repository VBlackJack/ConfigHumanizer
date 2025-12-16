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
using System.Runtime.InteropServices;
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
            Title = "Ouvrir un fichier de configuration",
            Filter = "Tous les fichiers de config|*.conf;*.cfg;*.ini;*.yaml;*.yml;*.json;*.xml;*.cnf;*.tf;*.tfvars;*.hcl;*.service;*.txt;*_config;sshd_config;sudoers;hosts;fstab;crontab;*.rules|" +
                     "SSH (sshd_config)|*sshd_config*;*ssh_config*|" +
                     "YAML (*.yaml;*.yml)|*.yaml;*.yml|" +
                     "JSON (*.json)|*.json|" +
                     "INI/CFG (*.ini;*.cfg;*.conf;*.cnf)|*.ini;*.cfg;*.conf;*.cnf|" +
                     "Terraform (*.tf;*.tfvars;*.hcl)|*.tf;*.tfvars;*.hcl|" +
                     "Systemd (*.service)|*.service;*.socket;*.timer|" +
                     "XML (*.xml;*.config)|*.xml;*.config|" +
                     "Texte (*.txt)|*.txt|" +
                     "Tous les fichiers (*.*)|*.*",
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

    public string? OpenFolder()
    {
        var dialog = (IFileOpenDialog)new FileOpenDialogCoClass();
        try
        {
            dialog.SetOptions(FOS.FOS_PICKFOLDERS | FOS.FOS_FORCEFILESYSTEM);
            dialog.SetTitle("Select a folder containing configuration files");

            if (dialog.Show(IntPtr.Zero) == 0)
            {
                dialog.GetResult(out var item);
                item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var path);
                return path;
            }
        }
        finally
        {
            Marshal.ReleaseComObject(dialog);
        }

        return null;
    }

    #region COM Interop for Folder Browser Dialog
    [ComImport]
    [Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
    private class FileOpenDialogCoClass { }

    [ComImport]
    [Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileOpenDialog
    {
        [PreserveSig] int Show(IntPtr parent);
        void SetFileTypes();
        void SetFileTypeIndex(uint iFileType);
        void GetFileTypeIndex(out uint piFileType);
        void Advise();
        void Unadvise(uint dwCookie);
        void SetOptions(FOS fos);
        void GetOptions(out FOS pfos);
        void SetDefaultFolder(IShellItem psi);
        void SetFolder(IShellItem psi);
        void GetFolder(out IShellItem ppsi);
        void GetCurrentSelection(out IShellItem ppsi);
        void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
        void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
        void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
        void GetResult(out IShellItem ppsi);
        void AddPlace(IShellItem psi, int fdap);
        void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
        void Close(int hr);
        void SetClientGuid(ref Guid guid);
        void ClearClientData();
        void SetFilter([MarshalAs(UnmanagedType.Interface)] object pFilter);
        void GetResults(out IShellItemArray ppenum);
        void GetSelectedItems(out IShellItemArray ppsai);
    }

    [ComImport]
    [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        void BindToHandler();
        void GetParent();
        void GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
        void GetAttributes();
        void Compare();
    }

    [ComImport]
    [Guid("b63ea76d-1f85-456f-a19c-48159efa858b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemArray { }

    [Flags]
    private enum FOS : uint
    {
        FOS_PICKFOLDERS = 0x20,
        FOS_FORCEFILESYSTEM = 0x40
    }

    private enum SIGDN : uint
    {
        SIGDN_FILESYSPATH = 0x80058000
    }
    #endregion
}
