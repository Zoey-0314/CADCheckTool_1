using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

using Correct_test1.Readers;
using Correct_test1.Checks;
using Correct_test1.Models;

using System.Collections.Generic;


namespace Correct_test1.Command
{

    public class TableTestCommand
    {


        [CommandMethod("TABLETEST")]
        public void TableTest()
        {

            Document doc =
                Application.DocumentManager
                .MdiActiveDocument;


            Editor ed =
                doc.Editor;



            ed.WriteMessage(
                "\n========== TABLE TEST START =========="
            );



            CadTableReader reader =
                new CadTableReader();



            List<CadTableData> tables =
                reader.Read(
                    doc.Database);



            ed.WriteMessage(
                $"\n图框过滤后Table数量:{tables.Count}"
            );



            BomTableRecognizer recognizer =
                new BomTableRecognizer();



            int index = 1;



            foreach (CadTableData table in tables)
            {


                ed.WriteMessage(
                    "\n\n----------------------------"
                );


                ed.WriteMessage(
                    $"\nTable:{index}"
                );


                ed.WriteMessage(
                    $"\nLayer:{table.LayerName}"
                );


                ed.WriteMessage(
                    $"\nRows:{table.Rows}"
                );


                ed.WriteMessage(
                    $"\nColumns:{table.Columns}"
                );



                bool isBom =
                    recognizer.IsBom(table);



                if (!isBom)
                {

                    ed.WriteMessage(
                        "\n类型:普通表格"
                    );


                    index++;

                    continue;

                }



                ed.WriteMessage(
                    "\n类型:BOM"
                );



                BomData bom =
                    recognizer.Parse(table);



                ed.WriteMessage(
                    $"\n图号:{bom.DrawingNumber}"
                );


                ed.WriteMessage(
                    $"\n零件数量:{bom.Items.Count}"
                );



                int itemIndex = 1;



                foreach (BomItem item in bom.Items)
                {


                    ed.WriteMessage(
                        $"\n {itemIndex}. "
                    );


                    ed.WriteMessage(
                        $"No:{item.No} "
                    );


                    ed.WriteMessage(
                        $"Part:{item.PartNumber} "
                    );


                    ed.WriteMessage(
                        $"Name:{item.Name} "
                    );


                    ed.WriteMessage(
                        $"Qut:{item.Quantity}"
                    );


                    itemIndex++;

                }



                index++;

            }



            ed.WriteMessage(
                "\n\n========== TABLE TEST END =========="
            );


        }


    }

}