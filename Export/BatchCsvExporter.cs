using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Correct_test1.Models;


namespace Correct_test1.Export
{

    public class BatchCsvExporter
    {


        public string Export(
            List<CheckResult> results,
            string folder)
        {


            string fileName =
                "批量检查结果_"
                +
                DateTime.Now.ToString(
                    "yyyyMMdd_HHmmss"
                )
                +
                ".csv";



            string path =
                Path.Combine(
                    folder,
                    fileName
                );



            StringBuilder sb =
                new StringBuilder();



            // CSV标题

            sb.AppendLine(
                "文件名,打开图纸,布局,标记,检查类型,缺失项,问题"
            );




            foreach (CheckResult result in results)
            {


                string link =
                    "=HYPERLINK(\""
                    +
                    result.FilePath
                    +
                    "\",\"打开图纸\")";



                sb.AppendLine(

                    Escape(result.FileName)
                    +
                    ","
                    +
                    Escape(link)
                    +
                    ","
                    +
                    Escape(result.LayoutName)
                    +
                    ","
                    +
                    Escape(result.Mark)
                    +
                    ","
                    +
                    Escape(result.Type)
                    +
                    ","
                    +
                    Escape(result.ExpectedValue)
                    +
                    ","
                    +
                    Escape(result.Message)

                );


            }



            File.WriteAllText(
                path,
                sb.ToString(),
                Encoding.UTF8
            );


            return path;


        }




        private string Escape(
            string value)
        {


            if (string.IsNullOrEmpty(value))
                return "";



            if (value.Contains(",")
                ||
               value.Contains("\"")
                ||
               value.Contains("\n"))
            {

                value =
                    value.Replace(
                        "\"",
                        "\"\""
                    );


                return "\""
                    + value
                    + "\"";

            }


            return value;


        }


    }

}