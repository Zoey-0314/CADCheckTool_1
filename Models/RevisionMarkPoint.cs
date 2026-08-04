namespace Correct_test1.Models
{
    public class RevisionMarkPoint
    {

        public string LayoutName { get; set; }

        public string Mark { get; set; }

        public string MissingField { get; set; }


        // CAD标记坐标

        public double X { get; set; }

        public double Y { get; set; }


        public string Message { get; set; }


        public RevisionMarkPoint()
        {
            LayoutName = "";
            Mark = "";
            MissingField = "";
            Message = "";

            X = 0;
            Y = 0;
        }

    }
}