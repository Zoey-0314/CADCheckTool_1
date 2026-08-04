using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Correct_test1.Models;
using System.Collections.Generic;

namespace Correct_test1.Readers
{
    /// <summary>
    /// CAD布局读取器
    /// 1.读取所有Layout
    /// 2.获取BlockTableRecord
    /// 3.获取布局整体范围
    /// </summary>
    public class LayoutReader
    {
        public List<LayoutInfo> ReadLayouts(
            Database db,
            Editor ed)
        {
            List<LayoutInfo> result = new List<LayoutInfo>();

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                DBDictionary layoutDict = trans.GetObject(db.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;
                if (layoutDict == null)
                    return result;

                foreach (DBDictionaryEntry entry in layoutDict)
                {
                    Autodesk.AutoCAD.DatabaseServices.Layout cadLayout = trans.GetObject(entry.Value, OpenMode.ForRead) as Autodesk.AutoCAD.DatabaseServices.Layout;
                    if (cadLayout == null)
                        continue;

                    LayoutInfo info = new LayoutInfo();

                    // 基本信息
                    info.LayoutName = cadLayout.LayoutName;
                    info.BlockTableRecordId = cadLayout.BlockTableRecordId;
                    info.IsModelSpace = cadLayout.ModelType;
                    info.IsValidDrawing = false;

                    BlockTableRecord btr = trans.GetObject(cadLayout.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
                    if (btr == null)
                        continue;

                    // 计算布局整体范围
                    Extents3d? totalExtents = null;
                    foreach (ObjectId id in btr)
                    {
                        Entity ent = trans.GetObject(id, OpenMode.ForRead) as Entity;
                        if (ent == null)
                            continue;

                        try
                        {
                            Extents3d ext = ent.GeometricExtents;
                            if (totalExtents == null)
                            {
                                totalExtents = ext;
                            }
                            else
                            {
                                Extents3d temp = totalExtents.Value;
                                temp.AddExtents(ext);
                                totalExtents = temp;
                            }
                        }
                        catch
                        {
                            // 部分CAD对象没有范围
                            continue;
                        }
                    }

                    if (totalExtents != null)
                    {
                        Extents3d ext = totalExtents.Value;
                        info.MinX = ext.MinPoint.X;
                        info.MinY = ext.MinPoint.Y;
                        info.Width = ext.MaxPoint.X - ext.MinPoint.X;
                        info.Height = ext.MaxPoint.Y - ext.MinPoint.Y;
                    }

                    result.Add(info);

                    if (ed != null)
                    {
                        ed.WriteMessage(
                            "\n布局:" + info.LayoutName + " Model:" + info.IsModelSpace + " 宽:" + info.Width + " 高:" + info.Height
                        );
                    }
                }

                trans.Commit();
            }

            return result;
        }
    }
}