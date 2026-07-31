using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.Models;

using System;
using System.Collections.Generic;
using System.Linq;



namespace Correct_test1.Readers
{


    /// <summary>
    /// 修改记录读取器
    ///
    /// 支持:
    /// 竖版标题栏
    /// 横版标题栏
    ///
    /// 输出:
    /// List<RevisionInfo>
    ///
    /// </summary>
    public class RevisionTableReader
    {



        /// <summary>
        /// 统一入口
        /// </summary>
        public List<RevisionInfo> Read(
            Database db,
            ObjectId blockId,
            bool horizontal)
        {


            if (horizontal)
            {

                return ReadHorizontal(
                    db,
                    blockId
                );

            }
            else
            {

                return ReadVertical(
                    db,
                    blockId
                );

            }


        }








        #region 竖版读取



        private List<RevisionInfo> ReadVertical(
            Database db,
            ObjectId blockId)
        {



            List<TitleText> texts =
                ReadTexts(
                    db,
                    blockId
                );




            /*
             
             竖版修改记录区域

             X:

             标记:
             82.7599~90.7599

             内容:
             90.7599~147.7611

             日期:
             147.7611~162.7611

             签名:
             162.7611~177.7611

             变更号:
             177.7611~192.7611


             Y:
             表头以下

             */


            List<TitleText> areaTexts =
                texts
                .Where(t =>
                    t.X >= 82.7599
                    &&
                    t.X <= 192.7611
                    &&
                    t.Y >= 65.4386
                    &&
                    t.Y <= 95.4386
                )
                .ToList();




            return ParseRows(
                areaTexts,
                true
            );


        }



        #endregion







        #region 横版读取



        private List<RevisionInfo> ReadHorizontal(
            Database db,
            ObjectId blockId)
        {



            List<TitleText> texts =
                ReadTexts(
                    db,
                    blockId
                );



            HorizontalRevisionTemplate temp =
                new HorizontalRevisionTemplate();





            List<TitleText> areaTexts =
                texts
                .Where(t =>
                    t.X >= temp.MinX
                    &&
                    t.X <= temp.MaxX
                    &&
                    t.Y >= temp.MinY
                    &&
                    t.Y <= temp.MaxY
                )
                .ToList();





            return ParseRows(
                areaTexts,
                false
            );


        }



        #endregion







        #region 行解析



        /// <summary>
        /// 根据Y坐标分行
        /// </summary>
        private List<RevisionInfo> ParseRows(
            List<TitleText> texts,
            bool vertical)
        {



            List<List<TitleText>> rows =
                GroupByRow(
                    texts
                );




            List<RevisionInfo> result =
                new List<RevisionInfo>();





            foreach (List<TitleText> row in rows)
            {



                RevisionInfo info =
                    new RevisionInfo();




                foreach (TitleText text in row)
                {



                    if (vertical)
                    {

                        ParseVerticalColumn(
                            text,
                            info
                        );


                    }
                    else
                    {

                        ParseHorizontalColumn(
                            text,
                            info
                        );


                    }


                }







                //过滤表头

                if (info.Mark == "标记"
                    ||
                    info.Description == "更改内容"
                    ||
                    info.Date == "更改日期")
                {

                    continue;

                }





                //过滤空行

                if (!string.IsNullOrWhiteSpace(info.Mark)
                    ||
                    !string.IsNullOrWhiteSpace(info.Description)
                    ||
                    !string.IsNullOrWhiteSpace(info.Date))
                {

                    result.Add(info);

                }



            }




            return result;


        }








        /// <summary>
        /// Y坐标聚类
        /// </summary>
        private List<List<TitleText>> GroupByRow(
            List<TitleText> texts)
        {



            List<List<TitleText>> rows =
                new List<List<TitleText>>();





            List<TitleText> sorted =
                texts
                .OrderByDescending(
                    t => t.Y)
                .ToList();





            foreach (TitleText text in sorted)
            {



                bool find = false;




                foreach (List<TitleText> row in rows)
                {


                    if (Math.Abs(
                        row[0].Y - text.Y)
                        < 1.5)
                    {

                        row.Add(text);

                        find = true;

                        break;

                    }


                }





                if (!find)
                {

                    rows.Add(
                        new List<TitleText>()
                        {
                            text
                        }
                    );

                }



            }





            return rows;


        }



        #endregion







        #region 列解析




        private void ParseVerticalColumn(
            TitleText text,
            RevisionInfo info)
        {



            if (text.X < 90.7599)
            {

                info.Mark =
                    text.Text;

            }

            else if (text.X < 147.7611)
            {

                info.Description =
                    text.Text;

            }

            else if (text.X < 162.7611)
            {

                info.Date =
                    text.Text;

            }

            else if (text.X < 177.7611)
            {

                info.Signer =
                    text.Text;

            }

            else
            {

                info.RevisionNumber =
                    text.Text;

            }


        }







        private void ParseHorizontalColumn(
            TitleText text,
            RevisionInfo info)
        {


            HorizontalRevisionTemplate temp =
                new HorizontalRevisionTemplate();



            if (text.X < temp.MarkEndX)
            {

                info.Mark =
                    text.Text;

            }

            else if (text.X < temp.DescriptionEndX)
            {

                info.Description =
                    text.Text;

            }

            else if (text.X < temp.DateEndX)
            {

                info.Date =
                    text.Text;

            }

            else if (text.X < temp.SignerEndX)
            {

                info.Signer =
                    text.Text;

            }

            else
            {

                info.RevisionNumber =
                    text.Text;

            }


        }




        #endregion







        #region CAD文字读取




        private List<TitleText> ReadTexts(
            Database db,
            ObjectId blockId)
        {



            List<TitleText> result =
                new List<TitleText>();




            using (Transaction tr =
                db.TransactionManager.StartTransaction())
            {



                BlockTableRecord btr =
                    tr.GetObject(
                        blockId,
                        OpenMode.ForRead)
                    as BlockTableRecord;




                foreach (ObjectId id in btr)
                {


                    Entity ent =
                        tr.GetObject(
                            id,
                            OpenMode.ForRead)
                        as Entity;





                    if (ent is DBText text)
                    {



                        result.Add(
                            new TitleText()
                            {

                                Text =
                                Clean(text.TextString),

                                X =
                                text.Position.X,

                                Y =
                                text.Position.Y

                            }
                        );



                    }





                    else if (ent is MText mt)
                    {



                        result.Add(
                            new TitleText()
                            {

                                Text =
                                Clean(mt.Text),

                                X =
                                mt.Location.X,

                                Y =
                                mt.Location.Y

                            }
                        );



                    }





                    else if (ent is BlockReference br)
                    {



                        foreach (ObjectId aid in br.AttributeCollection)
                        {



                            AttributeReference att =
                                tr.GetObject(
                                    aid,
                                    OpenMode.ForRead)
                                as AttributeReference;




                            if (att == null)
                                continue;




                            result.Add(
                                new TitleText()
                                {

                                    Text =
                                    Clean(att.TextString),

                                    X =
                                    att.Position.X,

                                    Y =
                                    att.Position.Y

                                }
                            );


                        }



                    }



                }





                tr.Commit();

            }




            return result;


        }






        private string Clean(
            string text)
        {

            if (string.IsNullOrEmpty(text))
                return "";



            return text
                .Replace("\\P", "")
                .Trim();


        }




        #endregion




    }


}