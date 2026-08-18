using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.Checks;
using Correct_test1.Core;
using Correct_test1.Markers;
using Correct_test1.Models;
using Correct_test1.VersionCheck.Core;
using Correct_test1.VersionCheck.Models;

using System;
using System.Collections.Generic;
using System.IO;


namespace Correct_test1.Batch
{
    /// <summary>
    /// 批量检查运行模式。
    /// </summary>
    public enum BatchCheckMode
    {
        /// <summary>
        /// 只检查并生成报告。
        ///
        /// 不自动修正页码，
        /// 不写检查Marker，
        /// 不保存DWG。
        /// </summary>
        ReportOnly,


        /// <summary>
        /// 检查并修改。
        ///
        /// 自动修正允许自动修正的内容，
        /// 写检查Marker，
        /// 最后安全保存DWG。
        /// </summary>
        ApplyChanges
    }


    public class BatchCheckerManager
    {
        //==================================================
        // 旧入口
        //
        // 保留兼容。
        //
        // 原来的默认行为就是：
        // 检查 + 修改 + 保存。
        //==================================================

        public List<CheckResult> CheckFolder(
            string folderPath)
        {
            return
                CheckFolder(
                    folderPath,
                    null,
                    BatchCheckMode.ApplyChanges);
        }


        //==================================================
        // 原带进度入口
        //
        // 保留兼容。
        //==================================================

        public List<CheckResult> CheckFolder(
            string folderPath,
            Action<int, int, string> progress)
        {
            return
                CheckFolder(
                    folderPath,
                    progress,
                    BatchCheckMode.ApplyChanges);
        }


        //==================================================
        // 新正式入口
        //==================================================

        public List<CheckResult> CheckFolder(
            string folderPath,
            Action<int, int, string> progress,
            BatchCheckMode mode)
        {
            List<CheckResult> results =
                new List<CheckResult>();


            //==================================================
            // 当前是否允许修改DWG
            //==================================================

            bool applyChanges =
                mode ==
                BatchCheckMode.ApplyChanges;


            AppLogger.Info(
                "批量检查模式："
                + (
                    applyChanges
                        ? "检查并修改"
                        : "只检查"
                  ),
                "BatchCheckerManager");


            //==================================================
            // 检查目录
            //==================================================

            if (string.IsNullOrWhiteSpace(
                    folderPath) ||
                !Directory.Exists(
                    folderPath))
            {
                return results;
            }


            string[] files;


            try
            {
                files =
                    Directory.GetFiles(
                        folderPath,
                        "*.dwg",
                        SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "BatchCheckerManager.GetFiles",
                    folderPath);


                results.Add(
                    new CheckResult
                    {
                        FilePath =
                            folderPath,

                        FileName =
                            "",

                        Type =
                            "批量检查错误",

                        ObjectName =
                            "文件夹",

                        Message =
                            "无法读取DWG文件："
                            + ex.Message,

                        IsError =
                            true
                    });


                return results;
            }


            if (files == null ||
                files.Length == 0)
            {
                return results;
            }


            //==================================================
            // 确保AutoCAD有稳定宿主Document
            //==================================================

            Document hostDocument =
                EnsureHostDocument();


            if (hostDocument == null ||
                hostDocument.Database == null ||
                hostDocument.Database.IsDisposed)
            {
                results.Add(
                    new CheckResult
                    {
                        FilePath =
                            "",

                        FileName =
                            "",

                        Type =
                            "批量检查错误",

                        ObjectName =
                            "AutoCAD",

                        Message =
                            "无法创建有效的AutoCAD宿主文档",

                        IsError =
                            true
                    });


                return results;
            }


            Database hostDatabase =
                hostDocument.Database;


            //==================================================
            // 设置稳定WorkingDatabase
            //==================================================

            try
            {
                HostApplicationServices
                    .WorkingDatabase =
                        hostDatabase;


                AppLogger.Info(
                    "批量检查宿主WorkingDatabase准备完成",
                    "BatchCheckerManager");
            }
            catch (Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "BatchCheckerManager.SetHostWorkingDatabase");


                results.Add(
                    new CheckResult
                    {
                        Type =
                            "批量检查错误",

                        ObjectName =
                            "AutoCAD",

                        Message =
                            "无法设置宿主WorkingDatabase："
                            + ex.Message,

                        IsError =
                            true
                    });


                return results;
            }


