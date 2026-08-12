using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Correct_test1.Configs;
using Correct_test1.Core;
using Correct_test1.Models;
using System.Collections.Generic;

namespace Correct_test1.Markers
{
    /// <summary>
    /// 在 CADCHECK_MARKER 图层上绘制BOM序号一致性问题标记
    /// </summary>
    public class BomCalloutMarker : MarkerBase
    {
        public void CreateMarkers(
            Database database,
            List<BomCalloutIssue> issues)
        {
            if (database == null || issues == null || issues.Count == 0)
                return;

            try
            {
                using (Transaction tr = database.TransactionManager.StartTransaction())
                {
                    ObjectId layerId = EnsureLayer(
                        database, tr,
                        MarkerManager.LayerName,
                        Color.FromRgb(255, 0, 0));

                    EnsureXDataApp(database, tr);

                    BlockTableRecord space = tr.GetObject(
                        database.CurrentSpaceId,
                        OpenMode.ForWrite) as BlockTableRecord;

                    if (space == null)
                    {
                        tr.Commit();
                        return;
                    }

                    foreach (BomCalloutIssue issue in issues)
                    {
                        if (issue == null)
                            continue;

                        Point3d pos = issue.MarkerPosition;

                        DBText text = new DBText
                        {
                            Position = pos,
                            Height = MarkerConfig.TextHeight,
                            TextString = issue.Message,
                            LayerId = layerId,
                            Color = Color.FromRgb(255, 0, 0)
                        };

                        space.AppendEntity(text);
                        tr.AddNewlyCreatedDBObject(text, true);

                        text.XData = new ResultBuffer(
                            new TypedValue(
                                (int)DxfCode.ExtendedDataRegAppName,
                                MarkerManager.XDataAppName),
                            new TypedValue(
                                (int)DxfCode.ExtendedDataAsciiString,
                                issue.Message));
                    }

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                AppLogger.Error(ex, "BomCalloutMarker.CreateMarkers");
            }
        }

        private static void EnsureXDataApp(Database database, Transaction tr)
        {
            RegAppTable table = tr.GetObject(
                database.RegAppTableId, OpenMode.ForRead) as RegAppTable;
            if (table.Has(MarkerManager.XDataAppName))
                return;

            table.UpgradeOpen();
            RegAppTableRecord record = new RegAppTableRecord
            {
                Name = MarkerManager.XDataAppName
            };
            table.Add(record);
            tr.AddNewlyCreatedDBObject(record, true);
        }
    }
}
