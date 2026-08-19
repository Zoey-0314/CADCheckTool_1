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


                    BomCalloutMarker marker =
                        new BomCalloutMarker();


                    foreach (
                        int missingNumber
                        in missingCallouts)
                    {
                        BomItem matchedItem =
                            null;


                        BomData matchedBom =
                            null;


                        // 找到这个序号真正属于哪个BOM、哪个Layout

                        foreach (
                            BomData bom
                            in boms)
                        {
                            if (bom == null ||
                                bom.Items == null)
                            {
                                continue;
                            }


                            foreach (
                                BomItem item
                                in bom.Items)
                            {
                                if (item == null)
                                    continue;


                                int itemNumber;


                                string cleaned =
                                    CadTextCleaner.Clean(
                                        item.No);


                                if (!int.TryParse(
                                        cleaned,
                                        NumberStyles.None,
                                        CultureInfo.InvariantCulture,
                                        out itemNumber))
                                {
                                    continue;
                                }


                                if (itemNumber !=
                                    missingNumber)
                                {
                                    continue;
                                }


                                matchedItem =
                                    item;


                                matchedBom =
                                    bom;


                                break;
                            }


                            if (matchedItem != null)
                            {
                                break;
                            }
                        }


                        if (matchedItem == null ||
                            matchedBom == null)
                        {
                            continue;
                        }


                        // 关键修正：
                        //
                        // 不再使用database.CurrentSpaceId。
                        //
                        // 使用这个BOM真正所属的Layout。

                        ObjectId spaceId =
                            GetLayoutSpaceId(
                                database,
                                transaction,
                                matchedBom.SourceLayoutName);


                        BomCalloutIssue issue =
                            new BomCalloutIssue
                            {
                                Number =
                                    missingNumber,

                                Position =
                                    matchedItem
                                        .NoCellPosition,

                                SpaceId =
                                    spaceId,

                                LayoutName =
                                    matchedBom
                                        .SourceLayoutName,

                                Message =
                                    "图中缺少序号："
                                    + missingNumber
                            };


                        marker.Create(
                            database,
                            transaction,
                            spaceId,
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
                        ObjectId spaceId =
    GetLayoutSpaceId(
        database,
        transaction,
        location.LayoutName);
                        if (spaceId.IsNull ||
    !spaceId.IsValid)
                        {
                            continue;
                        }


                        marker.Create(
                            database,
                            transaction,
                            spaceId,
                            layerId,
                            location,
                            expectedProject);
                    }

                    transaction.Commit();
                }
            }
            catch (System.Exception ex)
            {
                AppLogger.Error(ex, "MarkerManager.CreateProjectMarkers");
            }
        }

        public void CreateTextHeightMarkers(
    Database database,
    List<TextHeightIssue> issues)
        {
            if (database == null ||
                issues == null ||
                issues.Count == 0)
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


                    foreach (
                        TextHeightIssue issue
                        in issues)
                    {
                        if (issue == null)
                            continue;


                        // 关键：
                        //
                        // 每一个错误都找到它真正所属Layout

                        ObjectId spaceId =
                            GetLayoutSpaceId(
                                database,
                                transaction,
                                issue.LayoutName);


                        if (spaceId.IsNull ||
                            !spaceId.IsValid)
                        {
                            continue;
                        }


                        BlockTableRecord space =
                            transaction.GetObject(
                                spaceId,
                                OpenMode.ForWrite)
                            as BlockTableRecord;


                        if (space == null)
                            continue;


                        Point3d markerPosition =
                            issue.Position;


                        int markerIndex =
                            0;


                        bool isDrawingNumberIssue =
                            issue.Message != null &&
                            issue.Message.StartsWith(
                                "图号文字高度错误",
                                System.StringComparison.Ordinal);


                        if (isDrawingNumberIssue)
                        {
                            string markerKey =
                                (issue.LayoutName ?? "")
                                + "|DrawingNumber";


                            markerIndex =
                                RegisterTextHeightMarker(
                                    markerKey);
                        }


                        // 保持错位处理

                        if (isDrawingNumberIssue &&
                            markerIndex >= 2)
                        {
                            markerPosition =
                                new Point3d(
                                    issue.Position.X,
                                    issue.Position.Y + 10,
                                    issue.Position.Z);
                        }


                        DBText text =
                            new DBText();


                        text.SetDatabaseDefaults(
                            database);


                        text.Position =
                            markerPosition;


                        text.Height =
                            MarkerConfig.TextHeight;


                        text.TextString =
                            issue.Message ?? "";


                        text.LayerId =
                            layerId;


                        text.Color =
                            Color.FromRgb(
                                255,
                                0,
                                0);


                        // 写进这个issue自己的Layout

                        space.AppendEntity(
                            text);


                        transaction
                            .AddNewlyCreatedDBObject(
                                text,
                                true);


                        text.XData =
                            new ResultBuffer(
                                new TypedValue(
                                    (int)
                                        DxfCode
                                            .ExtendedDataRegAppName,
                                    XDataAppName),

                                new TypedValue(
                                    (int)
                                        DxfCode
                                            .ExtendedDataAsciiString,
                                    "TextHeight"));
                    }


                    transaction.Commit();
                }
            }
            catch (System.Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "MarkerManager.CreateTextHeightMarkers");
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
        // 新版：
        // 根据已经绑定Layout的Issue绘制缺少序号Marker。

        public void CreateMissingCalloutMarkers(
            Database database,
            List<BomCalloutIssue> issues)
        {
            if (database == null ||
                issues == null ||
                issues.Count == 0)
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


                    BomCalloutMarker marker =
                        new BomCalloutMarker();


                    foreach (
                        BomCalloutIssue issue
                        in issues)
                    {
                        if (issue == null ||
                            string.IsNullOrWhiteSpace(
                                issue.LayoutName))
                        {
                            continue;
                        }


                        ObjectId spaceId =
                            GetLayoutSpaceId(
                                database,
                                transaction,
                                issue.LayoutName);


                        if (spaceId.IsNull ||
                            !spaceId.IsValid)
                        {
                            continue;
                        }


                        issue.SpaceId =
                            spaceId;


                        marker.Create(
                            database,
                            transaction,
                            spaceId,
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
        // 新版：
        // 图中多余序号已经绑定所属Layout和位置。
        //
        // 实体本身位于ModelSpace，
        // 所以Marker仍然画到ModelSpace。

        public void CreateExtraCalloutMarkers(
            Database database,
            List<BomCalloutIssue> issues)
        {
            if (database == null ||
                issues == null ||
                issues.Count == 0)
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


                    BomCalloutMarker marker =
                        new BomCalloutMarker();


                    ObjectId modelSpaceId =
                        SymbolUtilityServices
                            .GetBlockModelSpaceId(
                                database);


                    if (modelSpaceId.IsNull ||
                        !modelSpaceId.IsValid)
                    {
                        return;
                    }


                    foreach (
                        BomCalloutIssue issue
                        in issues)
                    {
                        if (issue == null)
                        {
                            continue;
                        }


                        issue.SpaceId =
                            modelSpaceId;


                        marker.CreateExtraMarker(
                            database,
                            transaction,
                            modelSpaceId,
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
                    "MarkerManager.CreateExtraCalloutMarkers");
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
        /// 为“归档中不存在”的AB非标件创建标记。
        /// 继续使用：
        /// CADCHECK_MARKER
        /// 因此“清除检查标记”
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


                        // 标记放在Part No.单元格旁边

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


                        // 找到这个BOM真正所在的Layout

                        ObjectId spaceId =
                            GetLayoutSpaceId(
                                database,
                                transaction,
                                result.SourceLayoutName);


                        // 复用StandardPartMarker绘图。
                        //
                        // 只改变XData类型。

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
        /// 为“非标件号不存在”创建检查标记。
        /// 例如BOM：
        /// AB333T1
        /// 归档图纸中没有：
        /// AB333T + _1
        /// 则在当前BOM的AB333T1旁边提示。
        /// 继续使用CADCHECK_MARKER，
        /// 所以清除检查标记功能可以一起清除。
        /// </summary>
        public void CreateNonStandardPartNumberMarkers(
            Database database,
            List<NonStandardPartNumberCheckResult> results)
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
                        NonStandardPartNumberCheckResult result
                        in results)
                    {
                        if (result == null ||
                            result.BomItem == null)
                        {
                            continue;
                        }


                        // 提示放在当前BOM件号旁边

                        MarkerInfo info =
                             new MarkerInfo
                            {
                                Text =
                                result.Message,

                                Position =
                                result.MarkerPosition
                             };


                        // 写到当前BOM真正所在Layout

                        ObjectId spaceId =
                            GetLayoutSpaceId(
                                database,
                                transaction,
                                result.SourceLayoutName);


                        // 继续复用现有红色MText标记

                        marker.Create(
                            database,
                            transaction,
                            spaceId,
                            layerId,
                            info,
                            "NonStandardPartNumber");
                    }


                    transaction.Commit();
                }
            }
            catch (System.Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "MarkerManager.CreateNonStandardPartNumberMarkers");
            }
        }

        /// <summary>
        /// 创建版本号检查提示。
        /// 继续使用：
        /// CADCHECK_MARKER
        /// 所以“清除检查标记”
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


                    // 直接复用现有MText提示绘制器

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


                        // 必须写到版本号所在Layout

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
                            "VersionCheck",
                            5.0,
                            3.5,
                            true);
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
            if (database == null ||
                transaction == null)
            {
                return ObjectId.Null;
            }


            // Layout来源不明确：
            //
            // 宁可不画，
            // 也绝对不能画到当前Layout。

            if (string.IsNullOrWhiteSpace(
                    layoutName))
            {
                AppLogger.Info(
                    "跳过标记：LayoutName为空",
                    "MarkerManager.GetLayoutSpaceId");

                return ObjectId.Null;
            }


            DBDictionary layoutDictionary =
                transaction.GetObject(
                    database.LayoutDictionaryId,
                    OpenMode.ForRead)
                as DBDictionary;


            if (layoutDictionary == null)
            {
                return ObjectId.Null;
            }


            foreach (
                DBDictionaryEntry entry
                in layoutDictionary)
            {
                Layout layout =
                    transaction.GetObject(
                        entry.Value,
                        OpenMode.ForRead)
                    as Layout;


                if (layout == null)
                {
                    continue;
                }


                if (string.Equals(
                        layout.LayoutName,
                        layoutName,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    return
                        layout.BlockTableRecordId;
                }
            }


            AppLogger.Info(
                "跳过标记：找不到Layout："
                + layoutName,
                "MarkerManager.GetLayoutSpaceId");


            // 找不到目标布局时禁止回退到当前空间，避免标记写入错误布局。

            return ObjectId.Null;
        }
    }
}
