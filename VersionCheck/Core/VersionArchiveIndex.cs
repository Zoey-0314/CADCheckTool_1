using Correct_test1.Configs;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Correct_test1.VersionCheck.Core
{
    /// <summary>
    /// 版本号归档索引。
    ///
    /// 标准件：
    /// DrawingNumber -> 最大V
    ///
    /// 非标件：
    /// DrawingNumber + ProjectNumber -> 最大L
    /// </summary>
    public class VersionArchiveIndex
    {
        private readonly
            Dictionary<string, LatestVersionEntry>
            _standardVersions;


        private readonly
            Dictionary<string, LatestVersionEntry>
            _nonStandardVersions;


        public string RootPath
        {
            get;
            private set;
        }


        public bool IsAvailable
        {
            get;
            private set;
        }


        public string ErrorMessage
        {
            get;
            private set;
        }


        public int FileCount
        {
            get;
            private set;
        }


        private VersionArchiveIndex()
        {
            _standardVersions =
                new Dictionary<string, LatestVersionEntry>(
                    StringComparer.OrdinalIgnoreCase);


            _nonStandardVersions =
                new Dictionary<string, LatestVersionEntry>(
                    StringComparer.OrdinalIgnoreCase);


            RootPath = "";
            ErrorMessage = "";
        }


        public static VersionArchiveIndex Build()
        {
            AppPathSettings settings =
                AppPathConfig.Current;


            return Build(
                settings.VersionArchivePath);
        }


        /// <summary>
        /// 自己扫描目录。
        ///
        /// 只有版本路径和原非标归档路径不同时
        /// 才需要走这里。
        /// </summary>
        public static VersionArchiveIndex Build(
            string rootPath)
        {
            VersionArchiveIndex index =
                new VersionArchiveIndex();


            index.RootPath =
                rootPath ?? "";


            if (string.IsNullOrWhiteSpace(
                    rootPath))
            {
                index.ErrorMessage =
                    "版本检查归档目录为空。";

                return index;
            }


            if (!Directory.Exists(
                    rootPath))
            {
                index.ErrorMessage =
                    "无法访问版本检查归档目录："
                    + rootPath;

                return index;
            }


            List<string> files =
                new List<string>();


            Stack<string> directories =
                new Stack<string>();


            directories.Push(
                rootPath);


            while (directories.Count > 0)
            {
                string directory =
                    directories.Pop();


                try
                {
                    string[] currentFiles =
                        Directory.GetFiles(
                            directory,
                            "*",
                            SearchOption.TopDirectoryOnly);


                    files.AddRange(
                        currentFiles);
                }
                catch
                {
                }


                try
                {
                    string[] children =
                        Directory.GetDirectories(
                            directory,
                            "*",
                            SearchOption.TopDirectoryOnly);


                    foreach (string child in children)
                    {
                        directories.Push(
                            child);
                    }
                }
                catch
                {
                }
            }


            return BuildFromFilePaths(
                rootPath,
                files);
        }


        /// <summary>
        /// 直接用已有文件列表建立版本索引。
        ///
        /// 用于复用NonStandardArchiveIndex，
        /// 避免重复扫描Z盘。
        /// </summary>
        public static VersionArchiveIndex
            BuildFromFilePaths(
                string rootPath,
                IEnumerable<string> filePaths)
        {
            VersionArchiveIndex index =
                new VersionArchiveIndex();


            index.RootPath =
                rootPath ?? "";


            if (filePaths == null)
            {
                index.ErrorMessage =
                    "版本归档文件列表为空。";

                return index;
            }


            foreach (string filePath in filePaths)
            {
                if (string.IsNullOrWhiteSpace(
                        filePath))
                {
                    continue;
                }


                index.FileCount++;


                index.AddFile(
                    filePath);
            }


            index.IsAvailable =
                true;


            return index;
        }


        private void AddFile(
    string filePath)
        {
            //==================================================
            // 只认DWG和PDF
            //==================================================

            string extension;


            try
            {
                extension =
                    Path.GetExtension(
                        filePath);
            }
            catch
            {
                return;
            }


            bool isDwg =
                string.Equals(
                    extension,
                    ".dwg",
                    StringComparison.OrdinalIgnoreCase);


            bool isPdf =
                string.Equals(
                    extension,
                    ".pdf",
                    StringComparison.OrdinalIgnoreCase);


            if (!isDwg &&
                !isPdf)
            {
                return;
            }


            //==================================================
            // 文件名
            //==================================================

            string fileName;


            try
            {
                fileName =
                    Path.GetFileNameWithoutExtension(
                        filePath);
            }
            catch
            {
                return;
            }


            if (string.IsNullOrWhiteSpace(
                    fileName))
            {
                return;
            }


            //==================================================
            // 图号
            //
            // 文件名第一个字段。
            //
            // NS103AK 加强筋 ...
            // ↓
            // NS103AK
            //==================================================

            Match drawingMatch =
                Regex.Match(
                    fileName,
                    @"^\s*(?<drawing>\S+)");


            if (!drawingMatch.Success)
                return;


            string drawingNumber =
                drawingMatch
                    .Groups["drawing"]
                    .Value
                    .Trim()
                    .ToUpperInvariant();


            if (string.IsNullOrWhiteSpace(
                    drawingNumber))
            {
                return;
            }


            //==================================================
            // 有没有项目号
            //==================================================

            Match projectMatch =
                Regex.Match(
                    fileName,
                    @"N\d{4}[A-Z]{2}\d{3}",
                    RegexOptions.IgnoreCase);


            //==================================================
            // 有项目号：
            // 非标L版本
            //==================================================

            if (projectMatch.Success)
            {
                string projectNumber =
                    projectMatch
                        .Value
                        .ToUpperInvariant();


                //--------------------------------
                // 只在项目号后面找版本号
                //--------------------------------

                string afterProject =
                    fileName.Substring(
                        projectMatch.Index +
                        projectMatch.Length);


                //--------------------------------
                // 支持：
                //
                // N2604US003-L1
                // N2604US003 L1
                // N2604US003-PE1-L1
                // N2604US003-PE1 L1
                // N2604US003_PE1_L1
                //
                // 并允许L1后面继续有其他文字。
                //--------------------------------

                Match versionMatch =
                    Regex.Match(
                        afterProject,
                        @"(?:^|[-_\s])L(?<version>\d+)(?=$|[-_\s])",
                        RegexOptions.IgnoreCase);


                if (!versionMatch.Success)
                    return;


                int version;


                if (!int.TryParse(
                        versionMatch
                            .Groups["version"]
                            .Value,
                        out version))
                {
                    return;
                }


                string key =
                    BuildNonStandardKey(
                        drawingNumber,
                        projectNumber);


                AddLatest(
                    _nonStandardVersions,
                    key,
                    version,
                    filePath);


                return;
            }


            //==================================================
            // 没项目号：
            // 标准件V版本
            //==================================================

            Match standardVersionMatch =
                Regex.Match(
                    fileName,
                    @"(?:^|[-_\s])V(?<version>\d+)(?=$|[-_\s])",
                    RegexOptions.IgnoreCase);


            if (!standardVersionMatch.Success)
                return;


            int standardVersion;


            if (!int.TryParse(
                    standardVersionMatch
                        .Groups["version"]
                        .Value,
                    out standardVersion))
            {
                return;
            }


            AddLatest(
                _standardVersions,
                drawingNumber,
                standardVersion,
                filePath);
        }


        private static void AddLatest(
            Dictionary<string, LatestVersionEntry>
                dictionary,
            string key,
            int version,
            string filePath)
        {
            LatestVersionEntry current;


            if (dictionary.TryGetValue(
                    key,
                    out current))
            {
                if (version <=
                    current.Version)
                {
                    return;
                }
            }


            dictionary[key] =
                new LatestVersionEntry
                {
                    Version =
                        version,

                    FilePath =
                        filePath
                };
        }


        public bool TryGetLatestStandard(
            string drawingNumber,
            out int version,
            out string filePath)
        {
            version = -1;
            filePath = "";


            if (!IsAvailable ||
                string.IsNullOrWhiteSpace(
                    drawingNumber))
            {
                return false;
            }


            LatestVersionEntry entry;


            if (!_standardVersions.TryGetValue(
                    drawingNumber
                        .Trim()
                        .ToUpperInvariant(),
                    out entry))
            {
                return false;
            }


            version =
                entry.Version;


            filePath =
                entry.FilePath ?? "";


            return true;
        }


        public bool TryGetLatestNonStandard(
            string drawingNumber,
            string projectNumber,
            out int version,
            out string filePath)
        {
            version = -1;
            filePath = "";


            if (!IsAvailable ||
                string.IsNullOrWhiteSpace(
                    drawingNumber) ||
                string.IsNullOrWhiteSpace(
                    projectNumber))
            {
                return false;
            }


            string key =
                BuildNonStandardKey(
                    drawingNumber,
                    projectNumber);


            LatestVersionEntry entry;


            if (!_nonStandardVersions.TryGetValue(
                    key,
                    out entry))
            {
                return false;
            }


            version =
                entry.Version;


            filePath =
                entry.FilePath ?? "";


            return true;
        }


        private static string
            BuildNonStandardKey(
                string drawingNumber,
                string projectNumber)
        {
            return
                drawingNumber
                    .Trim()
                    .ToUpperInvariant()
                + "|"
                + projectNumber
                    .Trim()
                    .ToUpperInvariant();
        }


        private class LatestVersionEntry
        {
            public int Version
            {
                get;
                set;
            }


            public string FilePath
            {
                get;
                set;
            }
        }
    }
}