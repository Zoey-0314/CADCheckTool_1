using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Correct_test1.Core;
using Correct_test1.Models;
using Correct_test1.Readers;
using System.Collections.Generic;

namespace Correct_test1.Markers
{
    public class MarkerManager : MarkerBase
    {
        public const string LayerName = "Correct_test1_Marker";
        public const string XDataAppName = "Correct_test1_Marker";

        public void CreateMarkers(Database database, List<StandardPartCheckResult> results)
        {
            if (database == null || results == null)
                return;

            try
            {
                using (Transaction transaction = database.TransactionManager.StartTransaction())
                {
                    ObjectId layerId = EnsureLayer(database, transaction, LayerName, Color.FromRgb(255, 0, 0));
                    RegisterXDataApp(database, transaction);
                    StandardPartMarker marker = new StandardPartMarker();

                    foreach (StandardPartCheckResult result in results)
                    {
                        if (result == null || result.Status == StandardPartCheckStatus.Correct)
                            continue;

                        MarkerInfo info = new MarkerInfo
                        {
                            Result = result,
                            Text = BuildText(result),
                            Position = result.CellPosition
                        };

                        marker.Create(database, transaction, database.CurrentSpaceId, layerId, info);
                    }

                    transaction.Commit();
                }
            }
            catch (System.Exception ex)
            {
                AppLogger.Error(ex, "MarkerManager.CreateMarkers");
            }
        }

        public void CreateProjectMarkers(Database database, string currentProject, string expectedProject)
        {
            if (database == null || string.IsNullOrEmpty(currentProject) || string.IsNullOrEmpty(expectedProject) ||
                string.Equals(currentProject, expectedProject, System.StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                List<ProjectNumberLocation> locations = new ProjectReader().ReadProjectLocations(database);

                using (Transaction transaction = database.TransactionManager.StartTransaction())
                {
                    ObjectId layerId = EnsureLayer(database, transaction, LayerName, Color.FromRgb(255, 0, 0));
                    RegisterXDataApp(database, transaction);
                    ProjectNumberMarker marker = new ProjectNumberMarker();

                    foreach (ProjectNumberLocation location in locations)
                    {
                        if (!string.Equals(location.ProjectNumber, currentProject, System.StringComparison.OrdinalIgnoreCase))
                            continue;

                        marker.Create(database, transaction, database.CurrentSpaceId, layerId, location, expectedProject);
                    }

                    transaction.Commit();
                }
            }
            catch (System.Exception ex)
            {
                AppLogger.Error(ex, "MarkerManager.CreateProjectMarkers");
            }
        }

        public void ClearMarkers(Database database)
        {
            if (database == null)
                return;

            try
            {
                using (Transaction transaction = database.TransactionManager.StartTransaction())
                {
                    BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                    foreach (ObjectId blockId in blockTable)
                    {
                        BlockTableRecord block = transaction.GetObject(blockId, OpenMode.ForRead) as BlockTableRecord;
                        foreach (ObjectId entityId in block)
                        {
                            Entity entity = transaction.GetObject(entityId, OpenMode.ForRead) as Entity;
                            if (entity == null || entity.Layer != LayerName ||
                                entity.GetXDataForApplication(XDataAppName) == null)
                                continue;

                            entity.UpgradeOpen();
                            entity.Erase();
                        }
                    }

                    transaction.Commit();
                }
            }
            catch (System.Exception ex)
            {
                AppLogger.Error(ex, "MarkerManager.ClearMarkers");
            }
        }

        private static string BuildText(StandardPartCheckResult result)
        {
            switch (result.Status)
            {
                case StandardPartCheckStatus.FormatDifference:
                    return "格式错误  应该为: " + (result.CorrectPartNumber ?? "");
                case StandardPartCheckStatus.NameError:
                    return "名称错误  应该为: " + (result.CorrectName ?? "");
                case StandardPartCheckStatus.NotRegistered:
                    return "标准件未收录";
                case StandardPartCheckStatus.MultipleMatch:
                    return "存在多个匹配标准件，请确认";
                default:
                    return "";
            }
        }

        private static void RegisterXDataApp(Database database, Transaction transaction)
        {
            RegAppTable table = transaction.GetObject(database.RegAppTableId, OpenMode.ForRead) as RegAppTable;
            if (table.Has(XDataAppName))
                return;

            table.UpgradeOpen();
            RegAppTableRecord record = new RegAppTableRecord { Name = XDataAppName };
            table.Add(record);
            transaction.AddNewlyCreatedDBObject(record, true);
        }
    }
}
