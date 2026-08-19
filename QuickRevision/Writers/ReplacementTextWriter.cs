using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Correct_test1.QuickRevision.Models;

namespace Correct_test1.QuickRevision.Writers
{
    /// <summary>
    /// 创建快速划改后的新文字。
    /// 特点：
    /// 1. 原对象不修改
    /// 2. 新文字位于原文字右侧
    /// 3. 尽量使用原文字实际高度
    /// 4. 尽量继承原文字样式
    /// 5. 统一红色
    /// 6. 只创建水平文字
    /// </summary>
    public class ReplacementTextWriter
    {
        public ObjectId Write(
            Database database,
            Transaction transaction,
            RevisionTarget target,
            string replacementText)
        {
            if (database == null ||
                transaction == null ||
                target == null)
            {
                return ObjectId.Null;
            }


            if (!target.IsValid())
                return ObjectId.Null;


            if (string.IsNullOrWhiteSpace(
                    replacementText))
            {
                return ObjectId.Null;
            }


            replacementText =
                replacementText.Trim();


            // 红色QuickRevision图层

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


            // 直接使用Resolver取得的实际字高。

            double textHeight =
                GetTextHeight(
                    target);


            // 新旧内容之间留适当间距。

            double gap =
                textHeight *
                0.60;


            // 新文字左侧中心点

            Point3d location =
                new Point3d(
                    target.RightX +
                    gap,

                    target.CenterY,

                    0.0);


            if (!IsValidPoint(
                    location))
            {
                return ObjectId.Null;
            }


            MText text =
                new MText();


            bool appended =
                false;


            try
            {
                text.SetDatabaseDefaults(
                    database);


                // 内容

                text.Contents =
                    replacementText;


                // 原始真实字高

                text.TextHeight =
                    textHeight;


                // Location对应文字左侧中点

                text.Attachment =
                    AttachmentPoint.MiddleLeft;


                text.Location =
                    location;


                // 水平

                text.Rotation =
                    0.0;


                // 尽量继承原文字样式

                ApplyTextStyle(
                    text,
                    target);


                // 红色 + 专用图层

                RevisionEntityHelper
                    .ApplyRevisionAppearance(
                        text,
                        layerId);


                // XData

                RevisionEntityHelper
                    .ApplyXData(
                        text,
                        "ReplacementText");


                ObjectId textId =
                    targetSpace.AppendEntity(
                        text);


                appended =
                    true;


                transaction
                    .AddNewlyCreatedDBObject(
                        text,
                        true);


                return textId;
            }
            catch (System.Exception)
            {
                if (!appended)
                {
                    text.Dispose();
                }


                return ObjectId.Null;
            }
        }


        private static double GetTextHeight(
            RevisionTarget target)
        {
            if (target != null &&
                IsValidNumber(
                    target.TextHeight) &&
                target.TextHeight > 0)
            {
                return
                    target.TextHeight;
            }


            return 2.5;
        }


        private static void ApplyTextStyle(
            MText text,
            RevisionTarget target)
        {
            if (text == null ||
                target == null)
            {
                return;
            }


            ObjectId styleId =
                target.TextStyleId;


            if (styleId.IsNull ||
                !styleId.IsValid)
            {
                return;
            }


            try
            {
                text.TextStyleId =
                    styleId;
            }
            catch (System.Exception)
            {
                // 样式失败时使用数据库默认样式，
                // 不因此取消整个快速划改。
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