            //==================================================
            // 计算DWG权重
            //
            // 用于进度条。
            //==================================================

            DrawingWeightCalculator calculator =
                new DrawingWeightCalculator();


            Dictionary<string, double> weights =
                new Dictionary<string, double>(
                    StringComparer.OrdinalIgnoreCase);


            double totalWeight =
                0;


            foreach (
                string file
                in files)
            {
                double weight =
                    1;


                try
                {
                    weight =
                        calculator.Calculate(
                            file);
                }
                catch (Exception ex)
                {
                    AppLogger.Error(
                        ex,
                        "BatchCheckerManager.CalculateWeight",
                        file);


                    weight =
                        1;
                }


                if (weight <= 0)
                {
                    weight =
                        1;
                }


                weights[file] =
                    weight;


                totalWeight +=
                    weight;
            }


            if (totalWeight <= 0)
            {
                totalWeight =
                    1;
            }


            double finishedWeight =
                0;


            //==================================================
            // 非标归档索引
            //
            // 整个批量任务只建立/取得一次。
            //==================================================

            NonStandardArchiveIndex archiveIndex =
                NonStandardArchiveCache
                    .GetOrBuild();


            if (archiveIndex == null ||
                !archiveIndex.IsAvailable)
            {
                results.Add(
                    new CheckResult
                    {
                        FilePath =
                            "",

                        FileName =
                            "",

                        Type =
                            "非标归档检查",

                        ObjectName =
                            "归档目录",

                        CurrentValue =
                            archiveIndex == null
                                ? ""
                                : archiveIndex.RootPath,

                        ExpectedValue =
                            "归档目录可访问",

                        Message =
                            archiveIndex == null
                                ? "非标归档索引未建立。"
                                : archiveIndex.ErrorMessage,

                        IsError =
                            true
                    });
            }


            //==================================================
            // 版本归档索引
            //
            // 整个批量任务只建立/取得一次。
            //==================================================

            VersionArchiveIndex versionArchiveIndex =
                VersionArchiveCache
                    .GetOrBuild();


            if (versionArchiveIndex == null ||
                !versionArchiveIndex.IsAvailable)
            {
                results.Add(
                    new CheckResult
                    {
                        FilePath =
                            "",

                        FileName =
                            "",

                        Type =
                            "版本号检查",

                        ObjectName =
                            "版本归档目录",

                        CurrentValue =
                            versionArchiveIndex == null
                                ? ""
                                : versionArchiveIndex.RootPath,

                        ExpectedValue =
                            "版本归档目录可访问",

                        Message =
                            versionArchiveIndex == null
                                ? "版本归档索引未建立。"
                                : versionArchiveIndex.ErrorMessage,

                        IsError =
                            true
                    });
            }


            //==================================================
            // 正式逐张检查
            //==================================================

