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


                BomStandardPartChecker checker =
                    new BomStandardPartChecker();


                BomCalloutChecker calloutChecker =
                    new BomCalloutChecker();


                NonStandardArchiveChecker
                    nonStandardArchiveChecker =
                        new NonStandardArchiveChecker();


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
                    if (!recognizer.IsBom(
                            table))
                    {
                        continue;
                    }


                    BomData bom =
                        recognizer.Parse(
                            table);


                    boms.Add(
                        bom);


                    //--------------------------------
                    // 图号
                    //--------------------------------

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


                    //--------------------------------
                    // 原有标准件检查
                    //--------------------------------

                    if (report
                        .StandardPartDatabaseAvailable)
                    {
                        report.Results.AddRange(
                            checker.Check(
                                bom));
                    }


                    //--------------------------------
                    // 原有NS归档检查
                    //--------------------------------

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
                }


                //==================================================
                // BOM序号检查
                //==================================================

                HashSet<int> bomNumbers =
                    new HashSet<int>();


                foreach (
                    BomData bom
                    in boms)
                {
                    bomNumbers.UnionWith(
                        calloutChecker
                            .GetBomNumbers(
                                bom));
                }


                report.BomNumbers =
                    new HashSet<int>(
                        bomNumbers);


                LayoutReader layoutReader =
                    new LayoutReader();


                ViewportTextReader viewportTextReader =
                    new ViewportTextReader();


                ViewportLineReader viewportLineReader =
                    new ViewportLineReader();


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


                HashSet<int> drawingNumbers =
                    layoutReader
                        .IdentifyDrawingBomNumbers(
                            drawingTexts,
                            drawingLines);


                report.DrawingNumbers =
                    new HashSet<int>(
                        drawingNumbers);


                report.BomCalloutResult =
                    calloutChecker.Check(
                        bomNumbers,
                        drawingNumbers);


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