namespace Correct_test1.ProjectVersion.Configs
{
    public class ProjectVersionTemplate
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double TextHeight { get; set; }

        public double Width { get; set; }

        public double SearchTolerance { get; set; }

        public string TextStyleName { get; set; }
    }


    public static class ProjectVersionConfig
    {
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
            return Get(
                isHorizontal,
                0.0,
                0.0);
        }


        public static ProjectVersionTemplate Get(
            bool isHorizontal,
            double offsetX,
            double offsetY)
        {
            ProjectVersionTemplate source =
                isHorizontal
                    ? Horizontal
                    : Vertical;

            return new ProjectVersionTemplate
            {
                X = source.X + offsetX,
                Y = source.Y + offsetY,
                TextHeight = source.TextHeight,
                Width = source.Width,
                SearchTolerance = source.SearchTolerance,
                TextStyleName = source.TextStyleName
            };
        }
    }
}