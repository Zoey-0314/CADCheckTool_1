using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
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
    /// 3.获取布局整体范围
    /// </summary>
    public class LayoutReader
    {
        public List<LayoutInfo> ReadLayouts(
            Database db,
            Editor ed)
        {
            List<LayoutInfo> result = new List<LayoutInfo>();

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                DBDictionary layoutDict = trans.GetObject(db.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;
                if (layoutDict == null)
                    return result;

                foreach (DBDictionaryEntry entry in layoutDict)
                {
                    Autodesk.AutoCAD.DatabaseServices.Layout cadLayout = trans.GetObject(entry.Value, OpenMode.ForRead) as Autodesk.AutoCAD.DatabaseServices.Layout;
                    if (cadLayout == null)
                        continue;

                    LayoutInfo info = new LayoutInfo();

                    // 基本信息
                    info.LayoutName = cadLayout.LayoutName;
                    info.BlockTableRecordId = cadLayout.BlockTableRecordId;
                    info.IsModelSpace = cadLayout.ModelType;
                    info.IsValidDrawing = false;

                    BlockTableRecord btr = trans.GetObject(cadLayout.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
                    if (btr == null)
                        continue;

                    // 计算布局整体范围
                    Extents3d? totalExtents = null;
                    foreach (ObjectId id in btr)
                    {
                        Entity ent = trans.GetObject(id, OpenMode.ForRead) as Entity;
                        if (ent == null)
                            continue;

                        try
                        {
                            Extents3d ext = ent.GeometricExtents;
                            if (totalExtents == null)
                            {
                                totalExtents = ext;
                            }
                            else
                            {
                                Extents3d temp = totalExtents.Value;
                                temp.AddExtents(ext);
                                totalExtents = temp;
                            }
                        }
                        catch
                        {
                            // 部分CAD对象没有范围
                            continue;
                        }
                    }

                    if (totalExtents != null)
                    {
                        Extents3d ext = totalExtents.Value;
                        info.MinX = ext.MinPoint.X;
                        info.MinY = ext.MinPoint.Y;
                        info.Width = ext.MaxPoint.X - ext.MinPoint.X;
                        info.Height = ext.MaxPoint.Y - ext.MinPoint.Y;
                    }

                    result.Add(info);

                    if (ed != null)
                    {
                        ed.WriteMessage(
                            "\n布局:" + info.LayoutName + " Model:" + info.IsModelSpace + " 宽:" + info.Width + " 高:" + info.Height
                        );
                    }
                }

                trans.Commit();
            }

            return result;
        }

        public List<CadLineInfo> ReadLines(
            Database db,
            List<LayoutInfo> layouts)
        {
            List<CadLineInfo> result =
                new List<CadLineInfo>();

            if (db == null || layouts == null)
                return result;

            using (Transaction trans =
                db.TransactionManager.StartTransaction())
            {
                foreach (LayoutInfo layout in layouts)
                {
                    if (layout == null)
                        continue;

                    BlockTableRecord btr =
                        trans.GetObject(
                            layout.BlockTableRecordId,
                            OpenMode.ForRead)
                        as BlockTableRecord;

                    if (btr == null)
                        continue;

                    foreach (ObjectId id in btr)
                    {
                        Entity entity =
                            trans.GetObject(
                                id,
                                OpenMode.ForRead) as Entity;

                        if (entity == null)
                            continue;

                        Line line = entity as Line;

                        if (line != null)
                        {
                            result.Add(new CadLineInfo
                            {
                                StartPoint = line.StartPoint,
                                EndPoint = line.EndPoint,
                                LayoutName = layout.LayoutName
                            });

                            continue;
                        }

                        Polyline polyline = entity as Polyline;

                        if (polyline == null)
                            continue;

                        for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
                        {
                            // 弧段不当作直线参与序号判断
                            if (System.Math.Abs(polyline.GetBulgeAt(i)) > 0.000001)
                                continue;

                            result.Add(new CadLineInfo
                            {
                                StartPoint = polyline.GetPoint3dAt(i),
                                EndPoint = polyline.GetPoint3dAt(i + 1),
                                LayoutName = layout.LayoutName
                            });
                        }

                        if (polyline.Closed &&
                            polyline.NumberOfVertices > 1)
                        {
                            int last =
                                polyline.NumberOfVertices - 1;

                            if (System.Math.Abs(
                                    polyline.GetBulgeAt(last)) <= 0.000001)
                            {
                                result.Add(new CadLineInfo
                                {
                                    StartPoint =
                                        polyline.GetPoint3dAt(last),

                                    EndPoint =
                                        polyline.GetPoint3dAt(0),

                                    LayoutName =
                                        layout.LayoutName
                                });
                            }
                        }
                    }
                }

                trans.Commit();
            }

            return result;
        }

        public bool HasLineBelow(
            TitleText text,
            IEnumerable<CadLineInfo> lines)
        {
            return HasNearbyLine(
                text,
                lines,
                LineDirection.Below);
        }

        public bool IsWeldingCandidate(
            TitleText text,
            IEnumerable<CadLineInfo> lines)
        {
            return HasNearbyLine(
                text,
                lines,
                LineDirection.Left) &&
                !HasNearbyLine(
                    text,
                    lines,
                    LineDirection.Above);
        }

        public HashSet<int> IdentifyDrawingBomNumbers(
            List<TitleText> texts,
            List<CadLineInfo> lines,
            ISet<int> bomNumbers,
            IDictionary<string, bool> layoutDirections)
        {
            HashSet<int> result =
                new HashSet<int>();

            if (texts == null ||
                lines == null)
                return result;

            Editor editor =
                Autodesk.AutoCAD.ApplicationServices.Application
                .DocumentManager
                .MdiActiveDocument
                ?.Editor;

            int totalTextCount = texts.Count;
            int numericCandidateCount = 0;
            int validRegionPassCount = 0;
            int weldingExcludedCount = 0;
            int leadLinePassCount = 0;



            foreach (TitleText text in texts)
            {
                if (text == null ||
                    string.IsNullOrWhiteSpace(text.Text))
                {
                    continue;
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

                    numericCandidateCount++;

                    if (text.LayoutName == null ||
                        !layoutDirections.TryGetValue(
                            text.LayoutName,
                            out bool isHorizontal))
                    {
                        editor?.WriteMessage(
                            "\n方向判断:未知，忽略");

                        continue;
                    }

                    bool regionPass =
                        IsInsideValidRegion(
                            text,
                            isHorizontal);

                    if (!regionPass)
                    {
                        editor?.WriteMessage(
                            "\n区域判断失败:" +
                            " Text=" + text.Text +
                            " X=" + text.X +
                            " Y=" + text.Y +
                            " Layout=" + text.LayoutName);

                        continue;
                    }
                    bool existsInBom =
    bomNumbers != null &&
    bomNumbers.Contains(number);

                    if (existsInBom &&
                        number >= 100)
                    {
                        result.Add(number);

                        editor?.WriteMessage(
                            "\nBOM命中:是");

                        editor?.WriteMessage(
                            "\n序号>=100:直接确认存在");

                        editor?.WriteMessage(
                            "\n最终结果:加入DrawingNumbers");

                        continue;
                    }

                    validRegionPassCount++;
                    bool weldingExcluded = false;
                    int nearbyLineCount =
                        CountNearbyLines(text, lines);
                    bool hasLeadLine = false;

                    if (regionPass)
                    {
                        validRegionPassCount++;

                        weldingExcluded =
                            IsWeldingCandidateByRange(text, lines);

                        if (weldingExcluded)
                        {
                            weldingExcludedCount++;
                        }
                        else
                        {
                            double matchedDeltaY = -1;

                            hasLeadLine =
                                HasHorizontalLineBelow(
                                    text,
                                    lines,
                                    out matchedDeltaY);

                            if (hasLeadLine)
                                leadLinePassCount++;
                        }
                    }

                    editor?.WriteMessage(
                        "\n[文字候选]");
                    editor?.WriteMessage(
                        "\nText:" + numericText);
                    editor?.WriteMessage(
                        "\n位置:X=" +
                        text.X.ToString("0.####", CultureInfo.InvariantCulture) +
                        " Y=" +
                        text.Y.ToString("0.####", CultureInfo.InvariantCulture) +
                        " Layout=" + (text.LayoutName ?? ""));
                    editor?.WriteMessage(
                        "\n区域判断:" +
                        (regionPass ? "通过" : "失败"));
                    editor?.WriteMessage(
                        "\n焊接符号判断:" +
                        (weldingExcluded ? "排除" : "通过"));
                    editor?.WriteMessage(
                        "\n附近线数量:" + nearbyLineCount);
                    editor?.WriteMessage(
                        "\n引线判断:" +
                        (hasLeadLine ? "通过" : "失败"));

                    if (regionPass &&
                        !weldingExcluded &&
                        hasLeadLine)
                    {
                        result.Add(number);
                        editor?.WriteMessage(
                            "\n最终结果:加入DrawingNumbers");
                    }
                    else
                    {
                        editor?.WriteMessage(
                            "\n最终结果:忽略");
                    }
                }
            }

            editor?.WriteMessage("\n============================");
            editor?.WriteMessage("\n总文字数量:" + totalTextCount);
            editor?.WriteMessage("\n纯数字候选:" + numericCandidateCount);
            editor?.WriteMessage("\n有效区域通过:" + validRegionPassCount);
            editor?.WriteMessage("\n焊接符号排除:" + weldingExcludedCount);
            editor?.WriteMessage("\n找到引线:" + leadLinePassCount);
            editor?.WriteMessage("\n最终DrawingNumbers:" + result.Count);
            editor?.WriteMessage("\n============================");

            return result;
        }

        private IEnumerable<string> SplitNumericTexts(
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

        

        private bool IsInsideValidRegion(
            TitleText text,
            bool isHorizontal)
        {
            if (isHorizontal)
            {
                return text.X >= 45.2828 &&
                    text.X <= 449.8438 &&
                    text.Y >= 37.1450 &&
                    text.Y <= 318.7018;
            }

            return text.X >= 82.7599 &&
                text.X <= 282.7611 &&
                text.Y >= 65.4386 &&
                text.Y <= 352.4377;
        }

        private bool IsWeldingCandidateByRange(
            TitleText text,
            IEnumerable<CadLineInfo> lines)
        {
            bool hasLeftLine = false;
            bool hasUpperLine = false;

            foreach (CadLineInfo line in lines)
            {
                if (line == null ||
                    !string.Equals(
                        text.LayoutName,
                        line.LayoutName,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                double minX = System.Math.Min(line.StartPoint.X, line.EndPoint.X);
                double maxX = System.Math.Max(line.StartPoint.X, line.EndPoint.X);
                double minY = System.Math.Min(line.StartPoint.Y, line.EndPoint.Y);
                double maxY = System.Math.Max(line.StartPoint.Y, line.EndPoint.Y);

                if (!hasLeftLine &&
                    text.X - maxX > 0 &&
                    text.X - maxX <= 20 &&
                    text.Y >= minY - 5 &&
                    text.Y <= maxY + 5)
                {
                    hasLeftLine = true;
                }

                if (!hasUpperLine &&
                    minY > text.Y &&
                    minY - text.Y <= 50 &&
                    text.X >= minX - 10 &&
                    text.X <= maxX + 10)
                {
                    hasUpperLine = true;
                }

                if (hasLeftLine && hasUpperLine)
                    break;
            }

            return hasLeftLine && !hasUpperLine;
        }

        private bool HasHorizontalLineBelow(
     TitleText text,
     IEnumerable<CadLineInfo> lines,
     out double matchedDeltaY)
        {
            const double horizontalTolerance = 0.5;

            matchedDeltaY = -1;

            foreach (CadLineInfo line in lines)
            {
                if (line == null)
                    continue;

                if (!string.Equals(
                        text.LayoutName,
                        line.LayoutName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // 必须是水平线
                if (Math.Abs(
                        line.StartPoint.Y -
                        line.EndPoint.Y) >
                    horizontalTolerance)
                {
                    continue;
                }

                double lineY =
                    (line.StartPoint.Y +
                     line.EndPoint.Y) / 2.0;

                double deltaY =
                    text.Y - lineY;

                // 按你给的明确规则：
                // 数字下方Y距离0~10
                if (deltaY < 0 ||
                    deltaY > 10)
                {
                    continue;
                }

                matchedDeltaY = deltaY;

                return true;
            }

            return false;
        }

        private int CountNearbyLines(
            TitleText text,
            IEnumerable<CadLineInfo> lines)
        {
            int count = 0;

            foreach (CadLineInfo line in lines)
            {
                if (line == null ||
                    !string.Equals(
                        text.LayoutName,
                        line.LayoutName,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                double minX =
                    System.Math.Min(line.StartPoint.X, line.EndPoint.X);
                double maxX =
                    System.Math.Max(line.StartPoint.X, line.EndPoint.X);
                double minY =
                    System.Math.Min(line.StartPoint.Y, line.EndPoint.Y);
                double maxY =
                    System.Math.Max(line.StartPoint.Y, line.EndPoint.Y);

                if (maxX >= text.X - 30 &&
                    minX <= text.X + 30 &&
                    maxY >= text.Y - 50 &&
                    minY <= text.Y + 20)
                {
                    count++;
                }
            }

            return count;
        }

        private bool HasNearbyLine(
            TitleText text,
            IEnumerable<CadLineInfo> lines,
            LineDirection direction)
        {
            if (text == null || lines == null)
                return false;

            double tolerance =
                System.Math.Max(text.Height * 4.0, 5.0);
            double coordinateTolerance =
                System.Math.Max(text.Height, 1.0);

            foreach (CadLineInfo line in lines)
            {
                if (line == null ||
                    !string.Equals(
                        text.LayoutName,
                        line.LayoutName,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (direction == LineDirection.Below &&
                    IsBelow(text, line, tolerance, coordinateTolerance))
                    return true;

                if (direction == LineDirection.Left &&
                    IsLeft(text, line, tolerance, coordinateTolerance))
                    return true;

                if (direction == LineDirection.Above &&
                    IsAbove(text, line, tolerance, coordinateTolerance))
                    return true;
            }

            return false;
        }

        private bool IsBelow(
            TitleText text,
            CadLineInfo line,
            double tolerance,
            double coordinateTolerance)
        {
            double y = System.Math.Max(
                line.StartPoint.Y,
                line.EndPoint.Y);
            double x = (line.StartPoint.X + line.EndPoint.X) / 2.0;

            return y < text.Y &&
                text.Y - y <= tolerance &&
                System.Math.Abs(x - text.X) <= coordinateTolerance;
        }

        private bool IsLeft(
            TitleText text,
            CadLineInfo line,
            double tolerance,
            double coordinateTolerance)
        {
            double x = System.Math.Max(
                line.StartPoint.X,
                line.EndPoint.X);
            double y = (line.StartPoint.Y + line.EndPoint.Y) / 2.0;

            return x < text.X &&
                text.X - x <= tolerance &&
                System.Math.Abs(y - text.Y) <= coordinateTolerance;
        }

        private bool IsAbove(
            TitleText text,
            CadLineInfo line,
            double tolerance,
            double coordinateTolerance)
        {
            double y = System.Math.Min(
                line.StartPoint.Y,
                line.EndPoint.Y);
            double x = (line.StartPoint.X + line.EndPoint.X) / 2.0;

            return y > text.Y &&
                y - text.Y <= tolerance &&
                System.Math.Abs(x - text.X) <= coordinateTolerance;
        }

        private enum LineDirection
        {
            Below,
            Left,
            Above
        }
    }
}