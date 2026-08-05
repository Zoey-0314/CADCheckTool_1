using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using System.Collections.Generic;
using System.IO;


namespace Correct_test1.Markers
{

    /// <summary>
    /// 标题栏图号错误标记
    ///
    /// 功能:
    /// 1. 创建TITLEBLOCK_CHECK图层
    /// 2. 绘制绿色错误框
    /// 3. 添加修改提示文字
    /// 4. 清除自身产生的标记
    ///
    /// 不负责:
    /// 图号判断
    /// </summary>
    public class TitleBlockDrawingNumberMarker
    {

        private const string LayerName =
            "TITLEBLOCK_CHECK";



        /// <summary>
        /// 绘制标题栏图号错误标记
        /// 
        /// 注意:
        /// 批量检查不能直接使用传入blockId
        /// 需要根据layoutName重新获取真实布局空间
        /// </summary>
        public void DrawMarker(

            Database db,

            string layoutName,

            bool isHorizontal,

            string correctNumber

        )
        {


            File.AppendAllText(
                @"D:\title_debug.txt",
                "\n进入DrawMarker"
                + "\n布局:"
                + layoutName
                + "\n正确图号:"
                + correctNumber
                + "\n横竖:"
                + isHorizontal
            );



            try
            {
                using (Transaction tr =
                    db.TransactionManager.StartTransaction())
                {


                    //--------------------------------
                    // 根据布局名称获取真实布局空间
                    //--------------------------------

                    DBDictionary layoutDict =
                        tr.GetObject(
                            db.LayoutDictionaryId,
                            OpenMode.ForRead
                        )
                        as DBDictionary;



                    if (!layoutDict.Contains(layoutName))
                    {

                        File.AppendAllText(
                            @"D:\title_debug.txt",
                            "\n不存在布局:"
                            + layoutName
                        );


                        return;

                    }




                    Layout layout =
                        tr.GetObject(
                            layoutDict.GetAt(layoutName),
                            OpenMode.ForRead
                        )
                        as Layout;




                    if (layout == null)
                    {

                        File.AppendAllText(
                            @"D:\title_debug.txt",
                            "\nLayout为空"
                        );


                        return;

                    }




                    BlockTableRecord btr =
                        tr.GetObject(
                            layout.BlockTableRecordId,
                            OpenMode.ForWrite
                        )
                        as BlockTableRecord;




                    if (btr == null)
                    {

                        File.AppendAllText(
                            @"D:\title_debug.txt",
                            "\nBTR为空"
                        );


                        return;

                    }



                    File.AppendAllText(
                        @"D:\title_debug.txt",
                        "\n真实BTR:"
                        + btr.Name
                    );




                    //--------------------------------
                    // 创建图层
                    //--------------------------------

                    // Ensure layer exists and get its id (compatible with batch mode)
                    ObjectId layerId = CreateLayer(
                        db,
                        tr
                    );



                    double x1;
                    double x2;
                    double y1;
                    double y2;



                    //--------------------------------
                    // 图号区域坐标
                    //--------------------------------

                    if (isHorizontal)
                    {

                        // 横版

                        x1 = 389.8816;
                        x2 = 449.8816;

                        y1 = 69.3206;
                        y2 = 77.3206;

                    }
                    else
                    {

                        // 竖版

                        x1 = 232.7611;
                        x2 = 282.7611;

                        y1 = 97.4386;
                        y2 = 105.4386;

                    }



                    File.AppendAllText(
                        @"D:\title_debug.txt",
                        "\n坐标:"
                        + x1
                        + ","
                        + y1
                        + " -> "
                        + x2
                        + ","
                        + y2
                    );



                    //--------------------------------
                    // 创建绿色矩形
                    //--------------------------------


                    Polyline rect =
                        new Polyline();


                    rect.AddVertexAt(
                        0,
                        new Point2d(x1, y1),
                        0,
                        0,
                        0
                    );


                    rect.AddVertexAt(
                        1,
                        new Point2d(x2, y1),
                        0,
                        0,
                        0
                    );


                    rect.AddVertexAt(
                        2,
                        new Point2d(x2, y2),
                        0,
                        0,
                        0
                    );


                    rect.AddVertexAt(
                        3,
                        new Point2d(x1, y2),
                        0,
                        0,
                        0
                    );


                    rect.Closed =
                        true;


                    // assign layer by id to be robust in batch Database scenarios
                    rect.LayerId = layerId;

                    rect.Color =
                        Color.FromRgb(
                            0,
                            255,
                            0
                        );

                    // set database defaults
                    rect.SetDatabaseDefaults(db);

                    btr.AppendEntity(rect);
                    tr.AddNewlyCreatedDBObject(
                        rect,
                        true
                    );

                    File.AppendAllText(
                        @"D:\title_debug.txt",
                        "\n矩形加入成功"
                        + "\n矩形创建标志:" + (rect.ObjectId != ObjectId.Null)
                        + "\n矩形ObjectId:" + rect.ObjectId.ToString()
                    );


                    //--------------------------------
                    // 创建提示文字
                    //--------------------------------


                    DBText text =
                        new DBText();


                    text.TextString =
                        "应改为:"
                        +
                        correctNumber;


                    text.Position =
                        new Point3d(
                            x2 + 5,
                            y2,
                            0
                        );


                    text.Height =
                        3;


                    text.LayerId = layerId;

                    text.Color =
                        Color.FromRgb(
                            0,
                            255,
                            0
                        );

                    text.SetDatabaseDefaults(db);

                    btr.AppendEntity(text);
                    tr.AddNewlyCreatedDBObject(
                        text,
                        true
                    );

                    File.AppendAllText(
                        @"D:\title_debug.txt",
                        "\n文字加入成功:"
                        + text.TextString
                        + "\n文字创建标志:" + (text.ObjectId != ObjectId.Null)
                        + "\n文字ObjectId:" + text.ObjectId.ToString()
                    );


                    tr.Commit();


                    File.AppendAllText(
                        @"D:\title_debug.txt",
                        "\nCommit成功"
                    );

                }
            }
            catch (System.Exception ex)
            {
                File.AppendAllText(@"D:\title_debug.txt", ex.ToString());
                throw;
            }

        }
        //--------------------------------
        // 创建检查图层
        //--------------------------------

