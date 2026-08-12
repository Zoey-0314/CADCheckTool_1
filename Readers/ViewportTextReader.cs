using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Correct_test1.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Correct_test1.Readers
{
    public class ViewportTextReader
    {
        public List<TitleText> Read(Database db, bool includeNestedBlocks = true)
        {
            List<TitleText> result = new List<TitleText>();
            List<DebugRow> debugRows = new List<DebugRow>();

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
                        string source = "Viewport#" + viewport.Number + "(" + viewport.Handle.ToString() + ")";

                        foreach (ObjectId modelEntityId in modelSpace)
                        {
                            Entity entity = tr.GetObject(modelEntityId, OpenMode.ForRead) as Entity;
                            if (entity == null)
                                continue;

                            ReadEntityText(
                                tr,
                                entity,
                                Matrix3d.Identity,
                                layout.LayoutName,
                                source,
                                window,
                                includeNestedBlocks,
                                result,
                                debugRows);
                        }
                    }
                }

                tr.Commit();
            }

            WriteDebugCsv(debugRows);
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
            string source,
            ModelWindow window,
            bool includeNestedBlocks,
            List<TitleText> output,
            List<DebugRow> debugRows)
        {
            DBText dbText = entity as DBText;
            if (dbText != null)
            {
                Point3d position = dbText.Position.TransformBy(transform);
                AddText("DBText", dbText.TextString, position, layoutName, source, window, output, debugRows);
                return;
            }

            MText mText = entity as MText;
            if (mText != null)
            {
                Point3d position = mText.Location.TransformBy(transform);
                AddText("MText", mText.Text, position, layoutName, source, window, output, debugRows);
                return;
            }

            if (!includeNestedBlocks)
                return;

            BlockReference blockRef = entity as BlockReference;
            if (blockRef == null)
                return;

            BlockTableRecord blockDef = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
            if (blockDef == null)
                return;

            Matrix3d nestedTransform = transform * blockRef.BlockTransform;

            foreach (ObjectId childId in blockDef)
            {
                Entity child = tr.GetObject(childId, OpenMode.ForRead) as Entity;
                if (child == null)
                    continue;

                ReadEntityText(
                    tr,
                    child,
                    nestedTransform,
                    layoutName,
                    source,
                    window,
                    includeNestedBlocks,
                    output,
                    debugRows);
            }
        }

        private static void AddText(
            string type,
            string rawText,
            Point3d position,
            string layoutName,
            string source,
            ModelWindow window,
            List<TitleText> output,
            List<DebugRow> debugRows)
        {
            if (!window.Contains(position.X, position.Y))
                return;

            string text = Clean(rawText);
            TitleText titleText = new TitleText
            {
                Text = text,
                X = position.X,
                Y = position.Y,
                LayoutName = layoutName
            };

            output.Add(titleText);
            debugRows.Add(new DebugRow
            {
                Type = type,
                Text = text,
                X = position.X,
                Y = position.Y,
                Layout = layoutName,
                Source = source
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

        private static void WriteDebugCsv(List<DebugRow> rows)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.IsNullOrWhiteSpace(desktopPath))
                return;

            string filePath = Path.Combine(desktopPath, "ViewportTextDebug.csv");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Type,Text,X,Y,Layout,Source(Viewport)");

            foreach (DebugRow row in rows)
            {
                sb.AppendLine(
                    Escape(row.Type) + "," +
                    Escape(row.Text) + "," +
                    row.X.ToString("0.####", CultureInfo.InvariantCulture) + "," +
                    row.Y.ToString("0.####", CultureInfo.InvariantCulture) + "," +
                    Escape(row.Layout) + "," +
                    Escape(row.Source));
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";

            return value;
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

            public bool Contains(double x, double y)
            {
                return x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
            }
        }

        private class DebugRow
        {
            public string Type { get; set; }
            public string Text { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public string Layout { get; set; }
            public string Source { get; set; }
        }
    }
}
