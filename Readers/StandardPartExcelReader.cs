using Correct_test1.Models;
using OfficeOpenXml;
using System.Collections.Generic;
using System.IO;

namespace Correct_test1.Readers
{
    public class StandardPartExcelReader
    {
        public List<StandardPart> Read(
            string filePath)
        {
            List<StandardPart> result =
                new List<StandardPart>();

            using (ExcelPackage package =
                new ExcelPackage(
                    new FileInfo(filePath)))
            {
                if (package.Workbook.Worksheets.Count == 0)
                {
                    return result;
                }

                ExcelWorksheet sheet =
                    package.Workbook.Worksheets[0];

                if (sheet == null || sheet.Dimension == null)
                {
                    return result;
                }

                int rows = sheet.Dimension.Rows;

                for (int r = 2; r <= rows; r++)
                {
                    StandardPart part =
                        new StandardPart
                        {
                            Name = sheet.Cells[r, 1].Text.Trim(),
                            ExportPartNumber = sheet.Cells[r, 2].Text.Trim(),
                            NationalPartNumber = sheet.Cells[r, 3].Text.Trim(),
                            Usage = sheet.Cells[r, 4].Text.Trim()
                        };

                    result.Add(part);
                }
            }

            return result;
        }
    }
}
