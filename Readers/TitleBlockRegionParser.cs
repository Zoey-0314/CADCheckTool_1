using System;
using System.Collections.Generic;
using System.Linq;

using Correct_test1.Configs;
using Correct_test1.Models;


namespace Correct_test1.Readers
{

    /// <summary>
    /// 标题栏区域解析器
    ///
    /// 根据固定坐标区域解析标题栏信息
    /// 支持：
    /// 1. 多文字对象
    /// 2. 多行文字
    /// 3. 标签过滤
    /// </summary>
    public class TitleBlockRegionParser
    {


        /// <summary>
        /// 解析标题栏
        /// </summary>
        public DrawingInfo Parse(
            List<TitleText> texts,
            bool isHorizontal)
        {

            DrawingInfo info =
                new DrawingInfo();



            if (texts == null ||
                texts.Count == 0)
            {
                return info;
            }



            List<TitleFieldRegion> regions =
                isHorizontal
                ?
                TitleBlockHorizontalConfig.Regions
                :
                TitleBlockVerticalConfig.Regions;



            foreach (TitleFieldRegion region in regions)
            {

                List<TitleText> fieldTexts =
                    texts
                    .Where(t =>
                        region.Contains(
                            t.X,
                            t.Y))
                    .ToList();



                if (fieldTexts.Count == 0)
                    continue;

                if (string.Equals(
        region.FieldName,
        "PageNumber",
        StringComparison.OrdinalIgnoreCase))
                {
                    info.PageNumberSourceTexts =
                        new List<TitleText>(
                            fieldTexts);
                }

                string value =
                    MergeTexts(
                        fieldTexts);



                value =
                    CleanField(
                        region.FieldName,
                        value);



                SetValue(
                    info,
                    region.FieldName,
                    value);

            }



            return info;

        }





        /// <summary>
        /// 合并区域内多个文字
        /// </summary>
        private string MergeTexts(
            List<TitleText> texts)
        {


            /*
             * 先按Y排序
             *
             * 同一行:
             *       X从小到大
             *
             * 多行:
             *       Y从大到小
             *
             */


            List<TitleText> ordered =
                texts
                .OrderByDescending(t => t.Y)
                .ThenBy(t => t.X)
                .ToList();



            return string.Join(
                "",
                ordered
                .Select(t => t.Text)
            );

        }





        /// <summary>
        /// 清理字段标签
        /// </summary>
        private string CleanField(
            string fieldName,
            string value)
        {


            if (string.IsNullOrEmpty(value))
                return "";



            value =
                value
                .Replace("\\P", "")
                .Replace("\n", "")
                .Trim();



            switch (fieldName)
            {

                case "DrawingNumber":

                    value =
                        value
                        .Replace("图号", "")
                        .Replace(":", "")
                        .Replace("：", "");

                    break;



                case "Material":

                    value =
                        value
                        .Replace("材料", "")
                        .Replace(":", "")
                        .Replace("：", "");

                    break;



                case "Specification":

                    value =
                        value
                        .Replace("规格", "")
                        .Replace(":", "")
                        .Replace("：", "");

                    break;



                case "SurfaceTreatment":

                    value =
                        value
                        .Replace("表面处理", "")
                        .Replace(":", "")
                        .Replace("：", "");

                    break;



                case "Designer":

                    value =
                        value
                        .Replace("制图", "")
                        .Replace(":", "")
                        .Replace("：", "");

                    break;



                case "Checker":

                    value =
                        value
                        .Replace("校对", "")
                        .Replace(":", "")
                        .Replace("：", "");

                    break;



                case "Reviewer":

                    value =
                        value
                        .Replace("标审", "")
                        .Replace(":", "")
                        .Replace("：", "");

                    break;



                case "Approver":

                    value =
                        value
                        .Replace("批准", "")
                        .Replace(":", "")
                        .Replace("：", "");

                    break;



                case "TitleDate":

                    value =
                        value
                        .Replace("日期", "")
                        .Replace(":", "")
                        .Replace("：", "");

                    break;



                case "PageNumber":

                    value =
                        value
                        .Replace("页码", "")
                        .Replace(":", "")
                        .Replace("：", "");

                    break;

            }



            return value.Trim();

        }






        /// <summary>
        /// 写入 DrawingInfo
        /// </summary>
        private void SetValue(
            DrawingInfo info,
            string fieldName,
            string value)
        {

            switch (fieldName)
            {

                case "DrawingName":
                    info.DrawingName = value;
                    break;


                case "DrawingNumber":
                    info.DrawingNumber = value;
                    break;


                case "Material":
                    info.Material = value;
                    break;


                case "Specification":
                    info.Specification = value;
                    break;


                case "SurfaceTreatment":
                    info.SurfaceTreatment = value;
                    break;


                case "Designer":
                    info.Designer = value;
                    break;


                case "Checker":
                    info.Checker = value;
                    break;


                case "Reviewer":
                    info.Reviewer = value;
                    break;


                case "Approver":
                    info.Approver = value;
                    break;


                case "TitleDate":
                    info.TitleDate = value;
                    break;


                case "PageNumber":
                    info.PageNumber = value;
                    break;

            }

        }


    }

}