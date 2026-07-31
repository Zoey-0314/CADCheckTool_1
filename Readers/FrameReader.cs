using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

using Correct_test1.Models;

using System.Collections.Generic;


namespace Correct_test1.Readers
{


    public class FrameReader
    {


        public FrameInfo ReadFrame(
            Database db,
            ObjectId blockId,
            Editor ed)
        {



            FrameInfo frame =
                new FrameInfo();




            using (Transaction trans =
                db.TransactionManager.StartTransaction())
            {



                BlockTableRecord btr =
                    trans.GetObject(
                        blockId,
                        OpenMode.ForRead)
                    as BlockTableRecord;



                if (btr == null)
                    return frame;





                Extents3d? total =
                    null;




                foreach (ObjectId id in btr)
                {



                    Entity ent =
                        trans.GetObject(
                            id,
                            OpenMode.ForRead)
                        as Entity;



                    if (ent == null)
                        continue;




                    //读取线和多段线

                    if (ent is Line ||
                       ent is Polyline)
                    {

                        try
                        {

                            Extents3d ext =
                                ent.GeometricExtents;



                            if (total == null)
                            {

                                total =
                                    ext;

                            }
                            else
                            {

                                Extents3d temp =
                                    total.Value;


                                temp.AddExtents(
                                    ext
                                );


                                total =
                                    temp;

                            }


                        }
                        catch
                        {

                        }


                    }



                }





                if (total != null)
                {


                    Extents3d ext =
                        total.Value;



                    frame.MinX =
                        ext.MinPoint.X;


                    frame.MinY =
                        ext.MinPoint.Y;


                    frame.MaxX =
                        ext.MaxPoint.X;


                    frame.MaxY =
                        ext.MaxPoint.Y;



                    if (frame.Width >
                       frame.Height)
                    {

                        frame.Direction =
                            "Horizontal";

                    }
                    else
                    {

                        frame.Direction =
                            "Vertical";

                    }


                }



                trans.Commit();


            }





            if (ed != null)
            {

                ed.WriteMessage(
                    "\n图框:"
                    +
                    frame.Width
                    +
                    " x "
                    +
                    frame.Height
                    +
                    " "
                    +
                    frame.Direction
                );

            }



            return frame;


        }



    }

}