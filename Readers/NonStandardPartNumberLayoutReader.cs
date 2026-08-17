using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.Core;
using Correct_test1.Models;

using System;
using System.Collections.Generic;
using Correct_test1.Checks;

namespace Correct_test1.Readers
{
    /// <summary>
    /// 从归档非标DWG中读取：
    ///
    /// 图号 + 件号
    ///
    /// 例如：
    ///
    /// NS333T    _1
    /// 重量
    /// 备注
    ///
    /// 表示：
    ///
    /// NS333T1
    ///
    /// 存在。
    /// </summary>
    public class NonStandardPartNumberLayoutReader
    {
        /// <summary>
        /// 读取一张归档DWG中
        /// 所有Layout存在的“图号+件号”组合。
        ///
        /// 返回Key：
        ///
        /// NS333T|1
        /// NS333T|2
        /// ...
        /// </summary>
        public HashSet<string> ReadPartKeys(
            Database database)
        {
            HashSet<string> result =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);


            if (database == null)
            {
                return result;
            }


            LayoutReader layoutReader =
                new LayoutReader();


            RevisionTableReader textReader =
                new RevisionTableReader();


            List<LayoutInfo> layouts =
                layoutReader.ReadLayouts(
                    database);


            if (layouts == null)
            {
                return result;
            }


            foreach (
                LayoutInfo layout
                in layouts)
            {
                if (layout == null ||
                    layout.IsModelSpace)
                {
                    continue;
                }


                List<TitleText> texts =
                    textReader.ReadAllTexts(
                        database,
                        layout.BlockTableRecordId);


                if (texts == null ||
                    texts.Count == 0)
                {
                    continue;
                }


                ReadLayout(
                    texts,
                    result);
            }


            return result;
        }


        private void ReadLayout(
    List<TitleText> texts,
    HashSet<string> result)
        {
            if (texts == null ||
                result == null ||
                texts.Count == 0)
            {
                return;
            }


            //==================================================
            // 第一种格式：
            //
            // 完整件号直接写在一个单元格里
            //
            // 件号 | NS333H1
            //
            // ↓
            //
            // NS333H|1
            //==================================================

            foreach (
                TitleText text
                in texts)
            {
                if (text == null ||
                    string.IsNullOrWhiteSpace(
                        text.Text))
                {
                    continue;
                }


                string value =
                    Clean(
                        text.Text);


                if (string.IsNullOrWhiteSpace(
                        value))
                {
                    continue;
                }


                string drawingNumber;

                string partSuffix;


                if (TryReadCombinedPartNumber(
                        value,
                        out drawingNumber,
                        out partSuffix))
                {
                    result.Add(
                        BuildKey(
                            drawingNumber,
                            partSuffix));
                }
            }


            //==================================================
            // 第二种 / 第三种格式：
            //
            // NS333T    _1
            //
            // 或：
            //
            // NS333D_   _999  _998  _997 ...
            //
            // 只把“同一行”的件号配给这个图号。
            //==================================================

            foreach (
                TitleText drawingText
                in texts)
            {
                if (drawingText == null ||
                    string.IsNullOrWhiteSpace(
                        drawingText.Text))
                {
                    continue;
                }


                string drawingValue =
                    Clean(
                        drawingText.Text);


                if (!IsBaseDrawingNumber(
                        drawingValue))
                {
                    continue;
                }


                string drawingNumber =
                    NormalizeDrawingNumber(
                        drawingValue);


                if (string.IsNullOrWhiteSpace(
                        drawingNumber))
                {
                    continue;
                }


                //--------------------------------
                // 同一行Y容差
                //--------------------------------

                const double yTolerance =
                    2.5;


                foreach (
                    TitleText suffixText
                    in texts)
                {
                    if (suffixText == null ||
                        string.IsNullOrWhiteSpace(
                            suffixText.Text))
                    {
                        continue;
                    }


                    //--------------------------------
                    // 不能把自己当件号
                    //--------------------------------

                    if (object.ReferenceEquals(
                            drawingText,
                            suffixText))
                    {
                        continue;
                    }


                    //--------------------------------
                    // 必须基本位于同一行
                    //--------------------------------

                    if (Math.Abs(
                            suffixText.Y -
                            drawingText.Y)
                        > yTolerance)
                    {
                        continue;
                    }


                    //--------------------------------
                    // 件号必须位于图号右侧
                    //--------------------------------

                    if (suffixText.X <=
                        drawingText.X)
                    {
                        continue;
                    }


                    string suffixValue =
                        Clean(
                            suffixText.Text);


                    string suffix;


                    if (!TryReadPartSuffix(
                            suffixValue,
                            out suffix))
                    {
                        continue;
                    }


                    //--------------------------------
                    // NS333D_ + _999
                    //
                    // ↓
                    //
                    // NS333D|999
                    //--------------------------------

                    result.Add(
                        BuildKey(
                            drawingNumber,
                            suffix));
                }
            }




            ReadLegacySmallTables(
                texts,
                result);
        }


