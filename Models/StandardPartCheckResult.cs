namespace Correct_test1.Models
{

    public enum StandardPartCheckStatus
    {
        Correct,

        NameError,

        FormatDifference,

        NotRegistered,

        MultipleMatch
    }



    public class StandardPartCheckResult
    {

        /// <summary>
        /// 检查状态
        /// </summary>
        public StandardPartCheckStatus Status
        {
            get;
            set;
        }



        /// <summary>
        /// BOM中的零件
        /// </summary>
        public BomItem BomItem
        {
            get;
            set;
        }



        /// <summary>
        /// 匹配到的标准件
        /// </summary>
        public StandardPart StandardPart
        {
            get;
            set;
        }



        /// <summary>
        /// 正确图号
        /// </summary>
        public string CorrectPartNumber
        {
            get;
            set;
        }



        /// <summary>
        /// 正确名称
        /// </summary>
        public string CorrectName
        {
            get;
            set;
        }



        /// <summary>
        /// 错误说明
        /// </summary>
        public string Message
        {
            get;
            set;
        }

    }

}