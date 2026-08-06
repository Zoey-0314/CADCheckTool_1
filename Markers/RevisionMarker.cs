using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Correct_test1.Models;
using Correct_test1.Core;
using System.Collections.Generic;

namespace Correct_test1.Markers
{
    public class RevisionMarker : MarkerBase
    {
        private const string LayerName = "REVISION_CHECK";


        /// <summary>
        /// 清除当前Database中的检查框
        /// </summary>
        public void ClearMarkers(Database db)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                AppLogger.Info(
                    "开始执行ClearMarkers",
                    "RevisionMarker"
                );


                BlockTable bt =
                    tr.GetObject(
                        db.BlockTableId,
                        OpenMode.ForRead
                    ) as BlockTable;



                foreach (ObjectId id in bt)
                {
                    BlockTableRecord btr =
                        tr.GetObject(
                            id,
                            OpenMode.ForWrite
                        ) as BlockTableRecord;


                    List<ObjectId> remove =
                        new List<ObjectId>();



                    foreach (ObjectId entId in btr)
                    {
                        Entity ent =
                            tr.GetObject(
                                entId,
                                OpenMode.ForRead
                            ) as Entity;


                        if (ent != null &&
                           ent.Layer == LayerName)
                        {
                            remove.Add(entId);
                        }

                    }



                    foreach (ObjectId rid in remove)
                    {
                        Entity ent =
                            tr.GetObject(
                                rid,
                                OpenMode.ForWrite
                            ) as Entity;


                        if (ent != null)
                        {

                            AppLogger.Info(
                                $"删除Revision标记 ObjectId={rid}",
                                "RevisionMarker"
                            );


                            ent.Erase();

                        }
                    }
                }


                AppLogger.Info(
                    "ClearMarkers准备Commit",
                    "RevisionMarker"
                );


                tr.Commit();


                AppLogger.Info(
                    "ClearMarkers Commit成功",
                    "RevisionMarker"
                );

            }
        }



        /// <summary>
        /// 绘制检查绿色框
        /// </summary>
        public void DrawMarkers(
            Database db,
            ObjectId layoutBlockId,
            List<RevisionMarkPoint> points)
        {

            if (points == null || points.Count == 0)
                return;



            using (Transaction tr =
                db.TransactionManager.StartTransaction())
            {


                ObjectId layerId =
                    EnsureLayer(
                        db,
                        tr,
                        LayerName,
                        Color.FromColorIndex(
                            ColorMethod.ByAci,
                            3
                        )
                    );



                BlockTableRecord btr =
                    tr.GetObject(
                        layoutBlockId,
                        OpenMode.ForWrite
                    ) as BlockTableRecord;



                if (btr == null)
                    return;



                int count = 0;


                foreach (RevisionMarkPoint point in points)
                {

                    if (point.X == 0 &&
                       point.Y == 0)
                        continue;



                    Polyline rect =
                        CreateRectangle(
                            point.X,
                            point.Y
                        );


                    rect.LayerId =
                        layerId;


                    rect.SetDatabaseDefaults(db);



                    btr.AppendEntity(rect);


                    tr.AddNewlyCreatedDBObject(
                        rect,
                        true
                    );


                    count++;

                }



                tr.Commit();



                AppLogger.Info(
                    $"RevisionMarker绘制完成 数量:{count}",
                    "RevisionMarker"
                );

            }
        }



        private Polyline CreateRectangle(
            double x,
            double y)
        {

            double width = 18;
            double height = 5;


            Polyline pl =
                new Polyline();


            pl.AddVertexAt(
                0,
                new Point2d(
                    x - width / 2,
                    y - height / 2
                ),
                0,
                0,
                0
            );


            pl.AddVertexAt(
                1,
                new Point2d(
                    x + width / 2,
                    y - height / 2
                ),
                0,
                0,
                0
            );


            pl.AddVertexAt(
                2,
                new Point2d(
                    x + width / 2,
                    y + height / 2
                ),
                0,
                0,
                0
            );


            pl.AddVertexAt(
                3,
                new Point2d(
                    x - width / 2,
                    y + height / 2
                ),
                0,
                0,
                0
            );


            pl.Closed = true;


            return pl;
        }
    }
}