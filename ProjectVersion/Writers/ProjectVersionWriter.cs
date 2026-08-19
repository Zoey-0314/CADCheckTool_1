using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Correct_test1.Models;
using Correct_test1.ProjectVersion.Configs;
using Correct_test1.ProjectVersion.Models;

using System;
using System.Text.RegularExpressions;

namespace Correct_test1.ProjectVersion.Writers
{
    /// <summary>
    /// 非标图纸项目号+版本号写入器。
    /// 只负责一个Layout。
    /// </summary>
    public class ProjectVersionWriter
    {
        private static readonly Regex
            ProjectNumberRegex =
                new Regex(
                    @"^N\d{4}[A-Z]{2}\d{3}(?:-[A-Z0-9]+)?$",
                    RegexOptions.IgnoreCase);


        public ProjectVersionLayoutResult Write(
            Database database,
            LayoutInfo layout,
            string value,
            bool isHorizontal,
            double offsetX = 0.0,
            double offsetY = 0.0)
        {
            ProjectVersionLayoutResult result =
                new ProjectVersionLayoutResult();


            result.LayoutName =
                layout == null
                    ? ""
                    : layout.LayoutName;


            result.IsHorizontal =
                isHorizontal;


            if (database == null ||
                layout == null)
            {
                result.Message =
                    "Database或Layout无效。";

                return result;
            }


            if (string.IsNullOrWhiteSpace(
                    value))
            {
                result.Message =
                    "输入内容为空。";

                return result;
            }


            value =
                value.Trim();


            ProjectVersionTemplate template =
                ProjectVersionConfig.Get(
                    isHorizontal,
                    offsetX,
                    offsetY);


            try
            {
                using (
                    Transaction transaction =
                        database
                            .TransactionManager
                            .StartTransaction())
                {
                    BlockTableRecord layoutSpace =
                        transaction.GetObject(
                            layout.BlockTableRecordId,
                            OpenMode.ForWrite)
                        as BlockTableRecord;


                    if (layoutSpace == null)
                    {
                        result.Message =
                            "无法读取Layout空间。";

                        return result;
                    }


                    // 首先寻找已有项目号MText。
                    //
                    // 支持：
                    // 1. Layout直属MText
                    // 2. BlockReference内的MText

                    ObjectId existingId =
                        FindExistingProjectText(
                            transaction,
                            layoutSpace,
                            template);


                    if (!existingId.IsNull &&
                        existingId.IsValid)
                    {
                        try
                        {
                            MText existing =
                                transaction.GetObject(
                                    existingId,
                                    OpenMode.ForWrite)
                                as MText;


                            if (existing != null)
                            {
                                // 已存在：
                                //
                                // 只改文字内容。
                                //
                                // 字体、字高、颜色、
                                // 位置、宽度等全部保留。

                                existing.Contents =
                                    value;


                                transaction.Commit();


                                result.Success =
                                    true;


                                result.Created =
                                    false;


                                result.Message =
                                    "已修改现有项目号文字。";


                                return result;
                            }
                        }
                        catch
                        {
                            // 某些外部块 / 不可写块
                            // 无法修改时继续走创建逻辑。
                        }
                    }


                    // 没有项目号：
                    //
                    // 在当前Layout Paper Space
                    // 创建新的MText。
                    //
                    // 不直接修改共享块定义，
                    // 防止一个Block定义被多个实例共用时
                    // 意外影响其他地方。

                    ObjectId styleId =
                        EnsureTextStyleId(
                            database,
                            transaction,
                            template.TextStyleName);


                    if (styleId.IsNull ||
                        !styleId.IsValid)
                    {
                        result.Message =
                            "无法获取或创建文字样式："
                            + template.TextStyleName;

                        return result;
                    }


                    MText text =
                        new MText();


                    text.SetDatabaseDefaults(
                        database);


                    // 内容

                    text.Contents =
                        value;


                    // 样式：CONN

                    text.TextStyleId =
                        styleId;


                    // 图层：0

                    text.Layer =
                        "0";


                    // 红色 ACI 1

                    text.Color =
                        Color.FromColorIndex(
                            ColorMethod.ByAci,
                            1);


                    // 线宽：0.25 mm

                    text.LineWeight =
                        LineWeight.LineWeight025;


                    // 位置

                    text.Location =
                        new Point3d(
                            template.X,
                            template.Y,
                            0.0);


                    // 对正：左上

                    text.Attachment =
                        AttachmentPoint.TopLeft;


                    // 字高

                    text.TextHeight =
                        template.TextHeight;


                    // 定义宽度

                    text.Width =
                        template.Width;


                    // 旋转：0

                    text.Rotation =
                        0.0;


                    // 行距：
                    //
                    // 横版：
                    // 5 × 1.66666 = 8.3333
                    //
                    // 竖版：
                    // 4 × 1.66666 = 6.6667
                    //
                    // 对应截图的：
                    // 行距样式 = 至少

                    text.LineSpacingStyle =
                        LineSpacingStyle.AtLeast;


                    text.LineSpacingFactor =
                        1.0;


                    // 加入当前Layout

                    layoutSpace.AppendEntity(
                        text);


                    transaction
                        .AddNewlyCreatedDBObject(
                            text,
                            true);


                    transaction.Commit();


                    result.Success =
                        true;


                    result.Created =
                        true;


                    result.Message =
                        "原项目号不存在，已创建新的MText。";


                    return result;
                }
            }
            catch (System.Exception ex)
            {
                result.Success =
                    false;


                result.Message =
                    ex.Message;


                return result;
            }
        }


