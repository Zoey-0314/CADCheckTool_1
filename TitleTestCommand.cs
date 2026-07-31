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


    public class RevisionTestCommand
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
                        "布局,标记,更改内容,更改日期,签名,变更号"
                    );






                    foreach (LayoutInfo layout in layouts)
                    {



                        //跳过模型空间

                        if (layout.IsModelSpace)
                            continue;






                        ed.WriteMessage(
                            "\n读取布局:"
                            +
                            layout.LayoutName
                        );







                        List<RevisionInfo> revisions =
                            reader.Read(
    db,
    layout.BlockTableRecordId,
    layout.Width > layout.Height
);








                        foreach (RevisionInfo rev in revisions)
                        {


                            sw.WriteLine(

                                layout.LayoutName
                                +
                                ","

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

                            );


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
        /// 防止CSV逗号导致错列
        /// </summary>
        private string Escape(
            string value)
        {


            if (string.IsNullOrEmpty(value))
                return "";



            if (value.Contains(","))
            {

                return "\""
                    + value
                    + "\"";

            }



            return value;


        }



    }


}