            foreach (
                string file
                in files)
            {
                Database db =
                    null;

                Document mechanicalDocument =
                    null;

                DocumentLock mechanicalDocumentLock =
                    null;

                bool documentMode =
                    false;

                bool processingSucceeded =
                    false;


                try
                {
                    //==================================================
                    // 每张图开始前确认宿主有效
                    //==================================================

                    if (hostDatabase == null ||
                        hostDatabase.IsDisposed)
                    {
                        throw
                            new InvalidOperationException(
                                "宿主Database已经失效");
                    }


                    HostApplicationServices
                        .WorkingDatabase =
                            hostDatabase;


                    //==================================================
                    // 创建后台Database
                    //==================================================

                    db =
                        new Database(
                            false,
                            true);


                    //==================================================
                    // 加载DWG
                    //==================================================

                    db.ReadDwgFile(
                        file,
                        FileOpenMode
                            .OpenForReadAndAllShare,
                        false,
                        "");


                    //==================================================
                    // 当前后台图成为WorkingDatabase
                    //==================================================

                    HostApplicationServices
                        .WorkingDatabase =
                            db;


                    AppLogger.Info(
                        "WorkingDatabase切换到："
                        + Path.GetFileName(
                            file),
                        "BatchCheckerManager");


                    //==================================================
                    // 关闭输入流
                    //==================================================

                    db.CloseInput(
                        true);

                    //==================================================
                    // Mechanical特殊对象
                    //
                    // 普通DWG继续使用后台Database。
                    // 如果发现AMDTNOTE，则真正作为Document打开，
                    // 保证Mechanical对象按正常方式加载。
                    //==================================================

                    if (ContainsAmdtNote(db))
                    {
                        //==================================================
                        // 先恢复宿主Database
                        //==================================================

                        HostApplicationServices
                            .WorkingDatabase =
                                hostDatabase;


                        //==================================================
                        // 释放后台Database
                        //==================================================

                        db.Dispose();

                        db =
                            null;


                        //==================================================
                        // 真正打开DWG
                        //
                        // ReportOnly：只读打开
                        // ApplyChanges：可写打开
                        //==================================================

                        mechanicalDocument =
                            Application
                                .DocumentManager
                                .Open(
                                    file,
                                    !applyChanges);


                        if (mechanicalDocument == null ||
                            mechanicalDocument.Database == null ||
                            mechanicalDocument.Database.IsDisposed)
                        {
                            throw
                                new InvalidOperationException(
                                    "无法以Document模式打开Mechanical图纸");
                        }


                        //==================================================
                        // Document模式需要锁定
                        //==================================================

                        mechanicalDocumentLock =
                            mechanicalDocument
                                .LockDocument();


                        db =
                            mechanicalDocument.Database;


                        documentMode =
                            true;


                        HostApplicationServices
                            .WorkingDatabase =
                                db;
                    }

                    if (IsEffectivelyEmptyDrawing(db))
                    {
                        results.Add(
                            new CheckResult
                            {
                                FilePath = file,
                                FileName = Path.GetFileName(file),
                                Type = "空图纸",
                                ObjectName = "DWG",
                                CurrentValue = "无可检查实体",
                                ExpectedValue = "完整工程图",
                                Message = "图纸未完成或为空，已跳过检查。",
                                IsError = true
                            });

                        AppLogger.Info(
    "检测到空图纸，跳过："
    + Path.GetFileName(file),
    "BatchCheckerManager");

                        double emptyWeight;

                        if (!weights.TryGetValue(
                                file,
                                out emptyWeight))
                        {
                            emptyWeight = 1;
                        }

                        finishedWeight +=
                            emptyWeight;

                        int emptyPercent =
                            (int)(
                                finishedWeight /
                                totalWeight *
                                100);

                        if (emptyPercent > 100)
                        {
                            emptyPercent = 100;
                        }

                        try
                        {
                            progress?.Invoke(
                                emptyPercent,
                                files.Length,
                                Path.GetFileName(file));
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Error(
                                ex,
                                "BatchCheckerManager.Progress",
                                file);
                        }

                        continue;
                    }
                    //==================================================
                    // 第一阶段：
                    // BOM / 标准件 / 非标 / 版本等检查
                    //==================================================

                    CheckService checkService =
                        new CheckService();


                    CheckReport report =
                        checkService.Check(
                            db,
                            archiveIndex,
                            versionArchiveIndex);


                    if (report == null)
                    {
                        throw
                            new InvalidOperationException(
                                "CheckService返回空CheckReport");
                    }


                    //==================================================
                    // 第二阶段：
                    // 项目号 / 标题栏 / 修改记录等
                    //==================================================

                    DrawingCheckManager manager =
                        new DrawingCheckManager();


                    List<CheckResult> oneResults =
                        manager.CheckDrawing(
                            db,
                            file,

                            //==============================
                            // ReportOnly不绘制Marker
                            //==============================

                            applyChanges,

                            report.Boms,

                            //==============================
                            // ReportOnly禁止页码自动修改
                            //==============================

                            applyChanges);


                    if (oneResults != null)
                    {
                        results.AddRange(
                            oneResults);
                    }


                    //==================================================
                    // Marker
                    //
                    // 只有ApplyChanges模式才执行。
                    //==================================================

                    if (applyChanges)
                    {
                        MarkerManager markerManager =
                            new MarkerManager();


                        //==================================================
                        // 先清理旧检查Marker
                        //==================================================

                        markerManager.ClearMarkers(
                            db);


                        //==================================================
                        // 标准件Marker
                        //==================================================

                        if (report.Results != null)
                        {
                            markerManager.CreateMarkers(
                                db,
                                report.Results);
                        }


                        //==================================================
                        // 非标归档Marker
                        //==================================================

                        if (report
                                .NonStandardArchiveResults
                            != null)
                        {
                            markerManager
                                .CreateNonStandardArchiveMarkers(
                                    db,
                                    report
                                        .NonStandardArchiveResults);
                        }


                        //==================================================
                        // 非标件号Marker
                        //==================================================

                        if (report
                                .NonStandardPartNumberResults
                            != null)
                        {
                            markerManager
                                .CreateNonStandardPartNumberMarkers(
                                    db,
                                    report
                                        .NonStandardPartNumberResults);
                        }


                        //==================================================
                        // 版本Marker
                        //==================================================

                        if (report
                                .VersionCheckResults
                            != null)
                        {
                            markerManager
                                .CreateVersionMarkers(
                                    db,
                                    report
                                        .VersionCheckResults);
                        }


                        //==================================================
                        // BOM序号Marker
                        //
                        // 使用上一阶段已经修改好的：
                        //
                        // MissingIssues
                        // ExtraIssues
                        //
                        // 每个Issue自己携带Layout。
                        //==================================================

                        if (report
                                .BomCalloutResult
                            != null)
                        {
                            markerManager
                                .CreateMissingCalloutMarkers(
                                    db,
                                    report
                                        .BomCalloutResult
                                        .MissingIssues);


                            markerManager
                                .CreateExtraCalloutMarkers(
                                    db,
                                    report
                                        .BomCalloutResult
                                        .ExtraIssues);
                        }
                    }


                    //==================================================
                    // 标准件检查结果 -> CSV统一结果
                    //==================================================

                    if (report.Results != null)
                    {
                        foreach (
                            StandardPartCheckResult
                                standardResult
                            in report.Results)
                        {
                            if (standardResult == null)
                            {
                                continue;
                            }


                            if (standardResult.Status ==
                                StandardPartCheckStatus
                                    .Correct)
                            {
                                continue;
                            }


                            results.Add(
                                new CheckResult
                                {
                                    FilePath =
                                        file,

                                    FileName =
                                        Path.GetFileName(
                                            file),

                                    LayoutName =
                                        standardResult
                                            .SourceLayoutName,

                                    DrawingNumber =
                                        standardResult
                                            .DrawingNumber,

                                    PartNumber =
                                        standardResult.BomItem
                                            == null
                                                ? ""
                                                : standardResult
                                                    .BomItem
                                                    .PartNumber,

                                    PartName =
                                        standardResult.BomItem
                                            == null
                                                ? ""
                                                : standardResult
                                                    .BomItem
                                                    .Name,

                                    CorrectValue =
                                        standardResult.Status ==
                                        StandardPartCheckStatus
                                            .NameError

                                            ? standardResult
                                                .CorrectName

                                            : standardResult
                                                .CorrectPartNumber,

                                    Type =
                                        "标准件检查",

                                    ObjectName =
                                        "标准件",

                                    CurrentValue =
                                        standardResult.BomItem
                                            == null
                                                ? ""
                                                : standardResult
                                                    .BomItem
                                                    .PartNumber,

                                    ExpectedValue =
                                        standardResult
                                            .CorrectPartNumber,

                                    Message =
                                        standardResult
                                            .Message,

                                    IsError =
                                        true
                                });
                        }
                    }


                    //==================================================
                    // 非标归档检查结果
                    //==================================================

                    if (report
                            .NonStandardArchiveResults
                        != null)
                    {
                        foreach (
                            NonStandardArchiveCheckResult
                                archiveResult
                            in report
                                .NonStandardArchiveResults)
                        {
                            if (archiveResult == null)
                            {
                                continue;
                            }


                            BomItem item =
                                archiveResult
                                    .BomItem;


                            string archiveRoot =
                                archiveIndex == null
                                    ? ""
                                    : archiveIndex.RootPath;


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

                                    //==============================
                                    // 不再硬编码Z:\归档图纸
                                    // 使用实际配置路径
                                    //==============================

                                    ExpectedValue =
                                        archiveRoot
                                        + " 中存在图号 "
                                        + archiveResult
                                            .SearchKey
                                        + " 的归档文件",

                                    CorrectValue =
                                        archiveResult
                                            .SearchKey,

                                    Message =
                                        archiveResult
                                            .Message,

                                    IsError =
                                        true
                                });
                        }
                    }


