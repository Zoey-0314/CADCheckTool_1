using Autodesk.AutoCAD.DatabaseServices;
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
        /// 是否有效图纸
        /// </summary>
        public bool IsValidDrawing { get; set; }



        //==============================
        // 布局整体范围
        //==============================


        public double MinX { get; set; }



        public double MinY { get; set; }



        public double Width { get; set; }



        public double Height { get; set; }



        //==============================
        // 标题栏文字
        //==============================


        public List<TitleText> TitleTexts { get; set; }



        public LayoutInfo()
        {

            TitleTexts = new List<TitleText>();

        }

    }

}