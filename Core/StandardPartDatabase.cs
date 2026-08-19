using Correct_test1.Configs;
using Correct_test1.Models;
using Correct_test1.Readers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Correct_test1.Core
{
    public static class StandardPartDatabase
    {
        private static List<StandardPart>
            parts =
                new List<StandardPart>();


        // 严格索引

        private static Dictionary<string, List<StandardPart>>
            exportStrictIndex =
                new Dictionary<string, List<StandardPart>>(
                    StringComparer.OrdinalIgnoreCase);


        private static Dictionary<string, List<StandardPart>>
            nationalStrictIndex =
                new Dictionary<string, List<StandardPart>>(
                    StringComparer.OrdinalIgnoreCase);


        // 宽松索引

        private static Dictionary<string, List<StandardPart>>
            exportLooseIndex =
                new Dictionary<string, List<StandardPart>>(
                    StringComparer.OrdinalIgnoreCase);


        private static Dictionary<string, List<StandardPart>>
            nationalLooseIndex =
                new Dictionary<string, List<StandardPart>>(
                    StringComparer.OrdinalIgnoreCase);


        private static bool
            loaded;


        private static string
            loadedPath =
                "";


        private static DateTime
            loadedLastWriteTime =
                DateTime.MinValue;


        private static string
            lastError =
                "";


        private static readonly object
            loadLock =
                new object();


        private static readonly object
            preloadLock =
                new object();


        private static Task
            preloadTask;


        public static bool IsAvailable
        {
            get
            {
                lock (loadLock)
                {
                    return loaded;
                }
            }
        }


        public static string LastError
        {
            get
            {
                lock (loadLock)
                {
                    return
                        lastError ?? "";
                }
            }
        }


        public static string LoadedPath
        {
            get
            {
                lock (loadLock)
                {
                    return
                        loadedPath ?? "";
                }
            }
        }


        /// <summary>
        /// 插件启动时调用。
        /// 后台预加载标准件Excel，
        /// 不阻塞AutoCAD启动。
        /// </summary>
        public static void PreloadAsync()
        {
            lock (preloadLock)
            {
                if (preloadTask != null &&
                    !preloadTask.IsCompleted)
                {
                    return;
                }


                preloadTask =
                    Task.Run(
                        () =>
                        {
                            string error;


                            bool success =
                                TryEnsureLoaded(
                                    out error);


                            if (success)
                            {
                                AppLogger.Info(
                                    "标准件数据库后台预加载完成。"
                                    + " Path="
                                    + LoadedPath,
                                    "StandardPartDatabase");
                            }
                            else
                            {
                                AppLogger.Warn(
                                    "标准件数据库后台预加载失败："
                                    + error,
                                    "StandardPartDatabase");
                            }
                        });
            }
        }


        /// <summary>
        /// 用户修改路径以后调用。
        /// 旧预加载如果还没完成，
        /// 新任务会等待旧任务结束后，
        /// 再按照最新配置重新加载。
        /// </summary>
        public static void RefreshAsync()
        {
            lock (preloadLock)
            {
                Task previous =
                    preloadTask;


                preloadTask =
                    Task.Run(
                        () =>
                        {
                            if (previous != null)
                            {
                                try
                                {
                                    previous.Wait();
                                }
                                catch
                                {
                                }
                            }


                            lock (loadLock)
                            {
                                loaded =
                                    false;

                                loadedPath =
                                    "";

                                loadedLastWriteTime =
                                    DateTime.MinValue;

                                lastError =
                                    "";
                            }


                            string error;


                            bool success =
                                TryEnsureLoaded(
                                    out error);


                            if (success)
                            {
                                AppLogger.Info(
                                    "标准件数据库刷新完成。"
                                    + " Path="
                                    + LoadedPath,
                                    "StandardPartDatabase");
                            }
                            else
                            {
                                AppLogger.Warn(
                                    "标准件数据库刷新失败："
                                    + error,
                                    "StandardPartDatabase");
                            }
                        });
            }
        }


        /// <summary>
        /// 安全加载。
        /// 失败不会把所有标准件误判成
        /// “标准件库未收录”。
        /// </summary>
        public static bool TryEnsureLoaded(
            out string error)
        {
            try
            {
                EnsureLoaded();


                lock (loadLock)
                {
                    error =
                        lastError ?? "";


                    return loaded;
                }
            }
            catch (Exception ex)
            {
                lock (loadLock)
                {
                    loaded =
                        false;


                    lastError =
                        ex.Message;
                }


                AppLogger.Error(
                    ex,
                    "StandardPartDatabase.TryEnsureLoaded");


                error =
                    ex.Message;


                return false;
            }
        }


        public static void EnsureLoaded()
        {
            lock (loadLock)
            {
                string path =
                    AppPathConfig
                        .Current
                        .StandardPartDatabasePath;


                if (string.IsNullOrWhiteSpace(
                        path))
                {
                    throw new InvalidOperationException(
                        "标准件数据库路径为空。");
                }


                if (!File.Exists(
                        path))
                {
                    throw new FileNotFoundException(
                        "无法访问标准件数据库："
                        + path,
                        path);
                }


                DateTime lastWriteTime =
                    File.GetLastWriteTime(
                        path);


                // 已经加载，而且文件没有变化。

                if (loaded &&
                    string.Equals(
                        loadedPath,
                        path,
                        StringComparison
                            .OrdinalIgnoreCase) &&
                    loadedLastWriteTime ==
                        lastWriteTime)
                {
                    return;
                }


                // 正式读取Excel

                StandardPartExcelReader reader =
                    new StandardPartExcelReader();


                List<StandardPart> loadedParts =
                    reader.Read(
                        path);


                if (loadedParts == null)
                {
                    loadedParts =
                        new List<StandardPart>();
                }


                parts =
                    loadedParts;


                BuildIndex();


                loaded =
                    true;


                loadedPath =
                    path;


                loadedLastWriteTime =
                    lastWriteTime;


                lastError =
                    "";


                AppLogger.Info(
                    "标准件数据库加载完成。"
                    + " Count="
                    + parts.Count
                    + " Path="
                    + path,
                    "StandardPartDatabase");
            }
        }


        private static void BuildIndex()
        {
            exportStrictIndex.Clear();
            nationalStrictIndex.Clear();
            exportLooseIndex.Clear();
            nationalLooseIndex.Clear();


            foreach (
                StandardPart part
                in parts)
            {
                string exportStrictKey =
                    part == null ||
                    part.ExportPartNumber == null
                        ? ""
                        : part
                            .ExportPartNumber
                            .Trim();


                if (!string.IsNullOrEmpty(
                        exportStrictKey))
                {
                    List<StandardPart> list;


                    if (!exportStrictIndex
                        .TryGetValue(
                            exportStrictKey,
                            out list))
                    {
                        list =
                            new List<StandardPart>();


                        exportStrictIndex.Add(
                            exportStrictKey,
                            list);
                    }


                    list.Add(
                        part);
                }


                string nationalStrictKey =
                    part == null ||
                    part.NationalPartNumber == null
                        ? ""
                        : part
                            .NationalPartNumber
                            .Trim();


                if (!string.IsNullOrEmpty(
                        nationalStrictKey))
                {
                    List<StandardPart> list;


                    if (!nationalStrictIndex
                        .TryGetValue(
                            nationalStrictKey,
                            out list))
                    {
                        list =
                            new List<StandardPart>();


                        nationalStrictIndex.Add(
                            nationalStrictKey,
                            list);
                    }


                    list.Add(
                        part);
                }


                string exportLooseKey =
                    PartNumberNormalizer
                        .LooseNormalize(
                            part == null
                                ? null
                                : part
                                    .ExportPartNumber);


                if (!string.IsNullOrEmpty(
                        exportLooseKey))
                {
                    List<StandardPart> list;


                    if (!exportLooseIndex
                        .TryGetValue(
                            exportLooseKey,
                            out list))
                    {
                        list =
                            new List<StandardPart>();


                        exportLooseIndex.Add(
                            exportLooseKey,
                            list);
                    }


                    list.Add(
                        part);
                }


                string nationalLooseKey =
                    PartNumberNormalizer
                        .LooseNormalize(
                            part == null
                                ? null
                                : part
                                    .NationalPartNumber);


                if (!string.IsNullOrEmpty(
                        nationalLooseKey))
                {
                    List<StandardPart> list;


                    if (!nationalLooseIndex
                        .TryGetValue(
                            nationalLooseKey,
                            out list))
                    {
                        list =
                            new List<StandardPart>();


                        nationalLooseIndex.Add(
                            nationalLooseKey,
                            list);
                    }


                    list.Add(
                        part);
                }
            }
        }


        internal static List<StandardPart>
            FindByPartNumberLoaded(
                string partNumber)
        {
            lock (loadLock)
            {
                if (!loaded ||
                    string.IsNullOrWhiteSpace(
                        partNumber))
                {
                    return
                        new List<StandardPart>();
                }


                string strictKey =
                    partNumber.Trim();


                string looseKey =
                    PartNumberNormalizer
                        .LooseNormalize(
                            partNumber);


                List<StandardPart> matches;


                if (!string.IsNullOrEmpty(
                        strictKey))
                {
                    if (exportStrictIndex
                            .TryGetValue(
                                strictKey,
                                out matches) &&
                        matches.Count > 0)
                    {
                        return matches;
                    }


                    if (nationalStrictIndex
                            .TryGetValue(
                                strictKey,
                                out matches) &&
                        matches.Count > 0)
                    {
                        return matches;
                    }
                }


                if (!string.IsNullOrEmpty(
                        looseKey))
                {
                    if (exportLooseIndex
                            .TryGetValue(
                                looseKey,
                                out matches) &&
                        matches.Count > 0)
                    {
                        return matches;
                    }


                    if (nationalLooseIndex
                            .TryGetValue(
                                looseKey,
                                out matches) &&
                        matches.Count > 0)
                    {
                        return matches;
                    }
                }


                return
                    new List<StandardPart>();
            }
        }
    }
}
