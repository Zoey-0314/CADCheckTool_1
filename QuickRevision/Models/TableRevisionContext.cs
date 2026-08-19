using Autodesk.AutoCAD.DatabaseServices;

namespace Correct_test1.QuickRevision.Models
{
    /// <summary>
    /// Table单元格快速划改的附加上下文。
    /// 只有目标来自AutoCAD Table时才存在。
    /// 用于后续：
    /// NS开头BOM内容被划改后，
    /// 在该行最右侧表格外生成项目号。
    /// </summary>
    public class TableRevisionContext
    {
        /// <summary>
        /// 原始Table对象。
        /// </summary>
        public ObjectId TableId
        {
            get;
            set;
        }


        /// <summary>
        /// 用户点击的行号。
        /// </summary>
        public int Row
        {
            get;
            set;
        }


        /// <summary>
        /// 用户点击的列号。
        /// </summary>
        public int Column
        {
            get;
            set;
        }


        /// <summary>
        /// 整张Table最右侧X坐标。
        /// 后面项目号放置位置：
        /// TableRightX + gap
        /// </summary>
        public double TableRightX
        {
            get;
            set;
        }


        /// <summary>
        /// 当前BOM行的垂直中心Y坐标。
        /// 项目号最终与当前行保持同一高度。
        /// </summary>
        public double RowCenterY
        {
            get;
            set;
        }


        /// <summary>
        /// 当前行最下侧Y。
        /// </summary>
        public double RowBottomY
        {
            get;
            set;
        }


        /// <summary>
        /// 当前行最上侧Y。
        /// </summary>
        public double RowTopY
        {
            get;
            set;
        }


        /// <summary>
        /// 当前行高度。
        /// </summary>
        public double RowHeight
        {
            get
            {
                return
                    RowTopY -
                    RowBottomY;
            }
        }


        /// <summary>
        /// 检查Table上下文是否有效。
        /// </summary>
        public bool IsValid()
        {
            if (TableId.IsNull ||
                !TableId.IsValid)
            {
                return false;
            }


            if (Row < 0 ||
                Column < 0)
            {
                return false;
            }


            if (!IsValidNumber(
                    TableRightX))
            {
                return false;
            }


            if (!IsValidNumber(
                    RowCenterY))
            {
                return false;
            }


            return true;
        }


        private static bool IsValidNumber(
            double value)
        {
            return
                !double.IsNaN(value) &&
                !double.IsInfinity(value) &&
                System.Math.Abs(value) < 1E15;
        }
    }
}