        private static void ReadLegacySmallTables(
    List<TitleText> texts,
    HashSet<string> result)
        {
            if (texts == null ||
                result == null ||
                texts.Count == 0)
            {
                return;
            }


            foreach (
                TitleText drawingText
                in texts)
            {
                if (drawingText == null ||
                    string.IsNullOrWhiteSpace(
                        drawingText.Text))
                {
                    continue;
                }


                string drawingValue =
                    Clean(
                        drawingText.Text);


                //==================================================
                // 必须是真正的基础图号：
                //
                // NS333T
                // NS333D_
                //
                // 不能是：
                //
                // NS333T1
                //==================================================

                if (!IsBaseDrawingNumber(
                        drawingValue))
                {
                    continue;
                }


                //==================================================
                // 老式小表必须在当前图号附近
                // 同时存在：
                //
                // 重量
                // 备注
                //
                // 不再使用整个Layout全局判断。
                //==================================================

                if (!HasNearbyLegacyLabels(
                        texts,
                        drawingText))
                {
                    continue;
                }


                string drawingNumber =
                    NormalizeDrawingNumber(
                        drawingValue);


                if (string.IsNullOrWhiteSpace(
                        drawingNumber))
                {
                    continue;
                }


                //==================================================
                // 找当前图号附近的件号
                //==================================================

                foreach (
                    TitleText suffixText
                    in texts)
                {
                    if (suffixText == null ||
                        string.IsNullOrWhiteSpace(
                            suffixText.Text))
                    {
                        continue;
                    }


                    if (object.ReferenceEquals(
                            drawingText,
                            suffixText))
                    {
                        continue;
                    }


                    string suffix;


                    if (!TryReadPartSuffix(
                            Clean(
                                suffixText.Text),
                            out suffix))
                    {
                        continue;
                    }


                    //==================================================
                    // 必须在当前图号右侧。
                    //==================================================

                    double rightDistance =
                        suffixText.X -
                        drawingText.X;


                    if (rightDistance <= 0)
                    {
                        continue;
                    }


                    //==================================================
                    // 防止把很远的另一个表里的_1
                    // 误配给当前图号。
                    //==================================================

                    const double maxRightDistance =
                        150.0;


                    if (rightDistance >
                        maxRightDistance)
                    {
                        continue;
                    }


                    //==================================================
                    // 老图允许比新版矩阵更大的Y误差。
                    //
                    // 新版：
                    // 2.5
                    //
                    // 老式兼容：
                    // 12
                    //==================================================

                    const double legacyYTolerance =
                        12.0;


                    if (Math.Abs(
                            suffixText.Y -
                            drawingText.Y)
                        > legacyYTolerance)
                    {
                        continue;
                    }


                    //==================================================
                    // 如果附近还有其他基础图号，
                    //
                    // 这个suffix只能归给距离它最近的那个图号。
                    //
                    // 防止：
                    //
                    // NS333D_  _999
                    //
                    // NS386E_  _1
                    //
                    // 被交叉组合。
                    //==================================================

                    if (!IsNearestDrawingForSuffix(
                            texts,
                            drawingText,
                            suffixText,
                            legacyYTolerance,
                            maxRightDistance))
                    {
                        continue;
                    }


                    result.Add(
                        BuildKey(
                            drawingNumber,
                            suffix));
                }
            }
        }
        private static bool HasNearbyLegacyLabels(
    List<TitleText> texts,
    TitleText drawingText)
        {
            if (texts == null ||
                drawingText == null)
            {
                return false;
            }


            bool hasWeight =
                false;


            bool hasRemark =
                false;


            //==================================================
            // 老式小表不会特别大。
            //
            // 这里不是精确表框，
            // 只是限定“附近区域”，
            // 防止使用整个Layout的重量/备注。
            //==================================================

            const double maxHorizontalDistance =
                150.0;


            const double maxVerticalDistance =
                80.0;


            foreach (
                TitleText text
                in texts)
            {
                if (text == null ||
                    string.IsNullOrWhiteSpace(
                        text.Text))
                {
                    continue;
                }


                double dx =
                    Math.Abs(
                        text.X -
                        drawingText.X);


                double dy =
                    Math.Abs(
                        text.Y -
                        drawingText.Y);


                if (dx >
                        maxHorizontalDistance ||
                    dy >
                        maxVerticalDistance)
                {
                    continue;
                }


                string value =
                    Clean(
                        text.Text);


                if (string.Equals(
                        value,
                        "重量",
                        StringComparison.OrdinalIgnoreCase))
                {
                    hasWeight =
                        true;
                }


                if (string.Equals(
                        value,
                        "备注",
                        StringComparison.OrdinalIgnoreCase))
                {
                    hasRemark =
                        true;
                }


                if (hasWeight &&
                    hasRemark)
                {
                    return true;
                }
            }


            return false;
        }
        private static bool IsNearestDrawingForSuffix(
    List<TitleText> texts,
    TitleText currentDrawing,
    TitleText suffixText,
    double yTolerance,
    double maxRightDistance)
        {
            if (texts == null ||
                currentDrawing == null ||
                suffixText == null)
            {
                return false;
            }


            double currentDistance =
                GetDrawingSuffixDistance(
                    currentDrawing,
                    suffixText);


            foreach (
                TitleText otherDrawing
                in texts)
            {
                if (otherDrawing == null ||
                    object.ReferenceEquals(
                        otherDrawing,
                        currentDrawing) ||
                    string.IsNullOrWhiteSpace(
                        otherDrawing.Text))
                {
                    continue;
                }


                string value =
                    Clean(
                        otherDrawing.Text);


                if (!IsBaseDrawingNumber(
                        value))
                {
                    continue;
                }


                //==================================================
                // suffix也必须位于另一个候选图号右侧。
                //==================================================

                double rightDistance =
                    suffixText.X -
                    otherDrawing.X;


                if (rightDistance <= 0 ||
                    rightDistance >
                        maxRightDistance)
                {
                    continue;
                }


                if (Math.Abs(
                        suffixText.Y -
                        otherDrawing.Y)
                    > yTolerance)
                {
                    continue;
                }


                double otherDistance =
                    GetDrawingSuffixDistance(
                        otherDrawing,
                        suffixText);


                //==================================================
                // 找到明显更近的基础图号：
                //
                // 当前drawing不是这个suffix的归属。
                //==================================================

                if (otherDistance <
                    currentDistance)
                {
                    return false;
                }
            }


            return true;
        }
        private static double GetDrawingSuffixDistance(
    TitleText drawingText,
    TitleText suffixText)
        {
            if (drawingText == null ||
                suffixText == null)
            {
                return
                    double.MaxValue;
            }


            double dx =
                suffixText.X -
                drawingText.X;


            double dy =
                suffixText.Y -
                drawingText.Y;


            //==================================================
            // Y方向权重稍微放大。
            //
            // 原因：
            //
            // 对件号归属来说，
            // “是不是同一行”
            // 比纯X距离更重要。
            //==================================================

            return
                Math.Abs(dx)
                +
                Math.Abs(dy)
                * 5.0;
        }
        /// <summary>
        /// 解析完整非标件号。
        /// </summary>
        /// 
        private static bool TryReadCombinedPartNumber(
            string value,
            out string drawingNumber,
            out string partSuffix)
        {
            drawingNumber =
                "";


            partSuffix =
                "";


            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return false;
            }


