using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.Core;
using Correct_test1.Models;
using Correct_test1.Readers;

using Correct_test1.VersionCheck.Core;
using Correct_test1.VersionCheck.Services;

using System;
using System.Collections.Generic;
using System.Linq;


namespace Correct_test1.Checks
{
    public class CheckService
    {
        //==================================================
        // 普通单张检查入口
        //==================================================

        public CheckReport Check(
            Database database)
        {
            //--------------------------------
            // 原有非标归档缓存
            //--------------------------------

            NonStandardArchiveIndex archiveIndex =
                NonStandardArchiveCache
                    .GetOrBuild();


            //--------------------------------
            // 新增版本归档缓存
            //--------------------------------

            VersionArchiveIndex versionArchiveIndex =
                VersionArchiveCache
                    .GetOrBuild();


            return Check(
                database,
                archiveIndex,
                versionArchiveIndex);
        }


        //==================================================
        // 保留原有批量调用兼容
        //==================================================

        public CheckReport Check(
            Database database,
            NonStandardArchiveIndex archiveIndex)
        {
            VersionArchiveIndex versionArchiveIndex =
                VersionArchiveCache
                    .GetOrBuild();


            return Check(
                database,
                archiveIndex,
                versionArchiveIndex);
        }


        //==================================================
        // 完整检查入口
        //==================================================

