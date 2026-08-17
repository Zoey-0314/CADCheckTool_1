using Autodesk.AutoCAD.Geometry;

namespace Correct_test1.VersionCheck.Models
{
    /// <summary>
    /// 一条版本号检查提示。
    ///
    /// 只有当前版本落后于归档最高版本时
    /// 才产生此结果。
    /// </summary>
    public class VersionCheckResult
    {
        public string FilePath
        {
            get;
            set;
        }


        public string DrawingNumber
        {
            get;
            set;
        }


        public string LayoutName
        {
            get;
            set;
        }


        public bool IsNonStandard
        {
            get;
            set;
        }


        public string ProjectNumber
        {
            get;
            set;
        }


        public string CurrentVersion
        {
            get;
            set;
        }


        public string LatestVersion
        {
            get;
            set;
        }


        public string LatestFilePath
        {
            get;
            set;
        }


        public string Message
        {
            get;
            set;
        }


        public Point3d Position
        {
            get;
            set;
        }


        public VersionCheckResult()
        {
            FilePath = "";
            DrawingNumber = "";
            LayoutName = "";
            ProjectNumber = "";
            CurrentVersion = "";
            LatestVersion = "";
            LatestFilePath = "";
            Message = "";
            Position = Point3d.Origin;
        }
    }
}