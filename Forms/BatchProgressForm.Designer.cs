using System.Windows.Forms;

namespace Correct_test1
{
    partial class BatchProgressForm
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

            this.progressBar1 =
                new System.Windows.Forms.ProgressBar();


            this.lblStatus =
                new System.Windows.Forms.Label();



            this.SuspendLayout();



            // progressBar

            this.progressBar1.Location =
                new System.Drawing.Point(30, 80);


            this.progressBar1.Size =
                new System.Drawing.Size(420, 30);


            this.progressBar1.Minimum = 0;


            this.progressBar1.Maximum = 100;




            // label

            this.lblStatus.Location =
                new System.Drawing.Point(30, 30);


            this.lblStatus.Size =
                new System.Drawing.Size(420, 40);


            this.lblStatus.Text =
                "准备检查";




            // Form

            this.ClientSize =
                new System.Drawing.Size(
                    500,
                    150
                );


            this.Controls.Add(
                this.progressBar1
            );


            this.Controls.Add(
                this.lblStatus
            );


            this.FormBorderStyle =
                System.Windows.Forms.FormBorderStyle.FixedDialog;


            this.MaximizeBox = false;


            this.MinimizeBox = false;


            this.StartPosition =
                FormStartPosition.CenterScreen;


            this.Text =
                "批量检查进度";



            this.ResumeLayout(false);

        }



        private System.Windows.Forms.ProgressBar progressBar1;

        private System.Windows.Forms.Label lblStatus;

    }
}