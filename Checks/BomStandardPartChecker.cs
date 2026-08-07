using Correct_test1.Core;
using Correct_test1.Models;
using System;
using System.Collections.Generic;

namespace Correct_test1.Checks
{
    public class BomStandardPartChecker
    {
        public List<StandardPartCheckResult> Check(BomData bom)
        {
            List<StandardPartCheckResult> results =
                new List<StandardPartCheckResult>();

            if (bom == null || bom.Items == null)
            {
                return results;
            }

            foreach (BomItem item in bom.Items)
            {
                if (PartNumberTypeClassifier.Classify(item.PartNumber)
                    == PartNumberType.NonStandardPart)
                {
                    continue;
                }

                List<StandardPart> matches =
                    StandardPartDatabase.FindByPartNumber(item.PartNumber);

                StandardPartCheckResult result =
                    new StandardPartCheckResult();
                result.BomItem = item;
                result.DrawingNumber = bom.DrawingNumber;

                if (matches.Count == 0)
                {
                    result.Status = StandardPartCheckStatus.NotRegistered;
                    result.Message = "标准件库未收录";
                }
                else if (matches.Count > 1)
                {
                    result.Status = StandardPartCheckStatus.MultipleMatch;
                    result.Message = "标准件图号匹配到多个标准件";
                }
                else
                {
                    StandardPart standardPart = matches[0];
                    result.StandardPart = standardPart;
                    result.CorrectPartNumber = standardPart.ExportPartNumber;
                    result.CorrectName = standardPart.Name;

                    if (!string.Equals(
                        item.Name == null ? "" : item.Name.Trim(),
                        standardPart.Name == null ? "" : standardPart.Name.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                    {
                        result.Status = StandardPartCheckStatus.NameError;
                        result.Message = "标准件名称不一致";
                    }
                    else if (PartNumberNormalizer.StrictEquals(
                        item.PartNumber,
                        standardPart.ExportPartNumber))
                    {
                        result.Status = StandardPartCheckStatus.Correct;
                    }
                    else
                    {
                        result.Status = StandardPartCheckStatus.FormatDifference;
                        result.Message = "标准件图号格式不同";
                    }
                }

                result.BomRow = item.BomRow;
                if (result.Status == StandardPartCheckStatus.NameError)
                {
                    result.BomColumn = item.NameColumn;
                    result.CellPosition = item.NameCellPosition;
                }
                else
                {
                    result.BomColumn = item.PartNumberColumn;
                    result.CellPosition = item.PartNumberCellPosition;
                }

                results.Add(result);
            }

            return results;
        }
    }
}
