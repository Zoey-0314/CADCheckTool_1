namespace Correct_test1.Configs
{
    /// <summary>
    /// CADCheckTool所有可配置外部路径。
    /// </summary>
    public class AppPathSettings
    {
        /// <summary>
        /// 非标件归档图纸根目录。
        /// </summary>
        public string NonStandardArchivePath
        {
            get;
            set;
        }


        /// <summary>
        /// 诺升标准件Excel数据库。
        /// </summary>
        public string StandardPartDatabasePath
        {
            get;
            set;
        }


        public AppPathSettings Clone()
        {
            return new AppPathSettings
            {
                NonStandardArchivePath =
                    NonStandardArchivePath,

                StandardPartDatabasePath =
                    StandardPartDatabasePath
            };
        }
    }
}