                    //==================================================
                    // 非标件号检查结果
                    //==================================================

                    if (report
                            .NonStandardPartNumberResults
                        != null)
                    {
                        foreach (
                            NonStandardPartNumberCheckResult
                                partResult
                            in report
                                .NonStandardPartNumberResults)
                        {
                            if (partResult == null)
                            {
                                continue;
                            }


                            BomItem item =
                                partResult
                                    .BomItem;


                            results.Add(
                                new CheckResult
                                {
                                    FilePath =
                                        file,

                                    FileName =
                                        Path.GetFileName(
                                            file),

                                    LayoutName =
                                        partResult
                                            .SourceLayoutName,

                                    DrawingNumber =
                                        partResult
                                            .DrawingNumber,

                                    PartNumber =
                                        partResult
                                            .OriginalPartNumber,

                                    PartName =
                                        item == null
                                            ? ""
                                            : item.Name,

                                    Type =
                                        "非标件号检查",

                                    ObjectName =
                                        "非标件号",

                                    CurrentValue =
                                        partResult
                                            .OriginalPartNumber,

                                    ExpectedValue =
                                        partResult
                                            .ArchiveDrawingNumber
                                        + " + _"
                                        + partResult
                                            .PartSuffix,

                                    CorrectValue =
                                        partResult
                                            .ArchiveDrawingNumber
                                        + partResult
                                            .PartSuffix,

                                    Message =
                                        partResult
                                            .Message,

                                    IsError =
                                        true
                                });
                        }
                    }