        private ObjectId CreateLayer(
            Database db,
            Transaction tr)
        {

            LayerTable lt =
                tr.GetObject(
                    db.LayerTableId,
                    OpenMode.ForRead
                )
                as LayerTable;


            if (lt.Has(LayerName))
            {
                return lt[LayerName];
            }


            lt.UpgradeOpen();


            LayerTableRecord layer =
                new LayerTableRecord();


            layer.Name =
                LayerName;


            layer.Color =
                Color.FromRgb(
                    0,
                    255,
                    0
                );

            ObjectId layerId = lt.Add(layer);

            tr.AddNewlyCreatedDBObject(
                layer,
                true
            );

            return layerId;

        }



        /// <summary>
        /// 清除标题栏检查标记
        ///
        /// 保留 TITLEBLOCK_CHECK 图层
        /// 只删除该图层中的实体
        /// 与 RevisionMarker 保持一致
        /// </summary>
        public void ClearMarkers(
            Database db)
        {


            using (Transaction tr =
                db.TransactionManager.StartTransaction())
            {


                BlockTable bt =
                    tr.GetObject(
                        db.BlockTableId,
                        OpenMode.ForRead
                    )
                    as BlockTable;



                foreach (ObjectId btrId in bt)
                {


                    BlockTableRecord btr =
                        tr.GetObject(
                            btrId,
                            OpenMode.ForWrite
                        )
                        as BlockTableRecord;



                    if (btr == null)
                        continue;


                    List<ObjectId> remove =
                        new List<ObjectId>();


                    foreach (ObjectId entId in btr)
                    {


                        Entity ent =
                            tr.GetObject(
                                entId,
                                OpenMode.ForRead
                            )
                            as Entity;


                        if (ent != null &&
                           ent.Layer == LayerName)
                        {

                            remove.Add(entId);

                        }

                    }



                    foreach (ObjectId id in remove)
                    {


                        Entity ent =
                            tr.GetObject(
                                id,
                                OpenMode.ForWrite
                            )
                            as Entity;


                        if (ent != null)
                        {

                            ent.Erase();

                        }

                    }

                }


                tr.Commit();

            }

        }


    }

}