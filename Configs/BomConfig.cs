using System.Collections.Generic;


namespace Correct_test1.Configs
{

    public static class BomConfig
    {


        public static List<string> BomLayers =
            new List<string>
            {
                "0",
                "BOM",
                "BOM_TABLE"
            };



        /// <summary>
        /// 序号
        /// </summary>
        public static List<string> NoHeaders =
            new List<string>
            {
                "No.",
                "NO",
                "序号"
            };



        /// <summary>
        /// 零件号
        /// </summary>
        public static List<string> PartNumberHeaders =
            new List<string>
            {
                "Part No.",
                "PartNo",
                "图号",
                "零件号"
            };



        /// <summary>
        /// 名称
        /// </summary>
        public static List<string> NameHeaders =
            new List<string>
            {
                "Name",
                "名称",
                "零件名称"
            };



        /// <summary>
        /// 数量
        /// 当前模板:
        /// Qut.
        /// </summary>
        public static List<string> QuantityHeaders =
            new List<string>
            {
                "Qut.",
                "数量"
            };

        /// <summary>
        /// 图号识别规则
        /// </summary>
        public static string DrawingNumberPattern =
            @"^[A-Z]{2,}[A-Z0-9_-]{3,}$";
        public static string DrawingNumberSuffix
            = "_";

    }

}