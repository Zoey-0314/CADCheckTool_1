using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace Correct_test1.Models
{
    /// <summary>
    /// 图纸中识别出的一个零件序号标注
    /// </summary>
    public class PartCallout
    {
        /// <summary>序号数值（纯正整数）</summary>
        public int Number { get; set; }

        /// <summary>所属布局名称</summary>
        public string LayoutName { get; set; }

        /// <summary>文字实体位置</summary>
        public Point3d TextPosition { get; set; }

        /// <summary>文字实体 ObjectId（DBText 或 MText）</summary>
        public ObjectId TextObjectId { get; set; }

        /// <summary>引出线 ObjectId（Line 或 Polyline），可空</summary>
        public ObjectId LeaderObjectId { get; set; }

        /// <summary>短水平搁架线 ObjectId（Line），可空</summary>
        public ObjectId HorizontalLineObjectId { get; set; }
    }
}
