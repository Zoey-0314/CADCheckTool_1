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

                    DrawingCheckManager manager =
                        new DrawingCheckManager();

                    List<CheckResult> oneResults =
                        manager.CheckDrawing(
                            db,
                            file,
                            true
                        );

                    results.AddRange(
                        oneResults
                    );

                    CheckService checkService =
                        new CheckService();
                    CheckReport report =
                        checkService.Check(db);

                    MarkerManager markerManager =
                        new MarkerManager();
                    markerManager.CreateMarkers(
                        db,
                        report.Results);

                    //--------------------------------
                    // 保存绿色标记
                    //--------------------------------



                    Correct_test1.Core.SafeDwgSaver.Save(
                        db,
                        file
                    );


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

    }

}