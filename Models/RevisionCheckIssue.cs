using System;

namespace Correct_test1.Models
{
    public class RevisionCheckIssue
    {
        public string LayoutName { get; set; }

        /// <summary>
        /// "横版" 或 "竖版"
        /// </summary>
        public string Orientation { get; set; }

        /// <summary>
        /// 行号（若可用）
        /// </summary>
        public int RowNumber { get; set; }

        public string Mark { get; set; }

        /// <summary>
        /// 缺失字段名称，例如 "更改内容"/"更改日期"/"签名"
        /// </summary>
        public string MissingField { get; set; }

        public double X { get; set; }
        public double Y { get; set; }

        public string Message { get; set; }
    }
}