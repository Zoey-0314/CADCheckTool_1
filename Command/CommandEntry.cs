using Autodesk.AutoCAD.ApplicationServices;
using AcApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.Runtime;

using Correct_test1.Batch;
using Correct_test1.Export;
using Correct_test1.Models;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;


namespace Correct_test1
{
    public class CommandEntry
    {
        private static readonly object
            BatchSync =
                new object();


        private static PendingBatchRun
            PendingBatch =
                null;


        [CommandMethod("CHECKDRAWING")]
        public void OpenCheckForm()
        {
            try
            {
                CheckSelectForm form =
                    new CheckSelectForm();


                AcApplication
                    .ShowModelessDialog(
                        form);
            }
            catch (System.Exception ex)
            {
                AcApplication
                    .ShowAlertDialog(
                        ex.Message);
            }
        }


        internal static bool QueueBatchRun(
            string folderPath,
            BatchCheckMode mode)
        {
            if (string.IsNullOrWhiteSpace(
                    folderPath))
            {
                return false;
            }


            lock (BatchSync)
            {
                if (PendingBatch != null)
                {
                    return false;
                }


                PendingBatch =
                    new PendingBatchRun
                    {
                        FolderPath =
                            folderPath,

                        Mode =
                            mode
                    };


                return true;
            }
        }


        internal static void CancelQueuedBatchRun()
        {
            lock (BatchSync)
            {
                PendingBatch =
                    null;
            }
        }


        private static PendingBatchRun TakeBatchRun()
        {
            lock (BatchSync)
            {
                PendingBatchRun request =
                    PendingBatch;


                PendingBatch =
                    null;


                return request;
            }
        }


        // 批量检查必须从Session命令执行。
        //
        // 这样需要打开Mechanical图纸时，
        // Document切换发生在AutoCAD正式Session上下文，
        // 而不是modeless WinForms按钮回调里。

        [CommandMethod(
    "CADCHECKBATCHRUN",
    CommandFlags.Session)]
        public void RunQueuedBatch()
        {
            PendingBatchRun request =
                TakeBatchRun();


            if (request == null)
            {
                return;
            }


            BatchProgressForm progressForm =
                null;


            try
            {
                progressForm =
                    new BatchProgressForm();


                AcApplication
                    .ShowModelessDialog(
                        progressForm);


                BatchCheckerManager manager =
                    new BatchCheckerManager();


                List<CheckResult> results =
                    manager.CheckFolder(
                        request.FolderPath,

                        (percent, total, name) =>
                        {
                            if (progressForm == null ||
                                progressForm.IsDisposed)
                            {
                                return;
                            }


                            progressForm.UpdateProgress(
                                percent,
                                name);


                            try
                            {
                                System.Windows.Forms
                                    .Application
                                    .DoEvents();
                            }
                            catch
                            {
                            }
                        },

                        request.Mode);


                if (progressForm != null &&
                    !progressForm.IsDisposed)
                {
                    progressForm.Close();

                    progressForm =
                        null;
                }


                BatchCsvExporter exporter =
                    new BatchCsvExporter();


                string csvPath =
                    exporter.Export(
                        results,
                        request.FolderPath);


                BatchReportInfo.LastReportPath =
                    csvPath;


                DialogResult openResult =
                    MessageBox.Show(
                        "批量检查完成\n\n"
                        + "运行模式："
                        + (
                            request.Mode ==
                            BatchCheckMode.ApplyChanges

                                ? "检查并修改"
                                : "只检查（原DWG未修改）"
                          )
                        + "\n\n问题数量："
                        + results.Count
                        + "\n\n是否打开检查报告？",

                        "批量检查",

                        MessageBoxButtons.YesNo,

                        MessageBoxIcon.Information);


                if (openResult ==
                    DialogResult.Yes)
                {
                    Process.Start(
                        csvPath);
                }
            }
            catch (System.Exception ex)
            {
                AcApplication
                    .ShowAlertDialog(
                        "批量检查失败：\n"
                        + ex.Message);
            }
            finally
            {
                if (progressForm != null &&
                    !progressForm.IsDisposed)
                {
                    try
                    {
                        progressForm.Close();
                    }
                    catch
                    {
                    }
                }
            }
        }


        private sealed class PendingBatchRun
        {
            public string FolderPath
            {
                get;
                set;
            }


            public BatchCheckMode Mode
            {
                get;
                set;
            }
        }
    }
}