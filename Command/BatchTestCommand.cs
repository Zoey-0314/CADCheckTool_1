using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;

using Correct_test1.Batch;
using Correct_test1.Models;

using System.Collections.Generic;
using System.Windows.Forms;


namespace Correct_test1.Command
{

    public class BatchTestCommand
    {

        [CommandMethod("TESTCLEARMARK")]
        public void TestClearMark()
        {

            FolderBrowserDialog dialog =
                new FolderBrowserDialog();

            if (dialog.ShowDialog()
                != DialogResult.OK)
                return;

            BatchMarkerCleaner cleaner =
                new BatchMarkerCleaner();

            List<string> result =
                cleaner.ClearFolderMarkers(
                    dialog.SelectedPath
                );

            Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog(
                "清除完成\n数量:"
                +
                result.Count
            );

        }

        [CommandMethod("TESTBATCH")]
        public void TestBatch()
        {

            Document doc =
                Autodesk.AutoCAD.ApplicationServices.Application
                .DocumentManager
                .MdiActiveDocument;

            if (doc == null)
                return;

            FolderBrowserDialog dialog =
                new FolderBrowserDialog();

            dialog.Description =
                "请选择需要批量检查的DWG文件夹";

            if (dialog.ShowDialog()
                != DialogResult.OK)
            {
                return;
            }

            string folder =
                dialog.SelectedPath;

            doc.Editor.WriteMessage(
                "\n选择文件夹:"
                +
                folder
            );

            BatchCheckerManager manager =
                new BatchCheckerManager();

            List<CheckResult> results =
                manager.CheckFolder(
                    folder
                );

            doc.Editor.WriteMessage(
                "\n================"
            );

            doc.Editor.WriteMessage(
                "\n批量检查完成"
            );

            doc.Editor.WriteMessage(
                "\n问题数量:"
                +
                results.Count
            );

            foreach (CheckResult r in results)
            {

                doc.Editor.WriteMessage(
                    "\n"
                    +
                    r.FileName
                    +
                    " | "
                    +
                    r.Type
                    +
                    " | "
                    +
                    r.Message
                );

            }

            doc.Editor.WriteMessage(
                "\n================"
            );

        }

    }

}