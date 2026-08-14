using System;
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