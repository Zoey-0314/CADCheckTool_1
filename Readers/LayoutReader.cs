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
            List<TitleText> acceptedTexts;

            return IdentifyDrawingBomNumbers(
                texts,
                lines,
                out acceptedTexts);
        }


        public HashSet<int> IdentifyDrawingBomNumbers(
            List<TitleText> texts,
            List<CadLineInfo> lines,
            out List<TitleText> acceptedTexts)
        {
            HashSet<int> result =
                new HashSet<int>();

            acceptedTexts =
                new List<TitleText>();

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
                    if (number == 6)
                    {
                        Autodesk.AutoCAD.ApplicationServices.Document document =
                            Autodesk.AutoCAD.ApplicationServices.Application
                                .DocumentManager
                                .MdiActiveDocument;

                        if (document != null)
                        {
                            document.Editor.WriteMessage(
                                "\n[BOM6] 实际判断坐标：X="
                                + text.X.ToString("0.0000")
                                + "，Y="
                                + text.Y.ToString("0.0000")
                                + "，ViewportId="
                                + (text.ViewportId.IsNull
                                    ? "Null"
                                    : text.ViewportId.Handle.ToString())
                                + "，同视口线数量="
                                + (viewportLines == null
                                    ? "Null"
                                    : viewportLines.Count.ToString()));
                        }
                    }

                    // >=100 保留原有判断逻辑
                    if (number >= 100)
                    {
                        result.Add(number);

                        if (!acceptedTexts.Contains(text))
                        {
                            acceptedTexts.Add(text);
                        }

                        continue;
                    }

                    // 1~99 保留原有焊接符号判断逻辑
                    bool isWelding =
                        viewportLines != null &&
                        IsWeldingCandidateByRange(
                            text,
                            viewportLines);

                    if (isWelding)
                    {
                        // 焊接符号既不加入数字集合，
                        // 也不加入后续错误标记位置集合
                        continue;
                    }

                    result.Add(number);

                    if (!acceptedTexts.Contains(text))
                    {
                        acceptedTexts.Add(text);
                    }
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

                bool isHorizontal =
    Math.Abs(
        line.StartPoint.Y -
        line.EndPoint.Y) <= 0.5;

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


                // 数字中心正上方是否存在横线
                // 横线必须明显跨过数字中心，
                // 不能只是横线端点轻微擦过文字中心。
                double centerInset =
                    Math.Max(
                        0.5,
                        text.Height * 0.1);

                bool crossesTextCenter =
                    text.X >= minX + centerInset &&
                    text.X <= maxX - centerInset;

                if (!hasUpperLine &&
    isHorizontal &&
    minY > text.Y &&
    minY - text.Y <= 50 &&
    crossesTextCenter)
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