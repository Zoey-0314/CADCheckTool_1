using System;

namespace Correct_test1.Core
{
    /// <summary>
    /// 标准件图号格式处理器
    ///
    /// 用于判断两个标准件编号是否为同一个零件
    ///
    /// 第一层:
    /// 去除空格、斜杠后比较
    ///
    /// 第二层:
    /// 原始字符串严格比较
    /// </summary>
    public static class PartNumberNormalizer
    {


        /// <summary>
        /// 宽松标准化
        ///
        /// 用于判断:
        /// 格式差异是否为人为输入问题
        ///
        /// 处理:
        /// 1. 大小写
        /// 2. 空格
        /// 3. /
        ///
        /// 不处理:
        /// .
        /// -
        /// x
        /// 数字
        ///
        /// 因为这些可能代表规格差异
        /// </summary>
        public static string LooseNormalize(
            string value)
        {

            if (string.IsNullOrWhiteSpace(value))
                return "";


            return value
                .Trim()
                .ToUpper()
                .Replace(" ", "")
                .Replace("/", "");

        }



        /// <summary>
        /// 宽松比较
        ///
        /// 判断是否为同一个标准件
        /// 例如:
        ///
        /// ASME B18.2.1 5/8-11x2 G5
        ///
        /// ASME B18.2.1 5/8-11 x 2 G5
        ///
        /// 返回true
        /// </summary>
        public static bool LooseEquals(
            string a,
            string b)
        {

            return LooseNormalize(a)
                ==
                LooseNormalize(b);

        }





        /// <summary>
        /// 严格比较
        ///
        /// 用于判断:
        /// 是否完全符合标准库格式
        ///
        /// </summary>
        public static bool StrictEquals(
            string a,
            string b)
        {

            if (a == null ||
                b == null)
            {
                return false;
            }


            return string.Equals(
                a.Trim(),
                b.Trim(),
                StringComparison.OrdinalIgnoreCase);

        }



    }
}