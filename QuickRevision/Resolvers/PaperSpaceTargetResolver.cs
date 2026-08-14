using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Correct_test1.QuickRevision.Models;

using PaperDbTextResolver =
    Correct_test1.QuickRevision.Resolvers.PaperSpace.DbTextResolver;

using PaperMTextResolver =
    Correct_test1.QuickRevision.Resolvers.PaperSpace.MTextResolver;

using TableCellResolver =
    Correct_test1.QuickRevision.Resolvers.PaperSpace.TableCellResolver;

namespace Correct_test1.QuickRevision.Resolvers
{
    /// <summary>
    /// Paper Space目标统一解析器。
    ///
    /// 支持：
    /// Table Cell
    /// DBText
    /// MText
    /// </summary>
    public class PaperSpaceTargetResolver
    {
        private readonly PaperDbTextResolver
            _dbTextResolver;

        private readonly PaperMTextResolver
            _mTextResolver;

        private readonly TableCellResolver
            _tableCellResolver;


        public PaperSpaceTargetResolver()
        {
            _dbTextResolver =
                new PaperDbTextResolver();

            _mTextResolver =
                new PaperMTextResolver();

            _tableCellResolver =
                new TableCellResolver();
        }


        public RevisionTarget Resolve(
            Database database,
            Transaction transaction,
            Point3d paperPoint)
        {
            if (database == null ||
                transaction == null)
            {
                return null;
            }


            if (database.TileMode)
                return null;


            ObjectId paperSpaceId =
                GetCurrentPaperSpaceId(
                    transaction);


            if (paperSpaceId.IsNull ||
                !paperSpaceId.IsValid)
            {
                return null;
            }


            BlockTableRecord paperSpace =
                transaction.GetObject(
                    paperSpaceId,
                    OpenMode.ForRead)
                as BlockTableRecord;


            if (paperSpace == null)
                return null;


            RevisionTarget bestTarget =
                null;

            double bestDistance =
                double.MaxValue;


            foreach (ObjectId objectId
                in paperSpace)
            {
                if (objectId.IsNull ||
                    !objectId.IsValid)
                {
                    continue;
                }


                DBObject obj;

                try
                {
                    obj =
                        transaction.GetObject(
                            objectId,
                            OpenMode.ForRead);
                }
                catch (System.Exception)
                {
                    continue;
                }


                if (obj == null ||
                    obj.IsErased)
                {
                    continue;
                }


                //--------------------------------
                // Table：
                //
                // TableCellResolver已经根据点击点
                // 找到了准确Cell。
                //
                // 找到以后直接返回，
                // 不进行第二次通用范围过滤。
                //--------------------------------

                if (obj is Table)
                {
                    RevisionTarget tableTarget =
                        _tableCellResolver.Resolve(
                            database,
                            transaction,
                            objectId,
                            paperPoint);


                    if (tableTarget != null)
                    {
                        return tableTarget;
                    }


                    continue;
                }


                RevisionTarget target =
                    null;


                //--------------------------------
                // DBText
                //--------------------------------

                if (obj is DBText)
                {
                    target =
                        _dbTextResolver.Resolve(
                            database,
                            transaction,
                            objectId);
                }


                //--------------------------------
                // MText
                //--------------------------------

                else if (obj is MText)
                {
                    target =
                        _mTextResolver.Resolve(
                            database,
                            transaction,
                            objectId);
                }


                if (target == null)
                    continue;


                if (!IsPointNearTarget(
                        target,
                        paperPoint))
                {
                    continue;
                }


                double distance =
                    GetDistanceSquared(
                        target,
                        paperPoint);


                if (distance <
                    bestDistance)
                {
                    bestDistance =
                        distance;

                    bestTarget =
                        target;
                }
            }


            return bestTarget;
        }


        private static ObjectId GetCurrentPaperSpaceId(
            Transaction transaction)
        {
            try
            {
                LayoutManager manager =
                    LayoutManager.Current;


                if (manager == null)
                    return ObjectId.Null;


                string layoutName =
                    manager.CurrentLayout;


                if (string.IsNullOrWhiteSpace(
                        layoutName))
                {
                    return ObjectId.Null;
                }


                ObjectId layoutId =
                    manager.GetLayoutId(
                        layoutName);


                if (layoutId.IsNull ||
                    !layoutId.IsValid)
                {
                    return ObjectId.Null;
                }


                Layout layout =
                    transaction.GetObject(
                        layoutId,
                        OpenMode.ForRead)
                    as Layout;


                if (layout == null)
                    return ObjectId.Null;


                return
                    layout.BlockTableRecordId;
            }
            catch (System.Exception)
            {
                return ObjectId.Null;
            }
        }


        private static bool IsPointNearTarget(
            RevisionTarget target,
            Point3d point)
        {
            if (target == null)
                return false;


            double visualHeight =
                target.TopY -
                target.BottomY;


            if (visualHeight <
                target.TextHeight)
            {
                visualHeight =
                    target.TextHeight;
            }


            double tolerance =
                visualHeight *
                0.75;


            if (tolerance < 0.5)
                tolerance = 0.5;


            return
                point.X >=
                    target.LeftX -
                    tolerance &&

                point.X <=
                    target.RightX +
                    tolerance &&

                point.Y >=
                    target.BottomY -
                    tolerance &&

                point.Y <=
                    target.TopY +
                    tolerance;
        }


        private static double GetDistanceSquared(
            RevisionTarget target,
            Point3d point)
        {
            double centerX =
                (
                    target.LeftX +
                    target.RightX
                ) / 2.0;


            double centerY =
                target.CenterY;


            double dx =
                point.X -
                centerX;

            double dy =
                point.Y -
                centerY;


            return
                dx * dx +
                dy * dy;
        }
    }
}