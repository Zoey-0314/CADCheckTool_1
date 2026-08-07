using System.Collections.Generic;


namespace Correct_test1.Models
{

    /// <summary>
    /// AutoCAD Table 原始数据
    ///
    /// 只保存表格内容
    /// 不判断业务含义
    /// </summary>
    public class CadTableData
    {

        /// <summary>
        /// 表格所在图层
        /// </summary>
        public string LayerName { get; set; }



        /// <summary>
        /// 表格行数
        /// </summary>
        public int Rows { get; set; }



        /// <summary>
        /// 表格列数
        /// </summary>
        public int Columns { get; set; }



        /// <summary>
        /// 单元格数据
        ///
        /// 第一维: 行
        /// 第二维: 列
        /// </summary>
        public List<List<string>> Cells { get; set; }
            = new List<List<string>>();



        /// <summary>
        /// 获取指定单元格
        /// 防止越界
        /// </summary>
        public string GetCell(
            int row,
            int column)
        {

            if (row < 0 ||
               column < 0)
                return "";

            if (row >= Cells.Count)
                return "";


            if (column >= Cells[row].Count)
                return "";


            return Cells[row][column];

        }

    }

}