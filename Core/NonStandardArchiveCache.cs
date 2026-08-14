using System;
using System.Threading.Tasks;

namespace Correct_test1.Core
{
    /// <summary>
    /// 非标归档索引的AutoCAD会话级缓存。
    ///
    /// AutoCAD / 插件生命周期内：
    ///
    /// Z盘只需要建立一次索引。
    ///
    /// PluginInitializer负责提前后台加载，
    /// CheckService和BatchCheckerManager只取缓存。
    /// </summary>
    public static class NonStandardArchiveCache
    {
        private static readonly object
            SyncRoot =
                new object();


        private static NonStandardArchiveIndex
            _currentIndex;


        private static Task<NonStandardArchiveIndex>
            _loadTask;


        private static DateTime
            _lastLoadedTime =
                DateTime.MinValue;


        /// <summary>
        /// 当前是否已经有索引。
        ///
        /// 注意：
        /// 有索引不代表Z盘一定可用。
        /// 还需要判断Index.IsAvailable。
        /// </summary>
        public static bool HasSnapshot
        {
            get
            {
                lock (SyncRoot)
                {
                    return
                        _currentIndex != null;
                }
            }
        }


        /// <summary>
        /// 当前是否正在后台扫描Z盘。
        /// </summary>
        public static bool IsLoading
        {
            get
            {
                lock (SyncRoot)
                {
                    return
                        IsLoadingNoLock();
                }
            }
        }


        /// <summary>
        /// 上次完成索引建立的时间。
        /// </summary>
        public static DateTime LastLoadedTime
        {
            get
            {
                lock (SyncRoot)
                {
                    return
                        _lastLoadedTime;
                }
            }
        }


        /// <summary>
        /// 插件启动时调用。
        ///
        /// 后台预热Z盘索引。
        ///
        /// 此方法立即返回，
        /// 不阻塞AutoCAD启动。
        /// </summary>
        public static void Preload()
        {
            lock (SyncRoot)
            {
                //--------------------------------
                // 已经有快照
                //--------------------------------

                if (_currentIndex != null)
                    return;


                //--------------------------------
                // 已经有人正在扫描
                //--------------------------------

                if (IsLoadingNoLock())
                    return;


                StartBuildNoLock();
            }
        }


        /// <summary>
        /// 获取当前归档索引。
        ///
        /// 正常情况：
        /// PluginInitializer早已预加载完成，
        /// 这里立即返回。
        ///
        /// 极端情况：
        /// 用户NETLOAD后立刻点击检查，
        /// 预加载还未完成，
        /// 那么这里只等待“正在进行的那一次扫描”，
        /// 绝不会重新启动第二次扫描。
        /// </summary>
        public static NonStandardArchiveIndex
            GetOrBuild()
        {
            Task<NonStandardArchiveIndex>
                task;


            lock (SyncRoot)
            {
                //--------------------------------
                // 已经有快照：
                // 直接使用。
                //--------------------------------

                if (_currentIndex != null)
                {
                    //--------------------------------
                    // 如果启动时Z盘暂时不可用，
                    // 后续检查时在后台悄悄重试。
                    //
                    // 本次仍返回当前不可用状态，
                    // 不阻塞用户。
                    //--------------------------------

                    if (!_currentIndex.IsAvailable &&
                        !IsLoadingNoLock())
                    {
                        StartBuildNoLock();
                    }


                    return
                        _currentIndex;
                }


                //--------------------------------
                // 没有快照，也没有加载任务。
                //
                // 例如某些情况下PluginInitializer
                // 没来得及预热。
                //--------------------------------

                if (!IsLoadingNoLock())
                {
                    StartBuildNoLock();
                }


                task =
                    _loadTask;
            }


            //--------------------------------
            // 这里只可能发生在：
            //
            // 第一次缓存还没有建立完成。
            //
            // 等待现有任务即可。
            //--------------------------------

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
                    "NonStandardArchiveCache.GetOrBuild");


                return null;
            }
        }


        /// <summary>
        /// 后台刷新归档索引。
        ///
        /// 当前旧索引仍然可以继续使用，
        /// 不阻塞检查。
        ///
        /// 以后如果需要加“刷新归档索引”按钮，
        /// 直接调用这个方法即可。
        /// </summary>
        public static void RefreshAsync()
        {
            lock (SyncRoot)
            {
                if (IsLoadingNoLock())
                    return;


                StartBuildNoLock();
            }
        }


        /// <summary>
        /// 启动一次后台扫描。
        ///
        /// 调用前必须持有SyncRoot锁。
        /// </summary>
        private static void StartBuildNoLock()
        {
            _loadTask =
                Task.Run(
                    () =>
                    {
                        try
                        {
                            AppLogger.Info(
                                "开始后台建立非标归档索引。",
                                "NonStandardArchiveCache");


                            NonStandardArchiveIndex index =
                                NonStandardArchiveIndex.Build();


                            lock (SyncRoot)
                            {
                                _currentIndex =
                                    index;


                                _lastLoadedTime =
                                    DateTime.Now;
                            }


                            AppLogger.Info(
                                "非标归档索引后台加载完成。"
                                + " Files="
                                + (
                                    index == null
                                        ? 0
                                        : index.FileCount
                                  ),
                                "NonStandardArchiveCache");


                            return index;
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Error(
                                ex,
                                "NonStandardArchiveCache.Build");


                            return null;
                        }
                    });
        }


        private static bool IsLoadingNoLock()
        {
            return
                _loadTask != null &&
                !_loadTask.IsCompleted;
        }
    }
}