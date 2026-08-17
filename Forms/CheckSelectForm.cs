using Correct_test1.ProjectVersion.Models;
using Correct_test1.ProjectVersion.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;


namespace Correct_test1
{
    public partial class CheckSelectForm : Form
    {
        public CheckSelectForm()
        {
            InitializeComponent();
        }


        //==================================================
        // 单张检查
        //==================================================

        private void btnSingle_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                SingleCheckForm form =
                    new SingleCheckForm();


                //--------------------------------
                // 进入单张模式后先隐藏选择窗口
                //--------------------------------

                this.Hide();


                //--------------------------------
                // SingleCheckForm关闭以后，
                // 回到检查模式选择窗口
                //--------------------------------

                form.FormClosed +=
                    delegate
                    {
                        try
                        {
                            if (!this.IsDisposed)
                            {
                                this.Show();
                                this.Activate();
                            }
                        }
                        catch
                        {
                        }
                    };


                //--------------------------------
                // 使用AutoCAD Modeless窗口
                //--------------------------------

                Autodesk.AutoCAD
                    .ApplicationServices
                    .Application
                    .ShowModelessDialog(
                        form);
            }
            catch (Exception ex)
            {
                try
                {
                    this.Show();
                }
                catch
                {
                }


                MessageBox.Show(
                    "无法打开单张检查窗口："
                    + ex.Message,
                    "CAD检查助手");
            }
        }


        //==================================================
        // 批量检查
        //==================================================

        private void btnBatch_Click(
            object sender,
            EventArgs e)
        {
            Type t =
                Type.GetType(
                    "Correct_test1.BatchCheckForm");


            if (t != null)
            {
                try
                {
                    Form f =
                        (Form)
                        Activator.CreateInstance(
                            t);


                    f.ShowDialog(
                        this);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Failed to open BatchCheckForm: "
                        + ex.Message,
                        "Error");
                }
            }
            else
            {
                MessageBox.Show(
                    "BatchCheckForm is not available yet.",
                    "Info");
            }
        }


        //==================================================
        // 当前图纸版本号输入
        //==================================================

        private void btnProjectVersion_Click(
            object sender,
            EventArgs e)
        {
            Autodesk.AutoCAD
                .ApplicationServices
                .Document doc =
                    Autodesk.AutoCAD
                        .ApplicationServices
                        .Application
                        .DocumentManager
                        .MdiActiveDocument;


            //--------------------------------
            // 当前必须有打开的DWG
            //--------------------------------

            if (doc == null)
            {
                MessageBox.Show(
                    "当前没有打开的DWG图纸。",
                    "版本号输入");

                return;
            }


            //--------------------------------
            // 弹出输入框
            //--------------------------------

            using (
                ProjectVersionInputForm form =
                    new ProjectVersionInputForm())
            {
                if (form.ShowDialog(this)
                    != DialogResult.OK)
                {
                    return;
                }


                string value =
                    form.ProjectVersionText;


                try
                {
                    List<ProjectVersionLayoutResult>
                        results;


                    //--------------------------------
                    // 修改当前DWG时锁定Document
                    //--------------------------------

                    using (
                        Autodesk.AutoCAD
                            .ApplicationServices
                            .DocumentLock documentLock =
                                doc.LockDocument())
                    {
                        ProjectVersionWriteService service =
                            new ProjectVersionWriteService();


                        results =
                            service.WriteAllLayouts(
                                doc.Database,
                                value);
                    }


                    //--------------------------------
                    // 刷新图面
                    //--------------------------------

                    try
                    {
                        doc.Editor.Regen();


                        Autodesk.AutoCAD
                            .ApplicationServices
                            .Application
                            .UpdateScreen();
                    }
                    catch
                    {
                    }


                    //--------------------------------
                    // 统计结果
                    //--------------------------------

                    int modified = 0;

                    int created = 0;

                    int skipped = 0;

                    int failed = 0;


                    StringBuilder details =
                        new StringBuilder();


                    if (results != null)
                    {
                        foreach (
                            ProjectVersionLayoutResult result
                            in results)
                        {
                            if (result == null)
                                continue;


                            //--------------------------------
                            // 跳过
                            //--------------------------------

                            if (result.Skipped)
                            {
                                skipped++;


                                details.AppendLine(
                                    result.LayoutName
                                    + "：已跳过 - "
                                    + result.Message);


                                continue;
                            }


                            //--------------------------------
                            // 失败
                            //--------------------------------

                            if (!result.Success)
                            {
                                failed++;


                                details.AppendLine(
                                    result.LayoutName
                                    + "：失败 - "
                                    + result.Message);


                                continue;
                            }


                            //--------------------------------
                            // 成功
                            //--------------------------------

                            if (result.Created)
                            {
                                created++;
                            }
                            else
                            {
                                modified++;
                            }
                        }
                    }


                    //--------------------------------
                    // 完成提示
                    //--------------------------------

                    StringBuilder message =
                        new StringBuilder();


                    message.AppendLine(
                        "版本号输入完成。");


                    message.AppendLine();


                    message.AppendLine(
                        "写入内容："
                        + value);


                    message.AppendLine(
                        "修改已有项目号："
                        + modified);


                    message.AppendLine(
                        "新建项目号："
                        + created);


                    message.AppendLine(
                        "跳过布局："
                        + skipped);


                    message.AppendLine(
                        "失败布局："
                        + failed);


                    if (details.Length > 0)
                    {
                        message.AppendLine();


                        message.AppendLine(
                            "详细信息：");


                        message.Append(
                            details.ToString());
                    }


                    MessageBox.Show(
                        message.ToString(),
                        "版本号输入");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "版本号写入失败："
                        + ex.Message,
                        "版本号输入");
                }
            }
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
                // 选择需要处理的文件夹
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
                    // 递归查找所有DWG
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
                    // 输入完整项目号+版本号
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
                        // 执行前确认
                        //--------------------------------

                        DialogResult confirm =
                            MessageBox.Show(
                                "即将递归处理所选文件夹中的全部DWG。"
                                + "\n\n"
                                + "文件数量："
                                + files.Length
                                + "\n"
                                + "写入内容："
                                + value
                                + "\n\n"
                                + "程序将直接修改原DWG，"
                                + "并通过安全保存方式生成 .bak 备份。"
                                + "\n\n"
                                + "是否继续？",
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


                            if (results != null)
                            {
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


                                    //--------------------------------
                                    // 文件成功
                                    //--------------------------------

                                    if (result.Success)
                                    {
                                        successFiles++;
                                    }
                                    else
                                    {
                                        //--------------------------------
                                        // 文件失败
                                        //--------------------------------

                                        failedFiles++;


                                        details.AppendLine(
                                            result.FileName
                                            + "："
                                            + result.Message);
                                    }
                                }
                            }


                            //--------------------------------
                            // 最终结果提示
                            //--------------------------------

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
                                + (
                                    results == null
                                        ? 0
                                        : results.Count
                                  ));


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
                            //--------------------------------
                            // 无论成功失败都关闭进度窗口
                            //--------------------------------

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


        //==================================================
        // 路径设置
        //==================================================

        /// <summary>
        /// 打开路径设置窗口。
        ///
        /// 可以修改：
        /// 1. 非标归档图纸目录
        /// 2. 诺升标准件数据库Excel路径
        /// </summary>
        private void btnPathSettings_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                using (
                    PathSettingsForm form =
                        new PathSettingsForm())
                {
                    form.ShowDialog(
                        this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "无法打开路径设置："
                    + ex.Message,
                    "CAD检查助手");
            }
        }
    }
}