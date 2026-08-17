using Correct_test1.Configs;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;


namespace Correct_test1.Core
{
    /// <summary>
    /// 非标归档文件索引。
    ///
    /// Z盘只递归扫描一次。
    ///
    /// 建立三套内存索引：
    ///
    /// 1.
    /// DrawingNumber
    /// ->
    /// 所有归档文件
    ///
    /// 用于判断基础归档是否存在。
    ///
    ///
    /// 2.
    /// DrawingNumber
    /// ->
    /// 无项目号DWG
    ///
    /// 用于通用V版本。
    ///
    ///
    /// 3.
    /// DrawingNumber + ProjectNumber
    /// ->
    /// 项目专用DWG
    ///
    /// 用于项目L版本。
    /// </summary>
    public class NonStandardArchiveIndex
    {
        //==================================================
        // 原始完整文件列表
        //
        // 仍然保留。
        //
        // VersionArchiveIndex目前会复用它，
        // 所以不能删除。
        //==================================================

        private readonly
            List<string> _filePaths;


        //==================================================
        // 图号 -> 所有文件
        //
        // 包含：
        // DWG / PDF / 其他文件。
        //
        // 用于原有“归档图是否存在”检查。
        //==================================================

        private readonly
            Dictionary<string, List<string>>
            _filesByDrawingNumber;


        //==================================================
        // 图号 -> 无项目号DWG
        //
        // 例如：
        //
        // NS386DY
        // ->
        // NS386DY-V1.dwg
        // NS386DY-V3.dwg
        //==================================================

        private readonly
            Dictionary<string, List<string>>
            _genericDwgsByDrawingNumber;


        //==================================================
        // 图号|项目号 -> 项目专用DWG
        //
        // 例如：
        //
        // NS386DY|N2607US004
        // ->
        // NS386DY-N2607US004-L0.dwg
        // NS386DY-N2607US004-L2.dwg
        //==================================================

        private readonly
            Dictionary<string, List<string>>
            _projectDwgsByKey;


        //==================================================
        // 文件名开头的NS归档图号
        //
        // 支持：
        //
        // NS386DY
        // NS386DY-V2
        // NS386DY_N2607US004-L0
        // NS386DY N2607US004-L0
        //
        // 图号后面必须：
        //
        // 文件结束
        // 空格
        // -
        // _
        //
        // 防止：
        //
        // 查询NS386DY
        // 错误匹配NS386DYA
        //==================================================

        private static readonly Regex
            DrawingNumberRegex =
                new Regex(
                    @"^\s*(?<drawing>NS[0-9A-Z]+)(?=$|[-_\s])",
                    RegexOptions.IgnoreCase);


        //==================================================
        // 项目号
        //==================================================

        private static readonly Regex
            ProjectNumberRegex =
                new Regex(
                    @"N\d{4}[A-Z]{2}\d{3}",
                    RegexOptions.IgnoreCase);


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
            get
            {
                return
                    _filePaths.Count;
            }
        }


        public int SkippedDirectoryCount
        {
            get;
            private set;
        }


        private NonStandardArchiveIndex()
        {
            _filePaths =
                new List<string>();


            _filesByDrawingNumber =
                new Dictionary<string, List<string>>(
                    StringComparer.OrdinalIgnoreCase);


            _genericDwgsByDrawingNumber =
                new Dictionary<string, List<string>>(
                    StringComparer.OrdinalIgnoreCase);


            _projectDwgsByKey =
                new Dictionary<string, List<string>>(
                    StringComparer.OrdinalIgnoreCase);


            RootPath =
                "";


            ErrorMessage =
                "";
        }


        //==================================================
        // 供VersionArchiveIndex继续复用文件列表
        //==================================================

        public List<string> GetFilePathsSnapshot()
        {
            return
                new List<string>(
                    _filePaths);
        }


        //==================================================
        // 默认路径建立索引
        //==================================================

        public static NonStandardArchiveIndex Build()
        {
            AppPathSettings settings =
                AppPathConfig.Current;


            return
                Build(
                    settings
                        .NonStandardArchivePath);
        }


        //==================================================
        // 正式建立索引
        //==================================================

