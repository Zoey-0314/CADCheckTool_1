using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.QuickRevision.Models;

namespace Correct_test1.QuickRevision.Resolvers.PaperSpace
{
    /// <summary>
    /// Paper Space MText解析器。
    ///
    /// 使用实际显示文字范围。
    /// </summary>
    public class MTextResolver
    {
        public RevisionTarget Resolve(
            Database database,
            Transaction transaction,
            ObjectId objectId)
        {
            if (database == null ||
                transaction == null ||
                objectId.IsNull ||
                !objectId.IsValid)
            {
                return null;
            }


            MText text =
                transaction.GetObject(
                    objectId,
                    OpenMode.ForRead)
                as MText;


            if (text == null)
                return null;


            string content =
                TextGeometryHelper.CleanText(
                    text.Text);


            if (string.IsNullOrWhiteSpace(
                    content))
            {
                return null;
            }


            Extents3d extents;


            if (!TextGeometryHelper
                    .TryGetMTextExtents(
                        text,
                        out extents))
            {
                return null;
            }


            double leftX =
                extents.MinPoint.X;

            double rightX =
                extents.MaxPoint.X;

            double bottomY =
                extents.MinPoint.Y;

            double topY =
                extents.MaxPoint.Y;


            if (!IsValidRange(
                    leftX,
                    rightX,
                    bottomY,
                    topY))
            {
                return null;
            }


            RevisionTarget target =
                new RevisionTarget();


            target.SourceId =
                text.ObjectId;


            target.SourceType =
                "MText";


            target.Text =
                content;


            target.TargetSpaceId =
                text.OwnerId;


            target.LeftX =
                leftX;


            target.RightX =
                rightX;


            target.BottomY =
                bottomY;


            target.TopY =
                topY;


            target.CenterY =
                (
                    bottomY +
                    topY
                ) / 2.0;


            target.TextHeight =
                text.TextHeight;


            target.IsInViewport =
                false;


            target.ViewportId =
                ObjectId.Null;


            target.TextStyleId =
                text.TextStyleId;


            return target;
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