                    //==================================================
                    // 版本号检查结果
                    //==================================================

                    if (report
                            .VersionCheckResults
                        != null)
                    {
                        foreach (
                            VersionCheckResult
                                versionResult
                            in report
                                .VersionCheckResults)
                        {
                            if (versionResult == null)
                            {
                                continue;
                            }


                            results.Add(
                                new CheckResult
                                {
                                    FilePath =
                                        file,

                                    FileName =
                                        Path.GetFileName(
                                            file),

                                    LayoutName =
                                        versionResult
                                            .LayoutName,

                                    DrawingNumber =
                                        versionResult
                                            .DrawingNumber,

                                    Type =
                                        "版本号检查",

                                    ObjectName =
                                        "版本号",

                                    CurrentValue =
                                        versionResult
                                            .CurrentVersion,

                                    ExpectedValue =
                                        versionResult
                                            .LatestVersion,

                                    CorrectValue =
                                        versionResult
                                            .LatestVersion,

                                    Message =
                                        versionResult
                                            .Message,

                                    IsError =
                                        true
                                });
                        }
                    }


                    //==================================================
                    // BOM序号检查结果
                    //
                    // 这一段是必须加的。
                    //
                    // 因为ReportOnly不画Marker，
                    // 所以BOM序号问题必须进入CSV，
                    // 否则只检查模式下用户看不到这些错误。
                    //==================================================

