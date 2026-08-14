using Correct_test1.Models;
using Correct_test1.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;


namespace Correct_test1.Core
{

    public static class StandardPartDatabase
    {


        private static List<StandardPart> parts;
        // 严格索引（Trim, case-insensitive）
        private static Dictionary<string, List<StandardPart>> exportStrictIndex =
            new Dictionary<string, List<StandardPart>>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, List<StandardPart>> nationalStrictIndex =
            new Dictionary<string, List<StandardPart>>(StringComparer.OrdinalIgnoreCase);
        // 宽松索引（使用 PartNumberNormalizer.LooseNormalize）
        private static Dictionary<string, List<StandardPart>> exportLooseIndex =
            new Dictionary<string, List<StandardPart>>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, List<StandardPart>> nationalLooseIndex =
            new Dictionary<string, List<StandardPart>>(StringComparer.OrdinalIgnoreCase);
        private static bool loaded;
        private static string loadedPath;
        private static DateTime loadedLastWriteTime;
        private static readonly object loadLock = new object();


        public static void EnsureLoaded()
        {
            lock (loadLock)
            {
                string path = ResolveDatabasePath();
                DateTime lastWriteTime = GetLastWriteTime(path);

                if (loaded &&
                    string.Equals(loadedPath, path, StringComparison.OrdinalIgnoreCase) &&
                    loadedLastWriteTime == lastWriteTime)
                {
                    return;
                }

                Load(path);
            }
        }

        private static string ResolveDatabasePath()
        {
            StandardPartDatabaseConfig config = ReadConfig();

            if (config.UseExternalDatabase &&
                !string.IsNullOrWhiteSpace(config.ExternalDatabasePath) &&
                File.Exists(config.ExternalDatabasePath))
            {
                return config.ExternalDatabasePath;
            }

            if (config.FallbackToLocalDatabase)
            {
                return GetLocalDatabasePath();
            }

            return config.ExternalDatabasePath;
        }

        private static void Load(string path)
        {
            StandardPartExcelReader reader =
                new StandardPartExcelReader();

            try
            {
                parts = reader.Read(path);
            }
            catch (Exception ex)
            {
                string localPath = GetLocalDatabasePath();
                if (string.Equals(path, localPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw;
                }

                AppLogger.Error(ex, "StandardPartDatabase.LoadExternal");
                parts = reader.Read(localPath);
                path = localPath;
            }

            BuildIndex();
            loaded = true;
            loadedPath = path;
            loadedLastWriteTime = GetLastWriteTime(path);
        }

        private static string GetLocalDatabasePath()
        {
            string folder = Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location);
            return Path.Combine(folder, "Resources", "StandardParts.xlsx");
        }

        private static DateTime GetLastWriteTime(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path)
                ? File.GetLastWriteTime(path)
                : DateTime.MinValue;
        }

        private static StandardPartDatabaseConfig ReadConfig()
        {
            StandardPartDatabaseConfig config = new StandardPartDatabaseConfig
            {
                ExternalDatabasePath = @"Z:\图号管理\诺升标准件统一命名.xlsx",
                UseExternalDatabase = true,
                FallbackToLocalDatabase = true
            };

            string folder = Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location);
            string configPath = Path.Combine(
                folder,
                "Configs",
                "StandardPartConfig.json");

            if (!File.Exists(configPath))
                return config;

            string json = File.ReadAllText(configPath);
            Match pathMatch = Regex.Match(
                json,
                "\\\"ExternalDatabasePath\\\"\\s*:\\s*\\\"(?<value>[^\\\"]*)\\\"");
            if (pathMatch.Success)
            {
                config.ExternalDatabasePath = pathMatch.Groups["value"].Value
                    .Replace("\\\\", "\\");
            }

