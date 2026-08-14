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
            List<TitleText> result = new List<TitleText>();

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

                ObjectId modelSpaceId =
                    SymbolUtilityServices.GetBlockModelSpaceId(db);

                BlockTableRecord modelSpace =
                    tr.GetObject(
                        modelSpaceId,
                        OpenMode.ForRead) as BlockTableRecord;

                if (modelSpace == null)
                    return result;

                foreach (DBDictionaryEntry entry in layoutDict)
                {
                    Layout layout =
                        tr.GetObject(
                            entry.Value,
                            OpenMode.ForRead) as Layout;

                    if (layout == null || layout.ModelType)
                        continue;

                    ObjectIdCollection viewportIds =
                        layout.GetViewports();

                    // 第0个是PaperSpace自身Viewport，
                    // 从第1个开始处理真正的浮动Viewport。
                    for (int i = 1; i < viewportIds.Count; i++)
                    {
                        Viewport viewport =
                            tr.GetObject(
                                viewportIds[i],
                                OpenMode.ForRead) as Viewport;

                        if (viewport == null ||
                            !viewport.On ||
                            viewport.CustomScale <= 0)
                        {
                            continue;
                        }

                        ModelWindow window =
                            CreateWindow(viewport);

                        foreach (ObjectId modelEntityId in modelSpace)
                        {
                            Entity entity =
                                tr.GetObject(
                                    modelEntityId,
                                    OpenMode.ForRead) as Entity;

                            if (entity == null)
                                continue;

                            ReadEntityText(
                                tr,
                                entity,
                                Matrix3d.Identity,
                                layout.LayoutName,
                                window,
                                includeNestedBlocks,
                                useViewportFilter,
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

        private static void ReadEntityText(
            Transaction tr,
            Entity entity,
            Matrix3d transform,
            string layoutName,
            ModelWindow window,
            bool includeNestedBlocks,
            bool useViewportFilter,
            List<TitleText> output)
        {
            DBText dbText = entity as DBText;
            if (dbText != null)
            {
                Point3d position = dbText.Position.TransformBy(transform);
                AddText(
                    dbText.TextString,
                    position,
                    layoutName,
                    window,
                    useViewportFilter,
                    output);
                return;
            }

            MText mText = entity as MText;
            if (mText != null)
            {
                Point3d position = mText.Location.TransformBy(transform);
                AddText(
                    mText.Text,
                    position,
                    layoutName,
                    window,
                    useViewportFilter,
                    output);
                return;
            }

            if (!includeNestedBlocks)
                return;

            BlockReference blockRef = entity as BlockReference;
            if (blockRef == null)
                return;

            BlockTableRecord blockDef =
                tr.GetObject(
                    blockRef.BlockTableRecord,
                    OpenMode.ForRead) as BlockTableRecord;

            if (blockDef == null)
                return;

            Matrix3d nestedTransform =
                transform * blockRef.BlockTransform;

            foreach (ObjectId childId in blockDef)
            {
                Entity child =
                    tr.GetObject(
                        childId,
                        OpenMode.ForRead) as Entity;

                if (child == null)
                    continue;

                ReadEntityText(
                    tr,
                    child,
                    nestedTransform,
                    layoutName,
                    window,
                    includeNestedBlocks,
                    useViewportFilter,
                    output);
            }
        }

        private static void AddText(
            string rawText,
            Point3d position,
            string layoutName,
            ModelWindow window,
            bool useViewportFilter,
            List<TitleText> output)
        {
            if (useViewportFilter &&
                !window.Contains(position.X, position.Y))
            {
                return;
            }

            output.Add(new TitleText
            {
                Text = Clean(rawText),
                X = position.X,
                Y = position.Y,
                LayoutName = layoutName
            });
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
