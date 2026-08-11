using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

using Correct_test1.Models;
using Correct_test1.Readers;
using Correct_test1.Markers;
using Correct_test1.Configs;
using System.IO;
using Correct_test1.Core;
namespace Correct_test1.Checks
{


    /// <summary>
    /// 标题栏检查管理器
    ///
    /// 负责:
    /// 1. 读取标题栏文字
    /// 2. 判断横竖版
    /// 3. 解析标题栏
    /// 4. 调用标题栏检查
    ///
    /// 不负责:
    /// 绿色标记
    /// </summary>
    public class TitleBlockCheckManager
    {


        private readonly TitleBlockReader reader;

        private readonly TitleBlockRegionParser parser;

        private readonly TitleBlockChecker checker;

        private readonly TitleBlockDrawingNumberMarker drawingNumberMarker;


        public TitleBlockCheckManager()
        {

            reader =
                new TitleBlockReader();


            parser =
                new TitleBlockRegionParser();


            checker =
                new TitleBlockChecker();

            drawingNumberMarker =
    new TitleBlockDrawingNumberMarker();

        }




        /// <summary>
        /// 检查一个布局标题栏
        /// </summary>
        public List<CheckResult> Check(
            Autodesk.AutoCAD.DatabaseServices.Database db,
            LayoutInfo layout,
            string filePath,
            string fileName,
            bool drawMarker,
            int expectedPage = 0,
            int expectedPageCount = 0,
            string bomDrawingNumber = null,
            Autodesk.AutoCAD.Geometry.Point3d bomDrawingNumberPosition = default(Autodesk.AutoCAD.Geometry.Point3d))
        {


            List<CheckResult> results =
                new List<CheckResult>();



            if (layout == null)
                return results;



            //--------------------------------
            // 读取当前布局标题栏文字
            //--------------------------------

            List<TitleText> texts =
                reader.Read(
                    db,
                    new List<LayoutInfo>
                    {
                        layout
                    }
                );



            if (texts.Count == 0)
                return results;




            //--------------------------------
            // 判断横竖版
            // 保持原有逻辑
            //--------------------------------

            int markCount = 0;


            foreach (TitleText t in texts)
            {

                if (t.Text.Contains("标记"))
                {
                    markCount++;
                }

            }



            bool isHorizontal =
                markCount >= 2;



            //--------------------------------
            // 标题栏解析
            //--------------------------------

            DrawingInfo info =
                parser.Parse(
                    texts,
                    isHorizontal
                );



            info.FilePath =
                filePath;


            info.FileName =
                fileName;


            info.LayoutName =
                layout.LayoutName;


            info.IsHorizontal =
                isHorizontal;

            List<TextHeightIssue> textHeightIssues =
                CheckTextHeights(texts, isHorizontal);

            if (drawMarker)
            {
                new MarkerManager().CreateTextHeightMarkers(
                    db,
                    textHeightIssues);
            }




            //--------------------------------
            // 标题栏字段检查
            //--------------------------------

            results.AddRange(
                checker.Check(
                    info
                )
            );

            string pageMessage = CheckPageNumber(
                info.PageNumber,
                expectedPage,
                expectedPageCount);

            if (!string.IsNullOrEmpty(pageMessage))
            {
                results.Add(new CheckResult
                {
                    FilePath = filePath,
                    FileName = fileName,
                    LayoutName = info.LayoutName,
                    Type = "页码检查",
                    ObjectName = "页码",
                    CurrentValue = info.PageNumber ?? "",
                    ExpectedValue = expectedPage + "/" + expectedPageCount,
                    Message = pageMessage,
                    IsError = true
                });
            }

            if (drawMarker)
            {
                TitleBlockFieldMarker fieldMarker =
                    new TitleBlockFieldMarker();

                foreach (CheckResult result in results)
                {
                    if (!result.IsError ||
                        result.Type != "标题栏检查")
                    {
                        continue;
                    }

                    fieldMarker.DrawMarker(
                        db,
                        layout.LayoutName,
                        info.IsHorizontal,
                        result.ObjectName,
                        "标题栏" + result.ObjectName + "未填写");
                }

                if (!string.IsNullOrEmpty(pageMessage))
                {
                    fieldMarker.DrawMarker(
                        db,
                        layout.LayoutName,
                        info.IsHorizontal,
                        "PageNumber",
                        pageMessage);
                }
            }




            //--------------------------------
            // 标题栏图号一致性检查
            //--------------------------------

            try
            {

                FileNameDrawingNumberReader fileReader =
                    new FileNameDrawingNumberReader();



                string fileDrawingNumber =
                    fileReader.ReadDrawingNumber(
                        filePath
                    )
                    ?? "";



                string titleDrawingNumber =
                    info.DrawingNumber
                    ?? "";



                if (!string.IsNullOrWhiteSpace(fileDrawingNumber)
                    &&
                    !string.IsNullOrWhiteSpace(titleDrawingNumber)
                    &&
                    !fileDrawingNumber.Equals(
                        titleDrawingNumber,
                        System.StringComparison.Ordinal
                    ))
                {

                    results.Add(
     new CheckResult
     {
         FilePath = filePath,

         FileName = fileName,

         LayoutName = info.LayoutName,

         Mark = "",

         Type = "标题栏图号检查",

         ObjectName = "图号",

         CurrentValue = titleDrawingNumber,

         ExpectedValue = fileDrawingNumber,

         Message =
             "标题栏图号与文件名图号不一致",

         IsError = true
     }
 );
                     if (drawMarker)
                    {
                        drawingNumberMarker.DrawMarker(
                            db,
                            layout.LayoutName,
                            info.IsHorizontal,
                            fileDrawingNumber
                        );
                    }

                }

                if (!string.IsNullOrWhiteSpace(bomDrawingNumber)
                    && !string.IsNullOrWhiteSpace(titleDrawingNumber)
                    && !bomDrawingNumber.Equals(
                        titleDrawingNumber,
                        System.StringComparison.Ordinal))
                {
                    results.Add(
                        new CheckResult
                        {
                            FilePath = filePath,
                            FileName = fileName,
                            LayoutName = info.LayoutName,
                            Mark = "",
                            Type = "标题栏图号检查",
                            ObjectName = "图号",
                            CurrentValue = titleDrawingNumber,
                            ExpectedValue = bomDrawingNumber,
                            Message = "标题栏图号与BOM表上方图号不一致",
                            IsError = true
                        }
                    );

                     if (drawMarker &&
                         string.IsNullOrWhiteSpace(fileDrawingNumber))
                    {
                        drawingNumberMarker.DrawMarker(
                            db,
                            layout.LayoutName,
                            info.IsHorizontal,
                            bomDrawingNumber,
                            bomDrawingNumberPosition
                        );
                    }
                }

                if (!string.IsNullOrWhiteSpace(bomDrawingNumber)
                    && ((!string.IsNullOrWhiteSpace(fileDrawingNumber)
                            && !bomDrawingNumber.Equals(
                                fileDrawingNumber,
                                System.StringComparison.Ordinal))
                        || (string.IsNullOrWhiteSpace(fileDrawingNumber)
                            && !string.IsNullOrWhiteSpace(titleDrawingNumber)
                            && !bomDrawingNumber.Equals(
                                titleDrawingNumber,
                                System.StringComparison.Ordinal))))
                {
                    string expectedDrawingNumber =
                        !string.IsNullOrWhiteSpace(fileDrawingNumber)
                            ? fileDrawingNumber
                            : titleDrawingNumber;

                    results.Add(
                        new CheckResult
                        {
                            FilePath = filePath,
                            FileName = fileName,
                            LayoutName = info.LayoutName,
                            Mark = "",
                            Type = "标题栏图号检查",
                            ObjectName = "图号",
                            CurrentValue = bomDrawingNumber,
                            ExpectedValue = expectedDrawingNumber,
                            Message = "BOM表上方图号与文件名图号不一致",
                            IsError = true
                        }
                    );

                    if (drawMarker)
                    {
                        drawingNumberMarker.DrawMarker(
                            db,
                            layout.LayoutName,
                            info.IsHorizontal,
                            expectedDrawingNumber,
                            bomDrawingNumberPosition
                        );
                    }
                }

            }
            catch (System.Exception ex)
            {
                AppLogger.Error(ex, "TitleBlockCheckManager");
            }




            return results;

        }

