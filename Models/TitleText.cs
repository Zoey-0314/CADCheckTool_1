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
        /// CAD文字高度
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// 所属布局
        /// </summary>
        public string LayoutName { get; set; }

        public Autodesk.AutoCAD.DatabaseServices.ObjectId ViewportId
        {
            get;
            set;
        }

        /// <summary>
        /// 对应的原始CAD文字实体。
        ///
        /// 页码检查发现错误后，
        /// 用它直接修改原文字内容。
        /// </summary>
        public Autodesk.AutoCAD.DatabaseServices.ObjectId ObjectId { get; set; }
    }
}