using Autodesk.AutoCAD.Geometry;

namespace Correct_test1.Models
{
    public enum StandardPartCheckStatus
    {
        Correct,
        NameError,
        FormatDifference,
        NotRegistered,
        MultipleMatch
    }

    public class StandardPartCheckResult
    {
        public StandardPartCheckStatus Status { get; set; }

        public BomItem BomItem { get; set; }

        public string DrawingNumber { get; set; }

        public int BomRow { get; set; }

        public int BomColumn { get; set; }

        public Point3d CellPosition { get; set; }

        public StandardPart StandardPart { get; set; }

        public string CorrectPartNumber { get; set; }

        public string CorrectName { get; set; }

        public string Message { get; set; }
    }
}