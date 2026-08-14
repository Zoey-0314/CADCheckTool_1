using Correct_test1.Core;
using Correct_test1.Models;
using System.Collections.Generic;
using System.Globalization;

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
