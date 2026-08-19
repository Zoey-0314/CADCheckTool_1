using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace Correct_test1.QuickRevision.Resolvers
{
    /// <summary>
    /// 统一计算文字实际视觉范围。
    /// MText不能直接依赖GeometricExtents，
    /// 因为MText本身可能设置了较大的换行宽度。
    /// QuickRevision 当前只处理水平文字。
    /// </summary>
    internal static class TextGeometryHelper
    {
        /// <summary>
        /// 获取MText实际显示文字的紧凑范围。
        /// 优先：
        /// ActualWidth + ActualHeight + Attachment + Location
        /// 失败时才回退到GeometricExtents。
        /// </summary>
        public static bool TryGetMTextExtents(
            MText text,
            out Extents3d extents)
        {
            extents =
                new Extents3d();

            if (text == null)
                return false;


            try
            {
                double width =
                    text.ActualWidth;

                double height =
                    text.ActualHeight;


                if (IsValidNumber(width) &&
                    IsValidNumber(height) &&
                    width > 0 &&
                    height > 0)
                {
                    Point3d location =
                        text.Location;


                    double left;
                    double right;
                    double bottom;
                    double top;


                    switch (text.Attachment)
                    {
                        // TOP

                        case AttachmentPoint.TopLeft:

                            left =
                                location.X;

                            right =
                                location.X +
                                width;

                            top =
                                location.Y;

                            bottom =
                                location.Y -
                                height;

                            break;


                        case AttachmentPoint.TopCenter:

                            left =
                                location.X -
                                width / 2.0;

                            right =
                                location.X +
                                width / 2.0;

                            top =
                                location.Y;

                            bottom =
                                location.Y -
                                height;

                            break;


                        case AttachmentPoint.TopRight:

                            left =
                                location.X -
                                width;

                            right =
                                location.X;

                            top =
                                location.Y;

                            bottom =
                                location.Y -
                                height;

                            break;


                        // MIDDLE

                        case AttachmentPoint.MiddleLeft:

                            left =
                                location.X;

                            right =
                                location.X +
                                width;

                            bottom =
                                location.Y -
                                height / 2.0;

                            top =
                                location.Y +
                                height / 2.0;

                            break;


                        case AttachmentPoint.MiddleCenter:

                            left =
                                location.X -
                                width / 2.0;

                            right =
                                location.X +
                                width / 2.0;

                            bottom =
                                location.Y -
                                height / 2.0;

                            top =
                                location.Y +
                                height / 2.0;

                            break;


                        case AttachmentPoint.MiddleRight:

                            left =
                                location.X -
                                width;

                            right =
                                location.X;

                            bottom =
                                location.Y -
                                height / 2.0;

                            top =
                                location.Y +
                                height / 2.0;

                            break;


                        // BOTTOM

                        case AttachmentPoint.BottomLeft:

                            left =
                                location.X;

                            right =
                                location.X +
                                width;

                            bottom =
                                location.Y;

                            top =
                                location.Y +
                                height;

                            break;


                        case AttachmentPoint.BottomCenter:

                            left =
                                location.X -
                                width / 2.0;

                            right =
                                location.X +
                                width / 2.0;

                            bottom =
                                location.Y;

                            top =
                                location.Y +
                                height;

                            break;


                        case AttachmentPoint.BottomRight:

                            left =
                                location.X -
                                width;

                            right =
                                location.X;

                            bottom =
                                location.Y;

                            top =
                                location.Y +
                                height;

                            break;


                        // 未知Attachment

                        default:

                            return
                                TryGetGeometricExtents(
                                    text,
                                    out extents);
                    }


                    if (IsValidRange(
                            left,
                            right,
                            bottom,
                            top))
                    {
                        extents =
                            new Extents3d(
                                new Point3d(
                                    left,
                                    bottom,
                                    location.Z),

                                new Point3d(
                                    right,
                                    top,
                                    location.Z));


                        return true;
                    }
                }
            }
            catch (System.Exception)
            {
            }


            // ActualWidth失败才Fallback

            return
                TryGetGeometricExtents(
                    text,
                    out extents);
        }


        private static bool TryGetGeometricExtents(
            MText text,
            out Extents3d extents)
        {
            extents =
                new Extents3d();


            try
            {
                extents =
                    text.GeometricExtents;


                return IsValidRange(
                    extents.MinPoint.X,
                    extents.MaxPoint.X,
                    extents.MinPoint.Y,
                    extents.MaxPoint.Y);
            }
            catch (System.Exception)
            {
                return false;
            }
        }


        public static string CleanText(
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
                top > bottom;
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
