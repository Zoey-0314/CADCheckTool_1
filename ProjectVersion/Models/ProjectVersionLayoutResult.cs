namespace Correct_test1.ProjectVersion.Models
{
    /// <summary>
    /// 单个Layout的写入结果。
    /// </summary>
    public class ProjectVersionLayoutResult
    {
        public string LayoutName
        {
            get;
            set;
        }


        public bool Success
        {
            get;
            set;
        }


        public bool Skipped
        {
            get;
            set;
        }


        /// <summary>
        /// true：
        /// 原来不存在项目号，本次新建。
        ///
        /// false：
        /// 找到原有MText并修改。
        /// </summary>
        public bool Created
        {
            get;
            set;
        }


        public bool IsHorizontal
        {
            get;
            set;
        }


        public string Message
        {
            get;
            set;
        }


        public ProjectVersionLayoutResult()
        {
            LayoutName = "";
            Message = "";
        }
    }
}