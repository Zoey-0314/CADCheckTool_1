using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.QuickRevision.Writers;

namespace Correct_test1.QuickRevision.Services
{
    /// <summary>
    /// 清除QuickRevision生成内容。
    ///
    /// 只清除：
    /// CADCHECK_REVISION
    /// 图层中的实体。
    ///
    /// 不清除其他检查标记。
    /// 不删除图层本身。
    /// </summary>
    public class QuickRevisionClearService
    {
        /// <summary>
        /// 清除当前Document中的所有划改实体。
        ///
        /// 返回删除数量。
        /// </summary>
        public int Clear(
            Document document)
        {
            if (document == null)
                return 0;


            Database database =
                document.Database;


            if (database == null)
                return 0;


            int erasedCount =
                0;


            using (
                DocumentLock documentLock =
                    document.LockDocument())
            {
                using (
                    Transaction transaction =
                        database
                            .TransactionManager
                            .StartTransaction())
                {
                    //--------------------------------
                    // 查找CADCHECK_REVISION图层
                    //--------------------------------

                    LayerTable layerTable =
                        transaction.GetObject(
                            database.LayerTableId,
                            OpenMode.ForRead)
                        as LayerTable;


                    if (layerTable == null)
                        return 0;


                    if (!layerTable.Has(
                            RevisionEntityHelper
                                .RevisionLayerName))
                    {
                        return 0;
                    }


                    ObjectId revisionLayerId =
                        layerTable[
                            RevisionEntityHelper
                                .RevisionLayerName];


                    //--------------------------------
                    // 遍历所有BlockTableRecord。
                    //
                    // 包括：
                    // Model Space
                    // 所有Paper Space
                    //--------------------------------

                    BlockTable blockTable =
                        transaction.GetObject(
                            database.BlockTableId,
                            OpenMode.ForRead)
                        as BlockTable;


                    if (blockTable == null)
                        return 0;


                    foreach (
                        ObjectId blockRecordId
                        in blockTable)
                    {
                        BlockTableRecord blockRecord =
                            transaction.GetObject(
                                blockRecordId,
                                OpenMode.ForRead)
                            as BlockTableRecord;


                        if (blockRecord == null)
                            continue;


                        foreach (
                            ObjectId entityId
                            in blockRecord)
                        {
                            Entity entity;


                            try
                            {
                                entity =
                                    transaction.GetObject(
                                        entityId,
                                        OpenMode.ForRead,
                                        false)
                                    as Entity;
                            }
                            catch (System.Exception)
                            {
                                continue;
                            }


                            if (entity == null ||
                                entity.IsErased)
                            {
                                continue;
                            }


                            //--------------------------------
                            // 只清CADCHECK_REVISION层
                            //--------------------------------

                            if (entity.LayerId !=
                                revisionLayerId)
                            {
                                continue;
                            }


                            try
                            {
                                entity.UpgradeOpen();


                                entity.Erase();


                                erasedCount++;
                            }
                            catch (System.Exception)
                            {
                                //--------------------------------
                                // 单个实体删除失败，
                                // 不影响其他实体。
                                //--------------------------------
                            }
                        }
                    }


                    transaction.Commit();
                }
            }


            try
            {
                document.Editor.Regen();
            }
            catch (System.Exception)
            {
            }


            return erasedCount;
        }
    }
}