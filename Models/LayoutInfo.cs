using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;


namespace Correct_test1.Models
{


    /// <summary>
    /// CAD布局信息
    /// </summary>
    public class LayoutInfo
    {


        /// <summary>
        /// 布局名称
        /// </summary>
        public string LayoutName { get; set; }



        /// <summary>
        /// 对应空间ID
        /// </summary>
        public ObjectId BlockTableRecordId { get; set; }



        /// <summary>
        /// 是否模型空间
        /// </summary>
        public bool IsModelSpace { get; set; }

        /// <summary>
        /// AutoCAD 底部布局标签顺序
        /// </summary>
        public int TabOrder { get; set; }



        // 标题栏文字


        public List<TitleText> TitleTexts { get; set; }



        public LayoutInfo()
        {

            TitleTexts = new List<TitleText>();

        }

    }

    public class CadLineInfo
    {
        public Point3d StartPoint { get; set; }

        public Point3d EndPoint { get; set; }

        public string LayoutName { get; set; }

        public ObjectId ViewportId { get; set; }

        public bool IsBlue { get; set; }
    }

}