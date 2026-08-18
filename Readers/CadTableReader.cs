using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Correct_test1.Core;
using Correct_test1.Models;

using System;
using System.Collections.Generic;


namespace Correct_test1.Readers
{
    /// <summary>
    /// CAD表格读取器。
    ///
    /// 新版核心原则：
    ///
    /// 不再：
    /// 从BlockTableRecord反推Layout。
    ///
    /// 而是：
    /// 从每一个Layout出发递归读取。
    ///
    /// 因此即使Table位于：
    ///
    /// Layout6
    /// └─ Block
    ///    └─ Block
    ///       └─ Table
    ///
    /// 也始终知道：
    ///
    /// SourceLayoutName = Layout6
    ///
    /// 同时累计BlockTransform，
    /// 把Table单元格坐标转换到Layout坐标系。
    /// </summary>
    public class CadTableReader
    {
        //==================================================
        // 主入口
        //==================================================

        public List<CadTableData> Read(
            Database db)
        {
            List<CadTableData> tables =
                new List<CadTableData>();


            if (db == null)
            {
                return tables;
            }


            try
            {
                //--------------------------------
                // 先读取真正的Layout列表
                //--------------------------------

                LayoutReader layoutReader =
                    new LayoutReader();


                List<LayoutInfo> layouts =
                    layoutReader.ReadLayouts(
                        db);


                if (layouts == null ||
                    layouts.Count == 0)
                {
                    return tables;
                }

                DrawingFrameReader frameReader =
    new DrawingFrameReader();



                //==================================================
                // 每个Layout单独处理
                //==================================================

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


                    //==================================================
                    //这里直接读取当前Layout中的所有Table，
                    // 后续再由BomTableRecognizer过滤。
                    //==================================================

                    Extents3d? frame =
                        null;

                    try
                    {
                        frame =
                            frameReader.Read(
                                db,
                                layout.BlockTableRecordId);
                    }
                    catch
                    {
                        frame = null;
                    }

                    using (
                        Transaction tr =
                            db.TransactionManager
                                .StartTransaction())
                    {
                        BlockTableRecord layoutSpace =
                            tr.GetObject(
                                layout.BlockTableRecordId,
                                OpenMode.ForRead)
                            as BlockTableRecord;


                        if (layoutSpace == null)
                        {
                            continue;
                        }


                        //--------------------------------
                        // 防止异常循环块引用
                        //--------------------------------

                        HashSet<ObjectId>
                            activeBlockDefinitions =
                                new HashSet<ObjectId>();


                        ReadSpace(
                            db,
                            tr,
                            layoutSpace,
                            layout.LayoutName,
                            Matrix3d.Identity,
                            frame,
                            tables,
                            activeBlockDefinitions,
                            0);


                        tr.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "CadTableReader.Read");
            }


            return tables;
        }


        //==================================================
        // 递归读取一个Layout / Block中的实体
        //==================================================

