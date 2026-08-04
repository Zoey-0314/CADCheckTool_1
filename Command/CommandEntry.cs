using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Correcet_test1;
using Correct_test1.Checks;
using Correct_test1.Models;
using Correct_test1.Readers;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Correcet_test1
{
    public class CommandEntry
    {
        [CommandMethod("CHECKDRAWING")]
        public void OpenCheckForm()
        {
            try
            {
                // Open the new selection form as the plugin entry
                Correct_test1.CheckSelectForm form =
                    new Correct_test1.CheckSelectForm();

                Autodesk.AutoCAD.ApplicationServices.Application
                    .ShowModelessDialog(form);
            }
            catch (System.Exception ex)
            {
                Autodesk.AutoCAD.ApplicationServices.Application
                    .ShowAlertDialog(
                        ex.Message
                    );
            }
        }
        
    }
}