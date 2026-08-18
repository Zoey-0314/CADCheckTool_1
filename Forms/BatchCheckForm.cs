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
            BatchProgressForm progressForm =
                null;

            try
            {
                DialogResult modeResult =
                    MessageBox.Show(
                        "请选择批量检查模式：\n\n"
                        + "【是】检查并修改\n"
                        + "自动修正页码、写入检查标记并保存DWG。\n\n"
                        + "【否】只检查\n"
                        + "只生成CSV报告，不修改任何DWG。\n"
                        + "大量图纸测试推荐使用此模式。\n\n"
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

                        ? BatchCheckMode
                            .ApplyChanges

                        : BatchCheckMode
                            .ReportOnly;


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


                    string folderPath =
                        dialog.SelectedPath;


                    btnRunBatch.Enabled =
                        false;


                    progressForm =
                        new BatchProgressForm();


                    Autodesk.AutoCAD
                        .ApplicationServices
                        .Application
                        .ShowModelessDialog(
                            progressForm);


                    BatchCheckerManager manager =
                        new BatchCheckerManager();


                    List<CheckResult> results =
                        manager.CheckFolder(
                            folderPath,
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
                            },
                            mode);


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
                            folderPath);


                    BatchReportInfo.LastReportPath =
                        csvPath;


                    DialogResult dr =
                        MessageBox.Show(
                            "批量检查完成\n\n"
                            + "运行模式："
                            + (
                                mode ==
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


                    if (dr ==
                        DialogResult.Yes)
                    {
                        Process.Start(
                            csvPath);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "批量检查失败");
            }
            finally
            {
                btnRunBatch.Enabled =
                    true;


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
