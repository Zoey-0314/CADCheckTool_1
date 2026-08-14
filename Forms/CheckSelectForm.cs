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


        /// <summary>
        /// 进入单张图纸检查。
        ///
        /// SingleCheckForm必须使用AutoCAD的
        /// Modeless方式打开，
        /// 因为快速划改需要继续操作CAD图面。
        /// </summary>
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
                // 回到检查模式选择窗口。
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
                // 关键：
                //
                // 不再使用：
                // form.ShowDialog(this);
                //
                // 改成AutoCAD的Modeless窗口。
                //
                // 这样QREVMODE运行时，
                // 用户仍然可以操作CAD图面。
                //--------------------------------

                Autodesk.AutoCAD
                    .ApplicationServices
                    .Application
                    .ShowModelessDialog(
                        form);
            }
            catch (Exception ex)
            {
                //--------------------------------
                // 如果打开失败，
                // 把选择窗口恢复。
                //--------------------------------

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


        /// <summary>
        /// 进入批量检查。
        ///
        /// 批量检查不需要图面连续交互，
        /// 暂时保持原有逻辑。
        /// </summary>
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
    }
}