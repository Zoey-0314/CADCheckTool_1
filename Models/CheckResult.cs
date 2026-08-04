namespace Correct_test1.Models
{
    public class CheckResult
    {

        // 文件完整路径
        public string FilePath { get; set; }


        // 文件名
        public string FileName { get; set; }



        // 新增：布局名称
        public string LayoutName { get; set; }



        // 新增：修改记录标记
        public string Mark { get; set; }




        // 检查类型
        public string Type { get; set; }



        // 检查对象
        public string ObjectName { get; set; }



        // 当前值
        public string CurrentValue { get; set; }



        // 参考值
        public string ExpectedValue { get; set; }



        // 提示信息
        public string Message { get; set; }



        // 是否错误
        public bool IsError { get; set; }



        public CheckResult()
        {

            FilePath = "";

            FileName = "";

            LayoutName = "";

            Mark = "";

            Type = "";

            ObjectName = "";

            CurrentValue = "";

            ExpectedValue = "";

            Message = "";

            IsError = false;

        }


    }
}