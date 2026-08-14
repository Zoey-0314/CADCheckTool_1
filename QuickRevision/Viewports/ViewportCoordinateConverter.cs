using Autodesk.AutoCAD.Geometry;

using Correct_test1.QuickRevision.Models;

using System;

namespace Correct_test1.QuickRevision.Viewports
{
    /// <summary>
    /// Viewport坐标转换器。
    ///
    /// 负责：
    ///
    /// Paper Space坐标
    ///        ↕
    /// Model Space WCS坐标
    ///
    /// 用户不需要实际进入Viewport。
    /// </summary>
    public class ViewportCoordinateConverter
    {
        /// <summary>
        /// Paper Space点击点
        /// 转换为Model Space WCS坐标。
        /// </summary>
        public bool TryPaperToModel(
            ViewportContext context,
            out Point3d modelPoint)
        {
            modelPoint =
                Point3d.Origin;

            if (!IsContextValid(context))
                return false;

            try
            {
                //--------------------------------
                // 第一步
                //
                // Paper Space相对于Viewport中心
                // 的偏移量。
                //--------------------------------

                double paperOffsetX =
                    context.PaperPoint.X -
                    context.CenterPoint.X;

                double paperOffsetY =
                    context.PaperPoint.Y -
                    context.CenterPoint.Y;


                //--------------------------------
                // 第二步
                //
                // 根据Viewport比例换算成
                // Model View / DCS中的距离。
                //
                // CustomScale：
                //
                // Paper单位 / Model单位
                //
                // 所以Paper → Model需要除。
                //--------------------------------

                double modelOffsetX =
                    paperOffsetX /
                    context.CustomScale;

                double modelOffsetY =
                    paperOffsetY /
                    context.CustomScale;


                //--------------------------------
                // 第三步
                //
                // 加上Viewport当前ViewCenter，
                // 得到Model空间DCS坐标。
                //--------------------------------

                Point3d dcsPoint =
                    new Point3d(
                        context.ViewCenter.X +
                        modelOffsetX,

                        context.ViewCenter.Y +
                        modelOffsetY,

                        0);


                //--------------------------------
                // 第四步
                //
                // Model DCS → WCS
                //
                // Autodesk官方视图转换使用：
                //
                // PlaneToWorld
                // Target
                // TwistAngle
                //
                // 构造DCS和WCS之间的矩阵。
                //--------------------------------

                Matrix3d dcsToWcs =
                    CreateDcsToWcsMatrix(
                        context);

                modelPoint =
                    dcsPoint.TransformBy(
                        dcsToWcs);


                //--------------------------------
                // 防止异常数值进入后续Resolver
                //--------------------------------

                if (!IsValidPoint(modelPoint))
                {
                    modelPoint =
                        Point3d.Origin;

                    return false;
                }


                //--------------------------------
                // 同时保存到Context
                //--------------------------------

                context.ModelPoint =
                    modelPoint;

                return true;
            }
            catch (System.Exception)
            {
                modelPoint =
                    Point3d.Origin;

                return false;
            }
        }


        /// <summary>
        /// 指定一个Paper Space点，
        /// 转换为Model Space。
        ///
        /// 这个重载不会要求调用方
        /// 先修改context.PaperPoint。
        /// </summary>
        public bool TryPaperToModel(
            ViewportContext context,
            Point3d paperPoint,
            out Point3d modelPoint)
        {
            modelPoint =
                Point3d.Origin;

            if (context == null)
                return false;

            context.PaperPoint =
                paperPoint;

            return TryPaperToModel(
                context,
                out modelPoint);
        }


        /// <summary>
        /// Model Space WCS坐标
        /// 转换回Paper Space坐标。
        ///
        /// 后面如果需要判断一个Model对象
        /// 在Viewport中实际显示到布局的什么位置，
        /// 会使用这个方法。
        /// </summary>
        public bool TryModelToPaper(
            ViewportContext context,
            Point3d modelPoint,
            out Point3d paperPoint)
        {
            paperPoint =
                Point3d.Origin;

            if (!IsContextValid(context))
                return false;

            if (!IsValidPoint(modelPoint))
                return false;

            try
            {
                //--------------------------------
                // DCS → WCS矩阵
                //--------------------------------

                Matrix3d dcsToWcs =
                    CreateDcsToWcsMatrix(
                        context);


                //--------------------------------
                // 反矩阵：
                //
                // WCS → DCS
                //--------------------------------

                Matrix3d wcsToDcs =
                    dcsToWcs.Inverse();


                //--------------------------------
                // Model WCS → Model DCS
                //--------------------------------

                Point3d dcsPoint =
                    modelPoint.TransformBy(
                        wcsToDcs);


                //--------------------------------
                // 相对于ViewCenter的偏移
                //--------------------------------

                double modelOffsetX =
                    dcsPoint.X -
                    context.ViewCenter.X;

                double modelOffsetY =
                    dcsPoint.Y -
                    context.ViewCenter.Y;


                //--------------------------------
                // Model距离 → Paper距离
                //--------------------------------

                double paperOffsetX =
                    modelOffsetX *
                    context.CustomScale;

                double paperOffsetY =
                    modelOffsetY *
                    context.CustomScale;


                //--------------------------------
                // 加回Viewport在布局中的中心点
                //--------------------------------

                paperPoint =
                    new Point3d(
                        context.CenterPoint.X +
                        paperOffsetX,

                        context.CenterPoint.Y +
                        paperOffsetY,

                        0);


                if (!IsValidPoint(paperPoint))
                {
                    paperPoint =
                        Point3d.Origin;

                    return false;
                }

                return true;
            }
            catch (System.Exception)
            {
                paperPoint =
                    Point3d.Origin;

                return false;
            }
        }


        /// <summary>
        /// 创建Model DCS → WCS转换矩阵。
        /// </summary>
        private static Matrix3d CreateDcsToWcsMatrix(
            ViewportContext context)
        {
            //--------------------------------
            // Autodesk官方视图坐标转换方式：
            //
            // 1. ViewDirection建立观察平面
            // 2. 移动到ViewTarget
            // 3. 处理View Twist
            //--------------------------------

            Matrix3d matrix =
                Matrix3d.PlaneToWorld(
                    context.ViewDirection);


            matrix =
                Matrix3d.Displacement(
                    context.ViewTarget -
                    Point3d.Origin)
                *
                matrix;


            matrix =
                Matrix3d.Rotation(
                    -context.TwistAngle,
                    context.ViewDirection,
                    context.ViewTarget)
                *
                matrix;


            return matrix;
        }


        /// <summary>
        /// 检查ViewportContext是否可以进行坐标转换。
        /// </summary>
        private static bool IsContextValid(
            ViewportContext context)
        {
            if (context == null)
                return false;

            if (!context.IsValid())
                return false;

            if (context.CustomScale <= 0)
                return false;

            if (double.IsNaN(
                    context.CustomScale))
            {
                return false;
            }

            if (double.IsInfinity(
                    context.CustomScale))
            {
                return false;
            }

            if (context.ViewDirection.Length <=
                1E-10)
            {
                return false;
            }

            return true;
        }


        /// <summary>
        /// 防止NaN或Infinity坐标进入AutoCAD API。
        /// </summary>
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
                Math.Abs(value) < 1E15;
        }
    }
}