                    if (report
                            .BomCalloutResult
                        != null)
                    {
                        //==============================================
                        // BOM有，图中没有
                        //==============================================

                        if (report
                                .BomCalloutResult
                                .MissingIssues
                            != null)
                        {
                            foreach (
                                BomCalloutIssue issue
                                in report
                                    .BomCalloutResult
                                    .MissingIssues)
                            {
                                if (issue == null)
                                {
                                    continue;
                                }


                                results.Add(
                                    new CheckResult
                                    {
                                        FilePath =
                                            file,

                                        FileName =
                                            Path.GetFileName(
                                                file),

                                        LayoutName =
                                            issue.LayoutName,

                                        Type =
                                            "BOM序号检查",

                                        ObjectName =
                                            "序号"
                                            + issue.Number,

                                        CurrentValue =
                                            "BOM中存在，图中缺失",

                                        ExpectedValue =
                                            "图中应存在序号 "
                                            + issue.Number,

                                        CorrectValue =
                                            issue.Number
                                                .ToString(),

                                        Message =
                                            issue.Message,

                                        IsError =
                                            true
                                    });
                            }
                        }


                        //==============================================
                        // 图中有，BOM没有
                        //==============================================

                        if (report
                                .BomCalloutResult
                                .ExtraIssues
                            != null)
                        {
                            foreach (
                                BomCalloutIssue issue
                                in report
                                    .BomCalloutResult
                                    .ExtraIssues)
                            {
                                if (issue == null)
                                {
                                    continue;
                                }


                                results.Add(
                                    new CheckResult
                                    {
                                        FilePath =
                                            file,

                                        FileName =
                                            Path.GetFileName(
                                                file),

                                        LayoutName =
                                            issue.LayoutName,

                                        Type =
                                            "BOM序号检查",

                                        ObjectName =
                                            "序号"
                                            + issue.Number,

                                        CurrentValue =
                                            "图中存在序号 "
                                            + issue.Number,

                                        ExpectedValue =
                                            "BOM中应存在对应序号",

                                        CorrectValue =
                                            "",

                                        Message =
                                            issue.Message,

                                        IsError =
                                            true
                                    });
                            }
                        }
                    }


                    //==================================================
                    // 保存
                    //
                    // ReportOnly绝对不保存。
                    //==================================================

                    if (applyChanges)
                    {
                        //==================================================
                        // 普通后台Database
                        // 继续使用原来的SafeDwgSaver。
                        //
                        // Document模式不能在Document仍打开时
                        // 再让SafeDwgSaver替换这个DWG。
                        // 它会在finally中CloseAndSave。
                        //==================================================

                        if (!documentMode)
                        {
                            bool saved =
                                SafeDwgSaver.Save(
                                    db,
                                    file);


                            if (!saved)
                            {
                                results.Add(
                                    new CheckResult
                                    {
                                        FilePath =
                                            file,

                                        FileName =
                                            Path.GetFileName(
                                                file),

                                        Type =
                                            "文件保存错误",

                                        ObjectName =
                                            "DWG",

                                        Message =
                                            "SafeDwgSaver保存失败，详见日志",

                                        IsError =
                                            true
                                    });
                            }
                        }
                    }


