using Correct_test1.Models;
using System;

namespace Correct_test1.Core
{
    public static class PartNumberTypeClassifier
    {
        public static PartNumberType Classify(string partNumber)
        {
            if (string.IsNullOrWhiteSpace(partNumber))
            {
                return PartNumberType.Unknown;
            }

            string value = partNumber.Trim().ToUpper();

            if (value.StartsWith("NS", StringComparison.Ordinal))
            {
                return PartNumberType.NonStandardPart;
            }

            return PartNumberType.StandardPart;
        }
    }
}
