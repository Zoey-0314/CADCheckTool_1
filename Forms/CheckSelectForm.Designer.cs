namespace Correct_test1
{
    partial class CheckSelectForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer
            components = null;


        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
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


        /// <summary>
        /// Required method for Designer support.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnSingle =
                new System.Windows.Forms.Button();

            this.btnBatch =
                new System.Windows.Forms.Button();

            this.btnProjectVersion =
                new System.Windows.Forms.Button();

            this.btnBatchProjectVersion =
                new System.Windows.Forms.Button();

            this.btnPathSettings =
                new System.Windows.Forms.Button();

            this.lblTitle =
                new System.Windows.Forms.Label();


            this.SuspendLayout();


            // btnSingle

            this.btnSingle.Location =
                new System.Drawing.Point(
                    50,
                    80);

            this.btnSingle.Name =
                "btnSingle";

            this.btnSingle.Size =
                new System.Drawing.Size(
                    200,
                    40);

            this.btnSingle.TabIndex =
                0;

            this.btnSingle.Text =
                "单张检查";

            this.btnSingle.UseVisualStyleBackColor =
                true;

            this.btnSingle.Click +=
                new System.EventHandler(
                    this.btnSingle_Click);


            // btnBatch

            this.btnBatch.Location =
                new System.Drawing.Point(
                    50,
                    140);

            this.btnBatch.Name =
                "btnBatch";

            this.btnBatch.Size =
                new System.Drawing.Size(
                    200,
                    40);

            this.btnBatch.TabIndex =
                1;

            this.btnBatch.Text =
                "批量检查";

            this.btnBatch.UseVisualStyleBackColor =
                true;

            this.btnBatch.Click +=
                new System.EventHandler(
                    this.btnBatch_Click);


            // btnProjectVersion

            this.btnProjectVersion.Location =
                new System.Drawing.Point(
                    50,
                    200);

            this.btnProjectVersion.Name =
                "btnProjectVersion";

            this.btnProjectVersion.Size =
                new System.Drawing.Size(
                    200,
                    40);

            this.btnProjectVersion.TabIndex =
                2;

            this.btnProjectVersion.Text =
                "当前图纸版本号输入";

            this.btnProjectVersion.UseVisualStyleBackColor =
                true;

            this.btnProjectVersion.Click +=
                new System.EventHandler(
                    this.btnProjectVersion_Click);

            // btnBatchProjectVersion

            this.btnBatchProjectVersion.Location =
                new System.Drawing.Point(
                    50,
                    260);

            this.btnBatchProjectVersion.Name =
                "btnBatchProjectVersion";

            this.btnBatchProjectVersion.Size =
                new System.Drawing.Size(
                    200,
                    40);

            this.btnBatchProjectVersion.TabIndex =
                3;

            this.btnBatchProjectVersion.Text =
                "批量版本号输入";

            this.btnBatchProjectVersion.UseVisualStyleBackColor =
                true;

            this.btnBatchProjectVersion.Click +=
                new System.EventHandler(
                    this.btnBatchProjectVersion_Click);

            // btnPathSettings

            this.btnPathSettings.Location =
                new System.Drawing.Point(
                    50,
                    320);

            this.btnPathSettings.Name =
                "btnPathSettings";

            this.btnPathSettings.Size =
                new System.Drawing.Size(
                    200,
                    40);

            this.btnPathSettings.TabIndex =
                4;

            this.btnPathSettings.Text =
                "路径设置";

            this.btnPathSettings.UseVisualStyleBackColor =
                true;

            this.btnPathSettings.Click +=
                new System.EventHandler(
                    this.btnPathSettings_Click);


            // lblTitle

            this.lblTitle.AutoSize =
                true;

            this.lblTitle.Font =
                new System.Drawing.Font(
                    "Microsoft Sans Serif",
                    12F,
                    System.Drawing.FontStyle.Bold,
                    System.Drawing.GraphicsUnit.Point,
                    ((byte)(0)));

            this.lblTitle.Location =
                new System.Drawing.Point(
                    46,
                    30);

            this.lblTitle.Name =
                "lblTitle";

            this.lblTitle.Size =
                new System.Drawing.Size(
                    120,
                    20);

            this.lblTitle.TabIndex =
                5;

            this.lblTitle.Text =
                "CAD检查助手";


            // CheckSelectForm

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
                    300,
                    405);



            this.Controls.Add(
                this.lblTitle);

            this.Controls.Add(
                this.btnPathSettings);

            this.Controls.Add(
                this.btnProjectVersion);
            this.Controls.Add(
    this.btnBatchProjectVersion);

            this.Controls.Add(
                this.btnBatch);

            this.Controls.Add(
                this.btnSingle);


            this.FormBorderStyle =
                System.Windows.Forms
                    .FormBorderStyle
                    .FixedDialog;

            this.MaximizeBox =
                false;

            this.MinimizeBox =
                false;

            this.Name =
                "CheckSelectForm";

            this.StartPosition =
                System.Windows.Forms
                    .FormStartPosition
                    .CenterParent;

            this.Text =
                "CAD检查助手";


            this.ResumeLayout(
                false);

            this.PerformLayout();
        }


        #endregion
        private System.Windows.Forms.Button
    btnBatchProjectVersion;

        private System.Windows.Forms.Button
            btnSingle;


        private System.Windows.Forms.Button
            btnBatch;


        private System.Windows.Forms.Button
            btnProjectVersion;


        private System.Windows.Forms.Button
            btnPathSettings;


        private System.Windows.Forms.Label
            lblTitle;
    }
}