                    processingSucceeded =
                        true;
                }
                catch (Exception ex)
                {
                    AppLogger.Error(
                        ex,
                        "BatchCheckerManager.CheckFolder",
                        file);


                    results.Add(
                        new CheckResult
                        {
                            FilePath =
                                file,

                            FileName =
                                Path.GetFileName(
                                    file),

                            Type =
                                "文件处理错误",

                            ObjectName =
                                "DWG",

                            Message =
                                ex.Message
                                + Environment.NewLine
                                + ex.StackTrace,

                            IsError =
                                true
                        });
                }
                finally
                {
                    //==================================================
                    // Document锁必须先释放
                    //==================================================

                    if (mechanicalDocumentLock != null)
                    {
                        try
                        {
                            mechanicalDocumentLock
                                .Dispose();
                        }
                        catch
                        {
                        }

                        mechanicalDocumentLock =
                            null;
                    }


                    //==================================================
                    // 在关闭Mechanical Document前
                    // 先把WorkingDatabase恢复到宿主。
                    //==================================================

                    try
                    {
                        if (hostDatabase != null &&
                            !hostDatabase.IsDisposed)
                        {
                            HostApplicationServices
                                .WorkingDatabase =
                                    hostDatabase;
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error(
                            ex,
                            "BatchCheckerManager.RestoreWorkingDatabase",
                            file);
                    }


                    //==================================================
                    // Mechanical Document模式
                    //
                    // Database由Document管理，
                    // 这里绝对不能手动db.Dispose()。
                    //==================================================

                    if (documentMode &&
                        mechanicalDocument != null)
                    {
                        try
                        {
                            if (applyChanges &&
                                processingSucceeded)
                            {
                                mechanicalDocument
                                    .CloseAndSave(
                                        file);
                            }
                            else
                            {
                                mechanicalDocument
                                    .CloseAndDiscard();
                            }
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Error(
                                ex,
                                "BatchCheckerManager.CloseMechanicalDocument",
                                file);


                            results.Add(
                                new CheckResult
                                {
                                    FilePath =
                                        file,

                                    FileName =
                                        Path.GetFileName(
                                            file),

                                    Type =
                                        "文件关闭保存错误",

                                    ObjectName =
                                        "DWG",

                                    Message =
                                        ex.Message,

                                    IsError =
                                        true
                                });
                        }


                        mechanicalDocument =
                            null;

                        db =
                            null;
                    }


                    //==================================================
                    // 普通后台Database
                    //==================================================

                    if (!documentMode &&
                        db != null)
                    {
                        try
                        {
                            db.Dispose();
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Error(
                                ex,
                                "BatchCheckerManager.DisposeDatabase",
                                file);
                        }


                        db =
                            null;
                    }


                    //==================================================
                    // 最后恢复宿主Document
                    //==================================================

                    try
                    {
                        if (hostDocument != null &&
                            hostDocument.Database != null &&
                            !hostDocument.Database.IsDisposed)
                        {
                            Application
                                .DocumentManager
                                .MdiActiveDocument =
                                    hostDocument;
                        }
                    }
                    catch
                    {
                    }
                }


                //==================================================
                // 更新进度
                //==================================================

                double currentWeight;


                if (!weights.TryGetValue(
                        file,
                        out currentWeight))
                {
                    currentWeight =
                        1;
                }


                finishedWeight +=
                    currentWeight;


                int percent =
                    (int)(
                        finishedWeight
                        /
                        totalWeight
                        *
                        100
                    );


                if (percent < 0)
                {
                    percent =
                        0;
                }


                if (percent > 100)
                {
                    percent =
                        100;
                }


                try
                {
                    if (progress != null)
                    {
                        progress(
                            percent,
                            files.Length,
                            Path.GetFileName(
                                file));
                    }
                }
                catch (Exception ex)
                {
                    //--------------------------------
                    // UI进度更新失败不能中止整个批量任务
                    //--------------------------------

                    AppLogger.Error(
                        ex,
                        "BatchCheckerManager.Progress",
                        file);
                }
            }


            //==================================================
            // 批量完成后
            //
            // 最后再次保证WorkingDatabase恢复宿主。
            //==================================================

            try
            {
                if (hostDatabase != null &&
                    !hostDatabase.IsDisposed)
                {
                    HostApplicationServices
                        .WorkingDatabase =
                            hostDatabase;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "BatchCheckerManager.FinalRestore");
            }


