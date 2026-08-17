using System;
using System.Windows.Forms;

namespace Correct_test1
{
    public partial class ProjectVersionInputForm :
        Form
    {
        /// <summary>
        /// 用户输入的完整项目号+版本号。
        /// 例如：
        /// N2604US001-L0
        /// </summary>
        public string ProjectVersionText
        {
            get
            {
                if (txtProjectVersion == null)
                    return "";

                return txtProjectVersion
                    .Text
                    .Trim();
            }
        }


        public ProjectVersionInputForm()
        {
            InitializeComponent();
        }


        //==================================================
        // 确定
        //==================================================

        private void btnOk_Click(
            object sender,
            EventArgs e)
        {
            string value =
                txtProjectVersion
                    .Text
                    .Trim();


            if (string.IsNullOrWhiteSpace(
                    value))
            {
                MessageBox.Show(
                    "请输入完整项目号及版本号。",
                    "版本号输入");

                txtProjectVersion.Focus();

                return;
            }


            this.DialogResult =
                DialogResult.OK;

            this.Close();
        }


        //==================================================
        // 取消
        //==================================================

        private void btnCancel_Click(
            object sender,
            EventArgs e)
        {
            this.DialogResult =
                DialogResult.Cancel;

            this.Close();
        }
    }
}