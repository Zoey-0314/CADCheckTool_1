namespace Correct_test1.Models
{
    /// <summary>
    /// 标题栏字段区域
    /// 用于根据坐标识别标题栏信息
    /// </summary>
    public class TitleFieldRegion
    {

        /// <summary>
        /// 字段名称
        /// 例如 DrawingNumber
        /// </summary>
        public string FieldName { get; set; }


        /// <summary>
        /// 是否横版
        /// true 横版
        /// false 竖版
        /// </summary>
        public bool IsHorizontal { get; set; }



        public double MinX { get; set; }

        public double MaxX { get; set; }


        public double MinY { get; set; }

        public double MaxY { get; set; }



        /// <summary>
        /// 判断文字坐标是否在区域内
        /// </summary>
        public bool Contains(
            double x,
            double y)
        {

            return
                x >= MinX &&
                x <= MaxX &&
                y >= MinY &&
                y <= MaxY;

        }

    }
}