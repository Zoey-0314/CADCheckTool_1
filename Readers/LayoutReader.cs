using Autodesk.AutoCAD.DatabaseServices;
using Correct_test1.Models;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Correct_test1.Readers
{
    /// <summary>
    /// CAD布局读取器
    /// 1.读取所有Layout
    /// 2.获取BlockTableRecord
    /// 3.识别视口中的BOM序号
    /// </summary>
    public class LayoutReader
    {
        public List<LayoutInfo> ReadLayouts(
    Database db)
        {
            List<LayoutInfo> result =
                new List<LayoutInfo>();

            using (Transaction trans =
                db.TransactionManager.StartTransaction())
            {
                DBDictionary layoutDict =
                    trans.GetObject(
                        db.LayoutDictionaryId,
                        OpenMode.ForRead) as DBDictionary;

                if (layoutDict == null)
                    return result;

                foreach (DBDictionaryEntry entry in layoutDict)
                {
                    Autodesk.AutoCAD.DatabaseServices.Layout cadLayout =
                        trans.GetObject(
                            entry.Value,
                            OpenMode.ForRead)
                        as Autodesk.AutoCAD.DatabaseServices.Layout;

                    if (cadLayout == null)
                        continue;

                    if (cadLayout.ModelType)
                        continue;

                    result.Add(
                        new LayoutInfo
                        {
                            LayoutName =
                                cadLayout.LayoutName,

                            BlockTableRecordId =
                                cadLayout.BlockTableRecordId,

                            IsModelSpace =
                                false,

                            TabOrder =
                                cadLayout.TabOrder
                        });
                }

                trans.Commit();
            }

            return result;
        }

        public HashSet<int> IdentifyDrawingBomNumbers(
            List<TitleText> texts,
            List<CadLineInfo> lines)
        {
            HashSet<int> result =
                new HashSet<int>();

            if (texts == null)
                return result;

            Dictionary<ObjectId, List<CadLineInfo>> linesByViewport =
     new Dictionary<ObjectId, List<CadLineInfo>>();

            if (lines != null)
            {
                foreach (CadLineInfo line in lines)
                {
                    if (line == null ||
                        line.ViewportId.IsNull)
                    {
                        continue;
                    }

                    List<CadLineInfo> viewportLines;

                    if (!linesByViewport.TryGetValue(
                            line.ViewportId,
                            out viewportLines))
                    {
                        viewportLines =
                            new List<CadLineInfo>();

                        linesByViewport.Add(
                            line.ViewportId,
                            viewportLines);
                    }

                    viewportLines.Add(line);
                }
            }

            foreach (TitleText text in texts)
            {
                if (text == null ||
                    string.IsNullOrWhiteSpace(text.Text))
                {
                    continue;
                }

                List<CadLineInfo> viewportLines =
                    null;

                if (!text.ViewportId.IsNull)
                {
                    linesByViewport.TryGetValue(
                        text.ViewportId,
                        out viewportLines);
                }

                foreach (string numericText in
                    SplitNumericTexts(text.Text))
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

                    // >=100 的序号直接确认，不做焊接符号判断
                    if (number >= 100)
                    {
                        result.Add(number);
                        continue;
                    }

                    // 1~99 排除焊接符号
                    bool isWelding =
                        viewportLines != null &&
                        IsWeldingCandidateByRange(
                            text,
                            viewportLines);

                    if (isWelding)
                    {
                        continue;
                    }

                    result.Add(number);
                }
            }

            return result;
        }


        public static IEnumerable<string> SplitNumericTexts(
            string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                yield break;

            string normalized = text
                .Replace("\\P", "\n")
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");

            string[] parts = normalized.Split(
                new[] { '\n' },
                System.StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                parts = new[] { normalized };

            foreach (string part in parts)
            {
                string value = part == null
                    ? string.Empty
                    : part.Trim();

                if (string.IsNullOrEmpty(value))
                    continue;

                if (int.TryParse(
                    value,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out _))
                {
                    yield return value;
                }
            }
        }





        private bool IsWeldingCandidateByRange(
    TitleText text,
    IEnumerable<CadLineInfo> lines)
        {
            const double sideDistance = 40.0;

            bool hasSideLine = false;
            bool hasUpperLine = false;

            foreach (CadLineInfo line in lines)
            {
                if (line == null)
                    continue;

                double minX =
                    Math.Min(
                        line.StartPoint.X,
                        line.EndPoint.X);

                double maxX =
                    Math.Max(
                        line.StartPoint.X,
                        line.EndPoint.X);

                double minY =
                    Math.Min(
                        line.StartPoint.Y,
                        line.EndPoint.Y);

                double maxY =
                    Math.Max(
                        line.StartPoint.Y,
                        line.EndPoint.Y);


                // 左右蓝色竖线
                bool isVertical =
                    Math.Abs(
                        line.StartPoint.X -
                        line.EndPoint.X) <= 0.5;

                if (!hasSideLine &&
                    line.IsBlue &&
                    isVertical &&
                    text.Y >= minY - 10 &&
                    text.Y <= maxY + 10)
                {
                    double leftDistance =
                        text.X - maxX;

                    double rightDistance =
                        minX - text.X;

                    bool isLeftLine =
                        leftDistance > 0 &&
                        leftDistance <= sideDistance;

                    bool isRightLine =
                        rightDistance > 0 &&
                        rightDistance <= sideDistance;

                    if (isLeftLine ||
                        isRightLine)
                    {
                        hasSideLine = true;
                    }
                }


                // 数字上方是否存在横跨数字位置的线
                if (!hasUpperLine &&
                    minY > text.Y &&
                    minY - text.Y <= 50 &&
                    text.X >= minX - 10 &&
                    text.X <= maxX + 10)
                {
                    hasUpperLine = true;
                }


                if (hasSideLine &&
                    hasUpperLine)
                {
                    break;
                }
            }


            return
                hasSideLine &&
                !hasUpperLine;
        }


    }
}