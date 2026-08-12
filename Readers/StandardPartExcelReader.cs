using Correct_test1.Models;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;


namespace Correct_test1.Readers
{

    public class StandardPartExcelReader
    {


        public List<StandardPart> Read(
            string filePath)
        {

            List<StandardPart> result =
                new List<StandardPart>();



            using (
                ExcelPackage package =
                new ExcelPackage(
                    new FileInfo(filePath)))
            {

                var ed =
    Application.DocumentManager
    .MdiActiveDocument
    .Editor;


                ed.WriteMessage(
                    "\nWorksheet数量:"
                    + package.Workbook.Worksheets.Count
                );


                foreach (var ws in package.Workbook.Worksheets)
                {
                    ed.WriteMessage(
                        "\nSheet:"
                        + ws.Name
                    );
                }
                ExcelWorksheet sheet;


                if (package.Workbook.Worksheets.Count == 0)
                {
                    return result;
                }


                sheet =
                    package.Workbook.Worksheets[0];

                if (sheet == null || sheet.Dimension == null)
                {
                    return result;
                }

                int rows =
                    sheet.Dimension.Rows;



                for (int r = 2;
                    r <= rows;
                    r++)
                {

                    StandardPart part =
                        new StandardPart();



                    part.Name =
                        sheet.Cells[r, 1]
                        .Text.Trim();



                    part.ExportPartNumber =
                        sheet.Cells[r, 2]
                        .Text.Trim();



                    part.NationalPartNumber =
                        sheet.Cells[r, 3]
                        .Text.Trim();



                    part.Usage =
                        sheet.Cells[r, 4]
                        .Text.Trim();



                    result.Add(part);

                }


            }

            Autodesk.AutoCAD.ApplicationServices.Application
            .DocumentManager
            .MdiActiveDocument
            .Editor
            .WriteMessage(
                "\nStandardPartExcelReader count: "
                + result.Count
            );

            return result;

        }

    }

}