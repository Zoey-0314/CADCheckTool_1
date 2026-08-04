using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Correct_test1.Models;
using System.Collections.Generic;

namespace Correct_test1.Markers
{
    public class RevisionMarker
    {
        private const string LayerName = "REVISION_CHECK";

        /// <summary>
        /// 清除当前Database中的检查框
        /// </summary>
        public void ClearMarkers(Database db)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                foreach (ObjectId id in bt)
                {
                    BlockTableRecord btr = tr.GetObject(id, OpenMode.ForWrite) as BlockTableRecord;
                    List<ObjectId> remove = new List<ObjectId>();

                    foreach (ObjectId entId in btr)
                    {
                        Entity ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;
                        if (ent != null && ent.Layer == LayerName)
                        {
                            remove.Add(entId);
                        }
                    }

                    foreach (ObjectId rid in remove)
                    {
                        Entity ent = tr.GetObject(rid, OpenMode.ForWrite) as Entity;
                        if (ent != null)
                        {
                            ent.Erase();
                        }
                    }
                }

                tr.Commit();
            }
        }

        /// <summary>
        /// 绘制检查绿色框
        /// 支持：
        /// 1. 当前CAD文档
        /// 2. 后台Database批量检查
        /// </summary>
        public void DrawMarkers(Database db, ObjectId layoutBlockId, List<RevisionMarkPoint> points)
        {
            if (points == null || points.Count == 0)
                return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // 确保图层存在
                ObjectId layerId = EnsureLayer(db, tr);

                BlockTableRecord btr = tr.GetObject(layoutBlockId, OpenMode.ForWrite) as BlockTableRecord;
                if (btr == null)
                    return;

                foreach (RevisionMarkPoint point in points)
                {
                    // 无有效坐标跳过
                    if (point.X == 0 && point.Y == 0)
                        continue;

                    Polyline rect = CreateRectangle(point.X, point.Y);
                    // 再次确认图层存在
                    rect.LayerId = layerId;

                    btr.AppendEntity(rect);
                    tr.AddNewlyCreatedDBObject(rect, true);
                }

                tr.Commit();
            }
        }

        /// <summary>
        /// 确保检查图层存在
        /// 批量Database环境使用
        /// </summary>
        private ObjectId EnsureLayer(Database db, Transaction tr)
        {
            LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

            if (lt.Has(LayerName))
            {
                return lt[LayerName];
            }

            lt.UpgradeOpen();
            LayerTableRecord layer = new LayerTableRecord();
            layer.Name = LayerName;
            layer.Color = Color.FromColorIndex(ColorMethod.ByAci, 3);

            ObjectId layerId = lt.Add(layer);
            tr.AddNewlyCreatedDBObject(layer, true);

            return layerId;
        }

        /// <summary>
        /// 创建矩形框
        /// </summary>
        private Polyline CreateRectangle(double x, double y)
        {
            double width = 18;
            double height = 5;

            Polyline pl = new Polyline();

            pl.AddVertexAt(0, new Point2d(x - width / 2, y - height / 2), 0, 0, 0);
            pl.AddVertexAt(1, new Point2d(x + width / 2, y - height / 2), 0, 0, 0);
            pl.AddVertexAt(2, new Point2d(x + width / 2, y + height / 2), 0, 0, 0);
            pl.AddVertexAt(3, new Point2d(x - width / 2, y + height / 2), 0, 0, 0);

            pl.Closed = true;

            return pl;
        }
    }
}