using Correct_test1.Models;
using Correct_test1.Core;
using System.Collections.Generic;


namespace Correct_test1.Checks
{

    public class StandardPartChecker
    {


        public StandardPartCheckResult Check(
            BomItem item)
        {


            StandardPartCheckResult result =
                new StandardPartCheckResult();


            result.BomItem = item;



            List<StandardPart> matches =
                StandardPartDatabase
                .FindByPartNumber(
                    item.PartNumber
                );



            if (matches.Count == 0)
            {

                result.Status =
                    StandardPartCheckStatus
                    .NotRegistered;


                result.Message =
                    "标准件库未收录";


                return result;

            }



            if (matches.Count > 1)
            {

                result.Status =
                    StandardPartCheckStatus
                    .MultipleMatch;


                result.Message =
                    "存在多个匹配标准件";


                return result;

            }



            StandardPart standard =
                matches[0];


            result.StandardPart =
                standard;



            result.MatchSource =
                GetMatchSource(item.PartNumber, standard);

            result.CorrectPartNumber =
                result.MatchSource
                == StandardPartMatchSource.NationalPartNumber
                ? standard.NationalPartNumber
                : standard.ExportPartNumber;



            result.CorrectName =
                standard.Name;



            if (
                PartNumberNormalizer
                .StrictEquals(
                    item.PartNumber,
                    result.CorrectPartNumber
                )
                &&
                item.Name.Trim()
                ==
                standard.Name.Trim()
              )
            {

                result.Status =
                    StandardPartCheckStatus
                    .Correct;


                return result;

            }



            if (
                !item.Name.Trim()
                .Equals(
                    standard.Name.Trim()
                )
              )
            {

                result.Status =
                    StandardPartCheckStatus
                    .NameError;


                result.Message =
                    "名称错误";


                return result;

            }



            result.Status =
                StandardPartCheckStatus
                 .FormatDifference;


            result.Message =
                "图号格式错误";


            return result;


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