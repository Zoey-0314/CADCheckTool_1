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

    public partial class BatchCheckForm : Form
    {

        public BatchCheckForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 执行批量检查
        /// </summary>
        private void btnRunBatch_Click(
            object sender,
            EventArgs e)
        {

            try
            {

                FolderBrowserDialog dialog =
                    new FolderBrowserDialog();

                dialog.Description =
                    "请选择需要批量检查的DWG文件夹";

                if (dialog.ShowDialog()
                    != DialogResult.OK)
                {
                    return;
                }

                string folderPath =
                    dialog.SelectedPath;

                //打开进度窗口

                BatchProgressForm progressForm =
                    new BatchProgressForm();

                progressForm.Show();

                BatchCheckerManager manager =
                    new BatchCheckerManager();

                List<CheckResult> results =
                    manager.CheckFolder(

                        folderPath,

                        (percent, total, name) =>
                        {

                            progressForm.UpdateProgress(
                                percent,
                                name
                            );

                        }

                    );

                //关闭进度窗口

                progressForm.Close();

                //生成报告

                BatchCsvExporter exporter =
                    new BatchCsvExporter();

                string csvPath =
                    exporter.Export(
                        results,
                        folderPath
                    );

                BatchReportInfo.LastReportPath =
                    csvPath;

                DialogResult dr =
                    MessageBox.Show(

                        "批量检查完成\n\n"
                        +
                        "问题数量："
                        +
                        results.Count
                        +
                        "\n\n是否打开检查报告？",

                        "批量检查",

                        MessageBoxButtons.YesNo,

                        MessageBoxIcon.Information

                    );

                if (dr == DialogResult.Yes)
                {

                    Process.Start(
                        csvPath
                    );

                }

            }
            catch (Exception ex)
            {

                MessageBox.Show(

                    ex.Message,

                    "批量检查失败"

                );

            }

        }

        /// <summary>
        /// 打开最近报告
        /// </summary>
        private void btnOpenReport_Click(
            object sender,
            EventArgs e)
        {

            try
            {

                string path =
                    BatchReportInfo.LastReportPath;

                if (!string.IsNullOrEmpty(path)
                    &&
                   System.IO.File.Exists(path))
                {

                    Process.Start(
                        path
                    );

                }
                else
                {

                    MessageBox.Show(
                        "暂无批量检查报告",
                        "CAD检查助手"
                    );

                }

            }
            catch (Exception ex)
            {

                MessageBox.Show(
                    ex.Message,
                    "打开报告失败"
                );

            }

        }

        /// <summary>
        /// 清除当前打开图纸修改注释
        /// </summary>
        private void btnClearCurrent_Click(
            object sender,
            EventArgs e)
        {

            try
            {

                Document doc =
                    Autodesk.AutoCAD.ApplicationServices.Application
                    .DocumentManager
                    .MdiActiveDocument;

                if (doc == null)
                {
                    MessageBox.Show(
                        "当前没有打开CAD图纸"
                    );

                    return;

                }

                using (DocumentLock lockDoc =
                    doc.LockDocument())
                {

                    RevisionMarker marker =
                        new RevisionMarker();


                    marker.ClearMarkers(
                        doc.Database
                    );

                }

                MessageBox.Show(
                    "当前图纸修改注释已清除"
                );

            }
            catch (Exception ex)
            {

                MessageBox.Show(
                    ex.Message,
                    "清除失败"
                );

            }

        }

        /// <summary>
        /// 清除文件夹所有修改注释
        /// </summary>
        private void btnClearFolder_Click(
            object sender,
            EventArgs e)
        {

            try
            {

                FolderBrowserDialog dialog =
                    new FolderBrowserDialog();

                dialog.Description =
                    "请选择需要清除修改注释的DWG文件夹";

                if (dialog.ShowDialog()
                    != DialogResult.OK)
                {
                    return;
                }

                BatchMarkerCleaner cleaner =
                    new BatchMarkerCleaner();

                List<string> result =
                    cleaner.ClearFolderMarkers(
                        dialog.SelectedPath
                    );

                MessageBox.Show(

                    "清除完成\n\n"
                    +
                    "处理文件数量："
                    +
                    result.Count,


                    "批量清除"

                );

            }
            catch (Exception ex)
            {

                MessageBox.Show(
                    ex.Message,
                    "批量清除失败"
                );

            }

        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        private void btnClose_Click(
            object sender,
            EventArgs e)
        {

            Close();

        }

    }

}