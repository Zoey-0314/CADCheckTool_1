using Correct_test1.ProjectVersion.Models;
using Correct_test1.ProjectVersion.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;


namespace Correct_test1
{

    public partial class BatchProgressForm : Form
    {


        public BatchProgressForm()
        {
            InitializeComponent();
        }




        public void UpdateProgress(
    int percent,
    string fileName)
        {
            //--------------------------------
            // 原有调用保持原样
            //--------------------------------

            UpdateProgress(
                percent,
                fileName,
                "正在检查");
        }


        public void UpdateProgress(
            int percent,
            string fileName,
            string actionText)
        {
            if (percent < 0)
                percent = 0;


            if (percent > 100)
                percent = 100;


            progressBar1.Value =
                percent;


            if (string.IsNullOrWhiteSpace(
                    actionText))
            {
                actionText =
                    "正在处理";
            }


            lblStatus.Text =
                actionText
                + "："
                + fileName
                + "\n完成度："
                + percent
                + "%";


            Application.DoEvents();
        }
        //==================================================
        // 批量版本号输入
        //==================================================

        private void btnBatchProjectVersion_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                //--------------------------------
                // 第一步：
                // 选择文件夹
                //--------------------------------

                using (
                    FolderBrowserDialog folderDialog =
                        new FolderBrowserDialog())
                {
                    folderDialog.Description =
                        "请选择需要批量输入版本号的DWG文件夹";


                    if (folderDialog.ShowDialog(this)
                        != DialogResult.OK)
                    {
                        return;
                    }


                    string folderPath =
                        folderDialog.SelectedPath;


                    //--------------------------------
                    // 检查DWG数量
                    //--------------------------------

                    string[] files =
                        Directory.GetFiles(
                            folderPath,
                            "*.dwg",
                            SearchOption.AllDirectories);


                    if (files.Length == 0)
                    {
                        MessageBox.Show(
                            "所选文件夹及其子文件夹中没有找到DWG文件。",
                            "批量版本号输入");

                        return;
                    }


                    //--------------------------------
                    // 第二步：
                    // 输入一次完整版本号
                    //--------------------------------

                    using (
                        ProjectVersionInputForm inputForm =
                            new ProjectVersionInputForm())
                    {
                        inputForm.Text =
                            "批量版本号输入";


                        if (inputForm.ShowDialog(this)
                            != DialogResult.OK)
                        {
                            return;
                        }


                        string value =
                            inputForm.ProjectVersionText;


                        //--------------------------------
                        // 第三步：
                        // 最终确认
                        //--------------------------------

                        DialogResult confirm =
                            MessageBox.Show(
                                "即将递归处理所选文件夹中的全部DWG。"
                                + "\n\n文件数量："
                                + files.Length
                                + "\n"
                                + "写入内容："
                                + value
                                + "\n\n"
                                + "处理过程中会直接修改并安全保存原DWG，"
                                + "同时生成 .bak 备份。"
                                + "\n\n是否继续？",
                                "批量版本号输入",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning);


                        if (confirm !=
                            DialogResult.Yes)
                        {
                            return;
                        }


                        //--------------------------------
                        // 第四步：
                        // 打开现有批量进度窗口
                        //--------------------------------

                        BatchProgressForm progressForm =
                            new BatchProgressForm();


                        try
                        {
                            progressForm.Show();


                            BatchProjectVersionService service =
                                new BatchProjectVersionService();


                            List<BatchProjectVersionResult>
                                results =
                                    service.WriteFolder(
                                        folderPath,
                                        value,
                                        (
                                            percent,
                                            total,
                                            name
                                        ) =>
                                        {
                                            progressForm
                                                .UpdateProgress(
                                                    percent,
                                                    name,
                                                    "正在写入版本号");
                                        });


                            //--------------------------------
                            // 汇总结果
                            //--------------------------------

                            int successFiles = 0;
                            int failedFiles = 0;

                            int modifiedLayouts = 0;
                            int createdLayouts = 0;
                            int skippedLayouts = 0;
                            int failedLayouts = 0;


                            StringBuilder details =
                                new StringBuilder();


                            foreach (
                                BatchProjectVersionResult result
                                in results)
                            {
                                if (result == null)
                                    continue;


                                modifiedLayouts +=
                                    result.ModifiedCount;


                                createdLayouts +=
                                    result.CreatedCount;


                                skippedLayouts +=
                                    result.SkippedCount;


                                failedLayouts +=
                                    result.FailedLayoutCount;


                                if (result.Success)
                                {
                                    successFiles++;
                                }
                                else
                                {
                                    failedFiles++;


                                    details.AppendLine(
                                        result.FileName
                                        + "："
                                        + result.Message);
                                }
                            }


                            StringBuilder message =
                                new StringBuilder();


                            message.AppendLine(
                                "批量版本号输入完成。");


                            message.AppendLine();


                            message.AppendLine(
                                "写入内容："
                                + value);


                            message.AppendLine(
                                "DWG总数："
                                + results.Count);


                            message.AppendLine(
                                "成功文件："
                                + successFiles);


                            message.AppendLine(
                                "失败文件："
                                + failedFiles);


                            message.AppendLine();


                            message.AppendLine(
                                "修改已有项目号布局："
                                + modifiedLayouts);


                            message.AppendLine(
                                "新建项目号布局："
                                + createdLayouts);


                            message.AppendLine(
                                "跳过布局："
                                + skippedLayouts);


                            message.AppendLine(
                                "失败布局："
                                + failedLayouts);


                            if (details.Length > 0)
                            {
                                message.AppendLine();

                                message.AppendLine(
                                    "失败文件：");

                                message.Append(
                                    details.ToString());
                            }


                            MessageBox.Show(
                                message.ToString(),
                                "批量版本号输入",
                                MessageBoxButtons.OK,
                                failedFiles == 0
                                    ? MessageBoxIcon.Information
                                    : MessageBoxIcon.Warning);
                        }
                        finally
                        {
                            try
                            {
                                if (!progressForm.IsDisposed)
                                {
                                    progressForm.Close();
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "批量版本号输入失败："
                    + ex.Message,
                    "批量版本号输入");
            }
        }



    }

}