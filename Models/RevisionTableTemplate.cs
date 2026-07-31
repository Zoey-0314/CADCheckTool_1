using System.Collections.Generic;


namespace Correct_test1.Models
{


    /// <summary>
    /// 修改记录表模板
    /// </summary>
    public class RevisionTableTemplate
    {


        /// <summary>
        /// 列名称
        /// </summary>
        public List<string> Columns
        {
            get;
            set;
        }



        /// <summary>
        /// 列宽
        /// </summary>
        public List<double> ColumnWidths
        {
            get;
            set;
        }



        /// <summary>
        /// 行高
        /// </summary>
        public double RowHeight
        {
            get;
            set;
        }



        public RevisionTableTemplate()
        {

            Columns =
                new List<string>()
                {
                    "标记",
                    "更改内容",
                    "更改日期",
                    "签名",
                    "变更号"
                };



            ColumnWidths =
                new List<double>()
                {
                    8,
                    57.0012,
                    15,
                    15,
                    15
                };



            RowHeight = 6;


        }



    }


}