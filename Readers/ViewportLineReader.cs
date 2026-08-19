using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Correct_test1.Models;
using System;
using System.Collections.Generic;

namespace Correct_test1.Readers
{
    public class ViewportLineReader
    {
        public List<CadLineInfo> Read(
    Database db,
    bool includeNestedBlocks = true)
        {
            List<CadLineInfo> result =
                new List<CadLineInfo>();

            if (db == null)
                return result;

            using (Transaction tr =
                db.TransactionManager.StartTransaction())
            {
                DBDictionary layoutDict =
                    tr.GetObject(
                        db.LayoutDictionaryId,
                        OpenMode.ForRead) as DBDictionary;

                if (layoutDict == null)
                    return result;

                BlockTableRecord modelSpace =
                    tr.GetObject(
                        SymbolUtilityServices
                            .GetBlockModelSpaceId(db),
                        OpenMode.ForRead) as BlockTableRecord;

                if (modelSpace == null)
                    return result;

                List<CadLineInfo> modelLines =
                    new List<CadLineInfo>();

                HashSet<ObjectId> activeBlocks =
                    new HashSet<ObjectId>();

                foreach (ObjectId id in modelSpace)
                {
                    Entity entity =
                        tr.GetObject(
                            id,
                            OpenMode.ForRead) as Entity;

                    if (entity == null)
                        continue;

                    CollectEntityLines(
                        tr,
                        entity,
                        Matrix3d.Identity,
                        includeNestedBlocks,
                        activeBlocks,
                        modelLines);
                }

                foreach (DBDictionaryEntry entry in layoutDict)
                {
                    Layout layout =
                        tr.GetObject(
                            entry.Value,
                            OpenMode.ForRead) as Layout;

                    if (layout == null || layout.ModelType)
                        continue;

                    BlockTableRecord paperSpace =
    tr.GetObject(
        layout.BlockTableRecordId,
        OpenMode.ForRead)
    as BlockTableRecord;

                    if (paperSpace == null)
                        continue;

                    ObjectId paperViewportId =
                        ObjectId.Null;

                    try
                    {
                        ObjectIdCollection knownViewports =
                            layout.GetViewports();

                        if (knownViewports != null &&
                            knownViewports.Count > 0)
                        {
                            paperViewportId =
                                knownViewports[0];
                        }
                    }
                    catch
                    {
                    }

                    bool skippedFallbackPaperViewport =
                        false;

                    foreach (ObjectId entityId in paperSpace)
                    {
                        Viewport viewport =
                            tr.GetObject(
                                entityId,
                                OpenMode.ForRead)
                            as Viewport;

                        if (viewport == null)
                            continue;

                        if (!paperViewportId.IsNull &&
                            entityId == paperViewportId)
                        {
                            continue;
                        }

                        if (paperViewportId.IsNull &&
                            !skippedFallbackPaperViewport)
                        {
                            skippedFallbackPaperViewport = true;
                            continue;
                        }

                        if (viewport.CustomScale <= 0)
                        {
                            continue;
                        }

                        ModelWindow window =
                            CreateWindow(viewport);

                        foreach (CadLineInfo line in modelLines)
                        {
                            if (!window.IntersectsSegment(
                                    line.StartPoint,
                                    line.EndPoint))
                            {
                                continue;
                            }

                            result.Add(
                                new CadLineInfo
                                {
                                    StartPoint =
                                        line.StartPoint,

                                    EndPoint =
                                        line.EndPoint,

                                    LayoutName =
                                        layout.LayoutName,

                                    ViewportId =
                                        viewport.ObjectId,

                                    IsBlue =
                                        line.IsBlue
                                });
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

        private static void CollectEntityLines(
    Transaction tr,
    Entity entity,
    Matrix3d transform,
    bool includeNestedBlocks,
    HashSet<ObjectId> activeBlocks,
    List<CadLineInfo> output)
        {
            Line line =
                entity as Line;

            if (line != null)
            {
                AddModelLine(
                    line.StartPoint.TransformBy(transform),
                    line.EndPoint.TransformBy(transform),
                    IsBlue(tr, line),
                    output);

                return;
            }

            Polyline polyline =
                entity as Polyline;

            if (polyline != null)
            {
                for (int i = 0;
                     i < polyline.NumberOfVertices - 1;
                     i++)
                {
                    if (Math.Abs(
                            polyline.GetBulgeAt(i))
                        > 0.000001)
                    {
                        continue;
                    }

                    AddModelLine(
                        polyline.GetPoint3dAt(i)
                            .TransformBy(transform),

                        polyline.GetPoint3dAt(i + 1)
                            .TransformBy(transform),

                        IsBlue(tr, polyline),

                        output);
                }

                if (polyline.Closed &&
                    polyline.NumberOfVertices > 1)
                {
                    int last =
                        polyline.NumberOfVertices - 1;

                    if (Math.Abs(
                            polyline.GetBulgeAt(last))
                        <= 0.000001)
                    {
                        AddModelLine(
                            polyline.GetPoint3dAt(last)
                                .TransformBy(transform),

                            polyline.GetPoint3dAt(0)
                                .TransformBy(transform),

                            IsBlue(tr, polyline),

                            output);
                    }
                }

                return;
            }

            if (!includeNestedBlocks)
                return;

            BlockReference blockRef =
                entity as BlockReference;

            if (blockRef == null)
                return;

            ObjectId blockId =
                blockRef.BlockTableRecord;

            if (activeBlocks.Contains(blockId))
                return;

            BlockTableRecord blockDef =
                tr.GetObject(
                    blockId,
                    OpenMode.ForRead) as BlockTableRecord;

            if (blockDef == null ||
                blockDef.IsFromExternalReference)
            {
                return;
            }

            activeBlocks.Add(blockId);

            try
            {
                Matrix3d nestedTransform =
                    transform *
                    blockRef.BlockTransform;

                foreach (ObjectId childId in blockDef)
                {
                    Entity child =
                        tr.GetObject(
                            childId,
                            OpenMode.ForRead) as Entity;

                    if (child == null)
                        continue;

                    CollectEntityLines(
                        tr,
                        child,
                        nestedTransform,
                        includeNestedBlocks,
                        activeBlocks,
                        output);
                }
            }
            finally
            {
                activeBlocks.Remove(blockId);
            }
        }
        private static void AddModelLine(
    Point3d start,
    Point3d end,
    bool isBlue,
    List<CadLineInfo> output)
        {
            output.Add(
                new CadLineInfo
                {
                    StartPoint = start,
                    EndPoint = end,
                    IsBlue = isBlue
                });
        }

        private static bool IsBlue(
    Transaction tr,
    Entity entity)
        {
            if (entity == null)
                return false;

            if (entity.ColorIndex == 5)
                return true;

            if (entity.ColorIndex == 256)
            {
                LayerTableRecord layer =
                    tr.GetObject(
                        entity.LayerId,
                        OpenMode.ForRead)
                    as LayerTableRecord;

                if (layer != null &&
                    layer.Color != null &&
                    layer.Color.ColorIndex == 5)
                {
                    return true;
                }
            }

            return false;
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
