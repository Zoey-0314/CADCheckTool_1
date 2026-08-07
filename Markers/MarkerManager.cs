using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Correct_test1.Models;
using System.Collections.Generic;

namespace Correct_test1.Markers
{
    public class MarkerManager : MarkerBase
    {
        public const string LayerName = "Correct_test1_Marker";
        public const string XDataAppName = "Correct_test1_Marker";

        public void CreateMarkers(
            Database database,
            List<StandardPartCheckResult> results)
        {
            if (database == null || results == null)
            {
                return;
            }

            using (Transaction transaction =
                database.TransactionManager.StartTransaction())
            {
                ObjectId layerId = EnsureLayer(
                    database,
                    transaction,
                    LayerName,
                    Color.FromRgb(255, 0, 0));

                RegisterXDataApp(database, transaction);

                StandardPartMarker marker =
                    new StandardPartMarker();
                int markerIndex = 0;

                foreach (StandardPartCheckResult result in results)
                {
                    if (result == null ||
                        result.Status == StandardPartCheckStatus.Correct)
                    {
                        continue;
                    }

                    MarkerInfo info = new MarkerInfo();
                    info.Result = result;
                    info.Text = BuildText(result);
                    info.Position = new Point3d(
                        0,
                        markerIndex * 3.0,
                        0);

                    marker.Create(
                        database,
                        transaction,
                        database.CurrentSpaceId,
                        layerId,
                        info);
                    markerIndex++;
                }

                transaction.Commit();
            }
        }

        public void ClearMarkers(Database database)
        {
            if (database == null)
            {
                return;
            }

            using (Transaction transaction =
                database.TransactionManager.StartTransaction())
            {
                BlockTable blockTable =
                    transaction.GetObject(
                        database.BlockTableId,
                        OpenMode.ForRead) as BlockTable;

                foreach (ObjectId blockId in blockTable)
                {
                    BlockTableRecord block =
                        transaction.GetObject(
                            blockId,
                            OpenMode.ForRead) as BlockTableRecord;

                    foreach (ObjectId entityId in block)
                    {
                        Entity entity =
                            transaction.GetObject(
                                entityId,
                                OpenMode.ForRead) as Entity;

                        if (entity == null ||
                            entity.Layer != LayerName ||
                            entity.GetXDataForApplication(XDataAppName) == null)
                        {
                            continue;
                        }

                        entity.UpgradeOpen();
                        entity.Erase();
                    }
                }

                transaction.Commit();
            }
        }

        private static string BuildText(
            StandardPartCheckResult result)
        {
            string partNumber = result.BomItem == null
                ? ""
                : result.BomItem.PartNumber;

            return "标准件检查: " +
                result.Status +
                " " +
                partNumber;
        }

        private static void RegisterXDataApp(
            Database database,
            Transaction transaction)
        {
            RegAppTable table =
                transaction.GetObject(
                    database.RegAppTableId,
                    OpenMode.ForRead) as RegAppTable;

            if (table.Has(XDataAppName))
            {
                return;
            }

            table.UpgradeOpen();
            RegAppTableRecord record = new RegAppTableRecord();
            record.Name = XDataAppName;
            table.Add(record);
            transaction.AddNewlyCreatedDBObject(record, true);
        }
    }
}
