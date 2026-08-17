using Autodesk.AutoCAD.Geometry;

namespace Correct_test1.Models
{
    public class ProjectNumberLocation
    {
        public string ProjectNumber
        {
            get;
            set;
        }


        public Point3d Position
        {
            get;
            set;
        }


        /// <summary>
        /// 项目号真正所属的Layout。
        /// </summary>
        public string LayoutName
        {
            get;
            set;
        }


        public ProjectNumberLocation()
        {
            ProjectNumber = "";

            LayoutName = "";

            Position = Point3d.Origin;
        }
    }
}