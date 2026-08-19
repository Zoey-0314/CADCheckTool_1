using Correct_test1.Configs;
using Correct_test1.Core;

using System;
using System.Threading.Tasks;

namespace Correct_test1.VersionCheck.Core
{
    /// <summary>
    /// 版本归档的AutoCAD会话级缓存。
    /// </summary>
    public static class VersionArchiveCache
    {
        private static readonly object
            SyncRoot =
                new object();


        private static VersionArchiveIndex
            _currentIndex;


        private static Task<VersionArchiveIndex>
            _loadTask;


        private static bool
            _refreshQueued;


        public static void Preload()
        {
            lock (SyncRoot)
            {
                if (_currentIndex != null)
                    return;


                if (IsLoadingNoLock())
                    return;


                StartBuildNoLock();
            }
        }


        public static VersionArchiveIndex
            GetOrBuild()
        {
            Task<VersionArchiveIndex>
                task;


            lock (SyncRoot)
            {
                if (_currentIndex != null)
                    return _currentIndex;


                if (!IsLoadingNoLock())
                {
                    StartBuildNoLock();
                }


                task =
                    _loadTask;
            }


            try
            {
                return
                    task == null
                        ? null
                        : task
                            .GetAwaiter()
                            .GetResult();
            }
            catch (Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "VersionArchiveCache.GetOrBuild");

                return null;
            }
        }


        public static void RefreshAsync()
        {
            lock (SyncRoot)
            {
                if (!IsLoadingNoLock())
                {
                    StartBuildNoLock();

                    return;
                }


                if (_refreshQueued)
                    return;


                _refreshQueued =
                    true;


                Task<VersionArchiveIndex>
                    currentTask =
                        _loadTask;


                currentTask.ContinueWith(
                    task =>
                    {
                        lock (SyncRoot)
                        {
                            _refreshQueued =
                                false;


                            StartBuildNoLock();
                        }
                    });
            }
        }


        private static void StartBuildNoLock()
        {
            _loadTask =
                Task.Run(
                    () =>
                    {
                        try
                        {
                            VersionArchiveIndex index =
                                BuildIndex();


                            lock (SyncRoot)
                            {
                                _currentIndex =
                                    index;
                            }


                            AppLogger.Info(
                                "版本归档索引加载完成。"
                                + " Files="
                                + (
                                    index == null
                                        ? 0
                                        : index.FileCount
                                  ),
                                "VersionArchiveCache");


                            return index;
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Error(
                                ex,
                                "VersionArchiveCache.Build");

                            return null;
                        }
                    });
        }


        private static VersionArchiveIndex
            BuildIndex()
        {
            AppPathSettings settings =
                AppPathConfig.Current;


            // 两套路径完全相同：
            //
            // 直接使用已有归档扫描结果。
            // 不再访问一遍归档目录。

            if (PathsEqual(
                    settings.VersionArchivePath,
                    settings.NonStandardArchivePath))
            {
                NonStandardArchiveIndex archiveIndex =
                    NonStandardArchiveCache
                        .GetOrBuild();


                if (archiveIndex != null &&
                    archiveIndex.IsAvailable &&
                    PathsEqual(
                        archiveIndex.RootPath,
                        settings.VersionArchivePath))
                {
                    return
                        VersionArchiveIndex
                            .BuildFromFilePaths(
                                settings
                                    .VersionArchivePath,
                                archiveIndex
                                    .GetFilePathsSnapshot());
                }
            }


            // 路径不同：
            // 单独建立版本归档索引

            return
                VersionArchiveIndex.Build(
                    settings.VersionArchivePath);
        }


        private static bool PathsEqual(
            string left,
            string right)
        {
            if (string.IsNullOrWhiteSpace(
                    left) ||
                string.IsNullOrWhiteSpace(
                    right))
            {
                return false;
            }


            string a =
                left.Trim()
                    .TrimEnd(
                        '\\',
                        '/');


            string b =
                right.Trim()
                    .TrimEnd(
                        '\\',
                        '/');


            return string.Equals(
                a,
                b,
                StringComparison.OrdinalIgnoreCase);
        }


        private static bool IsLoadingNoLock()
        {
            return
                _loadTask != null &&
                !_loadTask.IsCompleted;
        }
    }
}
