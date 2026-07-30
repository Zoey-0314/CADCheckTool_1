using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Correct_test1.Readers
{
    public class ProjectReader
    {


        public List<string> ReadProjects(
            Database db,
            Editor ed)
        {


            List<string> projects =
                new List<string>();



            using (Transaction trans =
                db.TransactionManager.StartTransaction())
            {


                BlockTable bt =
                    trans.GetObject(
                        db.BlockTableId,
                        OpenMode.ForRead)
                    as BlockTable;



                ObjectId[] spaces =
                {
                    bt[BlockTableRecord.ModelSpace],
                    bt[BlockTableRecord.PaperSpace]
                };



                foreach (ObjectId spaceId in spaces)
                {


                    BlockTableRecord btr =
                        trans.GetObject(
                            spaceId,
                            OpenMode.ForRead)
                        as BlockTableRecord;



                    foreach (ObjectId id in btr)
                    {


                        Entity ent =
                            trans.GetObject(
                                id,
                                OpenMode.ForRead)
                            as Entity;



                        if (ent is BlockReference block)
                        {

                            ReadBlock(
                                block,
                                trans,
                                projects,
                                ed
                            );

                        }


                    }

                }


                trans.Commit();

            }



            return projects;

        }


        private bool IsProjectNumber(string text)
        {

            if (string.IsNullOrEmpty(text))
                return false;


            text = text.Trim();


            string pattern =
                @"^N\d{4}US\d{3}-L\d+$";


            return Regex.IsMatch(
                text,
                pattern
            );

        }


        private void ReadBlock(
            BlockReference block,
            Transaction trans,
            List<string> projects,
            Editor ed)
        {


            BlockTableRecord btr =
                trans.GetObject(
                    block.BlockTableRecord,
                    OpenMode.ForRead)
                as BlockTableRecord;



            foreach (ObjectId id in btr)
            {

                Entity ent =
                    trans.GetObject(
                        id,
                        OpenMode.ForRead)
                    as Entity;



                if (ent is DBText text)
                {

                    string value =
                        text.TextString;


                    if (IsProjectNumber(value))
                    {

                        projects.Add(value);


                        ed.WriteMessage(
                            "\n发现项目号:"
                            + value
                        );

                    }

                }



                else if (ent is MText mtext)
                {


                    string value =
                        mtext.Text.Trim();



                    if (IsProjectNumber(value))
                    {

                        projects.Add(value);


                        ed.WriteMessage(
                            "\n发现项目号:"
                            + value
                        );

                    }

                }


            }


        }


    }
}