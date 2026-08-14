using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace Correct_test1.Models
{
    public class BomCalloutIssue
    {
        public int Number { get; set; }

        public string LayoutName { get; set; }

        public Point3d Position { get; set; }

        public ObjectId SpaceId { get; set; }

        public string Message { get; set; }
    }

    public class BomCalloutResult
    {
        public HashSet<int> MissingCallouts { get; set; }

        public HashSet<int> ExtraCallouts { get; set; }
    }
}
