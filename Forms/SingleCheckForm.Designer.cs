namespace Correct_test1
{
    partial class SingleCheckForm
    {
        private System.ComponentModel.IContainer components = null;


        protected override void Dispose(
            bool disposing)
        {
            if (disposing &&
                (components != null))
            {
                components.Dispose();
            }

            base.Dispose(
                disposing);
        }


        #region Windows Form Designer generated code


        private void InitializeComponent()
        {
            this.lblTitle =
                new System.Windows.Forms.Label();

            this.btnCheck =
                new System.Windows.Forms.Button();

            this.btnClear =
                new System.Windows.Forms.Button();

            this.btnQuickRevision =
                new System.Windows.Forms.Button();

            this.btnClearQuickRevision =
                new System.Windows.Forms.Button();

            this.btnClose =
                new System.Windows.Forms.Button();


            this.SuspendLayout();


            //==================================================
            // lblTitle
            //==================================================

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
                    24,
                    18);

            this.lblTitle.Name =
                "lblTitle";

            this.lblTitle.Size =
                new System.Drawing.Size(
                    120,
                    20);

            this.lblTitle.TabIndex =
                0;

            this.lblTitle.Text =
                "单张图纸检查";


            //==================================================
            // btnCheck
            //==================================================

            this.btnCheck.Location =
                new System.Drawing.Point(
                    28,
                    60);

            this.btnCheck.Name =
                "btnCheck";

            this.btnCheck.Size =
                new System.Drawing.Size(
                    220,
                    40);

            this.btnCheck.TabIndex =
                1;

            this.btnCheck.Text =
                "检查当前图纸";

            this.btnCheck.UseVisualStyleBackColor =
                true;

            this.btnCheck.Click +=
                new System.EventHandler(
                    this.btnCheck_Click);


            //==================================================
            // btnClear
            //==================================================

            this.btnClear.Location =
                new System.Drawing.Point(
                    28,
                    110);

            this.btnClear.Name =
                "btnClear";

            this.btnClear.Size =
                new System.Drawing.Size(
                    220,
                    40);

            this.btnClear.TabIndex =
                2;

            this.btnClear.Text =
                "清除检查标记";

            this.btnClear.UseVisualStyleBackColor =
                true;

            this.btnClear.Click +=
                new System.EventHandler(
                    this.btnClear_Click);


            //==================================================
            // btnQuickRevision
            //==================================================

            this.btnQuickRevision.Location =
                new System.Drawing.Point(
                    28,
                    160);

            this.btnQuickRevision.Name =
                "btnQuickRevision";

            this.btnQuickRevision.Size =
                new System.Drawing.Size(
                    220,
                    40);

            this.btnQuickRevision.TabIndex =
                3;

            this.btnQuickRevision.Text =
                "快速划改";

            this.btnQuickRevision.UseVisualStyleBackColor =
                true;

            this.btnQuickRevision.Click +=
                new System.EventHandler(
                    this.btnQuickRevision_Click);


            //==================================================
            // btnClearQuickRevision
            //==================================================

            this.btnClearQuickRevision.Location =
                new System.Drawing.Point(
                    28,
                    210);

            this.btnClearQuickRevision.Name =
                "btnClearQuickRevision";

            this.btnClearQuickRevision.Size =
                new System.Drawing.Size(
                    220,
                    40);

            this.btnClearQuickRevision.TabIndex =
                4;

            this.btnClearQuickRevision.Text =
                "清除划改";

            this.btnClearQuickRevision.UseVisualStyleBackColor =
                true;

            this.btnClearQuickRevision.Click +=
                new System.EventHandler(
                    this.btnClearQuickRevision_Click);


            //==================================================
            // btnClose
            //==================================================

            this.btnClose.Location =
                new System.Drawing.Point(
                    28,
                    270);

            this.btnClose.Name =
                "btnClose";

            this.btnClose.Size =
                new System.Drawing.Size(
                    220,
                    30);

            this.btnClose.TabIndex =
                5;

            this.btnClose.Text =
                "关闭";

            this.btnClose.UseVisualStyleBackColor =
                true;

            this.btnClose.Click +=
                new System.EventHandler(
                    this.btnClose_Click);


            //==================================================
            // SingleCheckForm
            //==================================================

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
                    280,
                    325);


            this.Controls.Add(
                this.btnClose);

            this.Controls.Add(
                this.btnClearQuickRevision);

            this.Controls.Add(
                this.btnQuickRevision);

            this.Controls.Add(
                this.btnClear);

            this.Controls.Add(
                this.btnCheck);

            this.Controls.Add(
                this.lblTitle);


            this.FormBorderStyle =
                System.Windows.Forms
                    .FormBorderStyle
                    .FixedDialog;


            this.MaximizeBox =
                false;

            this.MinimizeBox =
                false;


            this.Name =
                "SingleCheckForm";


            //--------------------------------
            // 现在这是Modeless窗口，
            // 使用CenterScreen更合适。
            //--------------------------------

            this.StartPosition =
                System.Windows.Forms
                    .FormStartPosition
                    .CenterScreen;


            this.Text =
                "单张图纸检查";


            //--------------------------------
            // 无论关闭按钮还是右上角X，
            // 都检查是否需要退出QREVMODE。
            //--------------------------------

            this.FormClosing +=
                new System.Windows.Forms
                    .FormClosingEventHandler(
                        this.SingleCheckForm_FormClosing);


            this.ResumeLayout(
                false);

            this.PerformLayout();
        }


        #endregion


        private System.Windows.Forms.Label
            lblTitle;

        private System.Windows.Forms.Button
            btnCheck;

        private System.Windows.Forms.Button
            btnClear;

        private System.Windows.Forms.Button
            btnQuickRevision;

        private System.Windows.Forms.Button
            btnClearQuickRevision;

        private System.Windows.Forms.Button
            btnClose;
    }
}