namespace Correct_test1.Models
{
    /// <summary>
    /// CAD标题栏文字中间对象
    /// 保存文字内容和空间位置
    /// </summary>
    public class TitleText
    {
        /// <summary>
        /// 文字内容
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// X坐标
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Y坐标
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// 所属布局
        /// </summary>
        public string LayoutName { get; set; }
    }
}