using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Correct_test1.QuickRevision.Models;

using System.Globalization;

namespace Correct_test1.QuickRevision.Resolvers.ModelSpace
{
    /// <summary>
    /// Model Space Dimension解析器。
    ///
    /// RotatedDimension等Dimension，
    /// 优先通过Explode取得真正显示的文字。
    ///
    /// 不再使用整个Dimension.GeometricExtents，
    /// 也不再主要依赖TextPosition估算。
    /// </summary>
    public class DimensionResolver
    {
        public RevisionTarget Resolve(
            Database database,
            Transaction transaction,
            ObjectId objectId,
            ViewportContext viewportContext)
        {
            if (database == null ||
                transaction == null ||
                objectId.IsNull ||
                !objectId.IsValid)
            {
                return null;
            }


            Dimension dimension =
                transaction.GetObject(
                    objectId,
                    OpenMode.ForRead)
                as Dimension;


            if (dimension == null)
                return null;


            //--------------------------------
            // 优先找到尺寸实际显示文字
            //--------------------------------

            string displayText;

            Extents3d textExtents;

            double textHeight;

            ObjectId textStyleId;


            bool gotActualText =
                TryGetActualDimensionText(
                    dimension,
                    out displayText,
                    out textExtents,
                    out textHeight,
                    out textStyleId);


            if (gotActualText)
            {
                double leftX =
                    textExtents.MinPoint.X;

                double rightX =
                    textExtents.MaxPoint.X;

                double bottomY =
                    textExtents.MinPoint.Y;

                double topY =
                    textExtents.MaxPoint.Y;


                if (IsValidRange(
                        leftX,
                        rightX,
                        bottomY,
                        topY))
                {
                    RevisionTarget target =
                        new RevisionTarget();


                    target.SourceId =
                        dimension.ObjectId;


                    target.SourceType =
                        dimension
                            .GetType()
                            .Name;


                    target.Text =
                        displayText;


                    target.TargetSpaceId =
                        SymbolUtilityServices
                            .GetBlockModelSpaceId(
                                database);


                    target.LeftX =
                        leftX;

                    target.RightX =
                        rightX;

                    target.BottomY =
                        bottomY;

                    target.TopY =
                        topY;


                    //--------------------------------
                    // 关键：
                    //
                    // 删除线穿过实际文字包围框中心。
                    //--------------------------------

                    target.CenterY =
                        (
                            bottomY +
                            topY
                        ) / 2.0;


                    target.TextHeight =
                        textHeight;


                    target.IsInViewport =
                        true;


                    target.ViewportId =
                        viewportContext == null
                            ? ObjectId.Null
                            : viewportContext.ViewportId;


                    target.TextStyleId =
                        textStyleId;


                    return target;
                }
            }


            //--------------------------------
            // Explode失败时才Fallback
            //--------------------------------

            return ResolveFallback(
                database,
                dimension,
                viewportContext);
        }


        private static bool TryGetActualDimensionText(
            Dimension dimension,
            out string displayText,
            out Extents3d bestExtents,
            out double bestTextHeight,
            out ObjectId bestTextStyleId)
        {
            displayText =
                "";

            bestExtents =
                new Extents3d();

            bestTextHeight =
                0;

            bestTextStyleId =
                ObjectId.Null;


            DBObjectCollection exploded =
                new DBObjectCollection();


            try
            {
                dimension.Explode(
                    exploded);


                if (exploded.Count == 0)
                    return false;


                Point3d expectedPosition;

                try
                {
                    expectedPosition =
                        dimension.TextPosition;
                }
                catch (System.Exception)
                {
                    expectedPosition =
                        Point3d.Origin;
                }


                bool found =
                    false;

                double bestDistance =
                    double.MaxValue;


                foreach (DBObject obj
                    in exploded)
                {
                    string candidateText =
                        "";

                    Extents3d candidateExtents;

                    double candidateHeight =
                        0;

                    ObjectId candidateStyleId =
                        ObjectId.Null;


                    //--------------------------------
                    // MText
                    //--------------------------------

                    MText mtext =
     obj as MText;

                    if (mtext != null)
                    {
                        try
                        {
                            candidateText =
                                TextGeometryHelper.CleanText(
                                    mtext.Text);


                            //--------------------------------
                            // 关键修改：
                            //
                            // Table.Explode产生的MText
                            // 可能仍带有接近整个Cell宽度的
                            // MText定义宽度。
                            //
                            // 所以不能使用GeometricExtents。
                            //--------------------------------

                            if (!TextGeometryHelper
                                    .TryGetMTextExtents(
                                        mtext,
                                        out candidateExtents))
                            {
                                continue;
                            }


                            candidateHeight =
                                mtext.TextHeight;


                            candidateStyleId =
                                mtext.TextStyleId;
                        }
                        catch (System.Exception)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        //--------------------------------
                        // DBText
                        //--------------------------------

                        DBText dbText =
                            obj as DBText;


                        if (dbText == null)
                            continue;


                        try
                        {
                            candidateText =
                                CleanText(
                                    dbText.TextString);

                            candidateExtents =
                                dbText.GeometricExtents;

                            candidateHeight =
                                dbText.Height;

                            candidateStyleId =
                                dbText.TextStyleId;
                        }
                        catch (System.Exception)
                        {
                            continue;
                        }
                    }


                    if (!IsValidExtents(
                            candidateExtents))
                    {
                        continue;
                    }


                    Point3d center =
                        GetCenter(
                            candidateExtents);


                    double distance =
                        DistanceSquared(
                            center,
                            expectedPosition);


                    if (distance <
                        bestDistance)
                    {
                        bestDistance =
                            distance;


                        displayText =
                            candidateText;


                        bestExtents =
                            candidateExtents;


                        bestTextHeight =
                            candidateHeight;


                        bestTextStyleId =
                            candidateStyleId;


                        found =
                            true;
                    }
                }


                if (!found)
                    return false;


                if (string.IsNullOrWhiteSpace(
                        displayText))
                {
                    displayText =
                        GetDisplayText(
                            dimension);
                }


                if (!IsValidNumber(
                        bestTextHeight) ||
                    bestTextHeight <= 0)
                {
                    bestTextHeight =
                        dimension.Dimtxt;
                }


                return
                    !string.IsNullOrWhiteSpace(
                        displayText);
            }
            catch (System.Exception)
            {
                return false;
            }
            finally
            {
                foreach (DBObject obj
                    in exploded)
                {
                    try
                    {
                        obj.Dispose();
                    }
                    catch (System.Exception)
                    {
                    }
                }
            }
        }


