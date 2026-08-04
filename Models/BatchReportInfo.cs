namespace Correct_test1.Models
{
    /// <summary>
    /// 保存最近一次批量检查报告信息
    /// 用于：
    /// 1. 批量检查后记录CSV路径
    /// 2. 点击按钮重新打开报告
    /// </summary>
    public static class BatchReportInfo
    {


        /// <summary>
        /// 最近一次生成的CSV报告路径
        /// </summary>
        public static string LastReportPath
        {
            get;
            set;
        } = "";



    }
}