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

            List<BomData> allBoms = new List<BomData>();

            foreach (CadTableData table in tables)
            {
                if (!recognizer.IsBom(table))
                {
                    continue;
                }

                BomData bom = recognizer.Parse(table);
                allBoms.Add(bom);

                if (string.IsNullOrEmpty(report.DrawingNumber) &&
                    !string.IsNullOrEmpty(bom.DrawingNumber))
                {
                    report.DrawingNumber = bom.DrawingNumber;
                    report.DrawingNumberPosition = bom.DrawingNumberPosition;
                }

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

            // BOM序号与图纸零件序号一致性检查
            try
            {
                PartCalloutReader calloutReader = new PartCalloutReader();
                List<PartCallout> callouts = calloutReader.Read(database, allBoms);

                BomCalloutChecker calloutChecker = new BomCalloutChecker();
                report.BomCalloutIssues = calloutChecker.Check(allBoms, callouts);
            }
            catch (Exception ex)
            {
                Core.AppLogger.Error(ex, "CheckService.BomCalloutCheck");
            }

            return report;
        }
    }
}
