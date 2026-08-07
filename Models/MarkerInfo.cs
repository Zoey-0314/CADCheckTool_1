using Autodesk.AutoCAD.Geometry;
using Correct_test1.Models;

namespace Correct_test1.Models
{
    public class MarkerInfo
    {
        public string Text { get; set; }

        public Point3d Position { get; set; }

        public StandardPartCheckResult Result { get; set; }
    }
}
