using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.Checks;
using Correct_test1.Core;
using Correct_test1.Markers;
using Correct_test1.Models;

using System;
using System.Collections.Generic;
using System.IO;

namespace Correct_test1.Batch
{
    public class BatchCheckerManager
    {
        /// <summary>
        /// 原版本
        /// 保留兼容旧调用
        /// </summary>
        public List<CheckResult> CheckFolder(
            string folderPath)
        {
            return CheckFolder(
                folderPath,
                null
            );
        }

        /// <summary>
        /// 带真实进度回调的批量检查
        /// </summary>
        public List<CheckResult> CheckFolder(
            string folderPath,
            Action<int, int, string> progress)
        {
            List<CheckResult> results =
                new List<CheckResult>();

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

            //--------------------------------
            // 确保AutoCAD存在一个有效宿主Document
            //--------------------------------

            Document hostDocument =
                EnsureHostDocument();

            if (hostDocument == null ||
                hostDocument.Database == null ||
                hostDocument.Database.IsDisposed)
            {
                results.Add(
                    new CheckResult
                    {
                        FilePath = "",
                        FileName = "",
                        Type = "批量检查错误",
                        ObjectName = "AutoCAD",
                        Message = "无法创建有效的AutoCAD宿主文档",
                        IsError = true
                    }
                );

                return results;
            }

            Database hostDatabase =
                hostDocument.Database;

            //--------------------------------
            // 明确设置稳定WorkingDatabase
            //--------------------------------

            try
            {
                HostApplicationServices.WorkingDatabase =
                    hostDatabase;

                AppLogger.Info(
                    "批量检查宿主WorkingDatabase准备完成",
                    "BatchCheckerManager"
                );
            }
            catch (Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "BatchCheckerManager.SetHostWorkingDatabase"
                );

                return results;
            }

            //--------------------------------
            // 第一步：计算所有DWG权重
            //--------------------------------

            DrawingWeightCalculator calculator =
                new DrawingWeightCalculator();

            Dictionary<string, double> weights =
                new Dictionary<string, double>();

            double totalWeight = 0;

            foreach (string file in files)
            {
                double weight = 1;

                try
                {
                    weight =
                        calculator.Calculate(
                            file
                        );
                }
                catch (Exception ex)
                {
                    AppLogger.Error(
                        ex,
                        "BatchCheckerManager.CalculateWeight",
                        file
                    );

                    weight = 1;
                }

                if (weight <= 0)
                    weight = 1;

                weights[file] =
                    weight;

                totalWeight +=
                    weight;
            }

            if (totalWeight <= 0)
                totalWeight = 1;

            double finishedWeight = 0;

            //--------------------------------
            // 第二步：正式检查
            //--------------------------------
            //--------------------------------
            // 非标归档索引
            //
            // 整个批量任务只建立一次。
            //
            // 无论下面有多少张DWG，
            // 都共用这一份Z盘文件索引。
            //--------------------------------

            //--------------------------------
            // 整个批量检查使用AutoCAD会话级缓存。
            //--------------------------------

            NonStandardArchiveIndex archiveIndex =
                NonStandardArchiveCache
                    .GetOrBuild();


