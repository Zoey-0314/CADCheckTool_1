using Autodesk.AutoCAD.Geometry;

namespace Correct_test1.Models
{
    /// <summary>
    /// BOM序号与图纸零件序号一致性问题类型
    /// </summary>
    public enum BomCalloutIssueType
    {
        /// <summary>BOM中有该序号，图纸中无对应零件序号标注</summary>
        MissingDrawingCallout,

        /// <summary>图纸中有该序号标注，BOM中不存在该序号</summary>
        ExtraDrawingCallout
    }

    /// <summary>
    /// BOM序号与图纸标注一致性检查结果
    /// </summary>
    public class BomCalloutIssue
    {
        public BomCalloutIssueType IssueType { get; set; }

        /// <summary>序号数值</summary>
        public int Number { get; set; }

        /// <summary>提示信息</summary>
        public string Message { get; set; }

        /// <summary>
        /// Marker放置位置：
        /// - MissingDrawingCallout → BOM No.单元格坐标
        /// - ExtraDrawingCallout  → 图纸零件标注文字位置
        /// </summary>
        public Point3d MarkerPosition { get; set; }
    }
}
