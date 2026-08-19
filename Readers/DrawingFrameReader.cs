using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Correct_test1.Core;

using System;
using System.Collections.Generic;


namespace Correct_test1.Readers
{

    /// <summary>
    /// 自动识别工程图图框
    /// v3:
    /// 1. 只读取当前Layout空间
    /// 2. 支持四条Line组成的矩形图框
    /// 3. 自动适应横版/竖版
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
            if (db == null)
                return null;

            return Read(
                db,
                db.CurrentSpaceId);
        }


        public Extents3d? Read(
            Database db,
            ObjectId layoutSpaceId)
        {
            if (db == null ||
                layoutSpaceId.IsNull ||
                !layoutSpaceId.IsValid)
            {
                return null;
            }

            try
            {
                using (Transaction tr =
                    db.TransactionManager.StartTransaction())
                {
                    BlockTableRecord layoutBtr =
                        tr.GetObject(
                            layoutSpaceId,
                            OpenMode.ForRead)
                        as BlockTableRecord;

                    if (layoutBtr == null)
                        return null;

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

                        if (line == null)
                            continue;

                        double angle =
                            Math.Abs(
                                line.Angle *
                                180 /
                                Math.PI);

                        bool horizontal =
                            angle < 1 ||
                            Math.Abs(angle - 180) < 1;

                        bool vertical =
                            Math.Abs(angle - 90) < 1 ||
                            Math.Abs(angle - 270) < 1;

                        if (horizontal &&
                            line.Length >= 300)
                        {
                            lines.Add(line);
                        }
                        else if (vertical &&
                                 line.Length >= 200)
                        {
                            lines.Add(line);
                        }
                    }

                    Extents3d? frame =
                        FindRectangle(lines);

                    tr.Commit();

                    return frame;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "DrawingFrameReader.Read");

                return null;
            }
        }




        /// <summary>
        /// 从Line集合寻找最大矩形
        /// </summary>
        private Extents3d? FindRectangle(
    List<Line> lines)
        {
            if (lines == null ||
                lines.Count == 0)
            {
                return null;
            }

            List<Line> horizontal =
                new List<Line>();

            List<Line> vertical =
                new List<Line>();

            foreach (Line line in lines)
            {
                if (line == null)
                    continue;

                double dx =
                    Math.Abs(
                        line.EndPoint.X -
                        line.StartPoint.X);

                double dy =
                    Math.Abs(
                        line.EndPoint.Y -
                        line.StartPoint.Y);

                if (dy <= 1 &&
                    line.Length >= 300)
                {
                    horizontal.Add(line);
                }
                else if (dx <= 1 &&
                         line.Length >= 200)
                {
                    vertical.Add(line);
                }
            }

            if (horizontal.Count < 2 ||
                vertical.Count < 2)
            {
                return null;
            }

            Line left =
                vertical[0];

            Line right =
                vertical[0];

            foreach (Line line in vertical)
            {
                double x =
                    (
                        line.StartPoint.X +
                        line.EndPoint.X
                    ) / 2.0;

                double leftX =
                    (
                        left.StartPoint.X +
                        left.EndPoint.X
                    ) / 2.0;

                double rightX =
                    (
                        right.StartPoint.X +
                        right.EndPoint.X
                    ) / 2.0;

                if (x < leftX)
                    left = line;

                if (x > rightX)
                    right = line;
            }

            Line bottom =
                horizontal[0];

            Line top =
                horizontal[0];

            foreach (Line line in horizontal)
            {
                double y =
                    (
                        line.StartPoint.Y +
                        line.EndPoint.Y
                    ) / 2.0;

                double bottomY =
                    (
                        bottom.StartPoint.Y +
                        bottom.EndPoint.Y
                    ) / 2.0;

                double topY =
                    (
                        top.StartPoint.Y +
                        top.EndPoint.Y
                    ) / 2.0;

                if (y < bottomY)
                    bottom = line;

                if (y > topY)
                    top = line;
            }

            double minX =
                (
                    left.StartPoint.X +
                    left.EndPoint.X
                ) / 2.0;

            double maxX =
                (
                    right.StartPoint.X +
                    right.EndPoint.X
                ) / 2.0;

            double minY =
                (
                    bottom.StartPoint.Y +
                    bottom.EndPoint.Y
                ) / 2.0;

            double maxY =
                (
                    top.StartPoint.Y +
                    top.EndPoint.Y
                ) / 2.0;

            if (maxX - minX < 300 ||
                maxY - minY < 200)
            {
                return null;
            }

            return new Extents3d(
                new Point3d(
                    minX,
                    minY,
                    0),

                new Point3d(
                    maxX,
                    maxY,
                    0));
        }






    }

}