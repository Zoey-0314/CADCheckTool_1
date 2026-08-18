using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.Batch;
using Correct_test1.Export;
using Correct_test1.Markers;
using Correct_test1.Models;


namespace Correct_test1
{
    public partial class BatchCheckForm :
        Form
    {
        public BatchCheckForm()
        {
            InitializeComponent();
        }


        private void btnRunBatch_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                DialogResult modeResult =
                    MessageBox.Show(
                        "请选择批量检查模式：\n\n"
                        + "【是】检查并修改\n"
                        + "自动修正页码、写入检查标记并保存DWG。\n\n"
                        + "【否】只检查\n"
                        + "只生成CSV报告，不修改任何DWG。\n\n"
                        + "【取消】退出",
                        "批量检查模式",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);


                if (modeResult ==
                    DialogResult.Cancel)
                {
                    return;
                }


                BatchCheckMode mode =
                    modeResult ==
                    DialogResult.Yes

                        ? BatchCheckMode.ApplyChanges
                        : BatchCheckMode.ReportOnly;


                using (
                    FolderBrowserDialog dialog =
                        new FolderBrowserDialog())
                {
                    dialog.Description =
                        "请选择需要批量检查的DWG文件夹";


                    if (dialog.ShowDialog()
                        != DialogResult.OK)
                    {
                        return;
                    }


                    Document doc =
                        Autodesk.AutoCAD
                            .ApplicationServices
                            .Application
                            .DocumentManager
                            .MdiActiveDocument;


                    if (doc == null)
                    {
                        MessageBox.Show(
                            "当前没有有效的AutoCAD宿主图纸。",
                            "批量检查");

                        return;
                    }


                    if (!CommandEntry.QueueBatchRun(
                            dialog.SelectedPath,
                            mode))
                    {
                        MessageBox.Show(
                            "已有批量检查正在等待或运行，请勿重复启动。",
                            "批量检查");

                        return;
                    }


                    try
                    {
                        // Modeless按钮只负责排队。
                        // 真正批量检查由Session命令执行，
                        // 避免在modeless回调里直接切换/打开Document。
                        doc.SendStringToExecute(
                            "CADCHECKBATCHRUN ",
                            true,
                            false,
                            false);
                    }
                    catch
                    {
                        CommandEntry.CancelQueuedBatchRun();
                        throw;
                    }


                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "批量检查启动失败");
            }
        }


        private void btnOpenReport_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                string path =
                    BatchReportInfo.LastReportPath;


                if (!string.IsNullOrEmpty(
                        path) &&
                    System.IO.File.Exists(
                        path))
                {
                    Process.Start(
                        path);
                }
                else
                {
                    MessageBox.Show(
                        "暂无批量检查报告",
                        "CAD检查助手");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "打开报告失败");
            }
        }


        private void btnClearCurrent_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                Document doc =
                    Autodesk.AutoCAD
                        .ApplicationServices
                        .Application
                        .DocumentManager
                        .MdiActiveDocument;


                if (doc == null)
                {
                    MessageBox.Show(
                        "当前没有打开CAD图纸");

                    return;
                }


                using (
                    DocumentLock lockDoc =
                        doc.LockDocument())
                {
                    RevisionMarker revisionMarker =
                        new RevisionMarker();


                    revisionMarker.ClearMarkers(
                        doc.Database);


                    TitleBlockDrawingNumberMarker
                        titleBlockMarker =
                            new TitleBlockDrawingNumberMarker();


                    titleBlockMarker.ClearMarkers(
                        doc.Database);
                }


                MessageBox.Show(
                    "当前图纸修改注释已清除");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "清除失败");
            }
        }


        private void btnClearFolder_Click(
            object sender,
            EventArgs e)
        {
            BatchProgressForm progressForm =
                null;

            try
            {
                using (
                    FolderBrowserDialog dialog =
                        new FolderBrowserDialog())
                {
                    dialog.Description =
                        "请选择需要清除修改注释的DWG文件夹";


                    if (dialog.ShowDialog()
                        != DialogResult.OK)
                    {
                        return;
                    }


                    BatchMarkerCleaner cleaner =
                        new BatchMarkerCleaner();


                    progressForm =
                        new BatchProgressForm();


                    Autodesk.AutoCAD
                        .ApplicationServices
                        .Application
                        .ShowModelessDialog(
                            progressForm);


                    List<string> result =
                        cleaner.ClearFolderMarkers(
                            dialog.SelectedPath,
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
                            });


                    if (progressForm != null &&
                        !progressForm.IsDisposed)
                    {
                        progressForm.Close();

                        progressForm =
                            null;
                    }


                    MessageBox.Show(
                        "清除完成\n\n"
                        + "处理文件数量："
                        + result.Count,
                        "批量清除");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "批量清除失败");
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


        private void btnClose_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                Document doc =
                    Autodesk.AutoCAD
                        .ApplicationServices
                        .Application
                        .DocumentManager
                        .MdiActiveDocument;


                if (doc != null)
                {
                    doc.SendStringToExecute(
                        "\x03\x03",
                        true,
                        false,
                        false);
                }
            }
            catch
            {
            }


            this.Close();
        }
    }
}
