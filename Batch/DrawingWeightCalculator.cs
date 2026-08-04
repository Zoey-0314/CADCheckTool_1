using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.IO;


namespace Correct_test1.Batch
{

    public class DrawingWeightCalculator
    {


        public double Calculate(
            string file)
        {

            double weight = 0;



            try
            {

                FileInfo info =
                    new FileInfo(file);



                // 文件大小权重
                weight += info.Length / 1024.0 / 1024.0;



                Database db =
                    new Database(
                        false,
                        true
                    );



                db.ReadDwgFile(
                    file,
                    FileOpenMode.OpenForReadAndAllShare,
                    false,
                    ""
                );



                using (Transaction tr =
                    db.TransactionManager.StartTransaction())
                {


                    //布局数量

                    DBDictionary layouts =
                        tr.GetObject(
                            db.LayoutDictionaryId,
                            OpenMode.ForRead
                        )
                        as DBDictionary;



                    int layoutCount =
                        layouts.Count;



                    weight +=
                        layoutCount * 5;




                    //实体数量

                    BlockTable bt =
                        tr.GetObject(
                            db.BlockTableId,
                            OpenMode.ForRead
                        )
                        as BlockTable;



                    foreach (ObjectId id in bt)
                    {

                        BlockTableRecord btr =
                            tr.GetObject(
                                id,
                                OpenMode.ForRead
                            )
                            as BlockTableRecord;


                        if (btr != null)
                        {

                            int entityCount = 0;


                            foreach (ObjectId entId in btr)
                            {
                                entityCount++;
                            }


                            weight +=
                                entityCount * 0.01;

                        }

                    }


                    tr.Commit();

                }



                db.Dispose();


            }
            catch
            {

                //读取失败给一个基础权重

                weight = 1;

            }



            //防止权重为0

            if (weight <= 0)
                weight = 1;



            return weight;


        }


    }

}