        private void ReadSpace(
            Database db,
            Transaction tr,
            BlockTableRecord space,
            string sourceLayoutName,
            Matrix3d transform,
            Extents3d? frame,
            List<CadTableData> tables,
            HashSet<ObjectId> activeBlockDefinitions,
            int depth)
        {
            if (db == null ||
                tr == null ||
                space == null ||
                tables == null)
            {
                return;
            }


            //--------------------------------
            // 防止极端异常图纸无限递归
            //--------------------------------

            if (depth > 20)
            {
                return;
            }


            foreach (
                ObjectId entityId
                in space)
            {
                Entity entity =
                    tr.GetObject(
                        entityId,
                        OpenMode.ForRead)
                    as Entity;


                if (entity == null)
                {
                    continue;
                }


                //==================================================
                // 1. AutoCAD Table
                //==================================================

                Table table =
                    entity as Table;


                if (table != null)
                {
                    //--------------------------------
                    // 如果找到了图框：
                    // 只接受图框内的Table。
                    //
                    // 如果当前Layout没有成功识别图框：
                    // 不过滤。
                    //--------------------------------

                    if (frame != null)
                    {
                        if (!IsInsideFrame(
                                table,
                                frame.Value,
                                transform))
                        {
                            continue;
                        }
                    }


                    CadTableData data =
                        ReadTable(
                            table,
                            transform);


                    data.SourceLayoutName =
                        sourceLayoutName ?? "";


                    tables.Add(
                        data);


                    continue;
                }


                //==================================================
                // 2. BlockReference
                //
                // Table可能在嵌套块里面。
                //==================================================

                BlockReference block =
                    entity as BlockReference;


                if (block == null)
                {
                    continue;
                }


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


                //--------------------------------
                // 外部参照不递归
                //--------------------------------

                if (blockDefinition
                    .IsFromExternalReference)
                {
                    continue;
                }


                //--------------------------------
                // 防止块定义循环引用
                //--------------------------------

                ObjectId definitionId =
                    block.BlockTableRecord;


                if (activeBlockDefinitions
                    .Contains(
                        definitionId))
                {
                    continue;
                }


                //==================================================
                // 累计块变换
                //
                // Layout
                // ↓
                // Block1
                // ↓
                // Block2
                //
                // 最终Table坐标会转换回真正Layout坐标。
                //==================================================

                Matrix3d childTransform =
                    transform
                    * block.BlockTransform;


                activeBlockDefinitions.Add(
                    definitionId);


                try
                {
                    ReadSpace(
                        db,
                        tr,
                        blockDefinition,

                        //==============================
                        // LayoutName始终不改变
                        //==============================

                        sourceLayoutName,

                        childTransform,
                        frame,
                        tables,
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


        //==================================================
        // 判断Table是否位于当前Layout图框内
        //==================================================

        private bool IsInsideFrame(
            Table table,
            Extents3d frame,
            Matrix3d transform)
        {
            if (table == null)
            {
                return false;
            }


            Extents3d originalExtents;


            try
            {
                originalExtents =
                    table.GeometricExtents;
            }
            catch
            {
                return false;
            }


            Extents3d transformedExtents =
                TransformExtents(
                    originalExtents,
                    transform);


            double centerX =
                (
                    transformedExtents.MinPoint.X
                    +
                    transformedExtents.MaxPoint.X
                )
                / 2.0;


            double centerY =
                (
                    transformedExtents.MinPoint.Y
                    +
                    transformedExtents.MaxPoint.Y
                )
                / 2.0;


            return
                centerX >= frame.MinPoint.X &&
                centerX <= frame.MaxPoint.X &&
                centerY >= frame.MinPoint.Y &&
                centerY <= frame.MaxPoint.Y;
        }


        //==================================================
        // 转换Extents
        //
        // 不能只转换MinPoint / MaxPoint，
        // 因为Block可能旋转。
        //
        // 所以转换8个角点后重新计算包围盒。
        //==================================================

        private Extents3d TransformExtents(
            Extents3d extents,
            Matrix3d transform)
        {
            Point3d min =
                extents.MinPoint;


            Point3d max =
                extents.MaxPoint;


            Point3d[] points =
            {
                new Point3d(
                    min.X,
                    min.Y,
                    min.Z),

                new Point3d(
                    min.X,
                    min.Y,
                    max.Z),

                new Point3d(
                    min.X,
                    max.Y,
                    min.Z),

                new Point3d(
                    min.X,
                    max.Y,
                    max.Z),

                new Point3d(
                    max.X,
                    min.Y,
                    min.Z),

                new Point3d(
                    max.X,
                    min.Y,
                    max.Z),

                new Point3d(
                    max.X,
                    max.Y,
                    min.Z),

                new Point3d(
                    max.X,
                    max.Y,
                    max.Z)
            };


            double minX =
                double.MaxValue;

            double minY =
                double.MaxValue;

            double minZ =
                double.MaxValue;

            double maxX =
                double.MinValue;

            double maxY =
                double.MinValue;

            double maxZ =
                double.MinValue;


            foreach (
                Point3d point
                in points)
            {
                Point3d transformed =
                    point.TransformBy(
                        transform);


                minX =
                    Math.Min(
                        minX,
                        transformed.X);

                minY =
                    Math.Min(
                        minY,
                        transformed.Y);

                minZ =
                    Math.Min(
                        minZ,
                        transformed.Z);


                maxX =
                    Math.Max(
                        maxX,
                        transformed.X);

                maxY =
                    Math.Max(
                        maxY,
                        transformed.Y);

                maxZ =
                    Math.Max(
                        maxZ,
                        transformed.Z);
            }


            return
                new Extents3d(
                    new Point3d(
                        minX,
                        minY,
                        minZ),

                    new Point3d(
                        maxX,
                        maxY,
                        maxZ));
        }


        //==================================================
        // 读取单个Table
        //
        // transform用于把：
        //
        // Block内部坐标
        //
        // 转换为：
        //
        // Layout真实坐标
        //==================================================

        private CadTableData ReadTable(
            Table table,
            Matrix3d transform)
        {
            CadTableData data =
                new CadTableData();


            if (table == null)
            {
                return data;
            }


            data.LayerName =
                table.Layer;


            data.Rows =
                table.Rows.Count;


            data.Columns =
                table.Columns.Count;

            //==================================================
            // 保存Table实际边界
            //
            // 如果Table位于Block内部，
            // 使用同一个transform转换为Layout实际坐标。
            //==================================================

            try
            {
                Extents3d tableExtents =
                    TransformExtents(
                        table.GeometricExtents,
                        transform);


                data.TableMinX =
                    tableExtents.MinPoint.X;

                data.TableMaxX =
                    tableExtents.MaxPoint.X;

                data.TableMinY =
                    tableExtents.MinPoint.Y;

                data.TableMaxY =
                    tableExtents.MaxPoint.Y;
            }
            catch (System.Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "CadTableReader.ReadTableExtents");
            }


            //==================================================
            // 所有单元格
            //==================================================

            for (
                int r = 0;
                r < table.Rows.Count;
                r++)
            {
                List<string> row =
                    new List<string>();


                for (
                    int c = 0;
                    c < table.Columns.Count;
                    c++)
                {
                    //==================================================
                    // 文字
                    //==================================================

                    string value =
                        "";


                    try
                    {
                        value =
                            table.Cells[r, c]
                                .TextString
                                .Trim();
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error(
                            ex,
                            "CadTableReader.ReadCell");
                    }


                    row.Add(
                        value);


                    //==================================================
                    // 单元格中心坐标
                    //==================================================

                    try
                    {
                        Point3dCollection
                            cellPoints =
                                new Point3dCollection();


                        table.GetCellExtents(
                            r,
                            c,
                            false,
                            cellPoints);


                        if (cellPoints.Count > 0)
                        {
                            double minX =
                                double.MaxValue;

                            double minY =
                                double.MaxValue;

                            double minZ =
                                double.MaxValue;

                            double maxX =
                                double.MinValue;

                            double maxY =
                                double.MinValue;

                            double maxZ =
                                double.MinValue;


                            foreach (
                                Point3d originalPoint
                                in cellPoints)
                            {
                                //--------------------------------
                                // 最重要：
                                // 应用完整BlockTransform
                                //--------------------------------

                                Point3d point =
                                    originalPoint
                                        .TransformBy(
                                            transform);


                                minX =
                                    Math.Min(
                                        minX,
                                        point.X);

                                minY =
                                    Math.Min(
                                        minY,
                                        point.Y);

                                minZ =
                                    Math.Min(
                                        minZ,
                                        point.Z);


                                maxX =
                                    Math.Max(
                                        maxX,
                                        point.X);

                                maxY =
                                    Math.Max(
                                        maxY,
                                        point.Y);

                                maxZ =
                                    Math.Max(
                                        maxZ,
                                        point.Z);
                            }


                            data.CellPositions[
                                r + ":" + c] =
                                    new Point3d(
                                        (
                                            minX
                                            +
                                            maxX
                                        )
                                        / 2.0,

                                        (
                                            minY
                                            +
                                            maxY
                                        )
                                        / 2.0,

                                        (
                                            minZ
                                            +
                                            maxZ
                                        )
                                        / 2.0);
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error(
                            ex,
                            "CadTableReader.ReadCellPosition");
                    }
                }


                data.Cells.Add(
                    row);
            }


            return data;
        }
    }
}