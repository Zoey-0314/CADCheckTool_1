namespace Correct_test1.Models
{
    /// <summary>
    /// 非标件归档检查结果。
    /// 当前只会为“归档不存在”的AB件生成结果。
    /// </summary>
    public class NonStandardArchiveCheckResult
    {
        /// <summary>
        /// 对应BOM行。
        /// </summary>
        public BomItem BomItem
        {
            get;
            set;
        }


        /// <summary>
        /// BOM所属图纸图号。
        /// </summary>
        public string DrawingNumber
        {
            get;
            set;
        }


        /// <summary>
        /// BOM所在Layout。
        /// </summary>
        public string SourceLayoutName
        {
            get;
            set;
        }


        /// <summary>
        /// BOM原始非标件号。
        /// 例如：
        /// AB452J101
        /// </summary>
        public string OriginalPartNumber
        {
            get;
            set;
        }


        /// <summary>
        /// 删除末尾数字后的搜索关键字。
        /// 例如：
        /// AB452J
        /// </summary>
        public string SearchKey
        {
            get;
            set;
        }


        /// <summary>
        /// 错误说明。
        /// </summary>
        public string Message
        {
            get;
            set;
        }


        public NonStandardArchiveCheckResult()
        {
            DrawingNumber = "";
            SourceLayoutName = "";
            OriginalPartNumber = "";
            SearchKey = "";
            Message = "";
        }
    }
}