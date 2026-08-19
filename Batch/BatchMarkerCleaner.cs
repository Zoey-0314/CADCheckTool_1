using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.Core;
using Correct_test1.Markers;

using System;
using System.Collections.Generic;
using System.IO;

namespace Correct_test1.Batch
{
    public class BatchMarkerCleaner
    {
        /// <summary>
        /// 清除指定文件夹内所有DWG检查标记
        /// </summary>
        public List<string> ClearFolderMarkers(
            string folderPath)
        {
            return ClearFolderMarkers(
                folderPath,
                null
            );
        }

        public List<string> ClearFolderMarkers(
            string folderPath,
            Action<int, int, string> progress)
        {
            List<string> results =
                new List<string>();

            if (!Directory.Exists(folderPath))
                return results;

            string[] files =
                Directory.GetFiles(
                    folderPath,
                    "*.dwg",
                    SearchOption.AllDirectories
                );

            if (files.Length == 0)
                return results;

            // 确保有稳定AutoCAD宿主Document

            Document hostDocument =
                EnsureHostDocument();

            if (hostDocument == null ||
                hostDocument.Database == null ||
                hostDocument.Database.IsDisposed)
            {
                return results;
            }

            Database hostDatabase =
                hostDocument.Database;

            // 建立稳定WorkingDatabase

            try
            {
                HostApplicationServices.WorkingDatabase =
                    hostDatabase;
            }
            catch (Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "BatchMarkerCleaner.SetHostWorkingDatabase"
                );

                return results;
            }

            int finishedCount = 0;

            // 批量处理

            foreach (string file in files)
            {
                Database db = null;

                try
                {
                    // 每张图开始前恢复宿主Database

                    if (hostDatabase.IsDisposed)
                    {
                        throw new InvalidOperationException(
                            "宿主Database已经失效"
                        );
                    }

                    HostApplicationServices.WorkingDatabase =
                        hostDatabase;

                    // 创建后台Database

                    db =
                        new Database(
                            false,
                            true
                        );

                    // 读取DWG

                    db.ReadDwgFile(
                        file,
                        FileOpenMode.OpenForReadAndAllShare,
                        false,
                        ""
                    );

                    // 切换到当前后台DWG

                    HostApplicationServices.WorkingDatabase =
                        db;

                    AppLogger.Info(
                        "WorkingDatabase切换到清理文件:" +
                        Path.GetFileName(file),
                        "BatchMarkerCleaner"
                    );

                    db.CloseInput(true);


                    // 清Revision Marker

                    RevisionMarker revisionMarker =
                        new RevisionMarker();

                    revisionMarker.ClearMarkers(
                        db
                    );

                    // 清图号Marker

                    TitleBlockDrawingNumberMarker
                        titleBlockMarker =
                            new TitleBlockDrawingNumberMarker();

                    titleBlockMarker.ClearMarkers(
                        db
                    );

                    // 清通用Marker

                    MarkerManager markerManager =
                        new MarkerManager();

                    markerManager.ClearMarkers(
                        db
                    );

                    // 安全保存

                    AppLogger.Info(
                        "准备保存清理后的DWG:" +
                        Path.GetFileName(file),
                        "BatchMarkerCleaner"
                    );

                    bool saved =
                        SafeDwgSaver.Save(
                            db,
                            file
                        );

                    if (saved)
                    {
                        results.Add(
                            file
                        );
                    }
                    else
                    {
                        AppLogger.Error(
                            new IOException(
                                "SafeDwgSaver保存失败"
                            ),
                            "BatchMarkerCleaner",
                            file
                        );
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error(
                        ex,
                        "BatchMarkerCleaner",
                        file
                    );
                }
                finally
                {
                    // 必须先恢复宿主WorkingDatabase

                    try
                    {
                        if (hostDatabase != null &&
                            !hostDatabase.IsDisposed)
                        {
                            HostApplicationServices.WorkingDatabase =
                                hostDatabase;

                            AppLogger.Info(
                                "WorkingDatabase恢复成功:" +
                                Path.GetFileName(file),
                                "BatchMarkerCleaner"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error(
                            ex,
                            "BatchMarkerCleaner.RestoreWorkingDatabase",
                            file
                        );
                    }

                    // 再释放后台Database

                    if (db != null)
                    {
                        try
                        {
                            db.Dispose();

                            AppLogger.Info(
                                "后台Database释放成功:" +
                                Path.GetFileName(file),
                                "BatchMarkerCleaner"
                            );
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Error(
                                ex,
                                "BatchMarkerCleaner.DisposeDatabase",
                                file
                            );
                        }

                        db = null;
                    }
                }

                // 进度

                finishedCount++;

                int percent =
                    (int)(
                        (double)finishedCount
                        /
                        files.Length
                        *
                        100
                    );

                if (percent < 0)
                    percent = 0;

                if (percent > 100)
                    percent = 100;

                progress?.Invoke(
                    percent,
                    files.Length,
                    Path.GetFileName(file)
                );
            }

            // 最终恢复

            try
            {
                if (hostDatabase != null &&
                    !hostDatabase.IsDisposed)
                {
                    HostApplicationServices.WorkingDatabase =
                        hostDatabase;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "BatchMarkerCleaner.FinalRestore"
                );
            }

            return results;
        }

        /// <summary>
        /// 如果AutoCAD当前没有图纸，
        /// 建立一个空白图作为批处理宿主。
        /// </summary>
        private static Document EnsureHostDocument()
        {
            try
            {
                Document currentDocument =
                    Application.DocumentManager
                        .MdiActiveDocument;

                if (currentDocument != null &&
                    currentDocument.Database != null &&
                    !currentDocument.Database.IsDisposed)
                {
                    return currentDocument;
                }

                AppLogger.Info(
                    "当前没有活动图纸，创建空白宿主图纸",
                    "BatchMarkerCleaner"
                );

                Document newDocument =
                    Application.DocumentManager.Add(
                        ""
                    );

                if (newDocument != null &&
                    newDocument.Database != null &&
                    !newDocument.Database.IsDisposed)
                {
                    AppLogger.Info(
                        "空白宿主图纸创建成功",
                        "BatchMarkerCleaner"
                    );

                    return newDocument;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "BatchMarkerCleaner.EnsureHostDocument"
                );
            }

            return null;
        }
    }
}