using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices;
using Correct_test1.Models;
using Correct_test1.Readers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Correct_test1.Checks
{
    public class CheckService
    {
        public CheckReport Check(Database database)
        {
            CheckReport report = new CheckReport();
            report.CheckTime = DateTime.Now;

            if (database == null)
            {
                return report;
            }

            report.DrawingName = database.Filename;

            CadTableReader tableReader =
                new CadTableReader();
            BomTableRecognizer recognizer =
                new BomTableRecognizer();
            BomStandardPartChecker checker =
                new BomStandardPartChecker();

            List<CadTableData> tables =
                tableReader.Read(database);

            foreach (CadTableData table in tables)
            {
                if (!recognizer.IsBom(table))
                {
                    continue;
                }

                BomData bom = recognizer.Parse(table);
                report.Results.AddRange(checker.Check(bom));
            }

            report.TotalCount = report.Results.Count;
            report.CorrectCount = report.Results.Count(
                result => result.Status == StandardPartCheckStatus.Correct);
            report.ErrorCount = report.TotalCount - report.CorrectCount;

            foreach (StandardPartCheckResult result in report.Results)
            {
                if (string.IsNullOrEmpty(report.DrawingNumber) &&
                    !string.IsNullOrEmpty(result.DrawingNumber))
                {
                    report.DrawingNumber = result.DrawingNumber;
                }
            }

            return report;
        }
    }
}
