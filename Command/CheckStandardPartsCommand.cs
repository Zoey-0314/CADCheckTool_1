using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Correct_test1.Checks;
using Correct_test1.Models;
using Correct_test1.Readers;
using System.Collections.Generic;
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

                CadTableReader tableReader =
                    new CadTableReader();
                BomTableRecognizer recognizer =
                    new BomTableRecognizer();
                BomStandardPartChecker checker =
                    new BomStandardPartChecker();

                List<CadTableData> tables =
                    tableReader.Read(
                        Application.DocumentManager.MdiActiveDocument.Database);

                StringBuilder output = new StringBuilder();
                output.AppendLine("====================");
                output.AppendLine("标准件检查结果");

                int bomCount = 0;
                foreach (CadTableData table in tables)
                {
                    if (!recognizer.IsBom(table))
                    {
                        continue;
                    }

                    BomData bom = recognizer.Parse(table);
                    List<StandardPartCheckResult> results =
                        checker.Check(bom);
                    bomCount++;

                    foreach (StandardPartCheckResult result in results)
                    {
                        output.AppendLine();
                        output.AppendLine("图号:");
                        output.AppendLine(bom.DrawingNumber);
                        output.AppendLine();
                        output.AppendLine("No:" + result.BomItem.No);
                        output.AppendLine();
                        output.AppendLine("当前图号:");
                        output.AppendLine(result.BomItem.PartNumber);
                        output.AppendLine();
                        output.AppendLine("状态:");
                        output.AppendLine(result.Status.ToString());

                        if (!string.IsNullOrEmpty(result.CorrectPartNumber))
                        {
                            output.AppendLine();
                            output.AppendLine("标准图号:");
                            output.AppendLine(result.CorrectPartNumber);
                        }

                        if (!string.IsNullOrEmpty(result.CorrectName))
                        {
                            output.AppendLine();
                            output.AppendLine("名称:");
                            output.AppendLine(result.CorrectName);
                        }

                        if (!string.IsNullOrEmpty(result.Message))
                        {
                            output.AppendLine();
                            output.AppendLine(result.Message);
                        }

                        output.AppendLine();
                        output.AppendLine("====================");
                    }
                }

                if (bomCount == 0)
                {
                    output.AppendLine();
                    output.AppendLine("未找到BOM表");
                }

                editor.WriteMessage("\n" + output.ToString());
            }
            catch (System.Exception ex)
            {
                Application.ShowAlertDialog(ex.Message);
            }
        }
    }
}
