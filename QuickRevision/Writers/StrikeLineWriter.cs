using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Correct_test1.QuickRevision.Models;

namespace Correct_test1.QuickRevision.Writers
{
    /// <summary>
    /// 创建快速划改删除线。
    /// 仅处理水平删除线。
    /// 删除线严格按照RevisionTarget
    /// 的真实文字左右范围创建，
    /// 不额外延长。
    /// </summary>
    public class StrikeLineWriter
    {
        public ObjectId Write(
            Database database,
            Transaction transaction,
            RevisionTarget target)
        {
            if (database == null ||
                transaction == null ||
                target == null)
            {
                return ObjectId.Null;
            }


            if (!target.IsValid())
                return ObjectId.Null;


            // QuickRevision红色专用图层

            ObjectId layerId =
                RevisionEntityHelper
                    .EnsureRevisionLayer(
                        database,
                        transaction);


            if (layerId.IsNull ||
                !layerId.IsValid)
            {
                return ObjectId.Null;
            }


            RevisionEntityHelper
                .EnsureRegApp(
                    database,
                    transaction);


            // 获取写入空间

            BlockTableRecord targetSpace =
                transaction.GetObject(
                    target.TargetSpaceId,
                    OpenMode.ForWrite)
                as BlockTableRecord;


            if (targetSpace == null)
                return ObjectId.Null;


            // 不再增加margin。
            //
            // Resolver已经提供实际文字范围。

            double startX =
                target.LeftX;

            double endX =
                target.RightX;


            if (!IsValidNumber(startX) ||
                !IsValidNumber(endX) ||
                endX <= startX)
            {
                return ObjectId.Null;
            }


            // 删除线穿过文字中心

            Point3d startPoint =
                new Point3d(
                    startX,
                    target.CenterY,
                    0.0);


            Point3d endPoint =
                new Point3d(
                    endX,
                    target.CenterY,
                    0.0);


            if (!IsValidPoint(startPoint) ||
                !IsValidPoint(endPoint))
            {
                return ObjectId.Null;
            }


            Line line =
                new Line(
                    startPoint,
                    endPoint);


            bool appended =
                false;


            try
            {
                line.SetDatabaseDefaults(
                    database);


                // 图层 + 红色

                RevisionEntityHelper
                    .ApplyRevisionAppearance(
                        line,
                        layerId);


                // XData

                RevisionEntityHelper
                    .ApplyXData(
                        line,
                        "StrikeLine");


                ObjectId lineId =
                    targetSpace.AppendEntity(
                        line);


                appended =
                    true;


                transaction
                    .AddNewlyCreatedDBObject(
                        line,
                        true);


                return lineId;
            }
            catch (System.Exception)
            {
                if (!appended)
                {
                    line.Dispose();
                }


                return ObjectId.Null;
            }
        }


        private static bool IsValidPoint(
            Point3d point)
        {
            return
                IsValidNumber(point.X) &&
                IsValidNumber(point.Y) &&
                IsValidNumber(point.Z);
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