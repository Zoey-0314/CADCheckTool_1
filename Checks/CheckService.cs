using Autodesk.AutoCAD.DatabaseServices;
using Correct_test1.Models;
using Correct_test1.Readers;
using System.Collections.Generic;

namespace Correct_test1.Checks
{
    public class CheckService
    {
        public List<StandardPartCheckResult> Check(Database database)
        {
            List<StandardPartCheckResult> results =
                new List<StandardPartCheckResult>();

            if (database == null)
            {
                return results;
            }

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
                results.AddRange(checker.Check(bom));
            }

            return results;
        }
    }
}
