using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Correct_test1.Configs;
using Correct_test1.Core;
using Correct_test1.Models;
using Correct_test1.Readers;
using System.Collections.Generic;
using System.Globalization;
using Correct_test1.VersionCheck.Models;

namespace Correct_test1.Markers
{
    public class MarkerManager : MarkerBase
    {
        public const string LayerName = "CADCHECK_MARKER";
        public const string XDataAppName = "CADCHECK_MARKER";

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

                        ObjectId spaceId =
                            GetLayoutSpaceId(
                                database,
                                transaction,
                                result.SourceLayoutName);

                        marker.Create(
                            database,
                            transaction,
                            spaceId,
                            layerId,
                            info);
                    }

                    transaction.Commit();
                }
            }

            catch (System.Exception ex)
            {
                AppLogger.Error(ex, "MarkerManager.CreateMarkers");
            }
        }

        public void CreateExtraCalloutMarkers(
            Database database,
            HashSet<int> extraCallouts,
            List<TitleText> texts)
        {
            if (database == null ||
                extraCallouts == null ||
                extraCallouts.Count == 0 ||
                texts == null ||
                texts.Count == 0)
            {
                return;
            }

            try
            {
                using (Transaction transaction =
                    database.TransactionManager.StartTransaction())
                {
                    ObjectId layerId = EnsureLayer(
                        database,
                        transaction,
                        LayerName,
                        Color.FromRgb(255, 0, 0));
                    RegisterXDataApp(database, transaction);
                    BomCalloutMarker marker =
                        new BomCalloutMarker();

                    foreach (TitleText text in texts)
                    {
                        if (text == null ||
                            string.IsNullOrWhiteSpace(text.Text))
                        {
                            continue;
                        }

                        foreach (string numericText in
                            LayoutReader.SplitNumericTexts(text.Text))
                        {
                            int number;

                            if (!int.TryParse(
                                    numericText,
                                    NumberStyles.None,
                                    CultureInfo.InvariantCulture,
                                    out number))
                            {
                                continue;
                            }

                            if (!extraCallouts.Contains(number))
                            {
                                continue;
                            }

                            ObjectId spaceId =
                                SymbolUtilityServices.GetBlockModelSpaceId(database);

                            BomCalloutIssue issue =
                                new BomCalloutIssue
                                {
                                    Number = number,
                                    LayoutName = text.LayoutName,
                                    Position = new Point3d(
                                        text.X,
                                        text.Y,
                                        0),
                                    SpaceId = spaceId,
                                    Message = "序号错误：不在BOM中"
                                };

                            marker.CreateExtraMarker(
                                database,
                                transaction,
                                spaceId,
                                layerId,
                                issue);

                            break;
                        }
                    }

                    transaction.Commit();
                }
            }
            catch (System.Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "MarkerManager.CreateExtraCalloutMarkers");
            }
        }

        public void CreateMissingCalloutMarkers(
            Database database,
            HashSet<int> missingCallouts,
            List<BomData> boms)
        {
            if (database == null ||
                missingCallouts == null ||
                missingCallouts.Count == 0 ||
                boms == null ||
                boms.Count == 0)
            {
                return;
            }

            try
            {
                using (Transaction transaction =
                    database.TransactionManager.StartTransaction())
                {
                    ObjectId layerId = EnsureLayer(
                        database,
                        transaction,
                        LayerName,
                        Color.FromRgb(255, 0, 0));
                    RegisterXDataApp(database, transaction);
                    BomCalloutMarker marker = new BomCalloutMarker();

                    foreach (int missingNumber in missingCallouts)
                    {
                        BomItem matchedItem = FindBomItemByNumber(
                            boms,
                            missingNumber);

                        if (matchedItem == null)
                            continue;

                        BomCalloutIssue issue = new BomCalloutIssue
                        {
                            Number = missingNumber,
                            Position = matchedItem.NoCellPosition,
                            SpaceId = database.CurrentSpaceId,
                            Message = "图中缺少序号：" + missingNumber
                        };

                        marker.Create(
                            database,
                            transaction,
                            database.CurrentSpaceId,
                            layerId,
                            issue);
                    }

                    transaction.Commit();
                }
            }
            catch (System.Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "MarkerManager.CreateMissingCalloutMarkers");
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

        public void CreateTextHeightMarkers(Database database, List<TextHeightIssue> issues)
        {
            if (database == null || issues == null || issues.Count == 0)
                return;

            try
            {
                using (Transaction transaction = database.TransactionManager.StartTransaction())
                {
                    ObjectId layerId = EnsureLayer(database, transaction, LayerName, Color.FromRgb(255, 0, 0));
                    RegisterXDataApp(database, transaction);
                    BlockTableRecord space = transaction.GetObject(
                        database.CurrentSpaceId,
                        OpenMode.ForWrite) as BlockTableRecord;

                    if (space == null)
                        return;

                    foreach (TextHeightIssue issue in issues)
                    {
                        if (issue == null)
                            continue;

                        Point3d markerPosition = issue.Position;
                        int markerIndex = 0;
                        bool isDrawingNumberIssue =
                            issue.Message != null &&
                            issue.Message.StartsWith(
                                "图号文字高度错误",
                                System.StringComparison.Ordinal);

                        if (isDrawingNumberIssue)
                        {
                            string markerKey =
                                (issue.LayoutName ?? "") + "|DrawingNumber";
                            markerIndex = RegisterTextHeightMarker(markerKey);
                        }

                        if (isDrawingNumberIssue &&
                            markerIndex >= 2)
                        {
                            markerPosition = new Autodesk.AutoCAD.Geometry.Point3d(
                                issue.Position.X,
                                issue.Position.Y + 10,
                                issue.Position.Z);
                        }

                        DBText text = new DBText();

                        text.SetDatabaseDefaults(database);

                        text.Position = markerPosition;
                        text.Height = MarkerConfig.TextHeight;
                        text.TextString = issue.Message ?? "";
                        text.LayerId = layerId;
                        text.Color = Color.FromRgb(255, 0, 0);
                        space.AppendEntity(text);
                        transaction.AddNewlyCreatedDBObject(text, true);
                        text.XData = new ResultBuffer(
                            new TypedValue((int)DxfCode.ExtendedDataRegAppName, XDataAppName),
                            new TypedValue((int)DxfCode.ExtendedDataAsciiString, "TextHeight"));
                    }

                    transaction.Commit();
                }
            }
            catch (System.Exception ex)
            {
                AppLogger.Error(ex, "MarkerManager.CreateTextHeightMarkers");
            }
        }

        private static readonly Dictionary<string, int> TextHeightMarkerStates =
            new Dictionary<string, int>();

        private static int RegisterTextHeightMarker(string key)
        {
            lock (TextHeightMarkerStates)
            {
                int count;
                TextHeightMarkerStates.TryGetValue(key, out count);
                count++;
                TextHeightMarkerStates[key] = count;
                return count;
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

                    lock (TextHeightMarkerStates)
                    {
                        TextHeightMarkerStates.Clear();
                    }
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

        /// <summary>
        /// 为“归档中不存在”的NS非标件创建标记。
        ///
        /// 继续使用：
        /// CADCHECK_MARKER
        ///
        /// 因此原有“清除检查标记”
        /// 可以统一清除这些标记。
        /// </summary>
        public void CreateNonStandardArchiveMarkers(
            Database database,
            List<NonStandardArchiveCheckResult> results)
        {
            if (database == null ||
                results == null ||
                results.Count == 0)
            {
                return;
            }


            try
            {
                using (
                    Transaction transaction =
                        database
                            .TransactionManager
                            .StartTransaction())
                {
                    ObjectId layerId =
                        EnsureLayer(
                            database,
                            transaction,
                            LayerName,
                            Color.FromRgb(
                                255,
                                0,
                                0));


                    RegisterXDataApp(
                        database,
                        transaction);


                    StandardPartMarker marker =
                        new StandardPartMarker();


                    foreach (
                        NonStandardArchiveCheckResult result
                        in results)
                    {
                        if (result == null ||
                            result.BomItem == null)
                        {
                            continue;
                        }


                        //--------------------------------
                        // 标记放在Part No.单元格旁边
                        //--------------------------------

                        MarkerInfo info =
                            new MarkerInfo
                            {
                                Text =
                                    result.Message,

                                Position =
                                    result
                                        .BomItem
                                        .PartNumberCellPosition
                            };


                        //--------------------------------
                        // 找到这个BOM真正所在的Layout
                        //--------------------------------

                        ObjectId spaceId =
                            GetLayoutSpaceId(
                                database,
                                transaction,
                                result.SourceLayoutName);


                        //--------------------------------
                        // 复用StandardPartMarker绘图。
                        //
                        // 只改变XData类型。
                        //--------------------------------

                        marker.Create(
                            database,
                            transaction,
                            spaceId,
                            layerId,
                            info,
                            "NonStandardArchive");
                    }


                    transaction.Commit();
                }
            }
            catch (System.Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "MarkerManager.CreateNonStandardArchiveMarkers");
            }
        }

        /// <summary>
        /// 创建版本号检查提示。
        ///
        /// 继续使用：
        /// CADCHECK_MARKER
        ///
        /// 所以原有“清除检查标记”
        /// 可以直接清除。
        /// </summary>
        public void CreateVersionMarkers(
            Database database,
            List<VersionCheckResult> results)
        {
            if (database == null ||
                results == null ||
                results.Count == 0)
            {
                return;
            }


            try
            {
                using (
                    Transaction transaction =
                        database
                            .TransactionManager
                            .StartTransaction())
                {
                    ObjectId layerId =
                        EnsureLayer(
                            database,
                            transaction,
                            LayerName,
                            Color.FromRgb(
                                255,
                                0,
                                0));


                    RegisterXDataApp(
                        database,
                        transaction);


                    //--------------------------------
                    // 直接复用现有MText提示绘制器
                    //--------------------------------

                    StandardPartMarker marker =
                        new StandardPartMarker();


                    foreach (
                        VersionCheckResult result
                        in results)
                    {
                        if (result == null)
                            continue;


                        MarkerInfo info =
                            new MarkerInfo
                            {
                                Text =
                                    result.Message,

                                Position =
                                    result.Position
                            };


                        //--------------------------------
                        // 必须写到版本号所在Layout
                        //--------------------------------

                        ObjectId spaceId =
                            GetLayoutSpaceId(
                                database,
                                transaction,
                                result.LayoutName);


                        marker.Create(
                            database,
                            transaction,
                            spaceId,
                            layerId,
                            info,
                            "VersionCheck");
                    }


                    transaction.Commit();
                }
            }
            catch (System.Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "MarkerManager.CreateVersionMarkers");
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

        private static ObjectId GetLayoutSpaceId(
            Database database,
            Transaction transaction,
            string layoutName)
        {
            if (string.IsNullOrWhiteSpace(layoutName))
                return database.CurrentSpaceId;

            DBDictionary layoutDictionary =
                transaction.GetObject(
                    database.LayoutDictionaryId,
                    OpenMode.ForRead) as DBDictionary;

            if (layoutDictionary != null)
            {
                foreach (DBDictionaryEntry entry in layoutDictionary)
                {
                    Autodesk.AutoCAD.DatabaseServices.Layout layout =
                        transaction.GetObject(
                            entry.Value,
                            OpenMode.ForRead)
                        as Autodesk.AutoCAD.DatabaseServices.Layout;

                    if (layout != null &&
                        string.Equals(
                            layout.LayoutName,
                            layoutName,
                            System.StringComparison.OrdinalIgnoreCase))
                    {
                        return layout.BlockTableRecordId;
                    }
                }
            }

            return database.CurrentSpaceId;
        }

        private static BomItem FindBomItemByNumber(
            List<BomData> boms,
            int number)
        {
            foreach (BomData bom in boms)
            {
                if (bom == null || bom.Items == null)
                    continue;

                foreach (BomItem item in bom.Items)
                {
                    if (item == null)
                        continue;

                    int itemNumber;
                    string cleaned = CadTextCleaner.Clean(item.No);
                    if (int.TryParse(
                        cleaned,
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out itemNumber) &&
                        itemNumber == number)
                    {
                        return item;
                    }
                }
            }

            return null;
        }
    }
}