            config.UseExternalDatabase = ReadBoolean(
                json,
                "UseExternalDatabase",
                config.UseExternalDatabase);
            config.FallbackToLocalDatabase = ReadBoolean(
                json,
                "FallbackToLocalDatabase",
                config.FallbackToLocalDatabase);
            return config;
        }

        private static bool ReadBoolean(string json, string name, bool defaultValue)
        {
            Match match = Regex.Match(
                json,
                "\\\"" + name + "\\\"\\s*:\\s*(true|false)",
                RegexOptions.IgnoreCase);
            return match.Success
                ? bool.Parse(match.Groups[1].Value)
                : defaultValue;
        }

        private class StandardPartDatabaseConfig
        {
            public string ExternalDatabasePath { get; set; }

            public bool UseExternalDatabase { get; set; }

            public bool FallbackToLocalDatabase { get; set; }
        }

        private static void BuildIndex()
        {
            // 清空所有索引
            exportStrictIndex.Clear();
            nationalStrictIndex.Clear();
            exportLooseIndex.Clear();
            nationalLooseIndex.Clear();

            foreach (StandardPart part in parts)
            {
                // 严格键: Trim 后（不改变大小写比较，通过 Dictionary 的 comparer 忽略大小写）
                string exportStrictKey = part?.ExportPartNumber == null ? "" : part.ExportPartNumber.Trim();
                if (!string.IsNullOrEmpty(exportStrictKey))
                {
                    List<StandardPart> list;
                    if (!exportStrictIndex.TryGetValue(exportStrictKey, out list))
                    {
                        list = new List<StandardPart>();
                        exportStrictIndex.Add(exportStrictKey, list);
                    }
                    list.Add(part);
                }

                string nationalStrictKey = part?.NationalPartNumber == null ? "" : part.NationalPartNumber.Trim();
                if (!string.IsNullOrEmpty(nationalStrictKey))
                {
                    List<StandardPart> listN;
                    if (!nationalStrictIndex.TryGetValue(nationalStrictKey, out listN))
                    {
                        listN = new List<StandardPart>();
                        nationalStrictIndex.Add(nationalStrictKey, listN);
                    }
                    listN.Add(part);
                }

                // 宽松键: 使用 LooseNormalize
                string exportLooseKey = PartNumberNormalizer.LooseNormalize(part?.ExportPartNumber);
                if (!string.IsNullOrEmpty(exportLooseKey))
                {
                    List<StandardPart> listL;
                    if (!exportLooseIndex.TryGetValue(exportLooseKey, out listL))
                    {
                        listL = new List<StandardPart>();
                        exportLooseIndex.Add(exportLooseKey, listL);
                    }
                    listL.Add(part);
                }

                string nationalLooseKey = PartNumberNormalizer.LooseNormalize(part?.NationalPartNumber);
                if (!string.IsNullOrEmpty(nationalLooseKey))
                {
                    List<StandardPart> listNL;
                    if (!nationalLooseIndex.TryGetValue(nationalLooseKey, out listNL))
                    {
                        listNL = new List<StandardPart>();
                        nationalLooseIndex.Add(nationalLooseKey, listNL);
                    }
                    listNL.Add(part);
                }
            }
        }

        public static List<StandardPart> FindByPartNumber(
            string partNumber)
        {
            EnsureLoaded();
            return FindByPartNumberLoaded(partNumber);
        }

        internal static List<StandardPart> FindByPartNumberLoaded(
            string partNumber)
        {
            if (string.IsNullOrWhiteSpace(partNumber))
            {
                return new List<StandardPart>();
            }

            string strictKey = partNumber.Trim();
            string looseKey =
                PartNumberNormalizer.LooseNormalize(partNumber);

            List<StandardPart> matches;

            if (!string.IsNullOrEmpty(strictKey))
            {
                if (exportStrictIndex.TryGetValue(
                        strictKey,
                        out matches) &&
                    matches.Count > 0)
                {
                    return matches;
                }

                if (nationalStrictIndex.TryGetValue(
                        strictKey,
                        out matches) &&
                    matches.Count > 0)
                {
                    return matches;
                }
            }

            if (!string.IsNullOrEmpty(looseKey))
            {
                if (exportLooseIndex.TryGetValue(
                        looseKey,
                        out matches) &&
                    matches.Count > 0)
                {
                    return matches;
                }

                if (nationalLooseIndex.TryGetValue(
                        looseKey,
                        out matches) &&
                    matches.Count > 0)
                {
                    return matches;
                }
            }

            return new List<StandardPart>();
        }

    }

}