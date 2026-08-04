namespace Correcet_test1
{
    partial class CheckForm
    {


        private System.ComponentModel.IContainer components = null;


        protected override void Dispose(bool disposing)
        {
            if (disposing &&
                components != null)
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }



        private void InitializeComponent()
        {
            this.btnCheckDrawing = new System.Windows.Forms.Button();
            this.btnClearMarker = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnCheckDrawing
            // 
            this.btnCheckDrawing.Location = new System.Drawing.Point(162, 58);
            this.btnCheckDrawing.Name = "btnCheckDrawing";
            this.btnCheckDrawing.Size = new System.Drawing.Size(150, 60);
            this.btnCheckDrawing.TabIndex = 0;
            this.btnCheckDrawing.Text = "检查当前图纸";
            this.btnCheckDrawing.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnClearMarker
            // 
            this.btnClearMarker.Location = new System.Drawing.Point(162, 174);
            this.btnClearMarker.Name = "btnClearMarker";
            this.btnClearMarker.Size = new System.Drawing.Size(150, 60);
            this.btnClearMarker.TabIndex = 1;
            this.btnClearMarker.Text = "清除检查标记";
            this.btnClearMarker.Click += new System.EventHandler(this.btnClearMarker_Click);
            // 
            // CheckForm
            // 
            this.ClientSize = new System.Drawing.Size(489, 325);
            this.Controls.Add(this.btnCheckDrawing);
            this.Controls.Add(this.btnClearMarker);
            this.Name = "CheckForm";
            this.Text = "CAD检查助手";
            this.ResumeLayout(false);

        }


        private System.Windows.Forms.Button btnCheckDrawing;
        private System.Windows.Forms.Button btnClearMarker;


    }
}