        public CheckReport Check(
            Database database,
            NonStandardArchiveIndex archiveIndex,
            VersionArchiveIndex versionArchiveIndex)
        {
            CheckReport report =
                new CheckReport();


            report.CheckTime =
                DateTime.Now;


            if (database == null)
            {
                return report;
            }


            report.DrawingName =
                database.Filename;


            //==================================================
            // 外部数据源状态
            //==================================================

            //--------------------------------
            // 非标归档
            //--------------------------------

            report.NonStandardArchiveAvailable =
                archiveIndex != null &&
                archiveIndex.IsAvailable;


            if (!report.NonStandardArchiveAvailable)
            {
                report.NonStandardArchiveError =
                    archiveIndex == null
                        ? "非标归档索引未建立。"
                        : archiveIndex.ErrorMessage;
            }


            //--------------------------------
            // 新增：版本归档
            //--------------------------------

            report.VersionArchiveAvailable =
                versionArchiveIndex != null &&
                versionArchiveIndex.IsAvailable;


            if (!report.VersionArchiveAvailable)
            {
                report.VersionArchiveError =
                    versionArchiveIndex == null
                        ? "版本归档索引未建立。"
                        : versionArchiveIndex.ErrorMessage;
            }


            //--------------------------------
            // 标准件数据库
            //--------------------------------

            string standardPartError;


            report.StandardPartDatabaseAvailable =
                StandardPartDatabase
                    .TryEnsureLoaded(
                        out standardPartError);


            if (!report.StandardPartDatabaseAvailable)
            {
                report.StandardPartDatabaseError =
                    standardPartError ?? "";
            }


            Database previousWorkingDatabase =
                HostApplicationServices
                    .WorkingDatabase;


            try
            {
                HostApplicationServices
                    .WorkingDatabase =
                        database;


                CadTableReader tableReader =
                    new CadTableReader();


                BomTableRecognizer recognizer =
                    new BomTableRecognizer();
                BomProjectNumberReader
    bomProjectNumberReader =
        new BomProjectNumberReader();


                BomStandardPartChecker checker =
                    new BomStandardPartChecker();


                BomCalloutChecker calloutChecker =
                    new BomCalloutChecker();


                NonStandardArchiveChecker
                    nonStandardArchiveChecker =
                        new NonStandardArchiveChecker();

                //--------------------------------
                // 新增：非标件号存在性检查
                //--------------------------------

                NonStandardPartNumberChecker
                    nonStandardPartNumberChecker =
                        new NonStandardPartNumberChecker();


                List<CadTableData> tables =
                    tableReader.Read(
                        database);


                List<BomData> boms =
                    new List<BomData>();


                //==================================================
                // BOM检查
                //==================================================

                foreach (
    CadTableData table
    in tables)
                {
                    //--------------------------------
                    // 不是BOM表则跳过
                    //--------------------------------

                    if (!recognizer.IsBom(
                            table))
                    {
                        continue;
                    }


                    //--------------------------------
                    // 解析BOM
                    //--------------------------------

                    BomData bom =
                        recognizer.Parse(
                            table);


                    if (bom == null)
                    {
                        continue;
                    }

                    //==================================================
                    // 读取当前BOM自己右侧的项目号
                    //
                    // 注意：
                    // 不再使用当前DWG文件名决定这个BOM属于哪个项目。
                    //==================================================

                    bom.ProjectNumber =
                        bomProjectNumberReader.Read(
                            database,
                            table,
                            bom);
                    //--------------------------------
                    // 保存BOM
                    //--------------------------------

                    boms.Add(
                        bom);


                    //==================================================
                    // 当前图纸图号
                    //==================================================

                    if (string.IsNullOrEmpty(
                            report.DrawingNumber) &&
                        !string.IsNullOrEmpty(
                            bom.DrawingNumber))
                    {
                        report.DrawingNumber =
                            bom.DrawingNumber;


                        report.DrawingNumberPosition =
                            bom.DrawingNumberPosition;
                    }


                    //==================================================
                    // 原有标准件检查
                    //==================================================

                    if (report
                        .StandardPartDatabaseAvailable)
                    {
                        report.Results.AddRange(
                            checker.Check(
                                bom));
                    }


                    //==================================================
                    // 原有NS非标归档存在性检查
                    //
                    // 例如：
                    //
                    // NS333T1
                    // ↓
                    // 检查Z盘是否存在NS333T对应归档文件
                    //==================================================

                    if (report
                        .NonStandardArchiveAvailable)
                    {
                        report
                            .NonStandardArchiveResults
                            .AddRange(
                                nonStandardArchiveChecker
                                    .Check(
                                        bom,
                                        archiveIndex));
                    }


                    //==================================================
                    // 新增：NS非标件号存在性检查
                    //
                    // 例如：
                    //
                    // BOM：
                    // NS333T1
                    //
                    // ↓
                    //
                    // 找：
                    // NS333T + 当前项目号 的归档DWG
                    //
                    // ↓
                    //
                    // 打开归档DWG所有Layout
                    //
                    // ↓
                    //
                    // 查找：
                    //
                    // NS333T    _1
                    // 重量
                    // 备注
                    //==================================================

                    if (report
                        .NonStandardArchiveAvailable)
                    {
                        report
                            .NonStandardPartNumberResults
                            .AddRange(
                                nonStandardPartNumberChecker
                                    .Check(
                                        bom,
                                        database.Filename,
                                        archiveIndex));
                    }
                }



                //==================================================
                // BOM序号检查
                //
                // 新版：
                // 每一个Layout完全独立比较。
                //==================================================

                LayoutReader layoutReader =
                    new LayoutReader();


                ViewportTextReader viewportTextReader =
                    new ViewportTextReader();


                ViewportLineReader viewportLineReader =
                    new ViewportLineReader();


                //==================================================
                // 这里仍然只扫描一次CAD。
                //
                // 不会因为Layout分组而重复扫描。
                //==================================================

                List<TitleText> drawingTexts =
                    viewportTextReader.Read(
                        database,
                        true,
                        true);


                report.DrawingTexts =
                    drawingTexts;


                List<CadLineInfo> drawingLines =
                    viewportLineReader.Read(
                        database,
                        true);


                //==================================================
                // 最终总结果
                //==================================================

                BomCalloutResult totalCalloutResult =
                    new BomCalloutResult();


                HashSet<int> totalBomNumbers =
                    new HashSet<int>();


                HashSet<int> totalDrawingNumbers =
                    new HashSet<int>();


                //==================================================
                // 只处理真正存在BOM的Layout。
                //
                // 一个没有BOM的Layout，
                // 不应该因为存在数字就全部判成“多余BOM序号”。
                //==================================================

                HashSet<string> bomLayoutNames =
                    new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase);


                foreach (
                    BomData bom
                    in boms)
                {
                    if (bom == null ||
                        string.IsNullOrWhiteSpace(
                            bom.SourceLayoutName))
                    {
                        continue;
                    }


                    bomLayoutNames.Add(
                        bom.SourceLayoutName);
                }


                //==================================================
                // 一个Layout一个Layout处理
                //==================================================

                foreach (
                    string layoutName
                    in bomLayoutNames)
                {
                    //==================================================
                    // 当前Layout的BOM
                    //==================================================

                    List<BomData> layoutBoms =
                        boms
                            .Where(
                                bom =>
                                    bom != null &&
                                    string.Equals(
                                        bom.SourceLayoutName,
                                        layoutName,
                                        StringComparison
                                            .OrdinalIgnoreCase))
                            .ToList();


                    //==================================================
                    // 当前Layout视口中的文字
                    //==================================================

                    List<TitleText> layoutTexts =
                        drawingTexts
                            .Where(
                                text =>
                                    text != null &&
                                    string.Equals(
                                        text.LayoutName,
                                        layoutName,
                                        StringComparison
                                            .OrdinalIgnoreCase))
                            .ToList();


                    //==================================================
                    // 当前Layout视口中的线
                    //==================================================

                    List<CadLineInfo> layoutLines =
                        drawingLines
                            .Where(
                                line =>
                                    line != null &&
                                    string.Equals(
                                        line.LayoutName,
                                        layoutName,
                                        StringComparison
                                            .OrdinalIgnoreCase))
                            .ToList();


                    //==================================================
                    // 当前Layout的BOM序号
                    //==================================================

                    HashSet<int> layoutBomNumbers =
                        new HashSet<int>();


                    foreach (
                        BomData bom
                        in layoutBoms)
                    {
                        layoutBomNumbers.UnionWith(
                            calloutChecker
                                .GetBomNumbers(
                                    bom));
                    }


                    //==================================================
                    // 当前Layout图中实际序号
                    //==================================================

                    HashSet<int> layoutDrawingNumbers =
                        layoutReader
                            .IdentifyDrawingBomNumbers(
                                layoutTexts,
                                layoutLines);


                    //==================================================
                    // 保留总集合，
                    // 兼容CheckReport已有字段。
                    //
                    // 但这些总集合以后不再用于判断Marker。
                    //==================================================

                    totalBomNumbers.UnionWith(
                        layoutBomNumbers);


                    totalDrawingNumbers.UnionWith(
                        layoutDrawingNumbers);


                    //==================================================
                    // 真正按Layout检查
                    //==================================================

                    BomCalloutResult layoutResult =
                        calloutChecker.CheckLayout(
                            layoutName,
                            layoutBoms,
                            layoutDrawingNumbers,
                            layoutTexts);


                    //==================================================
                    // 聚合结果
                    //==================================================

                    totalCalloutResult
                        .MissingCallouts
                        .UnionWith(
                            layoutResult
                                .MissingCallouts);


                    totalCalloutResult
                        .ExtraCallouts
                        .UnionWith(
                            layoutResult
                                .ExtraCallouts);


                    totalCalloutResult
                        .MissingIssues
                        .AddRange(
                            layoutResult
                                .MissingIssues);


                    totalCalloutResult
                        .ExtraIssues
                        .AddRange(
                            layoutResult
                                .ExtraIssues);
                }


                //==================================================
                // 写入报告
                //==================================================

                report.BomNumbers =
                    totalBomNumbers;


                report.DrawingNumbers =
                    totalDrawingNumbers;


                report.BomCalloutResult =
                    totalCalloutResult;


                //==================================================
                // 新增：版本号检查
                //==================================================

                if (report.VersionArchiveAvailable)
                {
                    VersionCheckService
                        versionCheckService =
                            new VersionCheckService();


                    List<VersionCheck.Models.VersionCheckResult>
                        versionResults =
                            versionCheckService.Check(
                                database,
                                database.Filename,
                                versionArchiveIndex);


                    if (versionResults != null)
                    {
                        report
                            .VersionCheckResults
                            .AddRange(
                                versionResults);
                    }
                }


                //==================================================
                // 原有统计
                //==================================================

                report.TotalCount =
                    report.Results.Count;


                report.CorrectCount =
                    report.Results.Count(
                        result =>
                            result.Status ==
                            StandardPartCheckStatus
                                .Correct);


                report.ErrorCount =
                    report.TotalCount -
                    report.CorrectCount;


                foreach (
                    StandardPartCheckResult result
                    in report.Results)
                {
                    if (string.IsNullOrEmpty(
                            report.DrawingNumber) &&
                        !string.IsNullOrEmpty(
                            result.DrawingNumber))
                    {
                        report.DrawingNumber =
                            result.DrawingNumber;
                    }
                }


                report.Boms =
                    boms;


                return report;
            }
            finally
            {
                HostApplicationServices
                    .WorkingDatabase =
                        previousWorkingDatabase;
            }
        }
    }
}