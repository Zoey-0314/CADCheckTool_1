using Correct_test1.Core;

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Correct_test1.Configs
{
    /// <summary>
    /// CADCheckTool统一路径配置。
    /// 配置保存在：
    /// %APPDATA%
    /// \Correct_test1
    /// \AppPathSettings.json
    /// 不放插件安装目录，
    /// 避免Program Files写权限问题。
    /// </summary>
    public static class AppPathConfig
    {
        private static readonly object
            SyncRoot =
                new object();


        private static AppPathSettings
            _current;


        // 默认路径

        public const string
            DefaultNonStandardArchivePath =
                @"Z:\归档图纸";


        public const string
            DefaultStandardPartDatabasePath =
                @"Z:\图号管理\诺升标准件统一命名.xlsx";

        public const string
            DefaultVersionArchivePath =
                @"Z:\归档图纸";


        /// <summary>
        /// 用户配置文件。
        /// </summary>
        public static string ConfigFilePath
        {
            get
            {
                string folder =
                    Path.Combine(
                        Environment.GetFolderPath(
                            Environment.SpecialFolder
                                .ApplicationData),
                        "Correct_test1");


                return
                    Path.Combine(
                        folder,
                        "AppPathSettings.json");
            }
        }


        /// <summary>
        /// 获取当前配置。
        /// 返回副本，避免调用者直接修改内部缓存。
        /// </summary>
        public static AppPathSettings Current
        {
            get
            {
                lock (SyncRoot)
                {
                    if (_current == null)
                    {
                        _current =
                            LoadInternal();
                    }


                    return
                        _current.Clone();
                }
            }
        }


        /// <summary>
        /// 插件初始化时调用。
        /// 保证配置提前读取。
        /// </summary>
        public static void Initialize()
        {
            AppPathSettings settings =
                Current;
        }


        /// <summary>
        /// 保存用户配置。
        /// </summary>
        public static void Save(
            AppPathSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(
                    "settings");
            }


            AppPathSettings normalized =
                new AppPathSettings
                {
                    NonStandardArchivePath =
                        NormalizePath(
                            settings
                                .NonStandardArchivePath),

                    StandardPartDatabasePath =
                        NormalizePath(
                            settings
                                .StandardPartDatabasePath),

                    VersionArchivePath =
                        NormalizePath(
                            settings
                                .VersionArchivePath)
                };


            if (string.IsNullOrWhiteSpace(
                    normalized
                        .NonStandardArchivePath))
            {
                throw new InvalidOperationException(
                    "非标归档路径不能为空。");
            }


            if (string.IsNullOrWhiteSpace(
                    normalized
                        .StandardPartDatabasePath))
            {
                throw new InvalidOperationException(
                    "标准件数据库路径不能为空。");
            }
            if (string.IsNullOrWhiteSpace(
        normalized
            .VersionArchivePath))
            {
                throw new InvalidOperationException(
                    "版本检查归档路径不能为空。");
            }


            lock (SyncRoot)
            {
                WriteInternal(
                    normalized);


                _current =
                    normalized;
            }


            AppLogger.Info(
                "路径配置已保存。"
                + " Archive="
                + normalized.NonStandardArchivePath
                + " StandardPart="
                + normalized.StandardPartDatabasePath,
                "AppPathConfig");
        }


        private static AppPathSettings
            LoadInternal()
        {
            AppPathSettings defaults =
                CreateDefault();


            string path =
                ConfigFilePath;


            // 第一次运行：
            //
            // 自动生成默认配置文件。

            if (!File.Exists(path))
            {
                try
                {
                    WriteInternal(
                        defaults);
                }
                catch (Exception ex)
                {
                    AppLogger.Error(
                        ex,
                        "AppPathConfig.CreateDefault");
                }


                return defaults;
            }


            try
            {
                string json =
                    File.ReadAllText(
                        path,
                        Encoding.UTF8);


                string archivePath =
                    ReadJsonString(
                        json,
                        "NonStandardArchivePath");


                string standardPartPath =
                    ReadJsonString(
                        json,
                        "StandardPartDatabasePath");

                string versionArchivePath =
    ReadJsonString(
        json,
        "VersionArchivePath");


                return new AppPathSettings
                {
                    NonStandardArchivePath =
        string.IsNullOrWhiteSpace(
            archivePath)
            ? defaults.NonStandardArchivePath
            : NormalizePath(
                archivePath),

                    StandardPartDatabasePath =
        string.IsNullOrWhiteSpace(
            standardPartPath)
            ? defaults.StandardPartDatabasePath
            : NormalizePath(
                standardPartPath),

                    VersionArchivePath =
        string.IsNullOrWhiteSpace(
            versionArchivePath)
            ? defaults.VersionArchivePath
            : NormalizePath(
                versionArchivePath)
                };
            }
            catch (Exception ex)
            {
                // 配置损坏不能导致插件加载失败。
                //
                // 回退的是“默认路径”，
                // 不是旧标准件Excel文件。

                AppLogger.Error(
                    ex,
                    "AppPathConfig.Load");


                return defaults;
            }
        }


        private static AppPathSettings
    CreateDefault()
        {
            return new AppPathSettings
            {
                NonStandardArchivePath =
                    DefaultNonStandardArchivePath,

                StandardPartDatabasePath =
                    DefaultStandardPartDatabasePath,

                VersionArchivePath =
                    DefaultVersionArchivePath
            };
        }


        private static void WriteInternal(
            AppPathSettings settings)
        {
            string filePath =
                ConfigFilePath;


            string folder =
                Path.GetDirectoryName(
                    filePath);


            if (!Directory.Exists(
                    folder))
            {
                Directory.CreateDirectory(
                    folder);
            }


            string json =
                "{"
                + Environment.NewLine
                + "  \"NonStandardArchivePath\": \""
                + EscapeJson(
                    settings.NonStandardArchivePath)
                + "\","
                + Environment.NewLine
                + "  \"StandardPartDatabasePath\": \""
                + EscapeJson(
                    settings.StandardPartDatabasePath)
                + "\","
                + Environment.NewLine
                + "  \"VersionArchivePath\": \""
                + EscapeJson(
                    settings.VersionArchivePath)
                + "\""
                + Environment.NewLine
                + "}"
                + Environment.NewLine;


            File.WriteAllText(
                filePath,
                json,
                new UTF8Encoding(
                    false));
        }


        private static string ReadJsonString(
            string json,
            string propertyName)
        {
            if (string.IsNullOrWhiteSpace(
                    json))
            {
                return "";
            }


            string pattern =
                "\""
                + Regex.Escape(
                    propertyName)
                + "\"\\s*:\\s*\""
                + "(?<value>(?:\\\\.|[^\"])*)"
                + "\"";


            Match match =
                Regex.Match(
                    json,
                    pattern,
                    RegexOptions
                        .IgnoreCase);


            if (!match.Success)
                return "";


            string value =
                match
                    .Groups["value"]
                    .Value;


            return
                UnescapeJson(
                    value);
        }


        private static string EscapeJson(
            string value)
        {
            if (value == null)
                return "";


            return
                value
                    .Replace(
                        "\\",
                        "\\\\")
                    .Replace(
                        "\"",
                        "\\\"");
        }


        private static string UnescapeJson(
            string value)
        {
            if (value == null)
                return "";


            return
                value
                    .Replace(
                        "\\\"",
                        "\"")
                    .Replace(
                        "\\\\",
                        "\\");
        }


        private static string NormalizePath(
            string path)
        {
            if (string.IsNullOrWhiteSpace(
                    path))
            {
                return "";
            }


            return
                path.Trim()
                    .Trim('"');
        }
    }
}
