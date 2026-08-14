using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Correct_test1.QuickRevision.Models;
using Correct_test1.QuickRevision.Viewports;

using ModelDbTextResolver =
    Correct_test1.QuickRevision.Resolvers.ModelSpace.DbTextResolver;

using ModelMTextResolver =
    Correct_test1.QuickRevision.Resolvers.ModelSpace.MTextResolver;

using DimensionResolver =
    Correct_test1.QuickRevision.Resolvers.ModelSpace.DimensionResolver;

namespace Correct_test1.QuickRevision.Resolvers
{
    /// <summary>
    /// Viewport目标解析器。
    ///
    /// 用户始终在Paper Space点击，
    /// 不需要双击进入Viewport。
    ///
    /// 流程：
    ///
    /// Paper点击
    /// ↓
    /// 找到Viewport
    /// ↓
    /// Paper坐标转换为Model坐标
    /// ↓
    /// 在Model Space寻找目标
    ///
    /// 支持：
    /// DBText
    /// MText
    /// Dimension
    /// </summary>
    public class ViewportTargetResolver
    {
        private readonly ViewportResolver
            _viewportResolver;

        private readonly ViewportCoordinateConverter
            _coordinateConverter;

        private readonly ModelDbTextResolver
            _dbTextResolver;

        private readonly ModelMTextResolver
            _mTextResolver;

        private readonly DimensionResolver
            _dimensionResolver;


        public ViewportTargetResolver()
        {
            _viewportResolver =
                new ViewportResolver();

            _coordinateConverter =
                new ViewportCoordinateConverter();

            _dbTextResolver =
                new ModelDbTextResolver();

            _mTextResolver =
                new ModelMTextResolver();

            _dimensionResolver =
                new DimensionResolver();
        }


        /// <summary>
        /// 根据Paper Space中的点击位置，
        /// 尝试解析Viewport内的Model Space目标。
        /// </summary>
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


            //--------------------------------
            // 第一步：
            // 找到点击位置所在的Viewport
            //--------------------------------

            ViewportContext viewportContext =
                _viewportResolver.Resolve(
                    database,
                    transaction,
                    paperPoint);


            if (viewportContext == null)
                return null;


            if (!viewportContext.IsValid())
                return null;


            //--------------------------------
            // 第二步：
            // Paper坐标 → Model坐标
            //--------------------------------

            Point3d modelPoint;


            bool converted =
                _coordinateConverter.TryPaperToModel(
                    viewportContext,
                    paperPoint,
                    out modelPoint);


            if (!converted)
                return null;


            viewportContext.ModelPoint =
                modelPoint;


            //--------------------------------
            // 第三步：
            // 获取Model Space
            //--------------------------------

            ObjectId modelSpaceId;

            try
            {
                modelSpaceId =
                    SymbolUtilityServices
                        .GetBlockModelSpaceId(
                            database);
            }
            catch (System.Exception)
            {
                return null;
            }


            if (modelSpaceId.IsNull ||
                !modelSpaceId.IsValid)
            {
                return null;
            }


            BlockTableRecord modelSpace =
                transaction.GetObject(
                    modelSpaceId,
                    OpenMode.ForRead)
                as BlockTableRecord;


            if (modelSpace == null)
                return null;


            //--------------------------------
            // 找点击位置最近的目标
            //--------------------------------

            RevisionTarget bestTarget =
                null;

            double bestDistance =
                double.MaxValue;


            foreach (ObjectId objectId
                in modelSpace)
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


                RevisionTarget target =
                    null;


                //--------------------------------
                // Dimension
                //
                // RotatedDimension也属于这里。
                //--------------------------------

                if (obj is Dimension)
                {
                    target =
                        _dimensionResolver.Resolve(
                            database,
                            transaction,
                            objectId,
                            viewportContext);
                }


                //--------------------------------
                // DBText
                //--------------------------------

                else if (obj is DBText)
                {
                    target =
                        _dbTextResolver.Resolve(
                            database,
                            transaction,
                            objectId,
                            viewportContext);
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
                            objectId,
                            viewportContext);
                }


                if (target == null)
                    continue;


                //--------------------------------
                // 点击位置是否接近这个目标
                //--------------------------------

                if (!IsPointNearTarget(
                        target,
                        modelPoint))
                {
                    continue;
                }


                double distance =
                    GetDistanceSquared(
                        target,
                        modelPoint);


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


        /// <summary>
        /// 判断Model Space点击位置
        /// 是否接近目标文字区域。
        /// </summary>
        private static bool IsPointNearTarget(
    RevisionTarget target,
    Point3d point)
        {
            if (target == null)
                return false;


            double visualHeight =
                target.TopY -
                target.BottomY;


            if (!IsValidNumber(
                    visualHeight) ||
                visualHeight <= 0)
            {
                visualHeight =
                    target.TextHeight;
            }


            if (!IsValidNumber(
                    visualHeight) ||
                visualHeight <= 0)
            {
                return false;
            }


            //--------------------------------
            // 以前是：
            //
            // visualHeight * 0.75
            //
            // 对于1、2这种靠得很近的序号，
            // 点击区域太大，
            // 两个目标容易互相覆盖。
            //
            // 现在缩小到30%。
            //--------------------------------

            double tolerance =
                visualHeight *
                0.30;


            //--------------------------------
            // 不再使用固定0.5作为最小值。
            //
            // 因为不同图纸单位比例差异很大，
            // 固定0.5可能远大于一个小序号。
            //--------------------------------

            double minimumTolerance =
                visualHeight *
                0.10;


            if (tolerance <
                minimumTolerance)
            {
                tolerance =
                    minimumTolerance;
            }


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


        private static bool IsValidNumber(
            double value)
        {
            return
                !double.IsNaN(value) &&
                !double.IsInfinity(value) &&
                System.Math.Abs(value) < 1E15;
        }


        private static double GetDistanceSquared(
            RevisionTarget target,
            Point3d point)
        {
            double centerX =
                (
                    target.LeftX +
                    target.RightX
                )
                /
                2.0;


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