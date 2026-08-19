using Autodesk.AutoCAD.Geometry;


namespace Correct_test1.Models
{
    /// <summary>
    /// 非标件号存在性检查结果。
    /// 例如：
    /// BOM：
    /// NS333T1
    /// 归档图号：
    /// NS333T
    /// 件号：
    /// 1
    /// </summary>
    public class NonStandardPartNumberCheckResult
    {
        public BomItem BomItem
        {
            get;
            set;
        }


        /// <summary>
        /// 当前BOM所在图纸的图号。
        /// </summary>
        public string DrawingNumber
        {
            get;
            set;
        }


        /// <summary>
        /// 当前BOM所在Layout。
        /// </summary>
        public string SourceLayoutName
        {
            get;
            set;
        }


        /// <summary>
        /// BOM中的完整非标件号。
        /// 例如：
        /// NS333T1
        /// </summary>
        public string OriginalPartNumber
        {
            get;
            set;
        }


        /// <summary>
        /// 归档图号。
        /// NS333T1
        /// ->
        /// NS333T
        /// </summary>
        public string ArchiveDrawingNumber
        {
            get;
            set;
        }


        /// <summary>
        /// 件号。
        /// NS333T1
        /// ->
        /// 1
        /// </summary>
        public string PartSuffix
        {
            get;
            set;
        }


        /// <summary>
        /// 当前项目号。
        /// </summary>
        public string ProjectNumber
        {
            get;
            set;
        }


        /// <summary>
        /// 实际用于检查件号的归档DWG。
        /// </summary>
        public string ArchiveFilePath
        {
            get;
            set;
        }


        /// <summary>
        /// 是否因为归档DWG读取失败而无法完成检查。
        /// </summary>
        public bool InspectionFailed
        {
            get;
            set;
        }


        public string Message
        {
            get;
            set;
        }


        public Point3d MarkerPosition
        {
            get;
            set;
        }


        public NonStandardPartNumberCheckResult()
        {
            DrawingNumber = "";

            SourceLayoutName = "";

            OriginalPartNumber = "";

            ArchiveDrawingNumber = "";

            PartSuffix = "";

            ProjectNumber = "";

            ArchiveFilePath = "";

            Message = "";

            MarkerPosition =
                Point3d.Origin;
        }
    }
}