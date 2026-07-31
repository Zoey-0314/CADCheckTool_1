using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

using Correct_test1.Models;
using Correct_test1.Checks;
using Correct_test1.Readers;


namespace Correct_test1.Core
{
    public class DrawingCheckManager
    {


        public List<CheckResult> CheckDrawing(
            Database db,
            Editor ed,
            string expectedProject)
        {


            List<CheckResult> results =
                new List<CheckResult>();



            //1.读取项目号

            ProjectReader reader =
                new ProjectReader();


            List<string> projects =
                reader.ReadProjects(
                    db,
                    ed
                );



            //2.检查项目号

            ProjectChecker checker =
                new ProjectChecker();



            if (projects.Count == 0)
            {

                CheckResult result =
                    new CheckResult();


                result.Type =
                    "项目号检查";


                result.ObjectName =
                    "项目号";


                result.CurrentValue =
                    "";


                result.ExpectedValue =
                    expectedProject;


                result.Message =
                    "未找到项目号";


                result.IsError =
                    true;


                results.Add(result);

            }
            else
            {

                foreach (string project in projects)
                {

                    CheckResult result =
                        checker.CheckProject(
                            project,
                            expectedProject
                        );


                    results.Add(result);

                }

            }



            return results;


        }


    }
}