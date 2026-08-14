using Correct_test1.Configs;

using System;
using System.Collections.Generic;
using System.IO;

namespace Correct_test1.Core
{
    /// <summary>
    /// 非标归档文件索引。
    ///
    /// 一次递归扫描归档目录，
    /// 后续所有NS图号均在内存中查询。
    /// </summary>
    public class NonStandardArchiveIndex
    {
        private readonly List<string>
            _filePaths;

        private readonly Dictionary<string, string>
            _matchCache;


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
                return _filePaths.Count;
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


            _matchCache =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);


            RootPath = "";
            ErrorMessage = "";
        }


        /// <summary>
        /// 使用默认归档目录建立索引。
        /// </summary>
        public static NonStandardArchiveIndex Build()
        {
            return Build(
                NonStandardArchiveConfig
                    .ArchiveRootPath);
        }


        /// <summary>
        /// 建立归档文件索引。
        ///
        /// 会递归进入所有子文件夹。
        /// </summary>
        public static NonStandardArchiveIndex Build(
            string rootPath)
        {
            NonStandardArchiveIndex index =
                new NonStandardArchiveIndex();


            index.RootPath =
                rootPath ?? "";


            //--------------------------------
            // 根目录本身不可访问：
            //
            // 整个归档检查不可用。
            //
            // 注意：
            // 这种情况绝不能把所有NS判成不存在。
            //--------------------------------

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


            //--------------------------------
            // 手动递归。
            //
            // 不直接使用：
            //
            // Directory.GetFiles(
            //     root,
            //     "*",
            //     SearchOption.AllDirectories)
            //
            // 原因：
            // 只要某个深层目录没有权限，
            // 整个递归就可能直接抛异常终止。
            //
            // 这里按目录逐层处理，
            // 某个子目录失败只跳过该目录。
            //--------------------------------

            Stack<string> directories =
                new Stack<string>();


            directories.Push(
                rootPath);


            while (directories.Count > 0)
            {
                string currentDirectory =
                    directories.Pop();


                //--------------------------------
                // 当前目录下的文件
                //--------------------------------

                try
                {
                    string[] files =
                        Directory.GetFiles(
                            currentDirectory,
                            "*",
                            SearchOption.TopDirectoryOnly);


                    foreach (string file in files)
                    {
                        if (string.IsNullOrWhiteSpace(
                                file))
                        {
                            continue;
                        }


                        index._filePaths.Add(
                            file);
                    }
                }
                catch (Exception ex)
                {
                    index.SkippedDirectoryCount++;


                    AppLogger.Warn(
                        "读取归档目录文件失败："
                        + currentDirectory
                        + "；"
                        + ex.Message,
                        "NonStandardArchiveIndex");
                }


                //--------------------------------
                // 当前目录下的子文件夹
                //--------------------------------

                try
                {
                    string[] subDirectories =
                        Directory.GetDirectories(
                            currentDirectory,
                            "*",
                            SearchOption.TopDirectoryOnly);


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
                    index.SkippedDirectoryCount++;


                    AppLogger.Warn(
                        "读取归档子目录失败："
                        + currentDirectory
                        + "；"
                        + ex.Message,
                        "NonStandardArchiveIndex");
                }
            }


            //--------------------------------
            // 根目录能正常进入，
            // 就认为归档索引可用。
            //--------------------------------

            index.IsAvailable =
                true;


            AppLogger.Info(
                "非标归档索引建立完成。"
                + " Root="
                + rootPath
                + " Files="
                + index.FileCount
                + " SkippedDirectories="
                + index.SkippedDirectoryCount,
                "NonStandardArchiveIndex");


            return index;
        }


        /// <summary>
        /// 查询归档文件名中是否包含指定图号。
        ///
        /// 只比较文件名，
        /// 不比较完整目录路径。
        ///
        /// 忽略大小写。
        /// </summary>
        public bool Contains(
            string searchKey,
            out string matchedFilePath)
        {
            matchedFilePath =
                "";


            if (!IsAvailable)
                return false;


            if (string.IsNullOrWhiteSpace(
                    searchKey))
            {
                return false;
            }


            string key =
                searchKey.Trim();


            //--------------------------------
            // 优先查缓存。
            //
            // 相同NS图号在多张图中反复出现时，
            // 不需要重新遍历整个文件列表。
            //--------------------------------

            string cachedResult;


            if (_matchCache.TryGetValue(
                    key,
                    out cachedResult))
            {
                matchedFilePath =
                    cachedResult ?? "";


                return
                    !string.IsNullOrEmpty(
                        matchedFilePath);
            }


            //--------------------------------
            // 文件名Contains匹配
            //--------------------------------

            foreach (
                string filePath
                in _filePaths)
            {
                string fileName;


                try
                {
                    fileName =
                        Path.GetFileName(
                            filePath);
                }
                catch
                {
                    continue;
                }


                if (string.IsNullOrWhiteSpace(
                        fileName))
                {
                    continue;
                }


                if (fileName.IndexOf(
                        key,
                        StringComparison.OrdinalIgnoreCase)
                    < 0)
                {
                    continue;
                }


                //--------------------------------
                // 找到
                //--------------------------------

                matchedFilePath =
                    filePath;


                _matchCache[key] =
                    filePath;


                return true;
            }


            //--------------------------------
            // 没找到也缓存。
            //
            // 用空字符串表示未匹配。
            //--------------------------------

            _matchCache[key] =
                "";


            return false;
        }
    }
}