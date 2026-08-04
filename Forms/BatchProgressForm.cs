using System;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;


namespace Correct_test1
{

    public partial class BatchProgressForm : Form
    {


        public BatchProgressForm()
        {
            InitializeComponent();
        }




        public void UpdateProgress(
            int percent,
            string fileName)
        {

            if (percent < 0)
                percent = 0;


            if (percent > 100)
                percent = 100;



            progressBar1.Value =
                percent;



            lblStatus.Text =
                "正在检查："
                +
                fileName
                +
                "\n完成度："
                +
                percent
                +
                "%";



            Application.DoEvents();

        }



    }

}