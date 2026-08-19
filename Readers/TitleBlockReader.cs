using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Correct_test1.Models;
using System.Collections.Generic;

namespace Correct_test1.Readers
{
    /// <summary>
    /// 标题栏文字读取器
    /// 只负责：CAD实体 -> TitleText，不负责解析字段
    /// </summary>
    public class TitleBlockReader
    {
        public List<TitleText> Read(
    Database db,
    List<LayoutInfo> layouts)
        {
            List<TitleText> result =
                new List<TitleText>();


            if (db == null ||
                layouts == null)
            {
                return result;
            }


            using (
                Transaction tr =
                    db.TransactionManager
                        .StartTransaction())
            {
                foreach (
                    LayoutInfo layout
                    in layouts)
                {
                    if (layout == null ||
                        layout.BlockTableRecordId.IsNull ||
                        !layout.BlockTableRecordId.IsValid)
                    {
                        continue;
                    }


                    BlockTableRecord layoutSpace =
                        tr.GetObject(
                            layout.BlockTableRecordId,
                            OpenMode.ForRead)
                        as BlockTableRecord;


                    if (layoutSpace == null)
                    {
                        continue;
                    }


                    // 当前递归链中的块定义。
                    //
                    // 只用于防止异常循环引用，
                    // 不是全局Visited。
                    //
                    // 同一个块有多个实例时，
                    // 每个实例仍然都要读取。

                    HashSet<ObjectId>
                        activeBlockDefinitions =
                            new HashSet<ObjectId>();


                    ReadSpaceTexts(
                        tr,
                        layoutSpace,
                        layout.LayoutName,
                        Matrix3d.Identity,
                        result,
                        activeBlockDefinitions,
                        0);
                }


                tr.Commit();
            }


            return result;
        }



        private void ReadSpaceTexts(
    Transaction tr,
    BlockTableRecord space,
    string layoutName,
    Matrix3d transform,
    List<TitleText> result,
    HashSet<ObjectId> activeBlockDefinitions,
    int depth)
        {
            if (tr == null ||
                space == null ||
                result == null)
            {
                return;
            }


            // 防止异常图纸无限递归

            if (depth > 20)
            {
                return;
            }


            foreach (
                ObjectId id
                in space)
            {
                Entity ent;


                try
                {
                    ent =
                        tr.GetObject(
                            id,
                            OpenMode.ForRead)
                        as Entity;
                }
                catch
                {
                    continue;
                }


                if (ent == null)
                {
                    continue;
                }


                // AttributeDefinition不能作为真实标题栏文字读取。
                //
                // 真正显示的属性文字由
                // AttributeReference负责。
                //
                // 否则会出现：
                //
                // 属性默认值
                // +
                // 实际属性值
                //
                // 重复读取。

                if (ent is AttributeDefinition)
                {
                    continue;
                }


                // 1. 普通DBText

                DBText text =
                    ent as DBText;


                if (text != null)
                {
                    Point3d position =
                        text.Position
                            .TransformBy(
                                transform);


                    result.Add(
                        new TitleText
                        {
                            Text =
                                Clean(
                                    text.TextString),

                            X =
                                position.X,

                            Y =
                                position.Y,

                            Height =
                                TransformTextHeight(
                                    text.Height,
                                    transform),

                            LayoutName =
                                layoutName,

                            // 布局空间直接文字可以安全修改。
                            //
                            // 块定义内部固定文字不能直接自动修改，
                            // 因为修改块定义可能影响多个Block实例。
                            //
                            // 所以块内固定文字只参与：
                            //
                            // 读取
                            // 判断
                            // 字高检查
                            //
                            // 不参与自动页码写回。

                            ObjectId =
                                depth == 0
                                    ? text.ObjectId
                                    : ObjectId.Null
                        });


                    continue;
                }


                // 2. MText

                MText mtext =
                    ent as MText;


                if (mtext != null)
                {
                    Point3d position =
                        mtext.Location
                            .TransformBy(
                                transform);


                    result.Add(
                        new TitleText
                        {
                            Text =
                                Clean(
                                    mtext.Text),

                            X =
                                position.X,

                            Y =
                                position.Y,

                            Height =
                                TransformTextHeight(
                                    mtext.TextHeight,
                                    transform),

                            LayoutName =
                                layoutName,

                            ObjectId =
                                depth == 0
                                    ? mtext.ObjectId
                                    : ObjectId.Null
                        });


                    continue;
                }


                // 3. BlockReference

                BlockReference block =
                    ent as BlockReference;


                if (block == null)
                {
                    continue;
                }


                // 先读这个BlockReference自己的AttributeReference

                ReadBlockAttributes(
                    tr,
                    block,
                    layoutName,
                    transform,
                    result,
                    depth);


                // 再进入块定义读取固定DBText/MText

                BlockTableRecord blockDefinition;


                try
                {
                    blockDefinition =
                        tr.GetObject(
                            block.BlockTableRecord,
                            OpenMode.ForRead)
                        as BlockTableRecord;
                }
                catch
                {
                    continue;
                }


                if (blockDefinition == null)
                {
                    continue;
                }


                // 外部参照不进入

                if (blockDefinition
                        .IsFromExternalReference)
                {
                    continue;
                }


                ObjectId definitionId =
                    block.BlockTableRecord;


                // 防止块定义循环引用

                if (activeBlockDefinitions
                        .Contains(
                            definitionId))
                {
                    continue;
                }


                // 累计块变换
                //
                // 和当前CadTableReader保持相同顺序：
                //
                // parentTransform * block.BlockTransform

                Matrix3d childTransform =
                    transform
                    * block.BlockTransform;


                activeBlockDefinitions.Add(
                    definitionId);


                try
                {
                    ReadSpaceTexts(
                        tr,
                        blockDefinition,
                        layoutName,
                        childTransform,
                        result,
                        activeBlockDefinitions,
                        depth + 1);
                }
                finally
                {
                    activeBlockDefinitions.Remove(
                        definitionId);
                }
            }
        }

