namespace Correct_test1
{
    partial class PathSettingsForm
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


        private void InitializeComponent()
        {
            this.lblArchive =
                new System.Windows.Forms.Label();

            this.txtArchivePath =
                new System.Windows.Forms.TextBox();

            this.btnBrowseArchive =
                new System.Windows.Forms.Button();

            this.lblStandardPart =
                new System.Windows.Forms.Label();

            this.txtStandardPartPath =
                new System.Windows.Forms.TextBox();

            this.btnBrowseStandardPart =
                new System.Windows.Forms.Button();

            this.btnSave =
                new System.Windows.Forms.Button();

            this.btnDefault =
                new System.Windows.Forms.Button();

            this.btnCancel =
                new System.Windows.Forms.Button();


            this.SuspendLayout();


            // lblArchive
            this.lblArchive.AutoSize =
                true;

            this.lblArchive.Location =
                new System.Drawing.Point(
                    24,
                    25);

            this.lblArchive.Text =
                "非标归档图纸：";


            // txtArchivePath
            this.txtArchivePath.Location =
                new System.Drawing.Point(
                    27,
                    48);

            this.txtArchivePath.Size =
                new System.Drawing.Size(
                    430,
                    21);


            // btnBrowseArchive
            this.btnBrowseArchive.Location =
                new System.Drawing.Point(
                    470,
                    46);

            this.btnBrowseArchive.Size =
                new System.Drawing.Size(
                    80,
                    25);

            this.btnBrowseArchive.Text =
                "浏览...";

            this.btnBrowseArchive.Click +=
                new System.EventHandler(
                    this
                        .btnBrowseArchive_Click);


            // lblStandardPart
            this.lblStandardPart.AutoSize =
                true;

            this.lblStandardPart.Location =
                new System.Drawing.Point(
                    24,
                    92);

            this.lblStandardPart.Text =
                "诺升标准件数据库：";


            // txtStandardPartPath
            this.txtStandardPartPath.Location =
                new System.Drawing.Point(
                    27,
                    115);

            this.txtStandardPartPath.Size =
                new System.Drawing.Size(
                    430,
                    21);


            // btnBrowseStandardPart
            this.btnBrowseStandardPart.Location =
                new System.Drawing.Point(
                    470,
                    113);

            this.btnBrowseStandardPart.Size =
                new System.Drawing.Size(
                    80,
                    25);

            this.btnBrowseStandardPart.Text =
                "浏览...";

            this.btnBrowseStandardPart.Click +=
                new System.EventHandler(
                    this
                        .btnBrowseStandardPart_Click);


            // btnDefault
            this.btnDefault.Location =
                new System.Drawing.Point(
                    214,
                    170);

            this.btnDefault.Size =
                new System.Drawing.Size(
                    100,
                    32);

            this.btnDefault.Text =
                "恢复默认";

            this.btnDefault.Click +=
                new System.EventHandler(
                    this.btnDefault_Click);


            // btnSave
            this.btnSave.Location =
                new System.Drawing.Point(
                    326,
                    170);

            this.btnSave.Size =
                new System.Drawing.Size(
                    100,
                    32);

            this.btnSave.Text =
                "保存";

            this.btnSave.Click +=
                new System.EventHandler(
                    this.btnSave_Click);


            // btnCancel
            this.btnCancel.Location =
                new System.Drawing.Point(
                    438,
                    170);

            this.btnCancel.Size =
                new System.Drawing.Size(
                    100,
                    32);

            this.btnCancel.Text =
                "取消";

            this.btnCancel.Click +=
                new System.EventHandler(
                    this.btnCancel_Click);


            // PathSettingsForm
            this.AutoScaleDimensions =
                new System.Drawing.SizeF(
                    6F,
                    13F);

            this.AutoScaleMode =
                System.Windows.Forms
                    .AutoScaleMode
                    .Font;

            this.ClientSize =
                new System.Drawing.Size(
                    580,
                    230);

            this.Controls.Add(
                this.lblArchive);

            this.Controls.Add(
                this.txtArchivePath);

            this.Controls.Add(
                this.btnBrowseArchive);

            this.Controls.Add(
                this.lblStandardPart);

            this.Controls.Add(
                this.txtStandardPartPath);

            this.Controls.Add(
                this.btnBrowseStandardPart);

            this.Controls.Add(
                this.btnDefault);

            this.Controls.Add(
                this.btnSave);

            this.Controls.Add(
                this.btnCancel);

            this.FormBorderStyle =
                System.Windows.Forms
                    .FormBorderStyle
                    .FixedDialog;

            this.MaximizeBox =
                false;

            this.MinimizeBox =
                false;

            this.StartPosition =
                System.Windows.Forms
                    .FormStartPosition
                    .CenterParent;

            this.Name =
                "PathSettingsForm";

            this.Text =
                "CADCheckTool 路径设置";


            this.ResumeLayout(
                false);

            this.PerformLayout();
        }


        private System.Windows.Forms.Label
            lblArchive;

        private System.Windows.Forms.TextBox
            txtArchivePath;

        private System.Windows.Forms.Button
            btnBrowseArchive;

        private System.Windows.Forms.Label
            lblStandardPart;

        private System.Windows.Forms.TextBox
            txtStandardPartPath;

        private System.Windows.Forms.Button
            btnBrowseStandardPart;

        private System.Windows.Forms.Button
            btnSave;

        private System.Windows.Forms.Button
            btnDefault;

        private System.Windows.Forms.Button
            btnCancel;
    }
}