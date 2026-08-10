using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Correct_test1.Installer;
using System;


namespace Correct_test1.Commands
{
    public class TestInstallerCommand
    {


        [CommandMethod("TESTINSTALLER")]
        public void TestInstall()
        {

            Editor editor =
                Application.DocumentManager
                .MdiActiveDocument
                .Editor;


            try
            {

                string installDirectory =
                    @"C:\Program Files\CADCheckTool_1";


                editor.WriteMessage(
                    "\n[Installer Test] Install Directory:"
                    + installDirectory);



                CADCheckToolInstaller installer =
                    new CADCheckToolInstaller(
                        installDirectory);



                installer.Install();



                editor.WriteMessage(
                    "\n[Installer Test] Install Success");


                editor.WriteMessage(
                    "\nRegistered:"
                    + installer.InstallationDirectory);


            }
            catch (System.Exception ex)
            {

                editor.WriteMessage(
                    "\n[Installer Test] Failed:"
                    + ex.Message);

            }

        }





        [CommandMethod("TESTUNINSTALLER")]
        public void TestUninstall()
        {

            Editor editor =
                Application.DocumentManager
                .MdiActiveDocument
                .Editor;


            try
            {

                string installDirectory =
                    @"C:\Program Files\CADCheckTool_1";



                CADCheckToolInstaller installer =
                    new CADCheckToolInstaller(
                        installDirectory);



                installer.Uninstall();



                editor.WriteMessage(
                    "\n[Installer Test] Uninstall Success");

            }
            catch (System.Exception ex)
            {

                editor.WriteMessage(
                    "\n[Installer Test] Uninstall Failed:"
                    + ex.Message);

            }

        }


    }
}