using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;


namespace Correct_test1.Models
{

    public class BomData
    {


        /// <summary>
        /// 图号
        /// 例如:
        /// NS282Z_
        /// </summary>
        public string DrawingNumber { get; set; }

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