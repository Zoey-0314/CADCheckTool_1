using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;

namespace Correct_test1.Models
{
    public class CheckReport
    {
        public string DrawingName { get; set; }

        public string DrawingNumber { get; set; }

        public Point3d DrawingNumberPosition { get; set; }

        public DateTime CheckTime { get; set; }

        public int TotalCount { get; set; }

        public int CorrectCount { get; set; }

        public int ErrorCount { get; set; }

        public List<StandardPartCheckResult> Results { get; set; }
            = new List<StandardPartCheckResult>();

        public List<BomCalloutIssue> BomCalloutIssues { get; set; }
            = new List<BomCalloutIssue>();

        public BomCalloutResult BomCalloutResult { get; set; }
            = new BomCalloutResult();
    }
}