            if (!archiveIndex.IsAvailable)
            {
                results.Add(
                    new CheckResult
                    {
                        FilePath = "",

                        FileName = "",

                        Type =
                            "非标归档检查",

                        ObjectName =
                            "归档目录",

                        CurrentValue =
                            archiveIndex.RootPath,

                        ExpectedValue =
                            "归档目录可访问",

                        Message =
                            archiveIndex.ErrorMessage,

                        IsError =
                            true
                    });
            }
            foreach (string file in files)
            {
                Database db = null;

                try
                {
                    //--------------------------------
                    // 每张图开始前
                    // 先确保宿主Database仍然有效
                    //--------------------------------

                    if (hostDatabase.IsDisposed)
                    {
                        throw new InvalidOperationException(
                            "宿主Database已经失效"
                        );
                    }

                    HostApplicationServices.WorkingDatabase =
                        hostDatabase;

                    //--------------------------------
                    // 创建后台Database
                    //--------------------------------

                    db =
                        new Database(
                            false,
                            true
                        );

                    //--------------------------------
                    // 读取DWG
                    //--------------------------------

                    db.ReadDwgFile(
                        file,
                        FileOpenMode.OpenForReadAndAllShare,
                        false,
                        ""
                    );

                    //--------------------------------
                    // 当前后台DWG设为WorkingDatabase
                    //--------------------------------

                    HostApplicationServices.WorkingDatabase =
                        db;

                    AppLogger.Info(
                        "WorkingDatabase切换到:" +
                        Path.GetFileName(file),
                        "BatchCheckerManager"
                    );

                    //--------------------------------
                    // 关闭输入流
                    //--------------------------------

                    db.CloseInput(true);

                    //--------------------------------
                    // 执行检查
                    //--------------------------------

                    CheckService checkService =
                        new CheckService();

                    CheckReport report =
                        checkService.Check(
                            db,
                            archiveIndex);

                    if (report == null)
                    {
                        throw new InvalidOperationException(
                            "CheckService返回空CheckReport"
                        );
                    }

                    DrawingCheckManager manager =
                        new DrawingCheckManager();

                    List<CheckResult> oneResults =
                        manager.CheckDrawing(
                            db,
                            file,
                            true,
                            report.Boms
                        );

                    if (oneResults != null)
                    {
                        results.AddRange(
                            oneResults
                        );
                    }

                    //--------------------------------
                    // Marker
                    //--------------------------------

                    MarkerManager markerManager =
                        new MarkerManager();

                    markerManager.ClearMarkers(
                        db
                    );

                    if (report.Results != null)
                    {
                        markerManager.CreateMarkers(
                            db,
                            report.Results
                        );
                    }
                    //--------------------------------
                    // 非标归档缺失标记
                    //--------------------------------

                    if (report.NonStandardArchiveResults != null)
                    {
                        markerManager
                            .CreateNonStandardArchiveMarkers(
                                db,
                                report.NonStandardArchiveResults);
                    }

                    if (report.BomCalloutResult != null)
                    {
                        markerManager.CreateMissingCalloutMarkers(
                            db,
                            report.BomCalloutResult.MissingCallouts,
                            report.Boms
                        );

                        markerManager.CreateExtraCalloutMarkers(
                            db,
                            report.BomCalloutResult.ExtraCallouts,
                            report.DrawingTexts
                        );
                    }

                    //--------------------------------
                    // 标准件结果
                    //--------------------------------

                    if (report.Results != null)
                    {
                        foreach (
                            StandardPartCheckResult standardResult
                            in report.Results)
                        {
                            if (standardResult == null)
                                continue;

                            if (standardResult.Status ==
                                StandardPartCheckStatus.Correct)
                            {
                                continue;
                            }

                            results.Add(
                                new CheckResult
                                {
                                    FilePath =
                                        file,

                                    FileName =
                                        Path.GetFileName(file),

                                    DrawingNumber =
                                        standardResult.DrawingNumber,

                                    PartNumber =
                                        standardResult.BomItem == null
                                            ? ""
                                            : standardResult.BomItem.PartNumber,

                                    PartName =
                                        standardResult.BomItem == null
                                            ? ""
                                            : standardResult.BomItem.Name,

                                    CorrectValue =
                                        standardResult.Status ==
                                        StandardPartCheckStatus.NameError
                                            ? standardResult.CorrectName
                                            : standardResult.CorrectPartNumber,

                                    Type =
                                        "标准件检查",

                                    ObjectName =
                                        "标准件",

                                    CurrentValue =
                                        standardResult.BomItem == null
                                            ? ""
                                            : standardResult.BomItem.PartNumber,

                                    ExpectedValue =
                                        standardResult.CorrectPartNumber,

                                    Message =
                                        standardResult.Message,

                                    IsError =
                                        true
                                }
                            );
                        }
                    }
                    //--------------------------------
                    // 非标件归档检查结果
                    //--------------------------------

                    if (report.NonStandardArchiveResults != null)
                    {
                        foreach (
                            NonStandardArchiveCheckResult archiveResult
                            in report.NonStandardArchiveResults)
                        {
                            if (archiveResult == null)
                                continue;


                            BomItem item =
                                archiveResult.BomItem;


                            results.Add(
                                new CheckResult
                                {
                                    FilePath =
                                        file,

                                    FileName =
                                        Path.GetFileName(
                                            file),

                                    LayoutName =
                                        archiveResult
                                            .SourceLayoutName,

                                    DrawingNumber =
                                        archiveResult
                                            .DrawingNumber,

                                    PartNumber =
                                        archiveResult
                                            .OriginalPartNumber,

                                    PartName =
                                        item == null
                                            ? ""
                                            : item.Name,

                                    Type =
                                        "非标归档检查",

                                    ObjectName =
                                        "非标件",

                                    CurrentValue =
                                        archiveResult
                                            .OriginalPartNumber,

                                    ExpectedValue =
                                        "Z:\\归档图纸中存在包含 "
                                        + archiveResult.SearchKey
                                        + " 的文件",

                                    CorrectValue =
                                        archiveResult.SearchKey,

                                    Message =
                                        archiveResult.Message,

                                    IsError =
                                        true
                                });
                        }
                    }
                    //--------------------------------
                    // 保存
                    //--------------------------------

                    AppLogger.Info(
                        "准备安全保存:" +
                        Path.GetFileName(file),
                        "BatchCheckerManager"
                    );

                    bool saved =
                        SafeDwgSaver.Save(
                            db,
                            file
                        );

                    if (!saved)
                    {
                        results.Add(
                            new CheckResult
                            {
                                FilePath =
                                    file,

                                FileName =
                                    Path.GetFileName(file),

                                Type =
                                    "文件保存错误",

                                ObjectName =
                                    "DWG",

                                Message =
                                    "SafeDwgSaver保存失败，详见日志",

                                IsError =
                                    true
                            }
                        );
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error(
                        ex,
                        "BatchCheckerManager.CheckFolder",
                        file
                    );

                    results.Add(
                        new CheckResult
                        {
                            FilePath =
                                file,

                            FileName =
                                Path.GetFileName(file),

                            Type =
                                "文件处理错误",

                            ObjectName =
                                "DWG",

                            Message =
                                ex.Message
                                +
                                Environment.NewLine
                                +
                                ex.StackTrace,

                            IsError =
                                true
                        }
                    );
                }
                finally
                {
                    //--------------------------------
                    // 关键：
                    // 必须先恢复宿主WorkingDatabase
                    //--------------------------------

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
                                "BatchCheckerManager"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error(
                            ex,
                            "BatchCheckerManager.RestoreWorkingDatabase",
                            file
                        );
                    }

                    //--------------------------------
                    // 恢复完成后才能Dispose后台Database
                    //--------------------------------

                    if (db != null)
                    {
                        try
                        {
                            db.Dispose();

                            AppLogger.Info(
                                "后台Database释放成功:" +
                                Path.GetFileName(file),
                                "BatchCheckerManager"
                            );
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Error(
                                ex,
                                "BatchCheckerManager.DisposeDatabase",
                                file
                            );
                        }

                        db = null;
                    }
                }

                //--------------------------------
                // 更新进度
                //--------------------------------

                finishedWeight +=
                    weights[file];

                int percent =
                    (int)(
                        finishedWeight
                        /
                        totalWeight
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

            //--------------------------------
            // 批量完成
            // 最后再次保证WorkingDatabase正确
            //--------------------------------

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
                    "BatchCheckerManager.FinalRestore"
                );
            }

            return results;
        }

        /// <summary>
        /// 确保存在一个有效AutoCAD Document。
        ///
        /// 如果用户关闭了最后一张图纸，
        /// 自动建立一个空白图作为稳定宿主。
        /// </summary>
        internal static Document EnsureHostDocument()
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
                    "BatchCheckerManager"
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
                        "BatchCheckerManager"
                    );

                    return newDocument;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "BatchCheckerManager.EnsureHostDocument"
                );
            }

            return null;
        }
    }
}