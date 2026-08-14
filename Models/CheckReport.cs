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

        public BomCalloutResult BomCalloutResult { get; set; }
            = new BomCalloutResult();

        public List<BomData> Boms { get; set; }
    = new List<BomData>();

        public List<TitleText> DrawingTexts { get; set; }
    = new List<TitleText>();

        public HashSet<int> BomNumbers { get; set; }
    = new HashSet<int>();

        public HashSet<int> DrawingNumbers { get; set; }
    = new HashSet<int>();
    }
}
