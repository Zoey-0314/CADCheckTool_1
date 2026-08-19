using System;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Correct_test1.Configs;
using Correct_test1.Core;
using Correct_test1.Models;

namespace Correct_test1.Markers
{
    public class TitleBlockFieldMarker : MarkerBase
    {
        public void DrawMarker(
            Database db,
            string layoutName,
            bool isHorizontal,
            string fieldName,
            string message,
            double offsetX = 0.0,
            double offsetY = 0.0)
        {
            using (Transaction transaction = db.TransactionManager.StartTransaction())
            {
                DBDictionary layouts = transaction.GetObject(
                    db.LayoutDictionaryId,
                    OpenMode.ForRead) as DBDictionary;

                if (!layouts.Contains(layoutName))
                    return;

                Layout layout = transaction.GetObject(
                    layouts.GetAt(layoutName),
                    OpenMode.ForRead) as Layout;

                BlockTableRecord space = transaction.GetObject(
                    layout.BlockTableRecordId,
                    OpenMode.ForWrite) as BlockTableRecord;

                string regionName = ToRegionName(fieldName);

                TitleFieldRegion region = (isHorizontal
                    ? TitleBlockHorizontalConfig.Regions
                    : TitleBlockVerticalConfig.Regions)
                    .Find(x => x.FieldName == regionName);

                if (space == null || region == null)
                    return;

                double minX = region.MinX + offsetX;
                double maxX = region.MaxX + offsetX;
                double minY = region.MinY + offsetY;
                double maxY = region.MaxY + offsetY;

                ObjectId layerId = EnsureLayer(
                    db,
                    transaction,
                    MarkerConfig.TitleBlockLayerName,
                    Color.FromRgb(0, 255, 0));

                Polyline rectangle = new Polyline();
                rectangle.AddVertexAt(0, new Point2d(minX, minY), 0, 0, 0);
                rectangle.AddVertexAt(1, new Point2d(maxX, minY), 0, 0, 0);
                rectangle.AddVertexAt(2, new Point2d(maxX, maxY), 0, 0, 0);
                rectangle.AddVertexAt(3, new Point2d(minX, maxY), 0, 0, 0);
                rectangle.Closed = true;
                rectangle.LayerId = layerId;
                rectangle.Color = Color.FromRgb(0, 255, 0);

                space.AppendEntity(rectangle);
                transaction.AddNewlyCreatedDBObject(rectangle, true);

                double textHeight = MarkerConfig.TextHeight;
                double textY = maxY;

                if (regionName == "DrawingNumber")
                {
                    bool hasExistingText = false;

                    foreach (ObjectId entityId in space)
                    {
                        DBText existingText = transaction.GetObject(
                            entityId,
                            OpenMode.ForRead) as DBText;

                        if (existingText != null &&
                            existingText.Layer == MarkerConfig.TitleBlockLayerName &&
                            Math.Abs(existingText.Position.X - (maxX + 5)) < 0.001 &&
                            existingText.Position.Y >= maxY - 0.001)
                        {
                            hasExistingText = true;
                            break;
                        }
                    }

                    if (hasExistingText)
                        textY += 10;
                }

                DBText text = new DBText
                {
                    TextString = message,
                    Position = new Point3d(maxX + 5, maxY, 0),
                    Height = textHeight,
                    LayerId = layerId,
                    Color = Color.FromRgb(0, 255, 0)
                };

                text.Position = new Point3d(maxX + 5, textY, 0);

                space.AppendEntity(text);
                transaction.AddNewlyCreatedDBObject(text, true);

                transaction.Commit();
            }
        }

        private static string ToRegionName(string fieldName)
        {
            switch (fieldName)
            {
                case "图号": return "DrawingNumber";
                case "图纸名称": return "DrawingName";
                case "材料": return "Material";
                case "规格": return "Specification";
                case "表面处理": return "SurfaceTreatment";
                case "制图": return "Designer";
                case "校对": return "Checker";
                case "标审": return "Reviewer";
                case "批准": return "Approver";
                case "日期": return "TitleDate";
                case "页码": return "PageNumber";
                default: return fieldName;
            }
        }
    }
}