        private static RevisionTarget ResolveFallback(
            Database database,
            Dimension dimension,
            ViewportContext viewportContext)
        {
            string displayText =
                GetDisplayText(
                    dimension);


            if (string.IsNullOrWhiteSpace(
                    displayText))
            {
                return null;
            }


            double textHeight =
                dimension.Dimtxt;


            if (!IsValidNumber(textHeight) ||
                textHeight <= 0)
            {
                textHeight =
                    2.5;
            }


            Point3d textPosition;

            try
            {
                textPosition =
                    dimension.TextPosition;
            }
            catch (System.Exception)
            {
                return null;
            }


            //--------------------------------
            // Fallback才使用估算。
            //--------------------------------

            double textWidth =
                EstimateTextWidth(
                    displayText,
                    textHeight);


            double leftX =
                textPosition.X -
                textWidth / 2.0;

            double rightX =
                textPosition.X +
                textWidth / 2.0;

            double bottomY =
                textPosition.Y -
                textHeight / 2.0;

            double topY =
                textPosition.Y +
                textHeight / 2.0;


            RevisionTarget target =
                new RevisionTarget();


            target.SourceId =
                dimension.ObjectId;


            target.SourceType =
                dimension
                    .GetType()
                    .Name;


            target.Text =
                displayText;


            target.TargetSpaceId =
                SymbolUtilityServices
                    .GetBlockModelSpaceId(
                        database);


            target.LeftX =
                leftX;

            target.RightX =
                rightX;

            target.BottomY =
                bottomY;

            target.TopY =
                topY;


            target.CenterY =
                textPosition.Y;


            target.TextHeight =
                textHeight;


            target.IsInViewport =
                true;


            target.ViewportId =
                viewportContext == null
                    ? ObjectId.Null
                    : viewportContext.ViewportId;


            target.TextStyleId =
                ObjectId.Null;


            return target;
        }


        private static string GetDisplayText(
            Dimension dimension)
        {
            if (dimension == null)
                return "";


            string overrideText =
                dimension.DimensionText;


            if (string.IsNullOrWhiteSpace(
                    overrideText) ||
                overrideText == "<>")
            {
                return FormatMeasurement(
                    dimension.Measurement);
            }


            if (overrideText.Contains("<>"))
            {
                return overrideText.Replace(
                    "<>",
                    FormatMeasurement(
                        dimension.Measurement));
            }


            return overrideText;
        }


        private static string FormatMeasurement(
            double measurement)
        {
            if (!IsValidNumber(
                    measurement))
            {
                return "";
            }


            return measurement.ToString(
                "0.###",
                CultureInfo.InvariantCulture);
        }


        private static string CleanText(
            string text)
        {
            if (text == null)
                return "";


            return text
                .Replace("\\P", " ")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
        }


        private static Point3d GetCenter(
            Extents3d extents)
        {
            return new Point3d(
                (
                    extents.MinPoint.X +
                    extents.MaxPoint.X
                ) / 2.0,

                (
                    extents.MinPoint.Y +
                    extents.MaxPoint.Y
                ) / 2.0,

                (
                    extents.MinPoint.Z +
                    extents.MaxPoint.Z
                ) / 2.0);
        }


        private static double DistanceSquared(
            Point3d p1,
            Point3d p2)
        {
            double dx =
                p1.X -
                p2.X;

            double dy =
                p1.Y -
                p2.Y;


            return
                dx * dx +
                dy * dy;
        }


        private static double EstimateTextWidth(
            string text,
            double height)
        {
            if (string.IsNullOrEmpty(text))
                return height;


            double width =
                text.Length *
                height *
                0.60;


            if (width < height)
                width = height;


            return width;
        }


        private static bool IsValidExtents(
            Extents3d extents)
        {
            return IsValidRange(
                extents.MinPoint.X,
                extents.MaxPoint.X,
                extents.MinPoint.Y,
                extents.MaxPoint.Y);
        }


        private static bool IsValidRange(
            double left,
            double right,
            double bottom,
            double top)
        {
            return
                IsValidNumber(left) &&
                IsValidNumber(right) &&
                IsValidNumber(bottom) &&
                IsValidNumber(top) &&
                right > left &&
                top >= bottom;
        }


        private static bool IsValidNumber(
            double value)
        {
            return
                !double.IsNaN(value) &&
                !double.IsInfinity(value) &&
                System.Math.Abs(value) < 1E15;
        }
    }
}