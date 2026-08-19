using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;


namespace Correct_test1.Models
{

    public class BomData
    {


        /// <summary>
        /// 图号
        /// 例如:
        /// AB282Z_
        /// </summary>
        public string DrawingNumber { get; set; }
        /// <summary>
        /// 当前BOM右侧实际显示的项目号。
        /// 例如：
        /// P2026AB001
        /// 注意：
        /// 不保存-L0等版本后缀。
        /// 如果BOM右侧没有项目号：
        /// 保持为空字符串。
        /// </summary>
        public string ProjectNumber
        {
            get;
            set;
        }
        =
        "";
        public bool ProjectNumberAmbiguous
        {
            get;
            set;
        }
        =
        false;

        public Point3d DrawingNumberPosition { get; set; }
/// <summary>
        /// BOM明细
        /// </summary>
        public List<BomItem> Items { get; set; }
            = new List<BomItem>();
/// <summary>
        /// Table 所属布局
        /// 例如 Layout1、Layout6
        /// </summary>
        public string SourceLayoutName { get; set; }

    }

}