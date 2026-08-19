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
    /// 负责:
    /// 1. 读取标题栏文字
    /// 2. 判断横竖版
    /// 3. 解析标题栏
    /// 4. 调用标题栏检查
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
            List<BomData> boms = null,
            bool allowAutoFix = true)
        {


            List<CheckResult> results =
                new List<CheckResult>();



            if (layout == null)
                return results;



            // 读取当前布局标题栏文字

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




            // A3 / A4 直接决定横竖版。
            // 同时由图幅文字计算整张标题栏的平移偏移。

            TitleBlockAnchorInfo anchorInfo;

            bool hasAnchor =
                TitleBlockOrientationDetector
                    .TryResolveAnchor(
                        texts,
                        out anchorInfo);

            bool isHorizontal =
                hasAnchor
                    ? anchorInfo.IsHorizontal
                    : TitleBlockOrientationDetector
                        .IsHorizontal(
                            texts);

            double offsetX =
                hasAnchor
                    ? anchorInfo.OffsetX
                    : 0.0;

            double offsetY =
                hasAnchor
                    ? anchorInfo.OffsetY
                    : 0.0;

            List<TitleText> parseTexts =
                hasAnchor
                    ? TitleBlockOrientationDetector
                        .NormalizeToBaseline(
                            texts,
                            anchorInfo)
                    : texts;



            // 标题栏解析

            DrawingInfo info =
                parser.Parse(
                    parseTexts,
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
                CheckTextHeights(
                    texts,
                    isHorizontal,
                    offsetX,
                    offsetY);

            if (drawMarker)
            {
                new MarkerManager().CreateTextHeightMarkers(
                    db,
                    textHeightIssues);
            }




            // 标题栏字段检查

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
                string originalPage =
                    info.PageNumber ?? "";


                string expectedPageText =
                    expectedPage
                    + "/"
                    + expectedPageCount;


                bool corrected =
                    false;


                // 只有明确允许修改时，
                // 才真正写入页码。
                //
                // ReportOnly模式：
                //
                // 只报告：
                // 当前页码错误
                // 正确页码应该是什么
                //
                // 绝不修改CAD实体。

                corrected =
                    TryCorrectPageNumber(
                        db,
                        info.PageNumberSourceTexts,
                        expectedPageText);


                if (corrected)
                {
                    pageMessage =
                        "原页码："
                        + (string.IsNullOrWhiteSpace(
                                originalPage)
                            ? "空"
                            : originalPage)
                        + "，已修正为："
                        + expectedPageText;


                    info.PageNumber =
                        expectedPageText;


                    results.Add(
                        new CheckResult
                        {
                            FilePath = filePath,
                            FileName = fileName,
                            LayoutName = info.LayoutName,
                            Type = "页码自动修正",
                            ObjectName = "页码",
                            CurrentValue = originalPage,
                            ExpectedValue = expectedPageText,
                            Message = pageMessage,
                            IsError = false
                        });
                }
                else
                {
                    results.Add(
                        new CheckResult
                        {
                            FilePath = filePath,
                            FileName = fileName,
                            LayoutName = info.LayoutName,
                            Type = "页码检查",
                            ObjectName = "页码",
                            CurrentValue = originalPage,
                            ExpectedValue = expectedPageText,
                            Message = pageMessage,
                            IsError = true
                        });
                }
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
                        "标题栏" + result.ObjectName + "未填写",
                        offsetX,
                        offsetY);
                }

                if (!string.IsNullOrEmpty(pageMessage))
                {
                    fieldMarker.DrawMarker(
                        db,
                        layout.LayoutName,
                        info.IsHorizontal,
                        "PageNumber",
                        pageMessage,
                        offsetX,
                        offsetY);
                }
            }




            // 标题栏图号一致性检查

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
                            fileDrawingNumber,
                            default(Autodesk.AutoCAD.Geometry.Point3d),
                            offsetX,
                            offsetY
                        );
                    }

                }

                string expectedDrawingNumber =
     !string.IsNullOrWhiteSpace(fileDrawingNumber)
         ? fileDrawingNumber
         : titleDrawingNumber;

                if (boms != null &&
                    !string.IsNullOrWhiteSpace(expectedDrawingNumber))
                {
                    foreach (BomData bom in boms)
                    {
                        if (bom == null ||
                            string.IsNullOrWhiteSpace(bom.DrawingNumber))
                        {
                            continue;
                        }

                        // 只检查属于当前布局的 BOM
                        if (string.IsNullOrWhiteSpace(
                                bom.SourceLayoutName) ||
                            !string.Equals(
                                bom.SourceLayoutName,
                                layout.LayoutName,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (IsCompatibleBomDrawingNumber(
                            expectedDrawingNumber,
                            bom.DrawingNumber))
                        {
                            continue;
                        }

                        results.Add(
                            new CheckResult
                            {
                                FilePath = filePath,
                                FileName = fileName,
                                LayoutName = info.LayoutName,
                                Mark = "",
                                Type = "BOM图号检查",
                                ObjectName = "BOM图号",
                                CurrentValue = bom.DrawingNumber,
                                ExpectedValue = expectedDrawingNumber,
                                Message = "BOM表上方图号与图纸图号不一致",
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
                                bom.DrawingNumberPosition
                            );
                        }
                    }
                }

            }
            catch (System.Exception ex)
            {
                AppLogger.Error(ex, "TitleBlockCheckManager");
            }




            return results;

        }
        /// <summary>
        /// 页码错误时直接修改原来的文字实体。
        /// 只改变内容：
        /// 位置、样式、字高、颜色、图层、旋转全部不动。
        /// </summary>
        /// <summary>
        /// 页码错误时直接修改原来的文字实体。
        /// 只改变文字内容：
        /// 位置、样式、字高、颜色、图层、旋转全部不动。
        /// 支持：
        /// 1. 一个对象：1/3
        /// 2. 一个对象：页码 1/3
        /// 3. 两个对象：1 和 3
        /// 4. 带文字：第1页 共3页
        /// </summary>
        private bool TryCorrectPageNumber(
            Autodesk.AutoCAD.DatabaseServices.Database db,
            List<TitleText> pageSourceTexts,
            string expectedPageText)
        {
            if (db == null ||
                pageSourceTexts == null ||
                pageSourceTexts.Count == 0 ||
                string.IsNullOrWhiteSpace(
                    expectedPageText))
            {
                return false;
            }


            MatchCollection expectedMatches =
                Regex.Matches(
                    expectedPageText,
                    @"\d+");


            if (expectedMatches.Count < 2)
            {
                return false;
            }


            string expectedPage =
                expectedMatches[0].Value;


            string expectedCount =
                expectedMatches[1].Value;





            // 找出PageNumber区域中真正带数字的原文字。
            //
            // 注意：
            // 这里不再排除包含“页码”的文字。
            //
            // 因为实际图纸很可能是：
            //
            // "页码 1/3"
            //
            // 整体就是同一个文字对象。

            List<TitleText> candidates =
    pageSourceTexts
        .Where(
            item =>
                item != null &&
                !item.ObjectId.IsNull &&
                item.ObjectId.IsValid &&
                Regex.IsMatch(
                    item.Text ?? "",
                    @"\d+"))
        .OrderByDescending(
            item => item.Y)
        .ThenBy(
            item => item.X)
        .ToList();


            if (candidates.Count == 0)
            {
                AppLogger.Info(
                    "页码自动修正失败：PageNumber区域未找到可修改的数字文字。",
                    "TitleBlockCheckManager.TryCorrectPageNumber");

                return false;
            }


            // 优先寻找：
            // 单个文字对象中同时存在两个数字。
            //
            // 例如：
            // 1/3
            // 页码 1/3
            // 第1页 共3页

            TitleText combinedCandidate =
                candidates.Find(
                    item =>
                        Regex.Matches(
                            item.Text ?? "",
                            @"\d+")
                        .Count >= 2);


            try
            {
                using (
                    Autodesk.AutoCAD.DatabaseServices.Transaction
                        transaction =
                            db.TransactionManager
                                .StartTransaction())
                {
                    if (combinedCandidate != null)
                    {
                        Autodesk.AutoCAD.DatabaseServices.DBObject obj =
                            transaction.GetObject(
                                combinedCandidate.ObjectId,
                                Autodesk.AutoCAD.DatabaseServices
                                    .OpenMode.ForWrite);


                        string currentText =
                            GetEditableText(
                                obj);


                        if (string.IsNullOrWhiteSpace(
                                currentText))
                        {
                            return false;
                        }


                        string correctedText =
                            ReplaceFirstTwoNumbers(
                                currentText,
                                expectedPage,
                                expectedCount);


                        if (string.IsNullOrWhiteSpace(
                                correctedText))
                        {
                            return false;
                        }


                        if (!SetEditableText(
                                obj,
                                correctedText))
                        {
                            return false;
                        }


                        transaction.Commit();


                        combinedCandidate.Text =
                            correctedText;


                        return true;
                    }


                    // 没有单个对象包含两个数字。
                    //
                    // 那么按与TitleBlockRegionParser相同的顺序，
                    // 取前两个数字文字：
                    //
                    // 第一个 = 当前页
                    // 第二个 = 总页数

                    if (candidates.Count < 2)
                    {
                        // 只有一个数字对象，但原字段缺少总页数。
                        //
                        // 例如：
                        // "页码 2"
                        //
                        // 直接在这个原对象中改成：
                        // "页码 2/3"

                        TitleText only =
                            candidates[0];


                        Autodesk.AutoCAD.DatabaseServices.DBObject obj =
                            transaction.GetObject(
                                only.ObjectId,
                                Autodesk.AutoCAD.DatabaseServices
                                    .OpenMode.ForWrite);


                        string currentText =
                            GetEditableText(
                                obj);


                        string correctedText =
                            ReplaceSingleNumberWithPageText(
                                currentText,
                                expectedPageText);


                        if (string.IsNullOrWhiteSpace(
                                correctedText) ||
                            !SetEditableText(
                                obj,
                                correctedText))
                        {
                            return false;
                        }


                        transaction.Commit();


                        only.Text =
                            correctedText;


                        return true;
                    }


                    TitleText pageText =
                        candidates[0];


                    TitleText countText =
                        candidates[1];


                    Autodesk.AutoCAD.DatabaseServices.DBObject pageObject =
                        transaction.GetObject(
                            pageText.ObjectId,
                            Autodesk.AutoCAD.DatabaseServices
                                .OpenMode.ForWrite);


                    Autodesk.AutoCAD.DatabaseServices.DBObject countObject =
                        transaction.GetObject(
                            countText.ObjectId,
                            Autodesk.AutoCAD.DatabaseServices
                                .OpenMode.ForWrite);


                    string pageCurrentText =
                        GetEditableText(
                            pageObject);


                    string countCurrentText =
                        GetEditableText(
                            countObject);


                    string correctedPageText =
                        ReplaceFirstNumber(
                            pageCurrentText,
                            expectedPage);


                    string correctedCountText =
                        ReplaceFirstNumber(
                            countCurrentText,
                            expectedCount);


                    if (string.IsNullOrWhiteSpace(
                            correctedPageText) ||
                        string.IsNullOrWhiteSpace(
                            correctedCountText))
                    {
                        return false;
                    }


                    if (!SetEditableText(
                            pageObject,
                            correctedPageText) ||
                        !SetEditableText(
                            countObject,
                            correctedCountText))
                    {
                        return false;
                    }


                    transaction.Commit();


                    pageText.Text =
                        correctedPageText;


                    countText.Text =
                        correctedCountText;


                    return true;
                }
            }
            catch (System.Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "TitleBlockCheckManager.TryCorrectPageNumber");


                return false;
            }
        }


        /// <summary>
        /// 读取可编辑文字内容。
        /// </summary>
        private static string GetEditableText(
            Autodesk.AutoCAD.DatabaseServices.DBObject obj)
        {
            Autodesk.AutoCAD.DatabaseServices.DBText dbText =
                obj as Autodesk.AutoCAD.DatabaseServices.DBText;


            if (dbText != null)
            {
                return
                    dbText.TextString ?? "";
            }


            Autodesk.AutoCAD.DatabaseServices.MText mtext =
                obj as Autodesk.AutoCAD.DatabaseServices.MText;


            if (mtext != null)
            {
                // 使用显示文字。
                //
                // 修改Contents只影响这个MText的内容，
                // TextStyleId/TextHeight/Location等对象属性不变。
                return
                    mtext.Text ?? "";
            }


            Autodesk.AutoCAD.DatabaseServices.AttributeReference attribute =
                obj as Autodesk.AutoCAD.DatabaseServices.AttributeReference;


            if (attribute != null)
            {
                return
                    attribute.TextString ?? "";
            }


            return "";
        }


        /// <summary>
        /// 只回写内容，不改对象其他属性。
        /// </summary>
        private static bool SetEditableText(
            Autodesk.AutoCAD.DatabaseServices.DBObject obj,
            string value)
        {
            Autodesk.AutoCAD.DatabaseServices.DBText dbText =
                obj as Autodesk.AutoCAD.DatabaseServices.DBText;


            if (dbText != null)
            {
                dbText.TextString =
                    value;

                return true;
            }


            Autodesk.AutoCAD.DatabaseServices.MText mtext =
                obj as Autodesk.AutoCAD.DatabaseServices.MText;


            if (mtext != null)
            {
                mtext.Contents =
                    value;

                return true;
            }


            Autodesk.AutoCAD.DatabaseServices.AttributeReference attribute =
                obj as Autodesk.AutoCAD.DatabaseServices.AttributeReference;


            if (attribute != null)
            {
                attribute.TextString =
                    value;

                return true;
            }


            return false;
        }


        /// <summary>
        /// 把文字中的前两个数字替换为正确页码。
        /// 例如：
        /// 页码 4/9
        /// ->
        /// 页码 2/3
        /// </summary>
        private static string ReplaceFirstTwoNumbers(
            string source,
            string firstValue,
            string secondValue)
        {
            if (string.IsNullOrWhiteSpace(
                    source))
            {
                return "";
            }


            int index =
                0;


            return Regex.Replace(
                source,
                @"\d+",
                match =>
                {
                    index++;


                    if (index == 1)
                    {
                        return firstValue;
                    }


                    if (index == 2)
                    {
                        return secondValue;
                    }


                    return match.Value;
                });
        }


        /// <summary>
        /// 替换第一个数字，其他文字保持原样。
        /// </summary>
        private static string ReplaceFirstNumber(
    string source,
    string value)
        {
            if (string.IsNullOrWhiteSpace(
                    source))
            {
                return "";
            }


            Regex regex =
                new Regex(
                    @"\d+");


            return regex.Replace(
                source,
                value,
                1);
        }


        /// <summary>
        /// 原字段只有一个数字时，
        /// 把该数字替换成完整的 2/3。
        /// 例如：
        /// 页码 4
        /// ->
        /// 页码 2/3
        /// </summary>
        private static string ReplaceSingleNumberWithPageText(
    string source,
    string pageText)
        {
            if (string.IsNullOrWhiteSpace(
                    source))
            {
                return pageText;
            }


            if (!Regex.IsMatch(
                    source,
                    @"\d+"))
            {
                return
                    source
                    + pageText;
            }


            Regex regex =
                new Regex(
                    @"\d+");


            return regex.Replace(
                source,
                pageText,
                1);
        }

        private static bool IsCompatibleBomDrawingNumber(
    string baseDrawingNumber,
    string bomDrawingNumber)
        {
            string baseNumber =
                CadTextCleaner.Clean(
                    baseDrawingNumber ?? "")
                .Trim();

            string bomNumber =
                CadTextCleaner.Clean(
                    bomDrawingNumber ?? "")
                .Trim();

            if (string.IsNullOrWhiteSpace(baseNumber) ||
                string.IsNullOrWhiteSpace(bomNumber))
            {
                return false;
            }

            // 完全相同
            if (string.Equals(
                baseNumber,
                bomNumber,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // BOM图号必须以基础图号开头
            if (!bomNumber.StartsWith(
                baseNumber,
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // 取后缀
            string suffix =
                bomNumber.Substring(
                    baseNumber.Length);

            if (suffix.Length == 0)
                return true;

            // 允许：
            // NS282Z
            // NS282Z1
            // NS282Z001
            // NS282Z_
            // NS282Z_001

            if (suffix == "_")
            {
                return true;
            }

            if (suffix.StartsWith("_"))
            {
                suffix =
                    suffix.Substring(1);
            }

            if (suffix.Length == 0)
            {
                return true;
            }

            foreach (char c in suffix)
            {
                if (!char.IsDigit(c))
                {
                    return false;
                }
            }

            return true;
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
            bool isHorizontal,
            double offsetX,
            double offsetY)
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
                issues,
                offsetX,
                offsetY);

            AddRegionHeightIssues(
                texts,
                regions.Find(x => x.FieldName == "DrawingNumber"),
                3.5,
                "图号文字高度错误",
                issues,
                offsetX,
                offsetY);

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
            List<TextHeightIssue> issues,
            double offsetX,
            double offsetY)
        {
            if (region == null)
                return;

            foreach (TitleText text in texts)
            {
                if (region.Contains(
                        text.X - offsetX,
                        text.Y - offsetY))
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