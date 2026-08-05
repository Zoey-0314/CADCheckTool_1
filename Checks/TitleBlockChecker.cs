using System.Collections.Generic;

using Correct_test1.Models;


namespace Correct_test1.Checks
{

    /// <summary>
    /// 标题栏信息检查
    /// </summary>
    public class TitleBlockChecker
    {


        public List<CheckResult> Check(
            DrawingInfo info)
        {

            List<CheckResult> results =
                new List<CheckResult>();



            CheckField(
                results,
                info.FilePath,
                info.FileName,
                info.LayoutName,
                "图号",
                info.DrawingNumber
            );



            CheckField(
                results,
                info.FilePath,
                info.FileName,
                info.LayoutName,
                "图纸名称",
                info.DrawingName
            );



            CheckField(
                results,
                info.FilePath,
                info.FileName,
                info.LayoutName,
                "材料",
                info.Material
            );



            CheckField(
                results,
                info.FilePath,
                info.FileName,
                info.LayoutName,
                "规格",
                info.Specification
            );


            CheckField(
                results,
                info.FilePath,
                info.FileName,
                info.LayoutName,
                "表面处理",
                info.SurfaceTreatment
            );


            CheckField(
                results,
                info.FilePath,
                info.FileName,
                info.LayoutName,
                "制图",
                info.Designer
            );



            CheckField(
                results,
                info.FilePath,
                info.FileName,
                info.LayoutName,
                "校对",
                info.Checker
            );



            CheckField(
                results,
                info.FilePath,
                info.FileName,
                info.LayoutName,
                "标审",
                info.Reviewer
            );



            CheckField(
                results,
                info.FilePath,
                info.FileName,
                info.LayoutName,
                "批准",
                info.Approver
            );


            CheckField(
                results,
                info.FilePath,
                info.FileName,
                info.LayoutName,
                "日期",
                info.TitleDate
            );


            return results;

        }




        private void CheckField(
            List<CheckResult> results,
            string filePath,
            string fileName,
            string layoutName,
            string fieldName,
            string value)
        {


            if (string.IsNullOrWhiteSpace(value))
            {

                results.Add(
                    new CheckResult
                    {

                        FilePath = filePath,

                        FileName = fileName,

                        Type = "标题栏检查",

                        ObjectName = fieldName,

                        LayoutName = layoutName,

                        Message =
                            "标题栏"
                            +
                            fieldName
                            +
                            "未填写",

                        IsError = true

                    }
                );

            }

        }


    }

}