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

            string databaseError;


            if (!StandardPartDatabase.TryEnsureLoaded(
                    out databaseError))
            {
                // 数据库不可访问时，
                // 本次标准件检查跳过。
                //
                // 不能把所有件都误判为“未收录”。

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
                    StandardPartDatabase.FindByPartNumberLoaded(item.PartNumber);

                StandardPartCheckResult result =
                    new StandardPartCheckResult();

                result.BomItem = item;
                result.DrawingNumber = bom.DrawingNumber;
                result.SourceLayoutName = bom.SourceLayoutName;

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
                    result.MatchSource = GetMatchSource(item.PartNumber, standardPart);
                    result.CorrectPartNumber = result.MatchSource
                        == StandardPartMatchSource.NationalPartNumber
                        ? standardPart.NationalPartNumber
                        : standardPart.ExportPartNumber;
                    result.CorrectName = standardPart.Name;

                    bool nameError =
    !string.Equals(
        item.Name == null ? "" : item.Name.Trim(),
        standardPart.Name == null ? "" : standardPart.Name.Trim(),
        StringComparison.OrdinalIgnoreCase);

                    bool formatError =
                        !PartNumberNormalizer.StrictEquals(
                            item.PartNumber,
                            result.CorrectPartNumber);


                    // 名称错误
                    if (nameError)
                    {
                        result.Status =
                            StandardPartCheckStatus.NameError;

                        result.Message =
                            "标准件名称不一致";


                        // 如果格式同时也错误，再单独生成一个格式错误结果
                        if (formatError)
                        {
                            StandardPartCheckResult formatResult =
                                new StandardPartCheckResult();

                            formatResult.BomItem = item;
                            formatResult.DrawingNumber = bom.DrawingNumber;
                            formatResult.SourceLayoutName = bom.SourceLayoutName;
                            formatResult.StandardPart = standardPart;
                            formatResult.MatchSource = result.MatchSource;
                            formatResult.CorrectPartNumber =
                                result.CorrectPartNumber;
                            formatResult.CorrectName =
                                result.CorrectName;

                            formatResult.Status =
                                StandardPartCheckStatus.FormatDifference;

                            formatResult.Message =
                                "标准件图号格式不同";

                            formatResult.BomRow =
                                item.BomRow;

                            formatResult.BomColumn =
                                item.PartNumberColumn;

                            formatResult.CellPosition =
                                item.PartNumberCellPosition;

                            results.Add(formatResult);
                        }
                    }


                    // 名称正确，但格式错误
                    else if (formatError)
                    {
                        result.Status =
                            StandardPartCheckStatus.FormatDifference;

                        result.Message =
                            "标准件图号格式不同";
                    }


                    // 两个都正确
                    else
                    {
                        result.Status =
                            StandardPartCheckStatus.Correct;
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

        private static StandardPartMatchSource GetMatchSource(
            string partNumber,
            StandardPart standardPart)
        {
            if (PartNumberNormalizer.StrictEquals(
                partNumber,
                standardPart.ExportPartNumber))
            {
                return StandardPartMatchSource.ExportPartNumber;
            }

            if (PartNumberNormalizer.StrictEquals(
                partNumber,
                standardPart.NationalPartNumber))
            {
                return StandardPartMatchSource.NationalPartNumber;
            }

            if (PartNumberNormalizer.LooseEquals(
                partNumber,
                standardPart.ExportPartNumber))
            {
                return StandardPartMatchSource.ExportPartNumber;
            }

            if (PartNumberNormalizer.LooseEquals(
                partNumber,
                standardPart.NationalPartNumber))
            {
                return StandardPartMatchSource.NationalPartNumber;
            }

            return StandardPartMatchSource.None;
        }
    }
}
