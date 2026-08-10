using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Correct_test1.Checks;
using Correct_test1.Markers;
using Correct_test1.Models;
using System.Text;

namespace Correct_test1.Command
{
    public class CheckStandardPartsCommand
    {
        [CommandMethod("CHECKSTANDARDPARTS")]
        public void CheckStandardParts()
        {
            try
            {
                Editor editor =
                    Application.DocumentManager.MdiActiveDocument.Editor;

                CheckService checkService =
                    new CheckService();
                CheckReport report =
                    checkService.Check(
                        Application.DocumentManager.MdiActiveDocument.Database);

                MarkerManager markerManager =
                    new MarkerManager();
                markerManager.CreateMarkers(
                    Application.DocumentManager.MdiActiveDocument.Database,
                    report.Results);

                StringBuilder output = new StringBuilder();
                output.AppendLine("====================");
                output.AppendLine("标准件检查报告");
                output.AppendLine();
                output.AppendLine("图纸:");
                output.AppendLine(report.DrawingName);
                output.AppendLine();
                output.AppendLine("图号:");
                output.AppendLine(report.DrawingNumber);
                output.AppendLine();
                output.AppendLine("检查数量:");
                output.AppendLine(report.TotalCount.ToString());
                output.AppendLine();
                output.AppendLine("正确:");
                output.AppendLine(report.CorrectCount.ToString());
                output.AppendLine();
                output.AppendLine("错误:");
                output.AppendLine(report.ErrorCount.ToString());
                output.AppendLine();
                output.AppendLine("====================");

                editor.WriteMessage("\n" + output.ToString());
            }
            catch (System.Exception ex)
            {
                Application.ShowAlertDialog(ex.Message);
            }
        }
    }
}
