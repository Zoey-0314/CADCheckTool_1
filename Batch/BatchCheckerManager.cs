using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.Checks;
using Correct_test1.Core;
using Correct_test1.Markers;
using Correct_test1.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;

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
        /// 
        /// progress:
        /// percent 当前百分比
        /// total 文件数量
        /// fileName 当前文件
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
            // 第一步：计算所有DWG权重
            //--------------------------------

            DrawingWeightCalculator calculator =
                new DrawingWeightCalculator();

            Dictionary<string, double> weights =
                new Dictionary<string, double>();

            double totalWeight = 0;

            foreach (string file in files)
            {

                double weight =
                    calculator.Calculate(
                        file
                    );

                weights.Add(
                    file,
                    weight
                );

                totalWeight +=
                    weight;

            }

            if (totalWeight <= 0)
                totalWeight = 1;

            double finishedWeight = 0;

            //--------------------------------
            // 第二步：正式检查
            //--------------------------------

            foreach (string file in files)
            {

                Database db = null;

                try
                {

                    db =
                        new Database(
                            false,
                            true
                        );

                    db.ReadDwgFile(
                        file,
                        FileOpenMode.OpenForReadAndAllShare,
                        false,
                        ""
                    );

                    db.CloseInput(true);

                    CheckService checkService =
                        new CheckService();
                    CheckReport report =
                        checkService.Check(db);
                    AppendBomCalloutDebugCsv(
                        Path.GetFileName(file),
                        report);

                    DrawingCheckManager manager =
                        new DrawingCheckManager();

                    List<CheckResult> oneResults =
                        manager.CheckDrawing(
                            db,
                            file,
                            true,
                            report.DrawingNumber,
                            report.DrawingNumberPosition
                        );

                    results.AddRange(
                        oneResults
                    );

                    MarkerManager markerManager =
                        new MarkerManager();

                    markerManager.ClearMarkers(db);

                    markerManager.CreateMarkers(
                        db,
                        report.Results);

                    markerManager.CreateMissingCalloutMarkers(
                        db,
                        report.BomCalloutResult.MissingCallouts,
                        report.Boms);

                    markerManager.CreateExtraCalloutMarkers(
                        db,
                        report.BomCalloutResult.ExtraCallouts,
                        report.DrawingTexts);


                    foreach (StandardPartCheckResult standardResult in report.Results)
                    {
                        if (standardResult.Status == StandardPartCheckStatus.Correct)
                        {
                            continue;
                        }

                        results.Add(new CheckResult
                        {
                            FilePath = file,
                            FileName = Path.GetFileName(file),
                            DrawingNumber = standardResult.DrawingNumber,
                            PartNumber = standardResult.BomItem == null
                                ? ""
                                : standardResult.BomItem.PartNumber,
                            PartName = standardResult.BomItem == null
                                ? ""
                                : standardResult.BomItem.Name,
                            CorrectValue = standardResult.Status == StandardPartCheckStatus.NameError
                                ? standardResult.CorrectName
                                : standardResult.CorrectPartNumber,
                            Type = "标准件检查",
                            ObjectName = "标准件",
                            CurrentValue = standardResult.BomItem == null
                                ? ""
                                : standardResult.BomItem.PartNumber,
                            ExpectedValue = standardResult.CorrectPartNumber,
                            Message = standardResult.Message,
                            IsError = true
                        });
                    }

                    //--------------------------------
                    // 保存绿色标记
                    //--------------------------------



                    bool saved = Correct_test1.Core.SafeDwgSaver.Save(
                        db,
                        file
                    );

                    if (!saved)
                    {
                        results.Add(new CheckResult
                        {
                            FilePath = file,
                            FileName = Path.GetFileName(file),
                            Type = "文件保存错误",
                            ObjectName = "DWG",
                            Message = "SafeDwgSaver 保存失败，详见日志",
                            IsError = true
                        });
                    }


                }
                catch (Exception ex)
                {

                    Correct_test1.Core.AppLogger.Error(ex, "BatchCheckerManager.CheckFolder", file);

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
                                "\n"
                                +
                                ex.StackTrace,

                            IsError =
                                true

                        }
                    );

                }
                finally
                {

                    if (db != null)
                    {
                        db.Dispose();
                    }

                }

                //--------------------------------
                // 更新真实进度
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

                progress?.Invoke(
                    percent,
                    files.Length,
                    Path.GetFileName(file)
                );

            }

            return results;

        }

        private static void AppendBomCalloutDebugCsv(
            string fileName,
            CheckReport report)
        {
            string desktop =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.DesktopDirectory);

            if (string.IsNullOrWhiteSpace(desktop))
                return;

            string csvPath = Path.Combine(
                desktop,
                "BatchBomCalloutDebug.csv");

            bool fileExists = File.Exists(csvPath);
            StringBuilder sb = new StringBuilder();

            if (!fileExists)
            {
                sb.AppendLine(
                    "FileName,BOMNumbers,DrawingNumbers,MissingCallouts,ExtraCallouts");
            }

            IEnumerable<int> bomNumbers =
                ExtractBomNumbers(report);

            string drawingNumbers =
                JoinNumbers(report?.DrawingNumbers);

            string missingCallouts =
                JoinNumbers(report?.BomCalloutResult?.MissingCallouts);

            string extraCallouts =
                JoinNumbers(report?.BomCalloutResult?.ExtraCallouts);

            sb.AppendLine(
                EscapeCsv(fileName) + "," +
                EscapeCsv(JoinNumbers(bomNumbers)) + "," +
                EscapeCsv(drawingNumbers) + "," +
                EscapeCsv(missingCallouts) + "," +
                EscapeCsv(extraCallouts));

            File.AppendAllText(
                csvPath,
                sb.ToString(),
                Encoding.UTF8);
        }

        private static IEnumerable<int> ExtractBomNumbers(
            CheckReport report)
        {
            if (report?.BomNumbers != null &&
                report.BomNumbers.Count > 0)
            {
                return report.BomNumbers;
            }

            HashSet<int> result = new HashSet<int>();

            if (report?.Boms == null)
                return result;

            foreach (BomData bom in report.Boms)
            {
                if (bom?.Items == null)
                    continue;

                foreach (BomItem item in bom.Items)
                {
                    int number;
                    string value = CadTextCleaner.Clean(item?.No);

                    if (int.TryParse(
                            value,
                            NumberStyles.None,
                            CultureInfo.InvariantCulture,
                            out number))
                    {
                        result.Add(number);
                    }
                }
            }

            return result;
        }

        private static string JoinNumbers(IEnumerable<int> numbers)
        {
            if (numbers == null)
                return "";

            List<int> ordered = numbers
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            if (ordered.Count == 0)
                return "";

            return string.Join(";", ordered);
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(",") ||
                value.Contains("\"") ||
                value.Contains("\n") ||
                value.Contains("\r"))
            {
                return "\"" +
                    value.Replace("\"", "\"\"") +
                    "\"";
            }

            return value;
        }

    }

}