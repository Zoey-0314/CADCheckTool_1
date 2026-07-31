namespace Correcet_test1
{
    partial class CheckForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnCheckDrawing = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnCheckDrawing
            // 
            this.btnCheckDrawing.Location = new System.Drawing.Point(223, 132);
            this.btnCheckDrawing.Name = "btnCheckDrawing";
            this.btnCheckDrawing.Size = new System.Drawing.Size(144, 69);
            this.btnCheckDrawing.TabIndex = 0;
            this.btnCheckDrawing.Text = "检查当前图纸";
            this.btnCheckDrawing.UseVisualStyleBackColor = true;
            this.btnCheckDrawing.Click += new System.EventHandler(this.button1_Click);
            // 
            // CheckForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btnCheckDrawing);
            this.Name = "CheckForm";
            this.Text = "CheckForm";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnCheckDrawing;
    }
}