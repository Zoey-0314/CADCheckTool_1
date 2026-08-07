using Autodesk.AutoCAD.Runtime;
using Correct_test1.Readers;
using System;


namespace Correct_test1.Command
{

    public class StandardPartTestCommand
    {


        [CommandMethod("TESTSTANDARDPART")]
        public void Test()
        {


            string path =
                @"D:\你的路径\Resources\StandardParts.xlsx";



            StandardPartExcelReader reader =
                new StandardPartExcelReader();



            var parts =
                reader.Read(path);



            Autodesk.AutoCAD.ApplicationServices
            .Application
            .ShowAlertDialog(

                $"标准件加载成功\n\n数量:{parts.Count}"

            );


        }


    }

}