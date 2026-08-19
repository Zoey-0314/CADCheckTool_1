namespace Correct_test1.Models
{
    using Autodesk.AutoCAD.Geometry;

    /// <summary>
    /// BOM单行数据
    /// </summary>
    public class BomItem
    {

        /// <summary>
        /// 序号
        /// </summary>
        public string No { get; set; }



        /// <summary>
        /// 零件号
        /// </summary>
        public string PartNumber { get; set; }

        /// <summary>
        /// BOM中的P/N列。
        /// 例如：
        /// _999
        /// _998
        /// </summary>
        public string PartNumberSuffix
        {
            get;
            set;
        }
        =
        "";

        public int PartNumberSuffixColumn
        {
            get;
            set;
        }
=
-1;



        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }



        /// <summary>
        /// 数量
        /// </summary>
        public string Quantity { get; set; }

        public int BomRow { get; set; }

        public int NoColumn { get; set; }

        public int PartNumberColumn { get; set; }

        public int NameColumn { get; set; }

        public Point3d NoCellPosition { get; set; }

        public Point3d PartNumberCellPosition { get; set; }

        public Point3d NameCellPosition { get; set; }

        public Point3d PartNumberSuffixCellPosition
        {
            get;
            set;
        }
=
Point3d.Origin;

    }

}