            return results;
        }

        private static bool ContainsAmdtNote(
    Database database)
        {
            if (database == null ||
                database.IsDisposed)
            {
                return false;
            }


            try
            {
                using (
                    Transaction tr =
                        database
                            .TransactionManager
                            .StartTransaction())
                {
                    BlockTableRecord modelSpace =
                        tr.GetObject(
                            SymbolUtilityServices
                                .GetBlockModelSpaceId(
                                    database),
                            OpenMode.ForRead)
                        as BlockTableRecord;


                    if (modelSpace == null)
                    {
                        return false;
                    }


                    foreach (
                        ObjectId id
                        in modelSpace)
                    {
                        Entity entity;


                        try
                        {
                            entity =
                                tr.GetObject(
                                    id,
                                    OpenMode.ForRead)
                                as Entity;
                        }
                        catch
                        {
                            continue;
                        }


                        if (entity == null)
                        {
                            continue;
                        }


                        //==============================================
                        // 后台Database中的Mechanical代理对象
                        //==============================================

                        ProxyEntity proxy =
                            entity as ProxyEntity;


                        if (proxy != null)
                        {
                            try
                            {
                                if (string.Equals(
                                        proxy.OriginalDxfName,
                                        "AMDTNOTE",
                                        StringComparison
                                            .OrdinalIgnoreCase))
                                {
                                    return true;
                                }
                            }
                            catch
                            {
                            }
                        }


                        //==============================================
                        // 如果Mechanical对象本身已经正常加载
                        //==============================================

                        try
                        {
                            Autodesk.AutoCAD.Runtime.RXClass
                                rxClass =
                                    entity.GetRXClass();


                            if (rxClass != null &&
                                string.Equals(
                                    rxClass.DxfName,
                                    "AMDTNOTE",
                                    StringComparison
                                        .OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                        catch
                        {
                        }
                    }


                    tr.Commit();
                }
            }
            catch
            {
                return false;
            }


            return false;
        }
        private static bool IsEffectivelyEmptyDrawing(
    Database database)
        {
            if (database == null ||
                database.IsDisposed)
            {
                return false;
            }

            try
            {
                using (Transaction tr =
                    database.TransactionManager.StartTransaction())
                {
                    BlockTable blockTable =
                        tr.GetObject(
                            database.BlockTableId,
                            OpenMode.ForRead)
                        as BlockTable;

                    if (blockTable == null)
                        return false;

                    BlockTableRecord modelSpace =
                        tr.GetObject(
                            blockTable[BlockTableRecord.ModelSpace],
                            OpenMode.ForRead)
                        as BlockTableRecord;

                    if (modelSpace == null)
                        return false;

                    foreach (ObjectId id in modelSpace)
                    {
                        Entity entity =
                            tr.GetObject(
                                id,
                                OpenMode.ForRead)
                            as Entity;

                        if (entity == null ||
                            entity.IsErased)
                        {
                            continue;
                        }

                        return false;
                    }

                    DBDictionary layouts =
                        tr.GetObject(
                            database.LayoutDictionaryId,
                            OpenMode.ForRead)
                        as DBDictionary;

                    if (layouts != null)
                    {
                        foreach (DBDictionaryEntry entry in layouts)
                        {
                            Layout layout =
                                tr.GetObject(
                                    entry.Value,
                                    OpenMode.ForRead)
                                as Layout;

                            if (layout == null ||
                                layout.ModelType)
                            {
                                continue;
                            }

                            BlockTableRecord space =
                                tr.GetObject(
                                    layout.BlockTableRecordId,
                                    OpenMode.ForRead)
                                as BlockTableRecord;

                            if (space == null)
                                continue;

                            foreach (ObjectId id in space)
                            {
                                Entity entity =
                                    tr.GetObject(
                                        id,
                                        OpenMode.ForRead)
                                    as Entity;

                                if (entity is Table)
                                {
                                    return false;
                                }
                            }
                        }
                    }

                    tr.Commit();
                }

                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "BatchCheckerManager.IsEffectivelyEmptyDrawing");

                return false;
            }
        }

        //==================================================
        // 确保AutoCAD存在有效Document
        //==================================================

        /// <summary>
        /// 如果当前没有活动图纸，
        /// 自动建立空白宿主图纸。
        /// </summary>
        internal static Document EnsureHostDocument()
        {
            try
            {
                Document currentDocument =
                    Application
                        .DocumentManager
                        .MdiActiveDocument;


                if (currentDocument != null &&
                    currentDocument.Database != null &&
                    !currentDocument
                        .Database
                        .IsDisposed)
                {
                    return
                        currentDocument;
                }


                AppLogger.Info(
                    "当前没有活动图纸，创建空白宿主图纸",
                    "BatchCheckerManager");


                Document newDocument =
                    Application
                        .DocumentManager
                        .Add("");


                if (newDocument != null &&
                    newDocument.Database != null &&
                    !newDocument
                        .Database
                        .IsDisposed)
                {
                    AppLogger.Info(
                        "空白宿主图纸创建成功",
                        "BatchCheckerManager");


                    return
                        newDocument;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "BatchCheckerManager.EnsureHostDocument");
            }


            return null;
        }
    }
}