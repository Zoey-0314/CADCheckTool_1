using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace Correct_test1.Readers
{

    public class ProjectReader
    {


        //========================================
        // 原接口 保留
        //========================================

        public List<string> ReadProjects(
            Database db,
            Editor ed)
        {

            return ReadProjectsInternal(
                db,
                ed
            );

        }



        //========================================
        // 新接口 给批量使用
        //========================================

        public List<string> ReadProjects(
            Database db)
        {

            return ReadProjectsInternal(
                db,
                null
            );

        }





        private List<string> ReadProjectsInternal(
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



                        if (ent == null)
                            continue;



                        if (ent is BlockReference block)
                        {

                            ReadBlock(
                                block,
                                trans,
                                projects,
                                ed
                            );

                        }


                        else if (ent is DBText text)
                        {

                            AddProject(
                                text.TextString,
                                projects,
                                ed
                            );

                        }


                        else if (ent is MText mtext)
                        {

                            AddProject(
                                mtext.Text,
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








        private bool IsProjectNumber(
            string text)
        {


            if (string.IsNullOrEmpty(text))
                return false;



            text =
                text.Trim()
                .ToUpper();



            string pattern =
                @"N\d{4}[A-Z]{2}\d{3}(-[A-Z0-9]+)?";



            return Regex.IsMatch(
                text,
                pattern
            );

        }







        private string GetProjectNumber(
            string text)
        {


            Match match =
                Regex.Match(
                    text.ToUpper(),
                    @"N\d{4}[A-Z]{2}\d{3}"
                );



            if (match.Success)
                return match.Value;



            return null;


        }







        private void AddProject(
            string text,
            List<string> projects,
            Editor ed)
        {


            if (string.IsNullOrEmpty(text))
                return;



            text =
                text.Replace(
                    "\\P",
                    ""
                )
                .Trim();



            if (IsProjectNumber(text))
            {


                string projectNumber =
                    GetProjectNumber(text);



                if (!string.IsNullOrEmpty(projectNumber))
                {


                    projects.Add(
                        projectNumber
                    );



                    // 防止批量空引用
                    if (ed != null)
                    {

                        ed.WriteMessage(
                            "\n发现项目号:"
                            +
                            projectNumber
                        );

                    }


                }


            }


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



                if (ent == null)
                    continue;



                if (ent is DBText text)
                {


                    AddProject(
                        text.TextString,
                        projects,
                        ed
                    );


                }



                else if (ent is MText mtext)
                {


                    AddProject(
                        mtext.Text,
                        projects,
                        ed
                    );


                }


            }


        }


    }

}