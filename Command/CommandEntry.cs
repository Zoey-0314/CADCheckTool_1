using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Correct_test1;
using Correct_test1.Core;
using System;

namespace Correct_test1
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