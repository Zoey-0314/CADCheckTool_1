using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;


using Correct_test1.Models;
using Correct_test1.Readers;


using System.Collections.Generic;
using System.IO;
using System.Text;


using WinForms = System.Windows.Forms;



namespace Correct_test1
{


    public class TitleTestCommand
    {



        [CommandMethod("TESTREVISION")]
        public void TestRevision()
        {


            Document doc =
                Application.DocumentManager
                .MdiActiveDocument;



            if (doc == null)
                return;



            Database db =
                doc.Database;



            Editor ed =
                doc.Editor;





            try
            {


                //读取布局

                LayoutReader layoutReader =
                    new LayoutReader();




                List<LayoutInfo> layouts =
                    layoutReader.ReadLayouts(
                        db,
                        ed
                    );





                RevisionTableReader reader =
                    new RevisionTableReader();






                string path =
                    @"D:\Revision_Test.csv";






                using (StreamWriter sw =
                    new StreamWriter(
                        path,
                        false,
                        Encoding.UTF8))
                {



                    sw.WriteLine(
                        "布局,类型,标记,更改内容,更改日期,签名,变更号,右标记,右更改内容,右日期,右签名,右变更号"
                    );






                    foreach (LayoutInfo layout in layouts)
                    {


                        //跳过模型空间

                        if (layout.IsModelSpace)
                            continue;






                        // 根据修改记录表中的“标记”数量判断方向

                        List<TitleText> layoutTexts =
                            reader.ReadAllTexts(
                                db,
                                layout.BlockTableRecordId
                            );


                        int markCount =
                            0;


                        foreach (TitleText text in layoutTexts)
                        {

                            if (text.Text.Trim() == "标记")
                            {
                                markCount++;
                            }

                        }



                        bool isHorizontal =
                            markCount >= 2;



                        ed.WriteMessage(
                            "\n标记数量:"
                            +
                            markCount
                        );






                        ed.WriteMessage(
                            "\n===================="
                        );


                        ed.WriteMessage(
                            "\n布局:"
                            +
                            layout.LayoutName
                        );


                        ed.WriteMessage(
                            "\n宽:"
                            +
                            layout.Width
                            +
                            " 高:"
                            +
                            layout.Height
                        );




                        ed.WriteMessage(
                            "\n判断方向:"
                            +
                            (
                            isHorizontal
                            ?
                            "横版"
                            :
                            "竖版"
                            )
                        );









                        //========================
                        // 横版
                        //========================


                        if (isHorizontal)
                        {



                            List<HorizontalRevisionRow> rows =
                                reader.ReadHorizontalRows(
                                    db,
                                    layout.BlockTableRecordId
                                );






                            ed.WriteMessage(
                                "\n横版读取数量:"
                                +
                                rows.Count
                            );







                            foreach (
                                HorizontalRevisionRow row
                                in rows)
                            {



                                sw.WriteLine(

                                    layout.LayoutName
                                    +
                                    ",横版,"
                                    +

                                    Escape(row.Left.Mark)
                                    +
                                    ","
                                    +

                                    Escape(row.Left.Description)
                                    +
                                    ","
                                    +

                                    Escape(row.Left.Date)
                                    +
                                    ","
                                    +

                                    Escape(row.Left.Signer)
                                    +
                                    ","
                                    +

                                    Escape(row.Left.RevisionNumber)
                                    +
                                    ","
                                    +

                                    Escape(row.Right.Mark)
                                    +
                                    ","
                                    +

                                    Escape(row.Right.Description)
                                    +
                                    ","
                                    +

                                    Escape(row.Right.Date)
                                    +
                                    ","
                                    +

                                    Escape(row.Right.Signer)
                                    +
                                    ","
                                    +

                                    Escape(row.Right.RevisionNumber)

                                );



                            }




                        }









                        //========================
                        // 竖版
                        //========================

                        else
                        {



                            List<RevisionInfo> revisions =
                                reader.ReadVertical(
                                    db,
                                    layout.BlockTableRecordId
                                );






                            ed.WriteMessage(
                                "\n竖版读取数量:"
                                +
                                revisions.Count
                            );








                            foreach (
                                RevisionInfo rev
                                in revisions)
                            {



                                sw.WriteLine(

                                    layout.LayoutName
                                    +
                                    ",竖版,"
                                    +

                                    Escape(rev.Mark)
                                    +
                                    ","
                                    +

                                    Escape(rev.Description)
                                    +
                                    ","
                                    +

                                    Escape(rev.Date)
                                    +
                                    ","
                                    +

                                    Escape(rev.Signer)
                                    +
                                    ","
                                    +

                                    Escape(rev.RevisionNumber)
                                    +
                                    ",,,,,"

                                );



                            }



                        }





                    }





                }








                WinForms.MessageBox.Show(
                    "完成\nCSV文件:\n"
                    +
                    path,
                    "修改记录测试"
                );




            }



            catch (System.Exception ex)
            {


                WinForms.MessageBox.Show(
                    ex.Message,
                    "错误"
                );


            }



        }









        /// <summary>
        /// CSV转义
        /// </summary>

        private string Escape(
            string value)
        {


            if (string.IsNullOrEmpty(value))
                return "";





            if (value.Contains(","))
            {

                return "\""
                    +
                    value
                    +
                    "\"";

            }





            return value;


        }



    }



}