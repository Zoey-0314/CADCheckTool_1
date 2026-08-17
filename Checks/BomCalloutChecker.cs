using Autodesk.AutoCAD.Geometry;

using Correct_test1.Core;
using Correct_test1.Models;
using Correct_test1.Readers;

using System;
using System.Collections.Generic;
using System.Globalization;


namespace Correct_test1.Checks
{
    public class BomCalloutChecker
    {
        //==================================================
        // 读取一个BOM中的所有序号
        //==================================================

        public HashSet<int> GetBomNumbers(
            BomData bom)
        {
            HashSet<int> numbers =
                new HashSet<int>();


            if (bom == null ||
                bom.Items == null)
            {
                return numbers;
            }


            foreach (
                BomItem item
                in bom.Items)
            {
                int number;


                if (item != null &&
                    TryGetNumber(
                        item.No,
                        out number))
                {
                    numbers.Add(
                        number);
                }
            }


            return numbers;
        }


        //==================================================
        // 保留旧版：
        //
        // 只负责两个数字集合的数学比较。
        //==================================================

        public BomCalloutResult Check(
            HashSet<int> bomNumbers,
            HashSet<int> drawingNumbers)
        {
            HashSet<int> missingCallouts =
                bomNumbers == null
                    ? new HashSet<int>()
                    : new HashSet<int>(
                        bomNumbers);


            HashSet<int> extraCallouts =
                drawingNumbers == null
                    ? new HashSet<int>()
                    : new HashSet<int>(
                        drawingNumbers);


            if (drawingNumbers != null)
            {
                missingCallouts.ExceptWith(
                    drawingNumbers);
            }


            if (bomNumbers != null)
            {
                extraCallouts.ExceptWith(
                    bomNumbers);
            }


            return
                new BomCalloutResult
                {
                    MissingCallouts =
                        missingCallouts,

                    ExtraCallouts =
                        extraCallouts
                };
        }


        //==================================================
        // 新版：
        //
        // 一个Layout单独检查。
        //
        // Layout1：
        // BOM ↔ Layout1视口
        //
        // Layout2：
        // BOM ↔ Layout2视口
        //
        // 绝不跨Layout合并。
        //==================================================

        public BomCalloutResult CheckLayout(
            string layoutName,
            List<BomData> layoutBoms,
            HashSet<int> drawingNumbers,
            List<TitleText> layoutDrawingTexts)
        {
            BomCalloutResult result =
                new BomCalloutResult();


            if (string.IsNullOrWhiteSpace(
                    layoutName))
            {
                return result;
            }


            //==================================================
            // 当前Layout自己的BOM序号
            //==================================================

            HashSet<int> bomNumbers =
                new HashSet<int>();


            if (layoutBoms != null)
            {
                foreach (
                    BomData bom
                    in layoutBoms)
                {
                    bomNumbers.UnionWith(
                        GetBomNumbers(
                            bom));
                }
            }


            //==================================================
            // 数学比较
            //==================================================

            BomCalloutResult basic =
                Check(
                    bomNumbers,
                    drawingNumbers);


            result.MissingCallouts =
                basic.MissingCallouts;


            result.ExtraCallouts =
                basic.ExtraCallouts;


            //==================================================
            // BOM有，图中没有
            //
            // Marker位置：
            // 当前Layout当前BOM的No.单元格。
            //==================================================

            foreach (
                int number
                in result.MissingCallouts)
            {
                BomItem matchedItem;


                if (!TryFindBomItem(
                        layoutBoms,
                        number,
                        out matchedItem))
                {
                    continue;
                }


                result.MissingIssues.Add(
                    new BomCalloutIssue
                    {
                        Number =
                            number,

                        LayoutName =
                            layoutName,

                        Position =
                            matchedItem
                                .NoCellPosition,

                        Message =
                            "图中缺少序号："
                            + number
                    });
            }


            //==================================================
            // 图中有，BOM没有
            //
            // Marker位置：
            // 当前Layout视口中真正识别到的文字位置。
            //==================================================

            foreach (
                int number
                in result.ExtraCallouts)
            {
                Point3d position;


                if (!TryFindDrawingPosition(
                        layoutDrawingTexts,
                        number,
                        out position))
                {
                    continue;
                }


                result.ExtraIssues.Add(
                    new BomCalloutIssue
                    {
                        Number =
                            number,

                        LayoutName =
                            layoutName,

                        Position =
                            position,

                        Message =
                            "序号错误：不在BOM中"
                    });
            }


            return result;
        }


        //==================================================
        // 只在当前Layout的BOM中找序号
        //==================================================

        private static bool TryFindBomItem(
            List<BomData> boms,
            int targetNumber,
            out BomItem matchedItem)
        {
            matchedItem =
                null;


            if (boms == null)
            {
                return false;
            }


            foreach (
                BomData bom
                in boms)
            {
                if (bom == null ||
                    bom.Items == null)
                {
                    continue;
                }


                foreach (
                    BomItem item
                    in bom.Items)
                {
                    if (item == null)
                    {
                        continue;
                    }


                    int number;


                    if (!TryGetNumber(
                            item.No,
                            out number))
                    {
                        continue;
                    }


                    if (number !=
                        targetNumber)
                    {
                        continue;
                    }


                    matchedItem =
                        item;


                    return true;
                }
            }


            return false;
        }


        //==================================================
        // 只在当前Layout对应的视口文字中
        // 找目标序号位置。
        //==================================================

        private static bool TryFindDrawingPosition(
            List<TitleText> texts,
            int targetNumber,
            out Point3d position)
        {
            position =
                Point3d.Origin;


            if (texts == null)
            {
                return false;
            }


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


                foreach (
                    string numericText
                    in LayoutReader
                        .SplitNumericTexts(
                            text.Text))
                {
                    int number;


                    if (!int.TryParse(
                            numericText,
                            NumberStyles.None,
                            CultureInfo.InvariantCulture,
                            out number))
                    {
                        continue;
                    }


                    if (number !=
                        targetNumber)
                    {
                        continue;
                    }


                    position =
                        new Point3d(
                            text.X,
                            text.Y,
                            0);


                    return true;
                }
            }


            return false;
        }


        private static bool TryGetNumber(
            string text,
            out int number)
        {
            string cleaned =
                CadTextCleaner.Clean(
                    text);


            return
                int.TryParse(
                    cleaned,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out number);
        }
    }
}