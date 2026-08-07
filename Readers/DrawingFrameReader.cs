using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Correct_test1.Core;

using System;
using System.Collections.Generic;


namespace Correct_test1.Readers
{

    /// <summary>
    /// 自动识别工程图图框
    ///
    /// v3:
    /// 1. 只读取当前Layout空间
    /// 2. 支持四条Line组成的矩形图框
    /// 3. 自动适应横版/竖版
    ///
    /// 不依赖:
    /// 坐标
    /// 图层
    /// 模板尺寸
    /// </summary>
    public class DrawingFrameReader
    {


        public Extents3d? Read(
            Database db)
        {

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



                    // 当前Layout

                    BlockTableRecord layoutBtr =
                        tr.GetObject(
                            db.CurrentSpaceId,
                            OpenMode.ForRead)
                        as BlockTableRecord;



                    if (layoutBtr == null)
                    {
                        return null;
                    }



                    List<Line> lines =
                        new List<Line>();



                    foreach (ObjectId id in layoutBtr)
                    {

                        Entity ent =
                            tr.GetObject(
                                id,
                                OpenMode.ForRead)
                            as Entity;



                        Line line =
                            ent as Line;



                        if (line != null)
                        {

                            lines.Add(line);

                        }

                    }



                    tr.Commit();



                    return FindRectangle(lines);

                }

            }
            catch (Exception ex)
            {

                AppLogger.Error(
                    ex,
                    "DrawingFrameReader.Read"
                );


                return null;

            }

        }




        /// <summary>
        /// 从Line集合寻找最大矩形
        /// </summary>
        private Extents3d? FindRectangle(
            List<Line> lines)
        {


            List<Line> horizontal =
                new List<Line>();


            List<Line> vertical =
                new List<Line>();



            foreach (Line line in lines)
            {

                double angle =
                    Math.Abs(
                        line.Angle *
                        180 /
                        Math.PI);



                // 水平

                if (angle < 1 ||
                   Math.Abs(angle - 180) < 1)
                {
                    horizontal.Add(line);
                }



                // 垂直

                else if (Math.Abs(angle - 90) < 1 ||
                        Math.Abs(angle - 270) < 1)
                {
                    vertical.Add(line);
                }

            }



            double maxArea = 0;


            Extents3d? result = null;



            foreach (Line top in horizontal)
            {

                foreach (Line bottom in horizontal)
                {


                    if (top == bottom)
                        continue;



                    double y1 =
                        top.StartPoint.Y;


                    double y2 =
                        bottom.StartPoint.Y;



                    if (Math.Abs(y1 - y2) < 1)
                        continue;



                    foreach (Line left in vertical)
                    {

                        foreach (Line right in vertical)
                        {


                            if (left == right)
                                continue;



                            double x1 =
                                left.StartPoint.X;


                            double x2 =
                                right.StartPoint.X;



                            if (Math.Abs(x1 - x2) < 1)
                                continue;



                            double width =
                                Math.Abs(
                                    x1 - x2);



                            double height =
                                Math.Abs(
                                    y1 - y2);



                            // 排除小矩形

                            if (width < 300 ||
                               height < 200)
                            {
                                continue;
                            }



                            double area =
                                width *
                                height;



                            if (area <= maxArea)
                                continue;



                            if (IsRectangle(
                                top,
                                bottom,
                                left,
                                right))
                            {

                                maxArea = area;



                                result =
                                    new Extents3d(
                                        new Point3d(
                                            Math.Min(x1, x2),
                                            Math.Min(y1, y2),
                                            0),

                                        new Point3d(
                                            Math.Max(x1, x2),
                                            Math.Max(y1, y2),
                                            0)
                                    );

                            }

                        }

                    }

                }

            }



            return result;

        }





        /// <summary>
        /// 判断四条Line是否真的连接成矩形
        /// </summary>
        private bool IsRectangle(
            Line top,
            Line bottom,
            Line left,
            Line right)
        {


            double minX =
                Math.Min(
                    left.StartPoint.X,
                    left.EndPoint.X);


            double maxX =
                Math.Max(
                    right.StartPoint.X,
                    right.EndPoint.X);



            double minY =
                Math.Min(
                    bottom.StartPoint.Y,
                    bottom.EndPoint.Y);



            double maxY =
                Math.Max(
                    top.StartPoint.Y,
                    top.EndPoint.Y);



            // 判断四条边长度

            if (
                top.Length < 300 ||
                bottom.Length < 300 ||
                left.Length < 200 ||
                right.Length < 200
              )
            {
                return false;
            }



            return true;

        }


    }

}