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
            BomCalloutChecker calloutChecker =
                new BomCalloutChecker();

            List<CadTableData> tables =
                tableReader.Read(database);
            List<BomData> boms = new List<BomData>();

            foreach (CadTableData table in tables)
            {
                if (!recognizer.IsBom(table))
                {
                    continue;
                }

                BomData bom = recognizer.Parse(table);
                boms.Add(bom);

                if (string.IsNullOrEmpty(report.DrawingNumber) &&
                    !string.IsNullOrEmpty(bom.DrawingNumber))
                {
                    report.DrawingNumber = bom.DrawingNumber;
                    report.DrawingNumberPosition = bom.DrawingNumberPosition;
                }

                report.Results.AddRange(checker.Check(bom));
            }

            HashSet<int> bomNumbers = new HashSet<int>();
            foreach (BomData bom in boms)
            {
                bomNumbers.UnionWith(calloutChecker.GetBomNumbers(bom));
            }

            LayoutReader layoutReader = new LayoutReader();

            List<LayoutInfo> layouts =
                layoutReader.ReadLayouts(database, null)
                    .Where(x => x != null && !x.IsModelSpace)
                    .ToList();
            Dictionary<string, bool> layoutDirections =
    new Dictionary<string, bool>(
        StringComparer.OrdinalIgnoreCase);

            FrameReader frameReader = new FrameReader();

            foreach (LayoutInfo layout in layouts)
            {
                FrameInfo frame =
                    frameReader.ReadFrame(
                        database,
                        layout.BlockTableRecordId,
                        null);

                if (frame.Direction == "Horizontal")
                {
                    layoutDirections[layout.LayoutName] = true;
                }
                else if (frame.Direction == "Vertical")
                {
                    layoutDirections[layout.LayoutName] = false;
                }
            }
            ViewportTextReader viewportTextReader =
    new ViewportTextReader();

            ViewportLineReader viewportLineReader =
                new ViewportLineReader();
            List<TitleText> drawingTexts =
    viewportTextReader.Read(
        database,
        true);


            List<CadLineInfo> drawingLines =
                viewportLineReader.Read(
                    database,
                    true);
            HashSet<int> drawingNumbers =
                layoutReader.IdentifyDrawingBomNumbers(
                    drawingTexts,
                    drawingLines,
                    bomNumbers,
                    layoutDirections);

            report.BomCalloutResult = calloutChecker.Check(
                bomNumbers,
                drawingNumbers);

            List<PartCallout> drawingCallouts =
                new PartCalloutReader().Read(database, bomNumbers);

            foreach (BomData bom in boms)
            {
                report.BomCalloutIssues.AddRange(
                    calloutChecker.Check(bom, drawingCallouts));
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
