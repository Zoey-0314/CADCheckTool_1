using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

using Correct_test1.Readers;
using Correct_test1.Models;

using System.Collections.Generic;


namespace Correct_test1.Command
{

    public class TableDebugCommand
    {

        [CommandMethod("TABLEDEBUG")]
        public void TableDebug()
        {

            Document doc =
                Application.DocumentManager
                .MdiActiveDocument;


            Editor ed =
                doc.Editor;



            CadTableReader reader =
                new CadTableReader();


            List<CadTableData> tables =
                reader.Read(
                    doc.Database);



            int tableIndex = 1;



            foreach (CadTableData table in tables)
            {

                ed.WriteMessage(
                    $"\n\n========= TABLE {tableIndex} ========="
                );


                for (int r = 0;
                    r < table.Rows;
                    r++)
                {

                    ed.WriteMessage(
                        $"\nRow {r}: "
                    );


                    for (int c = 0;
                        c < table.Columns;
                        c++)
                    {

                        string value =
                            table.GetCell(r, c);


                        ed.WriteMessage(
                            $"[{c}]={value} "
                        );

                    }

                }


                tableIndex++;

            }



            ed.WriteMessage(
                "\n\nDEBUG END"
            );

        }

    }

}