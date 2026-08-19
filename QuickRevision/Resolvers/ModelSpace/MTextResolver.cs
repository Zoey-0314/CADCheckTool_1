using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.QuickRevision.Models;

namespace Correct_test1.QuickRevision.Resolvers.ModelSpace
{
    /// <summary>
    /// Model Space MText解析器。
    ///
    /// 使用MText的实际显示范围，
    /// 不使用可能包含定义宽度的宽松范围。
    ///
    /// 第一版只处理水平文字。
    /// </summary>
    public class MTextResolver
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


            MText text =
                transaction.GetObject(
                    objectId,
                    OpenMode.ForRead)
                as MText;


            if (text == null)
                return null;


            //--------------------------------
            // 显示文字
            //--------------------------------

            string content =
                TextGeometryHelper.CleanText(
                    text.Text);


            if (string.IsNullOrWhiteSpace(
                    content))
            {
                return null;
            }


            //--------------------------------
            // 关键修改：
            //
            // 不再：
            // text.GeometricExtents
            //
            // 改为：
            // ActualWidth + ActualHeight
            //--------------------------------

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
            // 删除线穿过实际显示文字中心。
            //--------------------------------

            target.CenterY =
                (
                    bottomY +
                    topY
                ) / 2.0;


            target.TextHeight =
                text.TextHeight;


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