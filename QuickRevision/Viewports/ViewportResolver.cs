using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Correct_test1.QuickRevision.Models;

using System;

namespace Correct_test1.QuickRevision.Viewports
{
    /// <summary>
    /// 根据Paper Space中的点击位置，
    /// 找到用户点击的Viewport。
    /// 用户不需要双击进入Viewport。
    /// 只处理普通矩形Viewport，
    /// 暂不处理非矩形裁剪Viewport。
    /// </summary>
    public class ViewportResolver
    {
        /// <summary>
        /// 根据Paper Space中的点击点查找Viewport。
        /// 找到：
        /// 返回ViewportContext。
        /// 没找到：
        /// 返回null。
        /// </summary>
        public ViewportContext Resolve(
            Database database,
            Transaction transaction,
            Point3d paperPoint)
        {
            if (database == null)
                return null;

            if (transaction == null)
                return null;

            // TILEMODE=true表示当前是普通Model Space
            //
            // QuickRevision最终要求：
            // 用户从Layout/Paper Space启动

            if (database.TileMode)
                return null;

            // 获取当前Layout对应的Paper Space

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

            // 如果存在重叠Viewport，
            // 优先选择面积最小的那个。
            //
            // 这样小Viewport套在大Viewport里面时，
            // 点击小Viewport不会误选外层的大Viewport。

            ObjectId bestViewportId =
                ObjectId.Null;

            double bestArea =
                double.MaxValue;

            foreach (ObjectId objectId in paperSpace)
            {
                if (objectId.IsNull ||
                    !objectId.IsValid)
                {
                    continue;
                }

                Viewport viewport =
                    transaction.GetObject(
                        objectId,
                        OpenMode.ForRead)
                    as Viewport;

                if (viewport == null)
                    continue;

                // Number 1是Paper Space自身Viewport
                //
                // 我们只要浮动Viewport。

                if (viewport.Number <= 1)
                    continue;

                // 跳过关闭的Viewport

                if (!viewport.On)
                    continue;

                if (viewport.Width <= 0 ||
                    viewport.Height <= 0)
                {
                    continue;
                }

                // 判断点击点是否在Viewport矩形范围内

                if (!ContainsPoint(
                        viewport,
                        paperPoint))
                {
                    continue;
                }

                double area =
                    viewport.Width *
                    viewport.Height;

                if (area < bestArea)
                {
                    bestArea =
                        area;

                    bestViewportId =
                        viewport.ObjectId;
                }
            }

            // 没有找到Viewport

            if (bestViewportId.IsNull ||
                !bestViewportId.IsValid)
            {
                return null;
            }

            // 重新读取最终选中的Viewport

            Viewport selectedViewport =
                transaction.GetObject(
                    bestViewportId,
                    OpenMode.ForRead)
                as Viewport;

            if (selectedViewport == null)
                return null;

            // 创建ViewportContext

            ViewportContext context =
                new ViewportContext();

            context.ViewportId =
                selectedViewport.ObjectId;

            context.ViewportNumber =
                selectedViewport.Number;

            context.PaperPoint =
                paperPoint;

            context.CenterPoint =
                selectedViewport.CenterPoint;

            context.Width =
                selectedViewport.Width;

            context.Height =
                selectedViewport.Height;

            context.CustomScale =
                selectedViewport.CustomScale;

            context.ViewCenter =
                selectedViewport.ViewCenter;

            context.ViewTarget =
                selectedViewport.ViewTarget;

            context.ViewDirection =
                selectedViewport.ViewDirection;

            context.TwistAngle =
                selectedViewport.TwistAngle;

            return context;
        }


        /// <summary>
        /// 判断Paper Space中的一个点
        /// 是否位于Viewport矩形范围内。
        /// </summary>
        private static bool ContainsPoint(
            Viewport viewport,
            Point3d point)
        {
            if (viewport == null)
                return false;

            double halfWidth =
                viewport.Width / 2.0;

            double halfHeight =
                viewport.Height / 2.0;

            double minX =
                viewport.CenterPoint.X -
                halfWidth;

            double maxX =
                viewport.CenterPoint.X +
                halfWidth;

            double minY =
                viewport.CenterPoint.Y -
                halfHeight;

            double maxY =
                viewport.CenterPoint.Y +
                halfHeight;

            return
                point.X >= minX &&
                point.X <= maxX &&
                point.Y >= minY &&
                point.Y <= maxY;
        }


        /// <summary>
        /// 获取当前Layout的Paper Space
        /// BlockTableRecord ObjectId。
        /// </summary>
        private static ObjectId GetCurrentPaperSpaceId(
            Transaction transaction)
        {
            try
            {
                LayoutManager layoutManager =
                    LayoutManager.Current;

                if (layoutManager == null)
                    return ObjectId.Null;

                string currentLayoutName =
                    layoutManager.CurrentLayout;

                if (string.IsNullOrWhiteSpace(
                        currentLayoutName))
                {
                    return ObjectId.Null;
                }

                ObjectId layoutId =
                    layoutManager.GetLayoutId(
                        currentLayoutName);

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
    }
}
