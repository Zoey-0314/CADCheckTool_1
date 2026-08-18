using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Correct_test1.Models;
using System;
using System.Collections.Generic;

namespace Correct_test1.Readers
{
    public class ViewportTextReader
    {
        public List<TitleText> Read(
    Database db,
    bool includeNestedBlocks = true,
    bool useViewportFilter = true)
        {
            List<TitleText> result =
                new List<TitleText>();

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

                List<TitleText> modelTexts =
                    new List<TitleText>();

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

                    CollectEntityText(
                        tr,
                        entity,
                        Matrix3d.Identity,
                        includeNestedBlocks,
                        activeBlocks,
                        modelTexts);
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

                        if (!viewport.On ||
                            viewport.CustomScale <= 0)
                        {
                            continue;
                        }

                        ModelWindow window =
                            CreateWindow(viewport);

                        foreach (TitleText text in modelTexts)
                        {
                            if (useViewportFilter &&
                                !window.Contains(
                                    text.X,
                                    text.Y))
                            {
                                continue;
                            }

                            result.Add(
                                new TitleText
                                {
                                    Text = text.Text,
                                    X = text.X,
                                    Y = text.Y,
                                    Height = text.Height,

                                    LayoutName =
                                        layout.LayoutName,

                                    ViewportId =
                                        viewport.ObjectId
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

        private static void CollectEntityText(
    Transaction tr,
    Entity entity,
    Matrix3d transform,
    bool includeNestedBlocks,
    HashSet<ObjectId> activeBlocks,
    List<TitleText> output)
        {
            DBText dbText =
                entity as DBText;

            if (dbText != null)
            {
                Point3d position =
                    dbText.Position.TransformBy(
                        transform);

                output.Add(
                    new TitleText
                    {
                        Text = Clean(
                            dbText.TextString),
                        X = position.X,
                        Y = position.Y,
                        Height = dbText.Height
                    });

                return;
            }

            MText mText =
                entity as MText;

            if (mText != null)
            {
                Point3d position =
                    mText.Location.TransformBy(
                        transform);

                output.Add(
                    new TitleText
                    {
                        Text = Clean(
                            mText.Text),
                        X = position.X,
                        Y = position.Y,
                        Height = mText.TextHeight
                    });

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

                    CollectEntityText(
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

        private static string Clean(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return text
                .Replace("\\P", "\n")
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Trim();
        }

        private struct ModelWindow
        {
            public ModelWindow(
                double minX,
                double minY,
                double maxX,
                double maxY)
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

            public bool Contains(double x, double y)
            {
                return x >= MinX &&
                    x <= MaxX &&
                    y >= MinY &&
                    y <= MaxY;
            }
        }
    }
}
