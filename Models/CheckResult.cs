namespace Correct_test1.Models
{
    public class CheckResult
    {
        public string FilePath { get; set; }

        public string FileName { get; set; }
        //检查类型
        public string Type { get; set; }


        //对象名称，例如 CONN2块、标题栏
        public string ObjectName { get; set; }


        //当前发现内容
        public string CurrentValue { get; set; }


        //正确参考值
        public string ExpectedValue { get; set; }


        //提示信息
        public string Message { get; set; }


        //是否错误
        public bool IsError { get; set; }

    }
}