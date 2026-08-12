using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Correct_test1.Models;
using Correct_test1.Core;

using System;
using System.Collections.Generic;


namespace Correct_test1.Readers
{

    /// <summary>
    /// CAD表格读取器
    ///
    /// 作用：
    /// 1. 遍历DWG中的所有AutoCAD Table
    /// 2. 提取单元格数据
    /// 3. 转换为CadTableData
    ///
    /// 不负责：
    /// BOM判断
    /// 属性表判断
    /// 图号判断
    /// </summary>
    public class CadTableReader
    {


        /// <summary>
        /// 读取Database中的所有Table
        /// 
        /// 支持：
        /// ModelSpace
        /// Layout
        /// Block中的Table
        /// </summary>
        public List<CadTableData> Read(
            Database db)
        {

            List<CadTableData> tables =
                new List<CadTableData>();

            DrawingFrameReader frameReader =
                new DrawingFrameReader();


            Extents3d? frame =
                frameReader.Read(db);
            try
            {

                using (Transaction tr =
                    db.TransactionManager.StartTransaction())
                {


                    BlockTable bt =
                        tr.GetObject(
                            db.BlockTableId,
                            OpenMode.ForRead)
                        as BlockTable;



                    foreach (ObjectId btrId in bt)
                    {

                        BlockTableRecord btr =
                            tr.GetObject(
                                btrId,
                                OpenMode.ForRead)
                            as BlockTableRecord;



                        if (btr == null)
                            continue;



                        foreach (ObjectId id in btr)
                        {

                            Entity ent =
                                tr.GetObject(
                                    id,
                                    OpenMode.ForRead)
                                as Entity;



                            if (ent == null)
                                continue;



                            Table table =
                                ent as Table;



                            if (table == null)
                                continue;



                            if (frame != null)
                            {

                                if (!IsInsideFrame(
                                    table,
                                    frame.Value))
                                {
                                    continue;
                                }

                            }


                            CadTableData data =
                                ReadTable(table);


                            tables.Add(data);

                        }

                    }



                    tr.Commit();

                }


            }
            catch (Exception ex)
            {

                AppLogger.Error(
                    ex,
                    "CadTableReader.Read"
                );

            }



            return tables;

        }


        /// <summary>
        /// 判断Table是否位于图框内部
        /// 使用Table中心点判断
        /// </summary>
        private bool IsInsideFrame(
            Table table,
            Extents3d frame)
        {

            Extents3d tableExtents;


            try
            {
                tableExtents =
                    table.GeometricExtents;
            }
            catch
            {
                return false;
            }



            double centerX =
                (
                tableExtents.MinPoint.X +
                tableExtents.MaxPoint.X
                )
                / 2;



            double centerY =
                (
                tableExtents.MinPoint.Y +
                tableExtents.MaxPoint.Y
                )
                / 2;



            return
                centerX >= frame.MinPoint.X &&
                centerX <= frame.MaxPoint.X &&
                centerY >= frame.MinPoint.Y &&
                centerY <= frame.MaxPoint.Y;

        }

        /// <summary>
        /// 单个Table读取
        /// </summary>
        [Obsolete]
        private CadTableData ReadTable(
            Table table)
        {


            CadTableData data =
                new CadTableData();



            data.LayerName =
                table.Layer;



            data.Rows =
                table.Rows.Count;



            data.Columns =
                table.Columns.Count;



            for (int r = 0;
                r < table.Rows.Count;
                r++)
            {

                List<string> row =
                    new List<string>();



                for (int c = 0;
                    c < table.Columns.Count;
                    c++)
                {


                    string value = "";



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
                            "CadTableReader.ReadCell"
                        );

                    }



                    row.Add(value);

                    try
                    {
                        Point3dCollection cellPoints =
                            new Point3dCollection();
                        table.GetCellExtents(
                            r,
                            c,
                            false,
                            cellPoints);

                        double minX = double.MaxValue;
                        double minY = double.MaxValue;
                        double minZ = double.MaxValue;
                        double maxX = double.MinValue;
                        double maxY = double.MinValue;
                        double maxZ = double.MinValue;

                        foreach (Point3d point in cellPoints)
                        {
                            minX = Math.Min(minX, point.X);
                            minY = Math.Min(minY, point.Y);
                            minZ = Math.Min(minZ, point.Z);
                            maxX = Math.Max(maxX, point.X);
                            maxY = Math.Max(maxY, point.Y);
                            maxZ = Math.Max(maxZ, point.Z);
                        }

                        data.CellPositions.Add(
                            r + ":" + c,
                            new Point3d(
                                (minX + maxX) / 2,
                                (minY + maxY) / 2,
                                (minZ + maxZ) / 2));
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error(
                            ex,
                            "CadTableReader.ReadCellPosition");
                    }

                }



                data.Cells.Add(row);

            }



            return data;

        }


    }

}