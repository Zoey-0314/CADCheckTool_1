namespace Correcet_test1
{
    partial class CheckForm
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
            this.btnCheckDrawing = new System.Windows.Forms.Button();
            this.btnBatchCheck = new System.Windows.Forms.Button();
            this.btnClearMarker = new System.Windows.Forms.Button();
            this.SuspendLayout();


            // 
            // btnCheckDrawing
            // 
            this.btnCheckDrawing.Location = new System.Drawing.Point(220, 80);
            this.btnCheckDrawing.Name = "btnCheckDrawing";
            this.btnCheckDrawing.Size = new System.Drawing.Size(150, 60);
            this.btnCheckDrawing.TabIndex = 0;
            this.btnCheckDrawing.Text = "检查当前图纸";
            this.btnCheckDrawing.UseVisualStyleBackColor = true;
            this.btnCheckDrawing.Click += new System.EventHandler(this.button1_Click);



            //
            // btnBatchCheck
            //
            this.btnBatchCheck.Location = new System.Drawing.Point(220, 160);
            this.btnBatchCheck.Name = "btnBatchCheck";
            this.btnBatchCheck.Size = new System.Drawing.Size(150, 60);
            this.btnBatchCheck.TabIndex = 1;
            this.btnBatchCheck.Text = "批量检查";
            this.btnBatchCheck.UseVisualStyleBackColor = true;
            this.btnBatchCheck.Click += new System.EventHandler(this.btnBatchCheck_Click);



            //
            // btnClearMarker
            //
            this.btnClearMarker.Location = new System.Drawing.Point(220, 240);
            this.btnClearMarker.Name = "btnClearMarker";
            this.btnClearMarker.Size = new System.Drawing.Size(150, 60);
            this.btnClearMarker.TabIndex = 2;
            this.btnClearMarker.Text = "清除检查标记";
            this.btnClearMarker.UseVisualStyleBackColor = true;
            this.btnClearMarker.Click += new System.EventHandler(this.btnClearMarker_Click);



            // 
            // CheckForm
            // 
            this.AutoScaleDimensions =
                new System.Drawing.SizeF(6F, 12F);

            this.AutoScaleMode =
                System.Windows.Forms.AutoScaleMode.Font;


            this.ClientSize =
                new System.Drawing.Size(600, 400);


            this.Controls.Add(this.btnClearMarker);
            this.Controls.Add(this.btnBatchCheck);
            this.Controls.Add(this.btnCheckDrawing);


            this.Name = "CheckForm";
            this.Text = "CAD检查助手";

            this.ResumeLayout(false);

        }


        #endregion



        private System.Windows.Forms.Button btnCheckDrawing;

        private System.Windows.Forms.Button btnBatchCheck;

        private System.Windows.Forms.Button btnClearMarker;

    }
}