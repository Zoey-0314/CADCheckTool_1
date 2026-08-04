using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

using System;
using System.Collections.Generic;
using System.IO;

using Correct_test1.Core;
using Correct_test1.Models;


namespace Correct_test1.Batch
{

    public class BatchChecker_old
    {


        public List<CheckResult> CheckFolder(
            string folder,
            string expectedProject,
            Editor ed)
        {


            List<CheckResult> allResults =
                new List<CheckResult>();



            string[] files =
                Directory.GetFiles(
                    folder,
                    "*.dwg",
                    SearchOption.AllDirectories
                );



            foreach (string file in files)
            {


                ed.WriteMessage(
                    "\n正在检查:"
                    + file
                );



                Database db =
                    new Database(
                        false,
                        true
                    );


                try
                {


                    db.ReadDwgFile(
                        file,
                        FileOpenMode.OpenForReadAndAllShare,
                        true,
                        ""
                    );



                    DrawingCheckManager manager =
                        new DrawingCheckManager();



                    List<CheckResult> results =
                        manager.CheckDrawing(
                            db,
                            ed,
                            expectedProject
                        );



                    foreach (CheckResult r in results)
                    {

                        r.FilePath = file;

                        r.FileName =
                            Path.GetFileName(file);


                        allResults.Add(r);

                    }


                    db.CloseInput(true);


                }

                catch (Exception ex)
                {

                    CheckResult error =
                        new CheckResult();


                    error.Type =
                        "文件打开错误";


                    error.ObjectName =
                        Path.GetFileName(file);


                    error.Message =
                        ex.Message;


                    error.IsError = true;


                    allResults.Add(error);

                }


            }



            return allResults;


        }


    }

}