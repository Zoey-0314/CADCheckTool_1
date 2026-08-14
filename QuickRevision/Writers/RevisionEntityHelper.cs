using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

namespace Correct_test1.QuickRevision.Writers
{
    /// <summary>
    /// QuickRevision生成实体公共辅助类。
    ///
    /// 所有QuickRevision标记统一为红色。
    /// </summary>
    internal static class RevisionEntityHelper
    {
        public const string RevisionLayerName =
            "CADCHECK_REVISION";

        public const string RegAppName =
            "CADCHECK_QUICK_REVISION";


        private static Color CreateRevisionColor()
        {
            //--------------------------------
            // AutoCAD ACI 1 = Red
            //--------------------------------

            return Color.FromColorIndex(
                ColorMethod.ByAci,
                1);
        }


        /// <summary>
        /// 获取或创建QuickRevision专用图层。
        ///
        /// 已存在时也更新为红色。
        /// </summary>
        public static ObjectId EnsureRevisionLayer(
            Database database,
            Transaction transaction)
        {
            if (database == null ||
                transaction == null)
            {
                return ObjectId.Null;
            }


            LayerTable layerTable =
                transaction.GetObject(
                    database.LayerTableId,
                    OpenMode.ForRead)
                as LayerTable;


            if (layerTable == null)
                return ObjectId.Null;


            //--------------------------------
            // 图层已经存在
            //--------------------------------

            if (layerTable.Has(
                    RevisionLayerName))
            {
                ObjectId layerId =
                    layerTable[
                        RevisionLayerName];


                LayerTableRecord layer =
                    transaction.GetObject(
                        layerId,
                        OpenMode.ForWrite)
                    as LayerTableRecord;


                if (layer != null)
                {
                    layer.Color =
                        CreateRevisionColor();
                }


                return layerId;
            }


            //--------------------------------
            // 创建新图层
            //--------------------------------

            layerTable.UpgradeOpen();


            LayerTableRecord newLayer =
                new LayerTableRecord();


            newLayer.Name =
                RevisionLayerName;


            newLayer.Color =
                CreateRevisionColor();


            ObjectId newLayerId =
                layerTable.Add(
                    newLayer);


            transaction
                .AddNewlyCreatedDBObject(
                    newLayer,
                    true);


            return newLayerId;
        }


        /// <summary>
        /// 给QuickRevision新实体统一设置：
        ///
        /// 图层
        /// 红色
        /// </summary>
        public static void ApplyRevisionAppearance(
            Entity entity,
            ObjectId layerId)
        {
            if (entity == null)
                return;


            if (!layerId.IsNull &&
                layerId.IsValid)
            {
                entity.LayerId =
                    layerId;
            }


            //--------------------------------
            // 实体本身也明确设红色，
            // 不完全依赖图层颜色。
            //--------------------------------

            entity.Color =
                CreateRevisionColor();
        }


        public static void EnsureRegApp(
            Database database,
            Transaction transaction)
        {
            if (database == null ||
                transaction == null)
            {
                return;
            }


            RegAppTable regAppTable =
                transaction.GetObject(
                    database.RegAppTableId,
                    OpenMode.ForRead)
                as RegAppTable;


            if (regAppTable == null)
                return;


            if (regAppTable.Has(
                    RegAppName))
            {
                return;
            }


            regAppTable.UpgradeOpen();


            RegAppTableRecord record =
                new RegAppTableRecord();


            record.Name =
                RegAppName;


            regAppTable.Add(
                record);


            transaction
                .AddNewlyCreatedDBObject(
                    record,
                    true);
        }


        public static void ApplyXData(
            Entity entity,
            string entityType)
        {
            if (entity == null ||
                string.IsNullOrWhiteSpace(
                    entityType))
            {
                return;
            }


            using (ResultBuffer buffer =
                new ResultBuffer(
                    new TypedValue(
                        (int)
                        DxfCode
                            .ExtendedDataRegAppName,
                        RegAppName),

                    new TypedValue(
                        (int)
                        DxfCode
                            .ExtendedDataAsciiString,
                        entityType)))
            {
                entity.XData =
                    buffer;
            }
        }
    }
}