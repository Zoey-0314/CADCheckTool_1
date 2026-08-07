using System;
using System.Text.RegularExpressions;


namespace Correct_test1.Core
{

    public static class CadTextCleaner
    {


        /// <summary>
        /// 清理AutoCAD MText格式
        ///
        /// 输入:
        /// {\Fisocp2,hztxt|c134;螺母}
        ///
        /// 输出:
        /// 螺母
        /// </summary>
        public static string Clean(
            string text)
        {

            if (string.IsNullOrWhiteSpace(text))
                return "";


            text = text.Trim();



            /*
             * 处理:
             * {\xxxx;内容}
             */

            if (text.StartsWith("{")
               && text.EndsWith("}"))
            {

                int index =
                    text.LastIndexOf(';');


                if (index >= 0 &&
                   index < text.Length - 1)
                {

                    text =
                        text.Substring(
                            index + 1,
                            text.Length - index - 2);

                }

            }



            /*
             * 删除 AutoCAD 控制符
             *
             * 例如:
             * \A1;
             * \H1.5;
             * \C134;
             */

            text =
                Regex.Replace(
                    text,
                    @"\\[A-Za-z][0-9\.\-]*;",
                    ""
                );



            /*
             * 删除剩余大括号
             */

            text =
                text.Replace("{", "")
                    .Replace("}", "");



            return text.Trim();

        }


    }

}