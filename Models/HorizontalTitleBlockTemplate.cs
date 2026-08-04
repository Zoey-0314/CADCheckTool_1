namespace Correct_test1.Models
{
    public class HorizontalTitleBlockTemplate
    {
        public double RevisionMinX { get { return 45.2828; } }

        public double RevisionMaxX { get { return 329.8438; } }

        public double RevisionTopY { get { return 77.145; } }

        public double RevisionBottomY { get { return 37.145; } }

        // 十列边界
        public double[] XLines
        {
            get
            {
                return new double[]
                {
                    // 左五列
                    45.2828,
                    55.2828,
                    130.2828,
                    150.2828,
                    170.2828,
                    // 中间分割线
                    187.5633,
                    // 右五列
                    197.5633,
                    272.5633,
                    292.5633,
                    312.5633,
                    329.8438
                };
            }
        }

        // 行边界
        public double[] YLines
        {
            get
            {
                return new double[]
                {
                    // 标题下面
                    77.145,
                    // 数据行
                    67.145,
                    61.145,
                    55.145,
                    49.145,
                    43.145,
                    37.145
                };
            }
        }
    }
}