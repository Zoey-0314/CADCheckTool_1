using Autodesk.AutoCAD.Geometry;

namespace Correct_test1.VersionCheck.Models
{
    public class DrawingVersionInfo
    {
        public string LayoutName
        {
            get;
            set;
        }


        public bool IsHorizontal
        {
            get;
            set;
        }


        /// <summary>
        /// true = 有项目号，按非标处理。
        /// false = 无项目号，按标准件V版本处理。
        /// </summary>
        public bool IsNonStandard
        {
            get;
            set;
        }


        /// <summary>
        /// 是否真正检测到了版本号。
        /// 标准件：
        /// V0 / V1 / V2...
        /// 非标：
        /// L0 / L1 / L2...
        /// </summary>
        public bool HasVersion
        {
            get;
            set;
        }


        public string ProjectNumber
        {
            get;
            set;
        }


        public int CurrentVersionNumber
        {
            get;
            set;
        }


        public string CurrentVersionText
        {
            get;
            set;
        }


        public string RawText
        {
            get;
            set;
        }


        public Point3d Position
        {
            get;
            set;
        }


        public DrawingVersionInfo()
        {
            LayoutName = "";

            ProjectNumber = "";

            CurrentVersionText = "";

            RawText = "";

            CurrentVersionNumber = -1;

            HasVersion = false;

            Position = Point3d.Origin;
        }
    }
}