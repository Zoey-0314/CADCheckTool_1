using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Correct_test1.Models;


namespace Correct_test1.Export
{

    public class CsvExporter
    {

        public void ExportError(
    List<CheckResult> results,
    string fileName)
        {


            List<string> csv =
                new List<string>();


            csv.Add(
                "文件路径,文件名,检查类型,当前值,标准值,错误说明"
            );


            foreach (CheckResult r in results)
            {


                if (r.IsError)
                {

                    csv.Add(

                        r.FilePath + "," +

                        r.FileName + "," +

                        r.Type + "," +

                        r.CurrentValue + "," +

                        r.ExpectedValue + "," +

                        r.Message

                    );

                }


            }


            File.WriteAllLines(
                fileName,
                csv,
                Encoding.UTF8
            );

        }

        public void Export(
            List<CheckResult> results,
            string fileName)
        {


            List<string> csv =
                new List<string>();


            csv.Add(
            "文件路径,文件名,检查类型,检查对象,当前值,标准值,结果,错误说明"
            );



            foreach (CheckResult r in results)
            {


                csv.Add(

                    r.FilePath + "," +

                    r.FileName + "," +

                    r.Type + "," +

                    r.ObjectName + "," +

                    r.CurrentValue + "," +

                    r.ExpectedValue + "," +

                    (r.IsError ? "失败" : "通过") + "," +

                    r.Message

                );


            }



            File.WriteAllLines(
                fileName,
                csv,
                Encoding.UTF8
            );


        }


    }
}