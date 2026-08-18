using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.Readers;

using System;
using System.Collections.Generic;
using System.IO;


namespace Correct_test1.Core
{
    public static class NonStandardPartNumberInspectionCache
    {
        private static readonly object
            SyncRoot =
                new object();


        private static readonly
            Dictionary<string, CacheEntry>
            Cache =
                new Dictionary<string, CacheEntry>(
                    StringComparer.OrdinalIgnoreCase);


        private static readonly TimeSpan
            FailureCacheDuration =
                TimeSpan.FromSeconds(30);


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


                database.CloseInput(
                    true);


                // 这里不再切换全局WorkingDatabase。
                // NonStandardPartNumberLayoutReader及其下游读取器
                // 都显式接收当前database/ObjectId，
                // 因此没有必要在主图检查过程中反复改全局数据库状态。
                NonStandardPartNumberLayoutReader reader =
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
