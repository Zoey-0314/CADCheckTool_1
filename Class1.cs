using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Correct_test1.Batch;
using Correct_test1.Checks;
using Correct_test1.Core;
using Correct_test1.Export;
using Correct_test1.Models;
using Correct_test1.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WinForms = System.Windows.Forms;
using static Autodesk.AutoCAD.LayerManager.LayerFilter;


namespace Correct_test1
{
    public class Class1
    {

        [CommandMethod("CHECKFOLDER")]
        public void CheckFolder()
        {


            Document doc =
                Autodesk.AutoCAD.ApplicationServices.Application
                .DocumentManager
                .MdiActiveDocument;


            Editor ed =
                doc.Editor;



            //选择文件夹
            WinForms.FolderBrowserDialog dialog =
                new WinForms.FolderBrowserDialog();


            if (dialog.ShowDialog() != WinForms.DialogResult.OK)
                return;



            string folder =
                dialog.SelectedPath;



            PromptStringOptions options =
                new PromptStringOptions(
                    "\n请输入正确项目号:"
                );



            PromptResult input =
                ed.GetString(options);



            if (input.Status != PromptStatus.OK)
                return;



            string expected =
                input.StringResult;



            BatchChecker checker =
                new BatchChecker();



            List<CheckResult> results =
                checker.CheckFolder(
                    folder,
                    expected,
                    ed
                );



            string path =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.Desktop)
                +
                "\\CAD批量项目号检查报告.csv";



            CsvExporter exporter =
                new CsvExporter();


            exporter.Export(
                results,
                path
            );
            string errorPath =
    Environment.GetFolderPath(
        Environment.SpecialFolder.Desktop)
    +
    "\\CAD错误报告.csv";


            exporter.ExportError(
                results,
                errorPath
            );




            Autodesk.AutoCAD.ApplicationServices.Application
            .ShowAlertDialog(
                "批量检查完成!\n报告:"
                +
                path
            );


        }
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



        [CommandMethod("TESTPROJECT")]
        public void TestProject()
        {
            {

                ProjectChecker checker =
                    new ProjectChecker();


                var result =
                    checker.CheckProject(
                        "N2607US004-L0",
                        "N2607US004-L0"
                    );


                Application.ShowAlertDialog(
                    result.Message
                );

            }


        }

        [CommandMethod("CHECKPROJECT")]
        public void CheckProject()
        {


            Document doc =
                Autodesk.AutoCAD.ApplicationServices.Application
                .DocumentManager
                .MdiActiveDocument;



            Editor ed = doc.Editor;



            Database db = doc.Database;



            ProjectReader reader =
                new ProjectReader();



            List<string> projects =
                reader.ReadProjects(
                    db,
                    ed
                );



            //让用户输入项目号

            PromptStringOptions options =
                new PromptStringOptions(
                    "\n请输入正确项目号:"
                );


            options.AllowSpaces = false;


            PromptResult input =
                ed.GetString(options);



            if (input.Status != PromptStatus.OK)
            {
                return;
            }


            string expected =
                input.StringResult;



            ProjectChecker checker =
                new ProjectChecker();



            foreach (string p in projects)
            {


                CheckResult result =
                    checker.CheckProject(
                        p,
                        expected
                    );



                if (!result.IsError)
                {

                    Autodesk.AutoCAD.ApplicationServices.Application
                    .ShowAlertDialog(
                        result.Message
                    );


                    return;

                }


            }



            Autodesk.AutoCAD.ApplicationServices.Application
            .ShowAlertDialog(
                "未找到正确项目号"
            );


        }
        [CommandMethod("CHECKDRAWING")]
        public void CheckDrawing()
        {


            Document doc =
                Autodesk.AutoCAD.ApplicationServices.Application
                .DocumentManager
                .MdiActiveDocument;


            Editor ed = doc.Editor;


            Database db = doc.Database;



            //输入项目号

            PromptStringOptions options =
                new PromptStringOptions(
                    "\n请输入正确项目号:"
                );


            PromptResult input =
                ed.GetString(options);



            if (input.Status != PromptStatus.OK)
                return;



            string expected =
                input.StringResult;



            //调用检查中心

            DrawingCheckManager manager =
                new DrawingCheckManager();



            List<CheckResult> results =
                manager.CheckDrawing(
                    db,
                    ed,
                    expected
                );



            //输出CSV

            string path =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.Desktop)
                +
                "\\CAD项目号检查报告.csv";



            CsvExporter exporter =
                new CsvExporter();


            exporter.Export(
                results,
                path
            );



            Autodesk.AutoCAD.ApplicationServices.Application
            .ShowAlertDialog(
                "检查完成!\n报告位置:\n"
                + path
            );


        }
    }
}