        private string CheckPageNumber(
            string value,
            int expectedPage,
            int expectedPageCount)
        {
            if (expectedPage <= 0 || expectedPageCount <= 0)
                return "";

            MatchCollection matches = Regex.Matches(
                value ?? "",
                @"\d+");

            if (matches.Count < 2)
            {
                return "页码错误  当前页: " +
                    (string.IsNullOrWhiteSpace(value) ? "空" : value) +
                    "  正确页码: " + expectedPage + "/" + expectedPageCount;
            }

            int actualPage = 0;
            int actualPageCount = 0;
            if (!int.TryParse(matches[0].Value, out actualPage) ||
                !int.TryParse(matches[1].Value, out actualPageCount) ||
                actualPage != expectedPage ||
                actualPageCount != expectedPageCount ||
                actualPage > actualPageCount)
            {
                return "页码错误  当前页: " + actualPage + "/" + actualPageCount +
                    "  正确页码: " + expectedPage + "/" + expectedPageCount;
            }

            return "";
        }

        private List<TextHeightIssue> CheckTextHeights(
            List<TitleText> texts,
            bool isHorizontal)
        {
            List<TextHeightIssue> issues = new List<TextHeightIssue>();
            List<TitleFieldRegion> regions = isHorizontal
                ? TitleBlockHorizontalConfig.Regions
                : TitleBlockVerticalConfig.Regions;

            AddRegionHeightIssues(
                texts,
                regions.Find(x => x.FieldName == "DrawingName"),
                5.0,
                "名称文字高度错误",
                issues);

            AddRegionHeightIssues(
                texts,
                regions.Find(x => x.FieldName == "DrawingNumber"),
                3.5,
                "图号文字高度错误",
                issues);

            TitleText technicalTitle = texts.Find(x =>
                (x.Text ?? "").Contains("技术要求"));

            if (technicalTitle != null)
            {
                AddHeightIssue(
                    technicalTitle,
                    5.0,
                    "技术要求标题文字高度错误",
                    issues);

                List<TitleText> technicalTexts = texts
                    .Where(x => x.Y < technicalTitle.Y &&
                                Math.Abs(x.X - technicalTitle.X) < 100 &&
                                Regex.IsMatch(
                                    (x.Text ?? "").Trim(),
                                    @"^\d+\s*[\.、．:：]"))
                    .OrderByDescending(x => x.Y)
                    .ToList();

                foreach (TitleText technicalText in technicalTexts)
                {
                    AddHeightIssue(
                        technicalText,
                        3.5,
                        "技术要求文字高度错误",
                        issues);
                }
            }

            return issues;
        }

        private void AddRegionHeightIssues(
            List<TitleText> texts,
            TitleFieldRegion region,
            double expectedHeight,
            string message,
            List<TextHeightIssue> issues)
        {
            if (region == null)
                return;

            foreach (TitleText text in texts)
            {
                if (region.Contains(text.X, text.Y))
                {
                    AddHeightIssue(text, expectedHeight, message, issues);
                }
            }
        }

        private void AddHeightIssue(
            TitleText text,
            double expectedHeight,
            string message,
            List<TextHeightIssue> issues)
        {
            if (Math.Abs(text.Height - expectedHeight) >= 0.01)
            {
                issues.Add(new TextHeightIssue
                {
                    LayoutName = text.LayoutName,
                    Position = new Autodesk.AutoCAD.Geometry.Point3d(
                        text.X + 5,
                        text.Y,
                        0),
                    Message = message +
                        " 当前高度:" + text.Height.ToString("0.###") +
                        " 正确高度:" + expectedHeight.ToString("0.0")
                });
            }
        }


    }

}