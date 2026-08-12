using Correct_test1.Core;
using Correct_test1.Models;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Correct_test1.Checks
{
    public class BomCalloutChecker
    {
        public HashSet<int> GetBomNumbers(BomData bom)
        {
            HashSet<int> numbers = new HashSet<int>();

            if (bom == null || bom.Items == null)
                return numbers;

            foreach (BomItem item in bom.Items)
            {
                int number;
                if (item != null && TryGetNumber(item.No, out number))
                    numbers.Add(number);
            }

            return numbers;
        }

        public List<BomCalloutIssue> Check(
            BomData bom,
            IEnumerable<PartCallout> drawingCallouts)
        {
            List<BomCalloutIssue> issues = new List<BomCalloutIssue>();
            HashSet<int> bomNumbers = GetBomNumbers(bom);
            List<PartCallout> callouts = drawingCallouts == null
                ? new List<PartCallout>()
                : drawingCallouts.ToList();
            HashSet<int> drawingNumbers = new HashSet<int>(
                callouts.Select(callout => callout.Number));

            foreach (int number in bomNumbers)
            {
                if (drawingNumbers.Contains(number))
                    continue;

                BomItem item = bom.Items.FirstOrDefault(x =>
                {
                    int itemNumber;
                    return x != null && TryGetNumber(x.No, out itemNumber) && itemNumber == number;
                });

                issues.Add(new BomCalloutIssue
                {
                    Type = BomCalloutIssueType.MissingDrawingCallout,
                    Number = number,
                    Position = item == null ? default(Autodesk.AutoCAD.Geometry.Point3d) : item.NoCellPosition,
                    Message = "BOM序号" + number + "未在图中标注"
                });
            }

            foreach (PartCallout callout in callouts)
            {
                if (bomNumbers.Contains(callout.Number))
                    continue;

                issues.Add(new BomCalloutIssue
                {
                    Type = BomCalloutIssueType.ExtraDrawingCallout,
                    Number = callout.Number,
                    LayoutName = callout.LayoutName,
                    Position = callout.TextPosition,
                    SpaceId = callout.SpaceId,
                    Message = "图中序号" + callout.Number + "不在BOM中"
                });
            }

            return issues;
        }

        public BomCalloutResult Check(
            HashSet<int> bomNumbers,
            HashSet<int> drawingNumbers)
        {
            HashSet<int> missingCallouts =
                bomNumbers == null
                ? new HashSet<int>()
                : new HashSet<int>(bomNumbers);

            HashSet<int> extraCallouts =
                drawingNumbers == null
                ? new HashSet<int>()
                : new HashSet<int>(drawingNumbers);

            if (drawingNumbers != null)
                missingCallouts.ExceptWith(drawingNumbers);

            if (bomNumbers != null)
                extraCallouts.ExceptWith(bomNumbers);

            return new BomCalloutResult
            {
                MissingCallouts = missingCallouts,
                ExtraCallouts = extraCallouts
            };
        }

        private static bool TryGetNumber(string text, out int number)
        {
            string cleaned = CadTextCleaner.Clean(text);
            return int.TryParse(
                cleaned,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out number);
        }
    }
}
