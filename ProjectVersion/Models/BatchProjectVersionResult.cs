namespace Correct_test1.ProjectVersion.Models
{
    /// <summary>
    /// 一张DWG的批量版本号写入结果。
    /// </summary>
    public class BatchProjectVersionResult
    {
        public string FilePath
        {
            get;
            set;
        }


        public string FileName
        {
            get;
            set;
        }


        public bool Success
        {
            get;
            set;
        }


        public bool Saved
        {
            get;
            set;
        }


        public int ModifiedCount
        {
            get;
            set;
        }


        public int CreatedCount
        {
            get;
            set;
        }


        public int SkippedCount
        {
            get;
            set;
        }


        public int FailedLayoutCount
        {
            get;
            set;
        }


        public string Message
        {
            get;
            set;
        }


        public BatchProjectVersionResult()
        {
            FilePath = "";
            FileName = "";
            Message = "";
        }
    }
}