namespace Correct_test1
{
    partial class ProjectVersionInputForm
    {
        private System.ComponentModel.IContainer
            components = null;


        protected override void Dispose(
            bool disposing)
        {
            if (disposing &&
                components != null)
            {
                components.Dispose();
            }

            base.Dispose(
                disposing);
        }


        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblPrompt =
                new System.Windows.Forms.Label();

            this.lblExample =
                new System.Windows.Forms.Label();

            this.txtProjectVersion =
                new System.Windows.Forms.TextBox();

            this.btnOk =
                new System.Windows.Forms.Button();

            this.btnCancel =
                new System.Windows.Forms.Button();

            this.SuspendLayout();


            // lblPrompt

            this.lblPrompt.AutoSize =
                true;

            this.lblPrompt.Location =
                new System.Drawing.Point(
                    24,
                    24);

            this.lblPrompt.Name =
                "lblPrompt";

            this.lblPrompt.Size =
                new System.Drawing.Size(
                    149,
                    13);

            this.lblPrompt.TabIndex =
                0;

            this.lblPrompt.Text =
                "请输入完整项目号及版本号：";


            // lblExample

            this.lblExample.AutoSize =
                true;

            this.lblExample.Location =
                new System.Drawing.Point(
                    24,
                    50);

            this.lblExample.Name =
                "lblExample";

            this.lblExample.Size =
                new System.Drawing.Size(
                    138,
                    13);

            this.lblExample.TabIndex =
                1;

            this.lblExample.Text =
                "例如：P2026AB003-L0";


            // txtProjectVersion

            this.txtProjectVersion.Location =
                new System.Drawing.Point(
                    27,
                    78);

            this.txtProjectVersion.Name =
                "txtProjectVersion";

            this.txtProjectVersion.Size =
                new System.Drawing.Size(
                    310,
                    20);

            this.txtProjectVersion.TabIndex =
                2;


            // btnOk

            this.btnOk.Location =
                new System.Drawing.Point(
                    161,
                    125);

            this.btnOk.Name =
                "btnOk";

            this.btnOk.Size =
                new System.Drawing.Size(
                    80,
                    32);

            this.btnOk.TabIndex =
                3;

            this.btnOk.Text =
                "确定";

            this.btnOk.UseVisualStyleBackColor =
                true;

            this.btnOk.Click +=
                new System.EventHandler(
                    this.btnOk_Click);


            // btnCancel

            this.btnCancel.DialogResult =
                System.Windows.Forms.DialogResult.Cancel;

            this.btnCancel.Location =
                new System.Drawing.Point(
                    257,
                    125);

            this.btnCancel.Name =
                "btnCancel";

            this.btnCancel.Size =
                new System.Drawing.Size(
                    80,
                    32);

            this.btnCancel.TabIndex =
                4;

            this.btnCancel.Text =
                "取消";

            this.btnCancel.UseVisualStyleBackColor =
                true;

            this.btnCancel.Click +=
                new System.EventHandler(
                    this.btnCancel_Click);


            // ProjectVersionInputForm

            this.AcceptButton =
                this.btnOk;

            this.CancelButton =
                this.btnCancel;

            this.AutoScaleDimensions =
                new System.Drawing.SizeF(
                    6F,
                    13F);

            this.AutoScaleMode =
                System.Windows.Forms.AutoScaleMode.Font;

            this.ClientSize =
                new System.Drawing.Size(
                    365,
                    185);

            this.Controls.Add(
                this.btnCancel);

            this.Controls.Add(
                this.btnOk);

            this.Controls.Add(
                this.txtProjectVersion);

            this.Controls.Add(
                this.lblExample);

            this.Controls.Add(
                this.lblPrompt);

            this.FormBorderStyle =
                System.Windows.Forms.FormBorderStyle.FixedDialog;

            this.MaximizeBox =
                false;

            this.MinimizeBox =
                false;

            this.Name =
                "ProjectVersionInputForm";

            this.StartPosition =
                System.Windows.Forms.FormStartPosition.CenterParent;

            this.Text =
                "当前图纸版本号输入";

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion


        private System.Windows.Forms.Label
            lblPrompt;

        private System.Windows.Forms.Label
            lblExample;

        private System.Windows.Forms.TextBox
            txtProjectVersion;

        private System.Windows.Forms.Button
            btnOk;

        private System.Windows.Forms.Button
            btnCancel;
    }
}