using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Correct_test1.Models;
using System;
using System.Collections.Generic;

namespace Correct_test1.Readers
{
    public class ViewportLineReader
    {
        public List<CadLineInfo> Read(Database db, bool includeNestedBlocks = true)
        {
            List<CadLineInfo> result = new List<CadLineInfo>();

            if (db == null)
                return result;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBDictionary layoutDict = tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;
                if (layoutDict == null)
                    return result;

                ObjectId modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                BlockTableRecord modelSpace = tr.GetObject(modelSpaceId, OpenMode.ForRead) as BlockTableRecord;
                if (modelSpace == null)
                    return result;

                foreach (DBDictionaryEntry entry in layoutDict)
                {
                    Layout layout = tr.GetObject(entry.Value, OpenMode.ForRead) as Layout;
                    if (layout == null || layout.ModelType)
                        continue;

                    BlockTableRecord paperSpace = tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
                    if (paperSpace == null)
                        continue;

                    foreach (ObjectId entityId in paperSpace)
                    {
                        Viewport viewport = tr.GetObject(entityId, OpenMode.ForRead) as Viewport;
                        if (viewport == null)
                            continue;

                        if (viewport.Number <= 1 || !viewport.On || viewport.CustomScale <= 0)
                            continue;

                        ModelWindow window = CreateWindow(viewport);

                        foreach (ObjectId modelEntityId in modelSpace)
                        {
                            Entity entity = tr.GetObject(modelEntityId, OpenMode.ForRead) as Entity;
                            if (entity == null)
                                continue;

                            ReadEntityLines(
                                tr,
                                entity,
                                Matrix3d.Identity,
                                layout.LayoutName,
                                window,
                                includeNestedBlocks,
                                new HashSet<ObjectId>(),
                                result);
                        }
                    }
                }

                tr.Commit();
            }

            return result;
        }

        private static ModelWindow CreateWindow(Viewport viewport)
        {
            double modelHeight = viewport.ViewHeight;
            double modelWidth = viewport.Width / viewport.CustomScale;

            double minX = viewport.ViewCenter.X - (modelWidth / 2.0);
            double maxX = viewport.ViewCenter.X + (modelWidth / 2.0);
            double minY = viewport.ViewCenter.Y - (modelHeight / 2.0);
            double maxY = viewport.ViewCenter.Y + (modelHeight / 2.0);

            return new ModelWindow(minX, minY, maxX, maxY);
        }

        private static void ReadEntityLines(
            Transaction tr,
            Entity entity,
            Matrix3d transform,
            string layoutName,
            ModelWindow window,
            bool includeNestedBlocks,
            HashSet<ObjectId> visitedBlocks,
            List<CadLineInfo> output)
        {
            Line line = entity as Line;
            if (line != null)
            {
                Point3d start = line.StartPoint.TransformBy(transform);
                Point3d end = line.EndPoint.TransformBy(transform);

                AddLine(start, end, layoutName, window, output);
                return;
            }

            Polyline polyline = entity as Polyline;
            if (polyline != null)
            {
                for (int index = 0; index < polyline.NumberOfVertices - 1; index++)
                {
                    if (Math.Abs(polyline.GetBulgeAt(index)) > 0.000001)
                        continue;

                    Point3d start = polyline.GetPoint3dAt(index).TransformBy(transform);
                    Point3d end = polyline.GetPoint3dAt(index + 1).TransformBy(transform);

                    AddLine(start, end, layoutName, window, output);
                }

                if (polyline.Closed && polyline.NumberOfVertices > 1)
                {
                    int last = polyline.NumberOfVertices - 1;
                    if (Math.Abs(polyline.GetBulgeAt(last)) <= 0.000001)
                    {
                        Point3d start = polyline.GetPoint3dAt(last).TransformBy(transform);
                        Point3d end = polyline.GetPoint3dAt(0).TransformBy(transform);

                        AddLine(start, end, layoutName, window, output);
                    }
                }

                return;
            }

            if (!includeNestedBlocks)
                return;

            BlockReference blockRef = entity as BlockReference;
            if (blockRef == null)
                return;

            ObjectId blockId = blockRef.BlockTableRecord;
            if (visitedBlocks.Contains(blockId))
                return;

            BlockTableRecord blockDef = tr.GetObject(blockId, OpenMode.ForRead) as BlockTableRecord;
            if (blockDef == null)
                return;

            visitedBlocks.Add(blockId);

            Matrix3d nestedTransform = transform * blockRef.BlockTransform;
            foreach (ObjectId childId in blockDef)
            {
                Entity child = tr.GetObject(childId, OpenMode.ForRead) as Entity;
                if (child == null)
                    continue;

                ReadEntityLines(
                    tr,
                    child,
                    nestedTransform,
                    layoutName,
                    window,
                    includeNestedBlocks,
                    visitedBlocks,
                    output);
            }

            visitedBlocks.Remove(blockId);
        }

        private static void AddLine(
            Point3d start,
            Point3d end,
            string layoutName,
            ModelWindow window,
            List<CadLineInfo> output)
        {
            if (!window.IntersectsSegment(start, end))
                return;

            output.Add(new CadLineInfo
            {
                StartPoint = start,
                EndPoint = end,
                LayoutName = layoutName
            });
        }

        private struct ModelWindow
        {
            public ModelWindow(double minX, double minY, double maxX, double maxY)
            {
                MinX = minX;
                MinY = minY;
                MaxX = maxX;
                MaxY = maxY;
            }

            public double MinX { get; }
            public double MinY { get; }
            public double MaxX { get; }
            public double MaxY { get; }

            public bool IntersectsSegment(Point3d start, Point3d end)
            {
                if (Contains(start.X, start.Y) || Contains(end.X, end.Y))
                    return true;

                double segmentMinX = Math.Min(start.X, end.X);
                double segmentMaxX = Math.Max(start.X, end.X);
                double segmentMinY = Math.Min(start.Y, end.Y);
                double segmentMaxY = Math.Max(start.Y, end.Y);

                if (segmentMaxX < MinX || segmentMinX > MaxX || segmentMaxY < MinY || segmentMinY > MaxY)
                    return false;

                return true;
            }

            private bool Contains(double x, double y)
            {
                return x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
            }
        }
    }
}
