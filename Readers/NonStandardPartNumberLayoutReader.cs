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


            //==================================================
            // 兼容老式小表：
            //
            // NS333T    _1
            // 重量
            // 备注
            //
            // 有些老图文字基点不完全在同一Y，
            // 所以保留原来的宽松识别。
            //==================================================

            bool hasWeight =
                ContainsExact(
                    texts,
                    "重量");


            bool hasRemark =
                ContainsExact(
                    texts,
                    "备注");


            if (!hasWeight ||
                !hasRemark)
            {
                return;
            }


            List<string> drawingNumbers =
                new List<string>();


            List<string> partSuffixes =
                new List<string>();


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


                //--------------------------------
                // 图号
                //--------------------------------

                if (IsBaseDrawingNumber(
                        value))
                {
                    string normalized =
                        NormalizeDrawingNumber(
                            value);


                    if (!drawingNumbers.Contains(
                            normalized))
                    {
                        drawingNumbers.Add(
                            normalized);
                    }


                    continue;
                }


                //--------------------------------
                // 件号
                //--------------------------------

                string suffix;


                if (TryReadPartSuffix(
                        value,
                        out suffix))
                {
                    if (!partSuffixes.Contains(
                            suffix))
                    {
                        partSuffixes.Add(
                            suffix);
                    }
                }
            }


            foreach (
                string drawingNumber
                in drawingNumbers)
            {
                foreach (
                    string suffix
                    in partSuffixes)
                {
                    result.Add(
                        BuildKey(
                            drawingNumber,
                            suffix));
                }
            }
        }

        /// <summary>
        /// 解析完整非标件号。
        ///
        /// 例如：
        ///
        /// NS333H1
        ///
        /// ↓
        ///
        /// drawingNumber = NS333H
        /// partSuffix = 1
        ///
        /// 也支持：
        ///
        /// NS333H12
        /// ↓
        /// NS333H + 12
        /// </summary>
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
        private static bool ContainsExact(
            List<TitleText> texts,
            string expected)
        {
            foreach (
                TitleText text
                in texts)
            {
                if (text == null)
                    continue;


                string value =
                    Clean(
                        text.Text);


                if (string.Equals(
                        value,
                        expected,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }


            return false;
        }


        /// <summary>
        /// 判断是否为基础归档图号。
        ///
        /// 支持：
        ///
        /// NS333T
        /// NS333D_
        ///
        /// 不支持：
        ///
        /// NS333H1
        ///
        /// 因为NS333H1是
        /// “图号+件号”组合形式。
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