using Autodesk.AutoCAD.Runtime;
using Correcet_test1;
using System.Windows.Forms;

namespace Correct_test1
{
    public class CommandEntry
    {

        [CommandMethod("CADCHECK")]
        public void OpenCheckWindow()
        {

            CheckForm form =
                new CheckForm();

            form.Show();

        }

    }
}