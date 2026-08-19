using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Correct_test1.QuickRevision.Models;

namespace Correct_test1.QuickRevision.Resolvers
{
    /// <summary>
    /// QuickRevision目标解析总入口。
    /// 本类只负责调度：
    /// 1. 先尝试Paper Space
    /// 2. 再尝试Viewport / Model Space
    /// 不负责具体CAD对象解析。
    /// </summary>
    public class RevisionTargetResolver
    {
        private readonly PaperSpaceTargetResolver
            _paperSpaceResolver;

        private readonly ViewportTargetResolver
            _viewportResolver;


        public RevisionTargetResolver()
        {
            _paperSpaceResolver =
                new PaperSpaceTargetResolver();

            _viewportResolver =
                new ViewportTargetResolver();
        }


        /// <summary>
        /// 根据用户在Layout中的点击位置
        /// 获取统一RevisionTarget。
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


            // QuickRevision：
            // 只从Layout中启动。

            if (database.TileMode)
                return null;


            // 第一优先级：
            // Paper Space
            //
            // 例如：
            // BOM Table
            // Paper DBText
            // Paper MText

            RevisionTarget paperTarget =
                _paperSpaceResolver.Resolve(
                    database,
                    transaction,
                    paperPoint);


            if (paperTarget != null)
            {
                return paperTarget;
            }


            // 第二优先级：
            // Viewport中的Model Space
            //
            // 例如：
            // Dimension
            // MText
            // DBText

            RevisionTarget viewportTarget =
                _viewportResolver.Resolve(
                    database,
                    transaction,
                    paperPoint);


            if (viewportTarget != null)
            {
                return viewportTarget;
            }


            // 都没有识别到

            return null;
        }
    }
}