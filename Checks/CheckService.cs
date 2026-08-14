using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.Core;
using Correct_test1.Models;
using Correct_test1.Readers;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Correct_test1.Checks
{
    public class CheckService
    {
        /// <summary>
        /// 普通单张检查入口。
        ///
        /// 单张检查时建立一次Z盘归档索引。
        /// </summary>
        public CheckReport Check(
    Database database)
        {
            //--------------------------------
            // 不再扫描Z盘。
            //
            // 优先使用PluginInitializer
            // 启动时已经预热好的缓存。
            //--------------------------------

            NonStandardArchiveIndex archiveIndex =
                NonStandardArchiveCache
                    .GetOrBuild();


            return Check(
                database,
                archiveIndex);
        }


        /// <summary>
        /// 带已有非标归档索引的检查入口。
        ///
        /// 批量检查使用此方法，
        /// 这样整个批量过程只扫描一次Z盘。
        /// </summary>
        public CheckReport Check(
            Database database,
            NonStandardArchiveIndex archiveIndex)
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


            //--------------------------------
            // 保存归档可用状态
            //--------------------------------

            report.NonStandardArchiveAvailable =
                archiveIndex != null &&
                archiveIndex.IsAvailable;
            //--------------------------------
            // 标准件数据库状态
            //--------------------------------

            string standardPartError;


            report.StandardPartDatabaseAvailable =
                StandardPartDatabase.TryEnsureLoaded(
                    out standardPartError);


            if (!report.StandardPartDatabaseAvailable)
            {
                report.StandardPartDatabaseError =
                    standardPartError ?? "";
            }

            if (!report.NonStandardArchiveAvailable)
            {
                report.NonStandardArchiveError =
                    archiveIndex == null
                        ? "非标归档索引未建立。"
                        : archiveIndex.ErrorMessage;
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


                //--------------------------------
                // BOM检查
                //--------------------------------

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

                    if (report.StandardPartDatabaseAvailable)
                    {
                        report.Results.AddRange(
                            checker.Check(
                                bom));
                    }


                    //--------------------------------
                    // 新增：
                    // NS非标件归档检查
                    //
                    // Z盘不可用时完全跳过，
                    // 绝不产生假“不存在”错误。
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


                //--------------------------------
                // 原有BOM序号检查
                //--------------------------------

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


                //--------------------------------
                // 原有统计
                //--------------------------------

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