namespace Correct_test1.Models
{
    /// <summary>
    /// 修改记录坐标信息
    /// 用于记录修改记录各字段在CAD中的位置
    /// 后续用于自动定位缺失项并在图纸中标记
    /// </summary>
    public class RevisionLocation
    {
        /// <summary>
        /// 所属布局名称
        /// </summary>
        public string LayoutName { get; set; }

        /// <summary>
        /// 修改记录标记
        /// 例如：1、4、11
        /// </summary>
        public string Mark { get; set; }

        // 标记坐标
        public double MarkX { get; set; }
        public double MarkY { get; set; }

        // 更改内容坐标
        public double DescriptionX { get; set; }
        public double DescriptionY { get; set; }

        // 更改日期坐标
        public double DateX { get; set; }
        public double DateY { get; set; }

        // 签名坐标
        public double SignerX { get; set; }
        public double SignerY { get; set; }

        public RevisionLocation()
        {
            LayoutName = "";
            Mark = "";
            MarkX = 0;
            MarkY = 0;
            DescriptionX = 0;
            DescriptionY = 0;
            DateX = 0;
            DateY = 0;
            SignerX = 0;
            SignerY = 0;
        }
    }
}