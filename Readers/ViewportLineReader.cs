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

                    foreach (ObjectId viewportId in paperSpace)
                    {
                        Viewport viewport =
                            tr.GetObject(
                                viewportId,
                                OpenMode.ForRead)
                            as Viewport;

                        if (viewport == null ||
                            !viewport.On ||
                            viewport.CustomScale <= 0)
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

        private static ModelWindow CreateWindow(
     Viewport viewport)
        {
            double modelHeight =
                viewport.ViewHeight;

            double modelWidth =
                viewport.Width /
                viewport.CustomScale;

            Matrix3d dcsToWcs =
                Matrix3d.PlaneToWorld(
                    viewport.ViewDirection);

            dcsToWcs =
                Matrix3d.Displacement(
                    viewport.ViewTarget -
                    Point3d.Origin)
                *
                dcsToWcs;

            dcsToWcs =
                Matrix3d.Rotation(
                    -viewport.TwistAngle,
                    viewport.ViewDirection,
                    viewport.ViewTarget)
                *
                dcsToWcs;

            Matrix3d wcsToDcs =
                dcsToWcs.Inverse();

            double minX =
                viewport.ViewCenter.X -
                modelWidth / 2.0;

            double maxX =
                viewport.ViewCenter.X +
                modelWidth / 2.0;

            double minY =
                viewport.ViewCenter.Y -
                modelHeight / 2.0;

            double maxY =
                viewport.ViewCenter.Y +
                modelHeight / 2.0;

            return new ModelWindow(
                minX,
                minY,
                maxX,
                maxY,
                wcsToDcs);
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
    List<CadLineInfo> output)
        {
            output.Add(
                new CadLineInfo
                {
                    StartPoint = start,
                    EndPoint = end
                });
        }
        private struct ModelWindow
        {
            public ModelWindow(
                double minX,
                double minY,
                double maxX,
                double maxY,
                Matrix3d wcsToDcs)
            {
                MinX = minX;
                MinY = minY;
                MaxX = maxX;
                MaxY = maxY;
                WcsToDcs = wcsToDcs;
            }

            public double MinX { get; }
            public double MinY { get; }
            public double MaxX { get; }
            public double MaxY { get; }

            private Matrix3d WcsToDcs { get; }

            public bool IntersectsSegment(
                Point3d start,
                Point3d end)
            {
                Point3d dcsStart =
                    start.TransformBy(
                        WcsToDcs);

                Point3d dcsEnd =
                    end.TransformBy(
                        WcsToDcs);

                if (Contains(
                        dcsStart.X,
                        dcsStart.Y) ||
                    Contains(
                        dcsEnd.X,
                        dcsEnd.Y))
                {
                    return true;
                }

                double segmentMinX =
                    Math.Min(
                        dcsStart.X,
                        dcsEnd.X);

                double segmentMaxX =
                    Math.Max(
                        dcsStart.X,
                        dcsEnd.X);

                double segmentMinY =
                    Math.Min(
                        dcsStart.Y,
                        dcsEnd.Y);

                double segmentMaxY =
                    Math.Max(
                        dcsStart.Y,
                        dcsEnd.Y);

                if (segmentMaxX < MinX ||
                    segmentMinX > MaxX ||
                    segmentMaxY < MinY ||
                    segmentMinY > MaxY)
                {
                    return false;
                }

                return true;
            }

            private bool Contains(
                double x,
                double y)
            {
                return
                    x >= MinX &&
                    x <= MaxX &&
                    y >= MinY &&
                    y <= MaxY;
            }
        }
    }
}
