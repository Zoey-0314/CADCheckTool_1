using System;
using System.Windows.Forms;

namespace Correct_test1
{
    public partial class CheckSelectForm : Form
    {
        public CheckSelectForm()
        {
            InitializeComponent();
        }

        private void btnSingle_Click(object sender, EventArgs e)
        {
            // Try to open SingleCheckForm if exists, otherwise notify
            Type t = Type.GetType("Correct_test1.SingleCheckForm");
            if (t != null)
            {
                try
                {
                    Form f = (Form)Activator.CreateInstance(t);
                    f.ShowDialog(this);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to open SingleCheckForm: " + ex.Message, "Error");
                }
            }
            else
            {
                MessageBox.Show("SingleCheckForm is not available yet.", "Info");
            }
        }

        private void btnBatch_Click(object sender, EventArgs e)
        {
            // Try to open BatchCheckForm if exists, otherwise notify
            Type t = Type.GetType("Correct_test1.BatchCheckForm");
            if (t != null)
            {
                try
                {
                    Form f = (Form)Activator.CreateInstance(t);
                    f.ShowDialog(this);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to open BatchCheckForm: " + ex.Message, "Error");
                }
            }
            else
            {
                MessageBox.Show("BatchCheckForm is not available yet.", "Info");
            }
        }
    }
}
