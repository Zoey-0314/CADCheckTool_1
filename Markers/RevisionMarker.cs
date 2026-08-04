using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;

using Correct_test1.Models;


namespace Correct_test1.Markers
{

    public class RevisionMarker
    {


        private const string LayerName =
            "REVISION_CHECK";

        public void ClearMarkers(
    Database db)
        {

            using (Transaction tr =
                db.TransactionManager.StartTransaction())
            {


                BlockTable bt =
                    tr.GetObject(
                        db.BlockTableId,
                        OpenMode.ForRead)
                    as BlockTable;



                foreach (ObjectId id in bt)
                {

                    BlockTableRecord btr =
                        tr.GetObject(
                            id,
                            OpenMode.ForWrite)
                        as BlockTableRecord;



                    List<ObjectId> remove =
                        new List<ObjectId>();



                    foreach (ObjectId entId in btr)
                    {

                        Entity ent =
                            tr.GetObject(
                                entId,
                                OpenMode.ForRead)
                            as Entity;


                        if (ent != null &&
                           ent.Layer == "REVISION_CHECK")
                        {
                            remove.Add(entId);
                        }

                    }



                    foreach (ObjectId rid in remove)
                    {

                        Entity ent =
                            tr.GetObject(
                                rid,
                                OpenMode.ForWrite)
                            as Entity;


                        ent.Erase();

                    }


                }


                tr.Commit();

            }

        }

        /// <summary>
        /// 在指定布局空间绘制检查框
        /// </summary>
        public void DrawMarkers(
            Database db,
            ObjectId layoutBlockId,
            List<RevisionMarkPoint> points)
        {


            if (points == null ||
               points.Count == 0)
                return;



            using (Transaction tr =
                db.TransactionManager.StartTransaction())
            {


                CreateLayer(
                    db,
                    tr
                );



                BlockTableRecord btr =
                    tr.GetObject(
                        layoutBlockId,
                        OpenMode.ForWrite)
                    as BlockTableRecord;



                if (btr == null)
                    return;




                foreach (RevisionMarkPoint point in points)
                {


                    // 防止异常坐标

                    if (point.X == 0 &&
                       point.Y == 0)
                        continue;




                    Polyline rect =
                        CreateRectangle(
                            point.X,
                            point.Y
                        );


                    rect.Layer =
                        LayerName;



                    btr.AppendEntity(rect);


                    tr.AddNewlyCreatedDBObject(
                        rect,
                        true
                    );


                }



                tr.Commit();

            }


        }





        /// <summary>
        /// 创建绿色图层
        /// </summary>

        private void CreateLayer(
            Database db,
            Transaction tr)
        {


            LayerTable lt =
                tr.GetObject(
                    db.LayerTableId,
                    OpenMode.ForRead)
                as LayerTable;



            if (lt.Has(LayerName))
                return;



            lt.UpgradeOpen();



            LayerTableRecord layer =
                new LayerTableRecord();


            layer.Name =
                LayerName;



            // AutoCAD绿色

            layer.Color =
                Color.FromColorIndex(
                    ColorMethod.ByAci,
                    3
                );



            lt.Add(layer);



            tr.AddNewlyCreatedDBObject(
                layer,
                true
            );


        }






        /// <summary>
        /// 根据中心点生成矩形
        /// </summary>

        private Polyline CreateRectangle(
            double x,
            double y)
        {


            // 暂定框大小
            // 后续可根据表格尺寸调整

            double width = 18;

            double height = 5;



            Polyline pl =
                new Polyline();



            pl.AddVertexAt(
                0,
                new Point2d(
                    x - width / 2,
                    y - height / 2),
                0,
                0,
                0);



            pl.AddVertexAt(
                1,
                new Point2d(
                    x + width / 2,
                    y - height / 2),
                0,
                0,
                0);



            pl.AddVertexAt(
                2,
                new Point2d(
                    x + width / 2,
                    y + height / 2),
                0,
                0,
                0);



            pl.AddVertexAt(
                3,
                new Point2d(
                    x - width / 2,
                    y + height / 2),
                0,
                0,
                0);



            pl.Closed = true;



            return pl;


        }


    }

}