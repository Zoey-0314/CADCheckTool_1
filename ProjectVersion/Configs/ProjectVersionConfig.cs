namespace Correct_test1.ProjectVersion.Configs
{
    /// <summary>
    /// 项目号+版本号文字模板。
    /// </summary>
    public class ProjectVersionTemplate
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double TextHeight { get; set; }

        public double Width { get; set; }

        public double SearchTolerance { get; set; }

        public string TextStyleName { get; set; }
    }


    /// <summary>
    /// 非标图纸项目号+版本号写入配置。
    /// </summary>
    public static class ProjectVersionConfig
    {
        /// <summary>
        /// 横版。
        ///
        /// 来源于现有横版图纸实际MText属性。
        /// </summary>
        public static readonly
            ProjectVersionTemplate Horizontal =
                new ProjectVersionTemplate
                {
                    X = 114.7533,

                    Y = 315.8613,

                    TextHeight = 5.0,

                    Width = 34.3439,

                    SearchTolerance = 15.0,

                    TextStyleName = "CONN"
                };


        /// <summary>
        /// 竖版。
        /// </summary>
        public static readonly
            ProjectVersionTemplate Vertical =
                new ProjectVersionTemplate
                {
                    X = 130.816,

                    Y = 351.0263,

                    TextHeight = 4.0,

                    Width = 27.4752,

                    SearchTolerance = 15.0,

                    TextStyleName = "CONN"
                };


        public static ProjectVersionTemplate Get(
            bool isHorizontal)
        {
            return isHorizontal
                ? Horizontal
                : Vertical;
        }
    }
}