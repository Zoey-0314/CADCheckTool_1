using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.Readers;

using System;
using System.Collections.Generic;
using System.IO;


namespace Correct_test1.Core
{
    /// <summary>
    /// 非标件号归档DWG读取缓存。
    ///
    /// 同一张归档DWG只解析一次，
    /// 后续直接查询内存中的：
    ///
    /// NS333T|1
    /// NS333T|2
    /// ...
    /// </summary>
    public static class
        NonStandardPartNumberInspectionCache
    {
        private static readonly object
            SyncRoot =
                new object();


        private static readonly
            Dictionary<string, CacheEntry>
            Cache =
                new Dictionary<string, CacheEntry>(
                    StringComparer.OrdinalIgnoreCase);


        /// <summary>
        /// 查询归档DWG中是否存在指定件号。
        ///
        /// 返回值：
        ///
        /// true：
        /// 检查过程正常完成。
        ///
        /// contains：
        /// 是否真正找到。
        ///
        /// false：
        /// 归档DWG读取失败。
        /// </summary>
        public static bool TryContains(
            string filePath,
            string drawingNumber,
            string partSuffix,
            out bool contains,
            out string errorMessage)
        {
            contains =
                false;


            errorMessage =
                "";


            if (string.IsNullOrWhiteSpace(
                    filePath))
            {
                errorMessage =
                    "归档DWG路径为空。";

                return false;
            }


            if (!File.Exists(
                    filePath))
            {
                errorMessage =
                    "归档DWG不存在："
                    + filePath;

                return false;
            }


            string cacheKey =
                BuildCacheKey(
                    filePath);


            CacheEntry entry;


            lock (SyncRoot)
            {
                if (Cache.TryGetValue(
                        cacheKey,
                        out entry))
                {
                    if (!entry.Success)
                    {
                        if (DateTime.UtcNow - entry.CachedAtUtc
                            < FailureCacheDuration)
                        {
                            errorMessage =
                                entry.ErrorMessage;

                            return false;
                        }

                        Cache.Remove(
                            cacheKey);
                    }
                    else
                    {
                        string targetKey =
                            NonStandardPartNumberLayoutReader
                                .BuildKey(
                                    drawingNumber,
                                    partSuffix);

                        contains =
                            entry.PartKeys.Contains(
                                targetKey);

                        return true;
                    }
                }
            }


            //--------------------------------
            // 第一次读取这张归档DWG
            //--------------------------------

            entry =
                Load(
                    filePath);

            entry.CachedAtUtc =
                DateTime.UtcNow;


            lock (SyncRoot)
            {
                Cache[cacheKey] =
                    entry;
            }


            if (!entry.Success)
            {
                errorMessage =
                    entry.ErrorMessage;

                return false;
            }


            string key =
                NonStandardPartNumberLayoutReader
                    .BuildKey(
                        drawingNumber,
                        partSuffix);


            contains =
                entry.PartKeys.Contains(
                    key);


            return true;
        }


        private static CacheEntry Load(
            string filePath)
        {
            CacheEntry result =
                new CacheEntry();


            Database previousDatabase =
                HostApplicationServices
                    .WorkingDatabase;


            Database database =
                null;


            try
            {
                database =
                    new Database(
                        false,
                        true);


                database.ReadDwgFile(
                    filePath,
                    FileOpenMode
                        .OpenForReadAndAllShare,
                    false,
                    "");


                HostApplicationServices
                    .WorkingDatabase =
                        database;


                database.CloseInput(
                    true);


                NonStandardPartNumberLayoutReader
                    reader =
                        new NonStandardPartNumberLayoutReader();


                result.PartKeys =
                    reader.ReadPartKeys(
                        database);


                result.Success =
                    true;


                AppLogger.Info(
                    "非标件号归档DWG读取完成："
                    + Path.GetFileName(
                        filePath)
                    + " Keys="
                    + result.PartKeys.Count,
                    "NonStandardPartNumberInspectionCache");
            }
            catch (Exception ex)
            {
                result.Success =
                    false;


                result.ErrorMessage =
                    ex.Message;


                AppLogger.Error(
                    ex,
                    "NonStandardPartNumberInspectionCache.Load",
                    filePath);
            }
            finally
            {
                //--------------------------------
                // 必须先恢复WorkingDatabase
                //--------------------------------

                try
                {
                    if (previousDatabase != null &&
                        !previousDatabase.IsDisposed)
                    {
                        HostApplicationServices
                            .WorkingDatabase =
                                previousDatabase;
                    }
                }
                catch
                {
                }


                //--------------------------------
                // 再释放归档Database
                //--------------------------------

                if (database != null)
                {
                    try
                    {
                        database.Dispose();
                    }
                    catch
                    {
                    }
                }
            }


            return result;
        }


        /// <summary>
        /// 文件发生修改后自动形成新缓存Key，
        /// 避免一直使用旧数据。
        /// </summary>
        private static string BuildCacheKey(
            string filePath)
        {
            long ticks =
                0;


            try
            {
                ticks =
                    File.GetLastWriteTimeUtc(
                        filePath)
                    .Ticks;
            }
            catch
            {
            }


            return
                filePath
                + "|"
                + ticks;
        }

        private static readonly TimeSpan
    FailureCacheDuration =
        TimeSpan.FromSeconds(30);
        private class CacheEntry
        {
            public DateTime CachedAtUtc
            {
                get;
                set;
            }
            public bool Success
            {
                get;
                set;
            }


            public HashSet<string> PartKeys
            {
                get;
                set;
            }
            =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);


            public string ErrorMessage
            {
                get;
                set;
            }
            =
            "";
        }
    }
}