using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace Correct_test1
{
    public class Class1
    {


        [CommandMethod("CHECKALL")]
        public void CheckAll()
        {

            Document doc =
                Autodesk.AutoCAD.ApplicationServices.Application
                .DocumentManager
                .MdiActiveDocument;


            Editor ed = doc.Editor;

            Database db = doc.Database;



            List<string> csv =
                new List<string>();


            // CSV标题
            csv.Add("类型,对象名称,内容");



            int entityCount = 0;



            using (Transaction trans =
                db.TransactionManager.StartTransaction())
            {



                BlockTable bt =
                    trans.GetObject(
                        db.BlockTableId,
                        OpenMode.ForRead
                    ) as BlockTable;



                //模型空间 + 布局空间

                ObjectId[] spaces =
                {
                    bt[BlockTableRecord.ModelSpace],
                    bt[BlockTableRecord.PaperSpace]
                };




                foreach (ObjectId spaceId in spaces)
                {


                    BlockTableRecord btr =
                        trans.GetObject(
                            spaceId,
                            OpenMode.ForRead
                        ) as BlockTableRecord;



                    foreach (ObjectId id in btr)
                    {

                        Entity ent =
                            trans.GetObject(
                                id,
                                OpenMode.ForRead
                            ) as Entity;


                        if (ent == null)
                            continue;


                        entityCount++;



                        //========================
                        // 1. Table表格(BOM)
                        //========================

                        if (ent is Table table)
                        {

                            ReadTable(
                                table,
                                csv,
                                ed
                            );


                        }



                        //========================
                        // 2. 普通文字
                        //========================

                        else if (ent is DBText text)
                        {


                            csv.Add(
                                "普通文字,,"
                                + Clean(text.TextString)
                            );


                            ed.WriteMessage(
                                "\n文字:"
                                + text.TextString
                            );


                        }



                        //========================
                        // 3. 多行文字
                        //========================

                        else if (ent is MText mtext)
                        {


                            csv.Add(
                                "多行文字,,"
                                + Clean(mtext.Text)
                            );


                            ed.WriteMessage(
                                "\n多行文字:"
                                + mtext.Text
                            );


                        }



                        //========================
                        // 4. 块
                        //========================

                        else if (ent is BlockReference block)
                        {


                            ed.WriteMessage(
                                "\n发现块:"
                                + block.Name
                            );


                            csv.Add(
                                "块,"
                                + block.Name
                                + ","
                            );



                            //读取块属性

                            foreach (ObjectId attId
                                in block.AttributeCollection)
                            {


                                AttributeReference att =
                                    trans.GetObject(
                                        attId,
                                        OpenMode.ForRead
                                    ) as AttributeReference;



                                if (att != null)
                                {


                                    csv.Add(
                                        "块属性,"
                                        + block.Name
                                        + ","
                                        + att.Tag
                                        + "="
                                        + Clean(att.TextString)
                                    );


                                    ed.WriteMessage(
                                        "\n属性:"
                                        + att.Tag
                                        + "="
                                        + att.TextString
                                    );

                                }

                            }



                            //重点：
                            //进入块内部继续搜索文字

                            ReadBlockContent(
                                block,
                                trans,
                                csv,
                                ed
                            );


                        }


                    }


                }



                trans.Commit();

            }



            //========================
            // 输出CSV
            //========================


            string path =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.Desktop)
                +
                "\\AutoCAD检查结果.csv";



            File.WriteAllLines(
                path,
                csv,
                Encoding.UTF8
            );



            ed.WriteMessage(
                "\n===================="
            );

            ed.WriteMessage(
                "\n扫描完成!"
            );

            ed.WriteMessage(
                "\n扫描对象数量:"
                + entityCount
            );


            ed.WriteMessage(
                "\nCSV位置:"
                + path
            );


        }





        //================================================
        // 读取块内部内容
        //================================================

        private void ReadBlockContent(
            BlockReference block,
            Transaction trans,
            List<string> csv,
            Editor ed)
        {



            BlockTableRecord blockDef =
                trans.GetObject(
                    block.BlockTableRecord,
                    OpenMode.ForRead
                ) as BlockTableRecord;



            if (blockDef == null)
                return;



            foreach (ObjectId id in blockDef)
            {



                Entity ent =
                    trans.GetObject(
                        id,
                        OpenMode.ForRead
                    ) as Entity;



                if (ent == null)
                    continue;




                //块内部文字

                if (ent is DBText text)
                {


                    csv.Add(
                        "块内部文字,"
                        + block.Name
                        + ","
                        + Clean(text.TextString)
                    );


                    ed.WriteMessage(
                        "\n块文字:"
                        + text.TextString
                    );


                }



                //块内部多行文字

                else if (ent is MText mtext)
                {


                    csv.Add(
                        "块内部多行文字,"
                        + block.Name
                        + ","
                        + Clean(mtext.Text)
                    );


                    ed.WriteMessage(
                        "\n块MText:"
                        + mtext.Text
                    );

                }



                //嵌套块

                else if (ent is BlockReference childBlock)
                {


                    ReadBlockContent(
                        childBlock,
                        trans,
                        csv,
                        ed
                    );

                }


            }


        }





        //================================================
        // 读取表格
        //================================================

        private void ReadTable(
            Table table,
            List<string> csv,
            Editor ed)
        {


            csv.Add(
                "BOM表,,开始"
            );



            ed.WriteMessage(
                "\n发现BOM表"
            );



            for (int r = 0;
                r < table.Rows.Count;
                r++)
            {


                string row = "";



                for (int c = 0;
                    c < table.Columns.Count;
                    c++)
                {


                    string value =
                        table.Cells[r, c]
                        .TextString;



                    row +=
                        Clean(value)
                        + " ";


                }



                csv.Add(
                    "BOM,"
                    + r
                    + ","
                    + row
                );



                ed.WriteMessage(
                    "\nBOM:"
                    + row
                );

            }


        }





        //================================================
        // 清理CSV特殊字符
        //================================================

        private string Clean(string text)
        {

            if (text == null)
                return "";


            return text
                .Replace(",", "，")
                .Replace("\n", " ")
                .Replace("\r", " ");

        }


    }
}