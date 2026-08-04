namespace Correct_test1
{
    partial class BatchCheckForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.btnRunBatch = new System.Windows.Forms.Button();
            this.btnOpenReport = new System.Windows.Forms.Button();
            this.btnClearCurrent = new System.Windows.Forms.Button();
            this.btnClearFolder = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold,
                System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.Location = new System.Drawing.Point(24, 18);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(120, 20);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "批量图纸检查";
            // 
            // btnRunBatch
            // 
            this.btnRunBatch.Location = new System.Drawing.Point(28, 60);
            this.btnRunBatch.Name = "btnRunBatch";
            this.btnRunBatch.Size = new System.Drawing.Size(220, 40);
            this.btnRunBatch.TabIndex = 1;
            this.btnRunBatch.Text = "执行批量检查";
            this.btnRunBatch.UseVisualStyleBackColor = true;
            this.btnRunBatch.Click += new System.EventHandler(this.btnRunBatch_Click);
            // 
            // btnOpenReport
            // 
            this.btnOpenReport.Location = new System.Drawing.Point(28, 110);
            this.btnOpenReport.Name = "btnOpenReport";
            this.btnOpenReport.Size = new System.Drawing.Size(220, 40);
            this.btnOpenReport.TabIndex = 2;
            this.btnOpenReport.Text = "打开批量检查报告";
            this.btnOpenReport.UseVisualStyleBackColor = true;
            this.btnOpenReport.Click += new System.EventHandler(this.btnOpenReport_Click);
            // 
            // btnClearCurrent
            // 
            this.btnClearCurrent.Location = new System.Drawing.Point(28, 160);
            this.btnClearCurrent.Name = "btnClearCurrent";
            this.btnClearCurrent.Size = new System.Drawing.Size(220, 40);
            this.btnClearCurrent.TabIndex = 3;
            this.btnClearCurrent.Text = "清除当前图纸修改注释";
            this.btnClearCurrent.UseVisualStyleBackColor = true;
            this.btnClearCurrent.Click += new System.EventHandler(this.btnClearCurrent_Click);
            // 
            // btnClearFolder
            // 
            this.btnClearFolder.Location = new System.Drawing.Point(28, 210);
            this.btnClearFolder.Name = "btnClearFolder";
            this.btnClearFolder.Size = new System.Drawing.Size(220, 40);
            this.btnClearFolder.TabIndex = 4;
            this.btnClearFolder.Text = "清除所有图纸修改注释";
            this.btnClearFolder.UseVisualStyleBackColor = true;
            this.btnClearFolder.Click += new System.EventHandler(this.btnClearFolder_Click);
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(28, 260);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(220, 30);
            this.btnClose.TabIndex = 5;
            this.btnClose.Text = "关闭";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // BatchCheckForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(280, 310);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnClearFolder);
            this.Controls.Add(this.btnClearCurrent);
            this.Controls.Add(this.btnOpenReport);
            this.Controls.Add(this.btnRunBatch);
            this.Controls.Add(this.lblTitle);

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BatchCheckForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "批量图纸检查";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Button btnRunBatch;
        private System.Windows.Forms.Button btnOpenReport;
        private System.Windows.Forms.Button btnClearCurrent;
        private System.Windows.Forms.Button btnClearFolder;
        private System.Windows.Forms.Button btnClose;
    }
}
