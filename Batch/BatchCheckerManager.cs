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
    public enum BatchCheckMode
    {
        ReportOnly,
        ApplyChanges
    }


    public class BatchCheckerManager
    {
        public List<CheckResult> CheckFolder(
            string folderPath)
        {
            return
                CheckFolder(
                    folderPath,
                    null,
                    BatchCheckMode.ApplyChanges);
        }


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


        public List<CheckResult> CheckFolder(
            string folderPath,
            Action<int, int, string> progress,
            BatchCheckMode mode)
        {
            List<CheckResult> results =
                new List<CheckResult>();

            bool applyChanges =
                mode == BatchCheckMode.ApplyChanges;

            AppLogger.Info(
                "批量检查模式："
                + (applyChanges
                    ? "检查并修改"
                    : "只检查"),
                "BatchCheckerManager");


            if (string.IsNullOrWhiteSpace(folderPath) ||
                !Directory.Exists(folderPath))
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
                        FilePath = folderPath,
                        FileName = "",
                        Type = "批量检查错误",
                        ObjectName = "文件夹",
                        Message = "无法读取DWG文件：" + ex.Message,
                        IsError = true
                    });

                return results;
            }


            if (files == null ||
                files.Length == 0)
            {
                return results;
            }


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
                    });

                return results;
            }


            Database hostDatabase =
                hostDocument.Database;

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
                        Type = "批量检查错误",
                        ObjectName = "AutoCAD",
                        Message =
                            "无法设置宿主WorkingDatabase："
                            + ex.Message,
                        IsError = true
                    });

                return results;
            }


            DrawingWeightCalculator calculator =
                new DrawingWeightCalculator();

            Dictionary<string, double> weights =
                new Dictionary<string, double>(
                    StringComparer.OrdinalIgnoreCase);

            double totalWeight =
                0;

            foreach (string file in files)
            {
                double weight =
                    1;

                try
                {
                    weight =
                        calculator.Calculate(file);
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


            NonStandardArchiveIndex archiveIndex =
                NonStandardArchiveCache
                    .GetOrBuild();

            if (archiveIndex == null ||
                !archiveIndex.IsAvailable)
            {
                results.Add(
                    new CheckResult
                    {
                        FilePath = "",
                        FileName = "",
                        Type = "非标归档检查",
                        ObjectName = "归档目录",
                        CurrentValue =
                            archiveIndex == null
                                ? ""
                                : archiveIndex.RootPath,
                        ExpectedValue = "归档目录可访问",
                        Message =
                            archiveIndex == null
                                ? "非标归档索引未建立。"
                                : archiveIndex.ErrorMessage,
                        IsError = true
                    });
            }


            VersionArchiveIndex versionArchiveIndex =
                VersionArchiveCache
                    .GetOrBuild();

            if (versionArchiveIndex == null ||
                !versionArchiveIndex.IsAvailable)
            {
                results.Add(
                    new CheckResult
                    {
                        FilePath = "",
                        FileName = "",
                        Type = "版本号检查",
                        ObjectName = "版本归档目录",
                        CurrentValue =
                            versionArchiveIndex == null
                                ? ""
                                : versionArchiveIndex.RootPath,
                        ExpectedValue = "版本归档目录可访问",
                        Message =
                            versionArchiveIndex == null
                                ? "版本归档索引未建立。"
                                : versionArchiveIndex.ErrorMessage,
                        IsError = true
                    });
            }


            double finishedWeight =
                0;


            foreach (string file in files)
            {
                Database db =
                    null;

                try
                {
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


                    db =
                        new Database(
                            false,
                            true);


                    // 关键：
                    // 先让当前后台Database成为WorkingDatabase，
                    // 再读取DWG。
                    // 这样自定义对象在ReadDwgFile阶段就处于正确数据库上下文，
                    // 同时整个批量流程不再打开/关闭真实Document。
                    HostApplicationServices
                        .WorkingDatabase =
                            db;


                    db.ReadDwgFile(
                        file,
                        FileOpenMode
                            .OpenForReadAndAllShare,
                        false,
                        "");


                    db.CloseInput(
                        true);


                    AppLogger.Info(
                        "WorkingDatabase切换到："
                        + Path.GetFileName(file),
                        "BatchCheckerManager");


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
                    }
                    else
                    {
                        ProcessDrawing(
                            db,
                            file,
                            applyChanges,
                            archiveIndex,
                            versionArchiveIndex,
                            results);
                    }
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
                            FilePath = file,
                            FileName = Path.GetFileName(file),
                            Type = "文件处理错误",
                            ObjectName = "DWG",
                            Message =
                                ex.Message
                                + Environment.NewLine
                                + ex.StackTrace,
                            IsError = true
                        });
                }
                finally
                {
                    // 顺序不能反：
                    // 必须先恢复宿主WorkingDatabase，
                    // 再释放后台Database。
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


                    if (db != null)
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
                }


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
                        100);

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
                    progress?.Invoke(
                        percent,
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
            }


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


        private static void ProcessDrawing(
            Database db,
            string file,
            bool applyChanges,
            NonStandardArchiveIndex archiveIndex,
            VersionArchiveIndex versionArchiveIndex,
            List<CheckResult> results)
        {
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


            DrawingCheckManager manager =
                new DrawingCheckManager();

            List<CheckResult> oneResults =
                manager.CheckDrawing(
                    db,
                    file,
                    applyChanges,
                    report.Boms,
                    applyChanges);

            if (oneResults != null)
            {
                results.AddRange(
                    oneResults);
            }


            if (applyChanges)
            {
                CreateMarkers(
                    db,
                    report);
            }


            AppendStandardPartResults(
                file,
                report,
                results);

            AppendNonStandardArchiveResults(
                file,
                report,
                archiveIndex,
                results);

            AppendNonStandardPartNumberResults(
                file,
                report,
                results);

            AppendVersionResults(
                file,
                report,
                results);

            AppendBomCalloutResults(
                file,
                report,
                results);


            if (applyChanges)
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
                            FilePath = file,
                            FileName = Path.GetFileName(file),
                            Type = "文件保存错误",
                            ObjectName = "DWG",
                            Message =
                                "SafeDwgSaver保存失败，详见日志",
                            IsError = true
                        });
                }
            }
        }


        private static void CreateMarkers(
            Database db,
            CheckReport report)
        {
            MarkerManager markerManager =
                new MarkerManager();

            markerManager.ClearMarkers(
                db);


            if (report.Results != null)
            {
                markerManager.CreateMarkers(
                    db,
                    report.Results);
            }


            if (report.NonStandardArchiveResults != null)
            {
                markerManager
                    .CreateNonStandardArchiveMarkers(
                        db,
                        report.NonStandardArchiveResults);
            }


            if (report.NonStandardPartNumberResults != null)
            {
                markerManager
                    .CreateNonStandardPartNumberMarkers(
                        db,
                        report.NonStandardPartNumberResults);
            }


            if (report.VersionCheckResults != null)
            {
                markerManager
                    .CreateVersionMarkers(
                        db,
                        report.VersionCheckResults);
            }


            if (report.BomCalloutResult != null)
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


        private static void AppendStandardPartResults(
            string file,
            CheckReport report,
            List<CheckResult> results)
        {
            if (report.Results == null)
            {
                return;
            }

            foreach (
                StandardPartCheckResult standardResult
                in report.Results)
            {
                if (standardResult == null ||
                    standardResult.Status ==
                    StandardPartCheckStatus.Correct)
                {
                    continue;
                }

                results.Add(
                    new CheckResult
                    {
                        FilePath = file,
                        FileName = Path.GetFileName(file),
                        LayoutName =
                            standardResult.SourceLayoutName,
                        DrawingNumber =
                            standardResult.DrawingNumber,
                        PartNumber =
                            standardResult.BomItem == null
                                ? ""
                                : standardResult
                                    .BomItem
                                    .PartNumber,
                        PartName =
                            standardResult.BomItem == null
                                ? ""
                                : standardResult
                                    .BomItem
                                    .Name,
                        CorrectValue =
                            standardResult.Status ==
                            StandardPartCheckStatus.NameError
                                ? standardResult.CorrectName
                                : standardResult.CorrectPartNumber,
                        Type = "标准件检查",
                        ObjectName = "标准件",
                        CurrentValue =
                            standardResult.BomItem == null
                                ? ""
                                : standardResult
                                    .BomItem
                                    .PartNumber,
                        ExpectedValue =
                            standardResult.CorrectPartNumber,
                        Message =
                            standardResult.Message,
                        IsError = true
                    });
            }
        }


        private static void AppendNonStandardArchiveResults(
            string file,
            CheckReport report,
            NonStandardArchiveIndex archiveIndex,
            List<CheckResult> results)
        {
            if (report.NonStandardArchiveResults == null)
            {
                return;
            }

            string archiveRoot =
                archiveIndex == null
                    ? ""
                    : archiveIndex.RootPath;

            foreach (
                NonStandardArchiveCheckResult archiveResult
                in report.NonStandardArchiveResults)
            {
                if (archiveResult == null)
                {
                    continue;
                }

                BomItem item =
                    archiveResult.BomItem;

                results.Add(
                    new CheckResult
                    {
                        FilePath = file,
                        FileName = Path.GetFileName(file),
                        LayoutName =
                            archiveResult.SourceLayoutName,
                        DrawingNumber =
                            archiveResult.DrawingNumber,
                        PartNumber =
                            archiveResult.OriginalPartNumber,
                        PartName =
                            item == null
                                ? ""
                                : item.Name,
                        Type = "非标归档检查",
                        ObjectName = "非标件",
                        CurrentValue =
                            archiveResult.OriginalPartNumber,
                        ExpectedValue =
                            archiveRoot
                            + " 中存在图号 "
                            + archiveResult.SearchKey
                            + " 的归档文件",
                        CorrectValue =
                            archiveResult.SearchKey,
                        Message =
                            archiveResult.Message,
                        IsError = true
                    });
            }
        }


        private static void AppendNonStandardPartNumberResults(
            string file,
            CheckReport report,
            List<CheckResult> results)
        {
            if (report.NonStandardPartNumberResults == null)
            {
                return;
            }

            foreach (
                NonStandardPartNumberCheckResult partResult
                in report.NonStandardPartNumberResults)
            {
                if (partResult == null)
                {
                    continue;
                }

                BomItem item =
                    partResult.BomItem;

                results.Add(
                    new CheckResult
                    {
                        FilePath = file,
                        FileName = Path.GetFileName(file),
                        LayoutName =
                            partResult.SourceLayoutName,
                        DrawingNumber =
                            partResult.DrawingNumber,
                        PartNumber =
                            partResult.OriginalPartNumber,
                        PartName =
                            item == null
                                ? ""
                                : item.Name,
                        Type = "非标件号检查",
                        ObjectName = "非标件号",
                        CurrentValue =
                            partResult.OriginalPartNumber,
                        ExpectedValue =
                            partResult.ArchiveDrawingNumber
                            + " + _"
                            + partResult.PartSuffix,
                        CorrectValue =
                            partResult.ArchiveDrawingNumber
                            + partResult.PartSuffix,
                        Message =
                            partResult.Message,
                        IsError = true
                    });
            }
        }


        private static void AppendVersionResults(
            string file,
            CheckReport report,
            List<CheckResult> results)
        {
            if (report.VersionCheckResults == null)
            {
                return;
            }

            foreach (
                VersionCheckResult versionResult
                in report.VersionCheckResults)
            {
                if (versionResult == null)
                {
                    continue;
                }

                results.Add(
                    new CheckResult
                    {
                        FilePath = file,
                        FileName = Path.GetFileName(file),
                        LayoutName =
                            versionResult.LayoutName,
                        DrawingNumber =
                            versionResult.DrawingNumber,
                        Type = "版本号检查",
                        ObjectName = "版本号",
                        CurrentValue =
                            versionResult.CurrentVersion,
                        ExpectedValue =
                            versionResult.LatestVersion,
                        CorrectValue =
                            versionResult.LatestVersion,
                        Message =
                            versionResult.Message,
                        IsError = true
                    });
            }
        }


        private static void AppendBomCalloutResults(
            string file,
            CheckReport report,
            List<CheckResult> results)
        {
            if (report.BomCalloutResult == null)
            {
                return;
            }


            if (report.BomCalloutResult.MissingIssues != null)
            {
                foreach (
                    BomCalloutIssue issue
                    in report.BomCalloutResult.MissingIssues)
                {
                    if (issue == null)
                    {
                        continue;
                    }

                    results.Add(
                        new CheckResult
                        {
                            FilePath = file,
                            FileName = Path.GetFileName(file),
                            LayoutName =
                                issue.LayoutName,
                            Type = "BOM序号检查",
                            ObjectName =
                                "序号" + issue.Number,
                            CurrentValue =
                                "BOM中存在，图中缺失",
                            ExpectedValue =
                                "图中应存在序号 "
                                + issue.Number,
                            CorrectValue =
                                issue.Number.ToString(),
                            Message =
                                issue.Message,
                            IsError = true
                        });
                }
            }


            if (report.BomCalloutResult.ExtraIssues != null)
            {
                foreach (
                    BomCalloutIssue issue
                    in report.BomCalloutResult.ExtraIssues)
                {
                    if (issue == null)
                    {
                        continue;
                    }

                    results.Add(
                        new CheckResult
                        {
                            FilePath = file,
                            FileName = Path.GetFileName(file),
                            LayoutName =
                                issue.LayoutName,
                            Type = "BOM序号检查",
                            ObjectName =
                                "序号" + issue.Number,
                            CurrentValue =
                                "图中存在序号 "
                                + issue.Number,
                            ExpectedValue =
                                "BOM中应存在对应序号",
                            CorrectValue = "",
                            Message =
                                issue.Message,
                            IsError = true
                        });
                }
            }
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
                using (
                    Transaction tr =
                        database
                            .TransactionManager
                            .StartTransaction())
                {
                    BlockTable blockTable =
                        tr.GetObject(
                            database.BlockTableId,
                            OpenMode.ForRead)
                        as BlockTable;

                    if (blockTable == null)
                    {
                        return false;
                    }

                    BlockTableRecord modelSpace =
                        tr.GetObject(
                            blockTable[
                                BlockTableRecord.ModelSpace],
                            OpenMode.ForRead)
                        as BlockTableRecord;

                    if (modelSpace == null)
                    {
                        return false;
                    }

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
                        foreach (
                            DBDictionaryEntry entry
                            in layouts)
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
                            {
                                continue;
                            }

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
                    !currentDocument.Database.IsDisposed)
                {
                    return currentDocument;
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
                    !newDocument.Database.IsDisposed)
                {
                    AppLogger.Info(
                        "空白宿主图纸创建成功",
                        "BatchCheckerManager");

                    return newDocument;
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