        private void ReadBlockAttributes(
    Transaction tr,
    BlockReference block,
    string layoutName,
    Matrix3d parentTransform,
    List<TitleText> result,
    int depth)
        {
            if (tr == null ||
                block == null ||
                result == null)
            {
                return;
            }


            try
            {
                foreach (
                    ObjectId attributeId
                    in block.AttributeCollection)
                {
                    AttributeReference att =
                        tr.GetObject(
                            attributeId,
                            OpenMode.ForRead)
                        as AttributeReference;


                    if (att == null)
                    {
                        continue;
                    }


                    // AttributeReference的位置已经包含
                    // 当前BlockReference自己的变换。
                    //
                    // 如果这个BlockReference本身又位于外层块定义中，
                    // 这里只需要再应用外层parentTransform。

                    Point3d position =
                        att.Position
                            .TransformBy(
                                parentTransform);


                    result.Add(
                        new TitleText
                        {
                            Text =
                                Clean(
                                    att.TextString),

                            X =
                                position.X,

                            Y =
                                position.Y,

                            Height =
                                TransformTextHeight(
                                    att.Height,
                                    parentTransform),

                            LayoutName =
                                layoutName,

                            // 最外层BlockReference的属性
                            // 是当前Layout真正的实例属性，
                            // 可以用于页码自动修正。
                            //
                            // 嵌套块中的属性属于块定义内部实例，
                            // 为避免修改共享块定义，
                            // 不开放自动修改。

                            ObjectId =
                                depth == 0
                                    ? att.ObjectId
                                    : ObjectId.Null
                        });
                }
            }
            catch
            {
                // 属性损坏不能影响其他标题栏内容读取
            }
        }

        private static double TransformTextHeight(
    double height,
    Matrix3d transform)
        {
            if (height <= 0)
            {
                return height;
            }


            try
            {
                Point3d start =
                    Point3d.Origin
                        .TransformBy(
                            transform);


                Point3d end =
                    new Point3d(
                        0,
                        height,
                        0)
                        .TransformBy(
                            transform);


                double transformedHeight =
                    start.DistanceTo(
                        end);


                if (transformedHeight >
                    0.000001)
                {
                    return transformedHeight;
                }
            }
            catch
            {
            }


            return height;
        }

        private string Clean(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            return text
                .Replace("\\P", "\n")
                .Trim();
        }
    }
}
