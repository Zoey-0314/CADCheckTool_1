using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace Correct_test1.Models
{
    public enum BomCalloutIssueType
    {
        MissingDrawingCallout,
        ExtraDrawingCallout
    }

    public class BomCalloutIssue
    {
        public BomCalloutIssueType Type { get; set; }

        public int Number { get; set; }

        public string LayoutName { get; set; }

        public Point3d Position { get; set; }

        public ObjectId SpaceId { get; set; }

        public string Message { get; set; }
        public bool IsBomCompareMarker { get; set; }
    }

    public class BomCalloutResult
    {
        public HashSet<int> MissingCallouts { get; set; }

        public HashSet<int> ExtraCallouts { get; set; }
    }
}
