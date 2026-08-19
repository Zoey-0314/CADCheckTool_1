using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.QuickRevision.Models;

namespace Correct_test1.QuickRevision.Resolvers.ModelSpace
{
    /// <summary>
    /// 解析Model Space中的DBText。
    /// 当前仅处理水平文字。
    /// </summary>
    public class DbTextResolver
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

            DBText text =
                transaction.GetObject(
                    objectId,
                    OpenMode.ForRead)
                as DBText;

            if (text == null)
                return null;

            string content =
                text.TextString ?? "";

            if (string.IsNullOrWhiteSpace(content))
                return null;

            Extents3d extents;

            try
            {
                extents =
                    text.GeometricExtents;
            }
            catch (System.Exception)
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
                "DBText";

            target.Text =
                content;

            target.TargetSpaceId =
                SymbolUtilityServices
                    .GetBlockModelSpaceId(database);

            target.LeftX =
                leftX;

            target.RightX =
                rightX;

            target.BottomY =
                bottomY;

            target.TopY =
                topY;

            target.CenterY =
                (bottomY + topY) / 2.0;

            target.TextHeight =
                text.Height;

            target.IsInViewport =
                true;

            target.ViewportId =
                viewportContext == null
                    ? ObjectId.Null
                    : viewportContext.ViewportId;

            target.TextStyleId =
                text.TextStyleId;

            return target;
        }


        private static bool IsValidRange(
            double leftX,
            double rightX,
            double bottomY,
            double topY)
        {
            if (!IsValidNumber(leftX) ||
                !IsValidNumber(rightX) ||
                !IsValidNumber(bottomY) ||
                !IsValidNumber(topY))
            {
                return false;
            }

            if (rightX <= leftX)
                return false;

            if (topY < bottomY)
                return false;

            return true;
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