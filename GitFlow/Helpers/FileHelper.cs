﻿using EnvDTE;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SamirBoulema.TGIT.Helpers
{
    public class FileHelper
    {
        private DTE _dte;

        public FileHelper(DTE dte)
        {
            _dte = dte;
        }

        public string GetTortoiseGitProc()
        {
            return (string) Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\TortoiseGit", "ProcPath", @"C:\Program Files\TortoiseGit\bin\TortoiseGitProc.exe");
        }

        public string GetMSysGit()
        {
            string regPath = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\TortoiseGit", "MSysGit", null);

            if (string.IsNullOrEmpty(regPath))
            {
                return @"C:\Program Files (x86)\Git\bin\git.exe";
            }
            return Path.Combine(regPath, "git.exe");
        }

        public void SaveAllFiles()
        {
            _dte.ExecuteCommand("File.SaveAll");
        }

        public string GetSolutionDir()
        {
            string fileName = _dte.Solution.FullName;
            if (!string.IsNullOrEmpty(fileName))
            {
                var path = Path.GetDirectoryName(fileName);
                return FindGitdir(path);
            }
            return string.Empty;
        }

        private static string FindGitdir(string path)
        {
            try
            {
                var di = new DirectoryInfo(path);
                if (di.GetDirectories().Any(d => d.Name.Equals(".git")))
                {
                    return di.FullName;
                }
                if (di.Parent != null)
                {
                    return FindGitdir(di.Parent.FullName);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "TGIT error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return string.Empty;
        }

        /// <summary>
        /// Get case sensitive path.
        /// http://stackoverflow.com/questions/325931/getting-actual-file-name-with-proper-casing-on-windows-with-net
        /// </summary>
        public string GetExactFileName(string pathName)
        {
            if (!(File.Exists(pathName) || Directory.Exists(pathName)))
                return pathName;

            var di = new DirectoryInfo(pathName);

            if (di.Parent != null)
            {
                return di.Parent.GetFileSystemInfos(di.Name)[0].Name;
            }
            return di.Name.ToUpper();
        }

        public string GetExactPathName(string pathName)
        {
            if (!(File.Exists(pathName) || Directory.Exists(pathName)))
                return pathName;

            var di = new DirectoryInfo(pathName);

            if (di.Parent != null)
            {
                return Path.Combine(
                    GetExactPathName(di.Parent.FullName),
                    di.Parent.GetFileSystemInfos(di.Name)[0].Name);
            }
            else {
                return di.Name.ToUpper();
            }
        }
    }
}
