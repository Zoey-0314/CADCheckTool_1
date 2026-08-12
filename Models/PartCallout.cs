using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace Correct_test1.Models
{
    public class PartCallout
    {
        public int Number { get; set; }
        public Point3d TextPosition { get; set; }
        public string LayoutName { get; set; }
        public ObjectId SpaceId { get; set; }
    }
}
