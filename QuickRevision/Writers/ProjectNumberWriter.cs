using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Correct_test1.QuickRevision.Models;

namespace Correct_test1.QuickRevision.Writers
{
    /// <summary>
    /// BOM中的NS内容被快速划改后，
    /// 在该BOM行最右侧外部生成项目号。
    ///
    /// 本类只负责写入。
    /// 不负责判断NS。
    /// 不负责从文件名读取项目号。
    /// </summary>
    public class ProjectNumberWriter
    {
        /// <summary>
        /// 创建项目号文字。
        ///
        /// 成功：
        /// 返回新MText的ObjectId。
        ///
        /// 失败：
        /// 返回ObjectId.Null。
        /// </summary>
        public ObjectId Write(
            Database database,
            Transaction transaction,
            RevisionTarget target,
            string projectNumber)
        {
            if (database == null ||
                transaction == null ||
                target == null)
            {
                return ObjectId.Null;
            }


            //--------------------------------
            // 必须来自TableCell
            //--------------------------------

            if (!target.IsTableCell ||
                target.TableContext == null)
            {
                return ObjectId.Null;
            }


            if (!target.TableContext.IsValid())
            {
                return ObjectId.Null;
            }


            //--------------------------------
            // 项目号不能为空
            //--------------------------------

            if (string.IsNullOrWhiteSpace(
                    projectNumber))
            {
                return ObjectId.Null;
            }


            projectNumber =
                projectNumber.Trim();


            //--------------------------------
            // 获取QuickRevision专用红色图层
            //--------------------------------

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


            //--------------------------------
            // 注册XData
            //--------------------------------

            RevisionEntityHelper
                .EnsureRegApp(
                    database,
                    transaction);


            //--------------------------------
            // 项目号应该写到与BOM相同的
            // Paper Space中。
            //--------------------------------

            BlockTableRecord targetSpace =
                transaction.GetObject(
                    target.TargetSpaceId,
                    OpenMode.ForWrite)
                as BlockTableRecord;


            if (targetSpace == null)
                return ObjectId.Null;


            //--------------------------------
            // 字高
            //
            // 直接继承原BOM文字高度。
            //--------------------------------

            double textHeight =
                GetTextHeight(
                    target);


            //--------------------------------
            // BOM右侧留一点距离。
            //
            // 使用字高作为比例，
            // 不写死图纸单位。
            //--------------------------------

            double gap =
                textHeight * 0.8;


            //--------------------------------
            // 放置位置：
            //
            // X = 整个BOM最右侧 + gap
            // Y = 当前BOM行中心
            //--------------------------------

            double x =
                target.TableContext
                    .TableRightX +
                gap;


            double y =
                target.TableContext
                    .RowCenterY;


            Point3d location =
                new Point3d(
                    x,
                    y,
                    0.0);


            if (!IsValidPoint(
                    location))
            {
                return ObjectId.Null;
            }


            //--------------------------------
            // 创建MText
            //--------------------------------

            MText text =
                new MText();


            bool appended =
                false;


            try
            {
                text.SetDatabaseDefaults(
                    database);


                //--------------------------------
                // 项目号内容
                //--------------------------------

                text.Contents =
                    projectNumber;


                //--------------------------------
                // 与原BOM文字一样高
                //--------------------------------

                text.TextHeight =
                    textHeight;


                //--------------------------------
                // location代表文字左侧中点。
                //
                // 因此文字会从BOM右边向右展开。
                //--------------------------------

                text.Attachment =
                    AttachmentPoint.MiddleLeft;


                text.Location =
                    location;


                //--------------------------------
                // 第一版水平
                //--------------------------------

                text.Rotation =
                    0.0;


                //--------------------------------
                // 尽量继承原BOM文字样式
                //--------------------------------

                ApplyTextStyle(
                    text,
                    target);


                //--------------------------------
                // QuickRevision统一：
                //
                // CADCHECK_REVISION
                // +
                // 红色
                //--------------------------------

                RevisionEntityHelper
                    .ApplyRevisionAppearance(
                        text,
                        layerId);


                //--------------------------------
                // XData
                //
                // 后续可以专门识别：
                // ProjectNumber
                //--------------------------------

                RevisionEntityHelper
                    .ApplyXData(
                        text,
                        "ProjectNumber");


                //--------------------------------
                // 加入Paper Space
                //--------------------------------

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


        /// <summary>
        /// 使用原BOM文字高度。
        /// </summary>
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


            //--------------------------------
            // 理论上TableResolver已经提供字高。
            // 这里只是最后Fallback。
            //--------------------------------

            return 2.5;
        }


        /// <summary>
        /// 尽量继承原BOM字体样式。
        /// </summary>
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
                //--------------------------------
                // 获取不到原样式时，
                // 保留数据库默认文字样式。
                //--------------------------------
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