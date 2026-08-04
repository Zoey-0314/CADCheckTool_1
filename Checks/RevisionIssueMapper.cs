using System.Collections.Generic;

using Correct_test1.Models;


namespace Correct_test1.Checks
{

    public class RevisionIssueMapper
    {


        public List<RevisionMarkPoint> Map(
            List<RevisionCheckIssue> issues,
            List<RevisionLocation> locations)
        {

            List<RevisionMarkPoint> result =
                new List<RevisionMarkPoint>();


            foreach (var issue in issues)
            {

                RevisionLocation loc =
                    FindLocation(
                        locations,
                        issue.Mark
                    );


                if (loc == null)
                    continue;



                RevisionMarkPoint point =
                    new RevisionMarkPoint();



                point.LayoutName =
                    issue.LayoutName;


                point.Mark =
                    issue.Mark;


                point.MissingField =
                    issue.MissingField;


                point.Message =
                    issue.Message;




                if (issue.MissingField == "更改日期")
                {

                    point.X =
                        loc.DateX;

                    point.Y =
                        loc.DateY;

                }


                else if (issue.MissingField == "签名")
                {

                    point.X =
                        loc.SignerX;

                    point.Y =
                        loc.SignerY;

                }


                result.Add(point);

            }


            return result;

        }





        private RevisionLocation FindLocation(
            List<RevisionLocation> locations,
            string mark)
        {

            foreach (var loc in locations)
            {

                if (loc.Mark == mark)
                    return loc;

            }


            return null;

        }


    }

}