        /// <summary>
        /// 找现有项目号MText。
        /// 按实际显示坐标与目标坐标距离判断。
        /// </summary>
        private ObjectId FindExistingProjectText(
            Transaction transaction,
            BlockTableRecord layoutSpace,
            ProjectVersionTemplate template)
        {
            Candidate best =
                new Candidate();


            best.Distance =
                double.MaxValue;


            foreach (
                ObjectId id
                in layoutSpace)
            {
                Entity entity =
                    transaction.GetObject(
                        id,
                        OpenMode.ForRead)
                    as Entity;


                FindCandidate(
                    transaction,
                    entity,
                    Matrix3d.Identity,
                    template,
                    best,
                    0);
            }


            return best.Id;
        }


        /// <summary>
        /// 递归进入BlockReference。
        /// </summary>
        private void FindCandidate(
            Transaction transaction,
            Entity entity,
            Matrix3d transform,
            ProjectVersionTemplate template,
            Candidate best,
            int depth)
        {
            if (entity == null)
                return;


            // 防止异常深层块递归

            if (depth > 8)
                return;


            MText mtext =
                entity as MText;


            if (mtext != null)
            {
                string content =
                    mtext.Text == null
                        ? ""
                        : mtext.Text.Trim();


                if (!ProjectNumberRegex.IsMatch(
                        content))
                {
                    return;
                }


                Point3d actualPosition =
                    mtext.Location
                        .TransformBy(
                            transform);


                double dx =
                    actualPosition.X -
                    template.X;


                double dy =
                    actualPosition.Y -
                    template.Y;


                double distance =
                    Math.Sqrt(
                        dx * dx +
                        dy * dy);


                if (distance >
                    template.SearchTolerance)
                {
                    return;
                }


                if (distance <
                    best.Distance)
                {
                    best.Distance =
                        distance;


                    best.Id =
                        mtext.ObjectId;
                }


                return;
            }


            BlockReference block =
                entity as BlockReference;


            if (block == null)
                return;


            BlockTableRecord blockDefinition;


            try
            {
                blockDefinition =
                    transaction.GetObject(
                        block.BlockTableRecord,
                        OpenMode.ForRead)
                    as BlockTableRecord;
            }
            catch
            {
                return;
            }


            if (blockDefinition == null)
                return;


            // 外部参照不修改

            if (blockDefinition
                .IsFromExternalReference)
            {
                return;
            }


            Matrix3d blockTransform =
                transform *
                block.BlockTransform;


            foreach (
                ObjectId childId
                in blockDefinition)
            {
                Entity child =
                    transaction.GetObject(
                        childId,
                        OpenMode.ForRead)
                    as Entity;


                FindCandidate(
                    transaction,
                    child,
                    blockTransform,
                    template,
                    best,
                    depth + 1);
            }
        }


        /// <summary>
        /// 确保文字样式存在。
        /// 已存在：直接使用。
        /// 不存在：自动创建标准CONN样式。
        /// </summary>
        private ObjectId EnsureTextStyleId(
            Database database,
            Transaction transaction,
            string styleName)
        {
            if (database == null ||
                transaction == null ||
                string.IsNullOrWhiteSpace(styleName))
            {
                return ObjectId.Null;
            }


            TextStyleTable table =
                transaction.GetObject(
                    database.TextStyleTableId,
                    OpenMode.ForRead)
                as TextStyleTable;


            if (table == null)
                return ObjectId.Null;


            // 已存在，直接使用

            if (table.Has(styleName))
            {
                return table[styleName];
            }


            // 不存在，自动创建

            table.UpgradeOpen();


            TextStyleTableRecord newStyle =
                new TextStyleTableRecord();


            newStyle.Name =
                styleName;


            // Arial + 粗体

            Autodesk.AutoCAD
                .GraphicsInterface
                .FontDescriptor font =
                    new Autodesk.AutoCAD
                        .GraphicsInterface
                        .FontDescriptor(
                            "Arial",
                            true,       // 粗体
                            false,      // 非斜体
                            0,
                            0);


            newStyle.Font =
                font;


            // CONN标准属性

            newStyle.TextSize =
                0.0;

            newStyle.XScale =
                0.8;

            newStyle.ObliquingAngle =
                0.0;

            newStyle.IsVertical =
                false;

            newStyle.FlagBits =
                0;


            // 加入文字样式表

            ObjectId styleId =
                table.Add(
                    newStyle);


            transaction
                .AddNewlyCreatedDBObject(
                    newStyle,
                    true);


            return styleId;
        }


        private class Candidate
        {
            public ObjectId Id
            {
                get;
                set;
            }


            public double Distance
            {
                get;
                set;
            }
        }
    }
}