        public static NonStandardArchiveIndex Build(
            string rootPath)
        {
            NonStandardArchiveIndex index =
                new NonStandardArchiveIndex();


            index.RootPath =
                rootPath ?? "";


            //==================================================
            // 根目录不可访问
            //==================================================

            if (string.IsNullOrWhiteSpace(
                    rootPath))
            {
                index.IsAvailable =
                    false;


                index.ErrorMessage =
                    "非标归档目录为空。";


                return index;
            }


            if (!Directory.Exists(
                    rootPath))
            {
                index.IsAvailable =
                    false;


                index.ErrorMessage =
                    "无法访问非标归档目录："
                    + rootPath;


                return index;
            }


            //==================================================
            // 手动递归
            //
            // 单个子目录失败不会导致整个Z盘索引失败。
            //==================================================

            Stack<string> directories =
                new Stack<string>();


            directories.Push(
                rootPath);


            while (directories.Count > 0)
            {
                string currentDirectory =
                    directories.Pop();


                //==================================================
                // 当前目录文件
                //==================================================

                try
                {
                    string[] files =
                        Directory.GetFiles(
                            currentDirectory,
                            "*",
                            SearchOption
                                .TopDirectoryOnly);


                    foreach (
                        string file
                        in files)
                    {
                        if (string.IsNullOrWhiteSpace(
                                file))
                        {
                            continue;
                        }


                        //--------------------------------
                        // 原始文件列表
                        //--------------------------------

                        index._filePaths.Add(
                            file);


                        //--------------------------------
                        // 同时建立Dictionary索引
                        //--------------------------------

                        index.IndexFile(
                            file);
                    }
                }
                catch (Exception ex)
                {
                    index
                        .SkippedDirectoryCount++;


                    AppLogger.Warn(
                        "读取归档目录文件失败："
                        + currentDirectory
                        + "；"
                        + ex.Message,
                        "NonStandardArchiveIndex");
                }


                //==================================================
                // 子目录
                //==================================================

                try
                {
                    string[] subDirectories =
                        Directory.GetDirectories(
                            currentDirectory,
                            "*",
                            SearchOption
                                .TopDirectoryOnly);


                    foreach (
                        string subDirectory
                        in subDirectories)
                    {
                        if (string.IsNullOrWhiteSpace(
                                subDirectory))
                        {
                            continue;
                        }


                        directories.Push(
                            subDirectory);
                    }
                }
                catch (Exception ex)
                {
                    index
                        .SkippedDirectoryCount++;


                    AppLogger.Warn(
                        "读取归档子目录失败："
                        + currentDirectory
                        + "；"
                        + ex.Message,
                        "NonStandardArchiveIndex");
                }
            }


            index.IsAvailable =
                true;


            AppLogger.Info(
                "非标归档索引建立完成。"
                + " Root="
                + rootPath
                + " Files="
                + index.FileCount
                + " DrawingKeys="
                + index
                    ._filesByDrawingNumber
                    .Count
                + " GenericDwgKeys="
                + index
                    ._genericDwgsByDrawingNumber
                    .Count
                + " ProjectDwgKeys="
                + index
                    ._projectDwgsByKey
                    .Count
                + " SkippedDirectories="
                + index
                    .SkippedDirectoryCount,
                "NonStandardArchiveIndex");


            return index;
        }


        //==================================================
        // 建立单个文件的索引
        //==================================================

