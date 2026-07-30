using Correct_test1.Models;


namespace Correct_test1.Checks
{
    public class ProjectChecker
    {


        public CheckResult CheckProject(
            string currentProject,
            string expectedProject)
        {


            CheckResult result =
                new CheckResult();


            result.Type = "项目号检查";


            result.ObjectName = "项目号";


            result.CurrentValue =
                currentProject;


            result.ExpectedValue =
                expectedProject;



            if (currentProject == expectedProject)
            {

                result.IsError = false;

                result.Message =
                    "项目号正确";

            }
            else
            {

                result.IsError = true;

                result.Message =
                    "当前项目号与要求项目号不一致";

            }


            return result;

        }


    }
}