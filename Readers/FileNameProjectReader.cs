using System;
using System.IO;
using System.Text.RegularExpressions;


namespace Correct_test1.Readers
{

    public class FileNameProjectReader
    {


        public class ProjectInfo
        {

            //项目号
            //例如 N2607US004
            public string ProjectNumber { get; set; }


            //版本号
            //例如 L0
            public string Version { get; set; }



            //完整编号
            //例如 N2607US004-L0
            public string FullNumber
            {
                get
                {

                    if (string.IsNullOrEmpty(Version))
                    {
                        return ProjectNumber;
                    }


                    return ProjectNumber
                        + "-"
                        + Version;

                }
            }

        }





        public ProjectInfo ReadProjectNumber(
            string filePath)
        {


            //获取文件名（不包含扩展名）

            string fileName =
                Path.GetFileNameWithoutExtension(
                    filePath
                );



            /*
             
            项目号规则：

            N
            +
            四位数字
            +
            两位字母
            +
            三位数字


            例如：

            N2607US004


            后面版本：

            -L0
            -PE1
            -CM1


            可以没有版本


            */


            string pattern =
                @"N\d{4}[A-Z]{2}\d{3}(?:-[A-Z0-9]+)?";



            Match match =
                Regex.Match(
                    fileName,
                    pattern,
                    RegexOptions.IgnoreCase
                );




            //没有找到

            if (!match.Success)
            {

                return null;

            }




            ProjectInfo info =
                new ProjectInfo();




            string fullNumber =
                match.Value.ToUpper();




            /*
             
            提取项目号

            N2607US004-L0

            ↓

            N2607US004

            */


            info.ProjectNumber =
                fullNumber
                .Split('-')[0];





            /*
             
            提取版本号

            N2607US004-L0

            ↓

            L0

            */


            if (fullNumber.Contains("-"))
            {

                info.Version =
                    fullNumber
                    .Split('-')[1];

            }




            return info;


        }



    }

}