        private void IndexFile(
            string filePath)
        {
            if (string.IsNullOrWhiteSpace(
                    filePath))
            {
                return;
            }


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
            // 从文件名开头读取基础图号
            //==================================================

            Match drawingMatch =
                DrawingNumberRegex.Match(
                    fileName);


            if (!drawingMatch.Success)
            {
                return;
            }


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
            // 所有文件：
            //
            // DrawingNumber -> file
            //==================================================

            AddToIndex(
                _filesByDrawingNumber,
                drawingNumber,
                filePath);


            //==================================================
            // 后面两个索引只处理DWG
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


            if (!string.Equals(
                    extension,
                    ".dwg",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }


            //==================================================
            // 判断候选DWG自己的项目号
            //==================================================

            Match projectMatch =
                ProjectNumberRegex.Match(
                    fileName);


            if (!projectMatch.Success)
            {
                //==================================================
                // 无项目号：
                //
                // 通用DWG
                //==================================================

                AddToIndex(
                    _genericDwgsByDrawingNumber,
                    drawingNumber,
                    filePath);


                return;
            }


            //==================================================
            // 项目专用DWG
            //==================================================

            string projectNumber =
                projectMatch
                    .Value
                    .Trim()
                    .ToUpperInvariant();


            string projectKey =
                BuildProjectKey(
                    drawingNumber,
                    projectNumber);


            AddToIndex(
                _projectDwgsByKey,
                projectKey,
                filePath);
        }


        //==================================================
        // Dictionary<string,List<string>> 添加
        //==================================================

        private static void AddToIndex(
            Dictionary<string, List<string>> dictionary,
            string key,
            string filePath)
        {
            if (dictionary == null ||
                string.IsNullOrWhiteSpace(
                    key) ||
                string.IsNullOrWhiteSpace(
                    filePath))
            {
                return;
            }


            List<string> list;


            if (!dictionary.TryGetValue(
                    key,
                    out list))
            {
                list =
                    new List<string>();


                dictionary.Add(
                    key,
                    list);
            }


            list.Add(
                filePath);
        }


        //==================================================
        // 基础归档是否存在
        //
        // 现在是Dictionary O(1)查询。
        //
        // 不再扫描整个_filePaths。
        //==================================================

        public bool Contains(
            string searchKey,
            out string matchedFilePath)
        {
            matchedFilePath =
                "";


            if (!IsAvailable ||
                string.IsNullOrWhiteSpace(
                    searchKey))
            {
                return false;
            }


            string key =
                NormalizeDrawingNumber(
                    searchKey);


            if (string.IsNullOrWhiteSpace(
                    key))
            {
                return false;
            }


            List<string> files;


            if (!_filesByDrawingNumber
                    .TryGetValue(
                        key,
                        out files) ||
                files == null ||
                files.Count == 0)
            {
                return false;
            }


            matchedFilePath =
                files[0];


            return true;
        }


        //==================================================
        // 获取通用无项目号DWG
        //
        // DrawingNumber
        // ->
        // List<DWG>
        //==================================================

        public List<string> GetGenericDwgs(
            string drawingNumber)
        {
            string key =
                NormalizeDrawingNumber(
                    drawingNumber);


            if (!IsAvailable ||
                string.IsNullOrWhiteSpace(
                    key))
            {
                return
                    new List<string>();
            }


            List<string> files;


            if (!_genericDwgsByDrawingNumber
                    .TryGetValue(
                        key,
                        out files) ||
                files == null)
            {
                return
                    new List<string>();
            }


            //--------------------------------
            // 返回副本，
            // 防止调用方修改内部索引。
            //--------------------------------

            return
                new List<string>(
                    files);
        }


        //==================================================
        // 获取项目专用DWG
        //
        // DrawingNumber + ProjectNumber
        // ->
        // List<DWG>
        //==================================================

        public List<string> GetProjectDwgs(
            string drawingNumber,
            string projectNumber)
        {
            string drawingKey =
                NormalizeDrawingNumber(
                    drawingNumber);


            string projectKey =
                NormalizeProjectNumber(
                    projectNumber);


            if (!IsAvailable ||
                string.IsNullOrWhiteSpace(
                    drawingKey) ||
                string.IsNullOrWhiteSpace(
                    projectKey))
            {
                return
                    new List<string>();
            }


            string key =
                BuildProjectKey(
                    drawingKey,
                    projectKey);


            List<string> files;


            if (!_projectDwgsByKey
                    .TryGetValue(
                        key,
                        out files) ||
                files == null)
            {
                return
                    new List<string>();
            }


            return
                new List<string>(
                    files);
        }


        //==================================================
        // Key
        //==================================================

        private static string BuildProjectKey(
            string drawingNumber,
            string projectNumber)
        {
            return
                NormalizeDrawingNumber(
                    drawingNumber)
                + "|"
                + NormalizeProjectNumber(
                    projectNumber);
        }


        private static string NormalizeDrawingNumber(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return "";
            }


            return
                value
                    .Trim()
                    .TrimEnd('_')
                    .ToUpperInvariant();
        }


        private static string NormalizeProjectNumber(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return "";
            }


            Match match =
                ProjectNumberRegex.Match(
                    value);


            return match.Success
                ? match.Value
                    .ToUpperInvariant()
                : "";
        }
    }
}