            string cleaned =
                Clean(
                    value)
                    .Trim()
                    .ToUpperInvariant();


            //--------------------------------
            // 必须是NS开头
            //--------------------------------

            if (!cleaned.StartsWith(
                    "NS",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }


            //--------------------------------
            // 直接复用现有归档图号规则。
            //
            // NS333H1
            // ↓
            // NS333H
            //--------------------------------

            string baseDrawingNumber =
                NonStandardArchiveChecker
                    .BuildSearchKey(
                        cleaned);


            if (string.IsNullOrWhiteSpace(
                    baseDrawingNumber))
            {
                return false;
            }


            //--------------------------------
            // 如果没有被截掉任何东西：
            //
            // NS333H
            // ↓
            // NS333H
            //
            // 说明它只有图号，没有件号。
            //--------------------------------

            if (cleaned.Length <=
                baseDrawingNumber.Length)
            {
                return false;
            }


            //--------------------------------
            // 取剩余部分。
            //
            // NS333H1
            //       ↓
            //       1
            //--------------------------------

            string suffix =
                cleaned
                    .Substring(
                        baseDrawingNumber.Length)
                    .Trim()
                    .TrimStart(
                        '_',
                        '-');


            if (string.IsNullOrWhiteSpace(
                    suffix))
            {
                return false;
            }


            //--------------------------------
            // 当前件号仍按纯数字处理
            //--------------------------------

            foreach (
                char character
                in suffix)
            {
                if (!char.IsDigit(
                        character))
                {
                    return false;
                }
            }


            drawingNumber =
                baseDrawingNumber
                    .Trim()
                    .ToUpperInvariant();


            partSuffix =
                suffix;


            return true;
        }
        

        /// <summary>
        /// 判断是否为基础归档图号。
        /// </summary>
        private static bool IsBaseDrawingNumber(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return false;
            }


            string cleaned =
                Clean(
                    value)
                    .Trim()
                    .ToUpperInvariant();


            if (!cleaned.StartsWith(
                    "NS",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }


            if (cleaned.Length <= 2)
            {
                return false;
            }


            //--------------------------------
            // NS333D_
            // ↓
            // NS333D
            //--------------------------------

            string withoutUnderscore =
                cleaned.TrimEnd(
                    '_');


            if (withoutUnderscore.Length <= 2)
            {
                return false;
            }


            //--------------------------------
            // 如果最后还是数字：
            //
            // NS333H1
            //
            // 说明它是完整件号，
            // 不是基础图号。
            //--------------------------------

            char last =
                withoutUnderscore[
                    withoutUnderscore.Length - 1];


            if (char.IsDigit(
                    last))
            {
                return false;
            }


            return true;
        }

        private static string NormalizeDrawingNumber(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return "";
            }


            return
                Clean(
                    value)
                .TrimEnd('_')
                .ToUpperInvariant();
        }


        /// <summary>
        /// 读取：
        ///
        /// _1
        /// _2
        /// _101
        ///
        /// 返回：
        ///
        /// 1
        /// 2
        /// 101
        /// </summary>
        private static bool TryReadPartSuffix(
            string value,
            out string suffix)
        {
            suffix =
                "";


            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return false;
            }


            value =
                value
                    .Trim()
                    .Replace(
                        " ",
                        "");


            //--------------------------------
            // 必须以 "_" 开头。
            //
            // 防止普通数字文字被误识别成件号。
            //--------------------------------

            if (!value.StartsWith(
                    "_",
                    StringComparison.Ordinal))
            {
                return false;
            }


            string candidate =
                value.TrimStart('_');


            if (string.IsNullOrWhiteSpace(
                    candidate))
            {
                return false;
            }


            //--------------------------------
            // 当前件号规则：
            // 数字。
            //--------------------------------

            foreach (
                char character
                in candidate)
            {
                if (!char.IsDigit(
                        character))
                {
                    return false;
                }
            }


            suffix =
                candidate;


            return true;
        }


        public static string BuildKey(
            string drawingNumber,
            string suffix)
        {
            string drawing =
                NormalizeDrawingNumber(
                    drawingNumber);


            string part =
                suffix == null
                    ? ""
                    : suffix
                        .Trim()
                        .TrimStart('_');


            return
                drawing
                + "|"
                + part;
        }


        private static string Clean(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return "";
            }


            return
                CadTextCleaner.Clean(
                    value)
                    .Replace(
                        "\\P",
                        "")
                    .Replace(
                        "\r",
                        "")
                    .Replace(
                        "\n",
                        "")
                    .Trim();
        }
    }
}