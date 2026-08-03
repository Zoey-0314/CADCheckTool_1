using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;



namespace Correct_test1.Readers
{


    /// <summary>
    /// 修改记录读取器
    ///
    /// 竖版：
    /// 保留原成功逻辑
    ///
    /// 横版：
    /// 新增十列表格读取
    ///
    /// </summary>
    public class RevisionTableReader
    {



        //================================================
        // 横版修改记录模板坐标
        //================================================


        private readonly double[] HorizontalXLines =
        {
    // 左五列

    45.2828,
    55.2828,
    130.2828,
    150.2828,
    170.2828,

    // 中间分割线

    187.5633,

    // 右五列

    197.5633,
    272.5633,
    292.5633,
    312.5633,
    329.8438
};



        private readonly double[] HorizontalYLines =
        {
            67.145,
            61.145,
            55.145,
            49.145,
            43.145,
            37.145
        };



        /// <summary>
        /// 横版修改记录读取
        ///
        /// 返回：
        /// 左五列 + 右五列
        /// </summary>
        public List<HorizontalRevisionRow> ReadHorizontalRows(
            Database db,
            ObjectId blockId)
        {



            List<TitleText> texts =
                ReadTexts(
                    db,
                    blockId
                );
            Editor ed =
    Autodesk.AutoCAD.ApplicationServices.Application
    .DocumentManager
    .MdiActiveDocument
    .Editor;


            foreach (TitleText t in texts)
            {
                if (
                    t.Y < 80 &&
                    t.Y > 30 &&
                    t.X > 40 &&
                    t.X < 330
                )
                {

                    ed.WriteMessage(
                        "\n文字:"
                        + t.Text
                        +
                        " X="
                        +
                        t.X
                        +
                        " Y="
                        +
                        t.Y
                    );

                }
            }
            // 调试横版文字坐标

            foreach (TitleText t in texts)
            {
                if (
                    t.Y < 80 &&
                    t.Y > 30 &&
                    t.X > 40 &&
                    t.X < 330
                )
                {
                    System.Diagnostics.Debug.WriteLine(
                        t.Text
                        +
                        "   X="
                        +
                        t.X
                        +
                        "   Y="
                        +
                        t.Y
                    );
                }
            }



            List<HorizontalRevisionRow> result =
                ParseHorizontalTable(
                    texts
                );


            return result;

        }




        public List<TitleText> ReadAllTexts(
    Database db,
    ObjectId blockId)
        {
            return ReadTexts(
                db,
                blockId
            );
        }
        /// <summary>
        /// 兼容旧测试命令
        /// 横版返回普通RevisionInfo列表
        /// </summary>
        public List<RevisionInfo> ReadHorizontal(
            Database db,
            ObjectId blockId)
        {


            List<HorizontalRevisionRow> rows =
                ReadHorizontalRows(
                    db,
                    blockId
                );



            List<RevisionInfo> result =
                new List<RevisionInfo>();



            foreach (HorizontalRevisionRow row in rows)
            {

                if (IsValid(row.Left))
                {
                    result.Add(row.Left);
                }



                if (IsValid(row.Right))
                {
                    result.Add(row.Right);
                }

            }



            return result;

        }







        //================================================
        // 竖版读取
        // 注意：
        // 这一部分保持原逻辑
        //================================================



        /// <summary>
        /// 读取竖版修改记录
        /// </summary>
        public List<RevisionInfo> ReadVertical(
            Database db,
            ObjectId blockId)
        {



            List<TitleText> texts =
                ReadTexts(
                    db,
                    blockId
                );




            // 竖版原坐标
            List<TitleText> revisionTexts =
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





            List<List<TitleText>> rows =
                GroupByRow(
                    revisionTexts
                );





            List<RevisionInfo> result =
                new List<RevisionInfo>();





            foreach (List<TitleText> row in rows)
            {



                RevisionInfo info =
                    ParseRow(row);





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
        //================================================
        // 横版十列表格读取核心
        //================================================


        private List<HorizontalRevisionRow> ParseHorizontalTable(
            List<TitleText> texts)
        {


            List<HorizontalRevisionRow> result =
                new List<HorizontalRevisionRow>();




            // 修改记录数据区域
            //
            // 注意：
            // 这里不包含：
            //
            // Y 77.145~73.145
            // 更改记录标题
            //
            // Y 73.145~67.145
            // 表头
            //
            // 只读取67.145以下数据


            List<TitleText> dataTexts =
                texts
                .Where(t =>

                    t.X >= 45.2828
                    &&
                    t.X <= 329.8438
                    &&
                    t.Y < 67.145
                    &&
                    t.Y > 37.145

                )
                .ToList();





            // 五个数据行

            for (int rowIndex = 0;
                rowIndex < HorizontalYLines.Length - 1;
                rowIndex++)
            {


                double top =
                    HorizontalYLines[rowIndex];


                double bottom =
                    HorizontalYLines[rowIndex + 1];





                List<TitleText> rowTexts =
                    dataTexts
                    .Where(t =>

                        t.Y < top
                        &&
                        t.Y > bottom

                    )
                    .OrderBy(t => t.X)
                    .ToList();




                RevisionInfo left =
                    ParseHorizontalSide(
                        rowTexts,
                        false
                    );




                RevisionInfo right =
                    ParseHorizontalSide(
                        rowTexts,
                        true
                    );






                // 空行不输出

                bool hasLeft =
                    IsValid(left);



                bool hasRight =
                    IsValid(right);




                if (!hasLeft && !hasRight)
                    continue;






                result.Add(
                    new HorizontalRevisionRow()
                    {

                        RowNumber =
                            rowIndex + 1,


                        Left =
                            left,


                        Right =
                            right

                    }
                );



            }





            return result;


        }






        /// <summary>
        /// 解析横版一侧五列
        ///
        /// right=false:
        /// 左侧五列
        ///
        /// right=true:
        /// 右侧五列
        /// </summary>
        private RevisionInfo ParseHorizontalSide(
            List<TitleText> rowTexts,
            bool right)
        {



            RevisionInfo info =
                new RevisionInfo();






            foreach (TitleText text in rowTexts)
            {



                int column =
                    GetHorizontalColumn(
                        text.X
                    );



                if (column < 0)
                    continue;






                // 左侧五列

                if (!right)
                {


                    if (column == 0)
                    {
                        info.Mark =
                            Append(
                                info.Mark,
                                text.Text
                            );
                    }


                    else if (column == 1)
                    {
                        info.Description =
                            Append(
                                info.Description,
                                text.Text
                            );
                    }


                    else if (column == 2)
                    {
                        info.Date =
                            Append(
                                info.Date,
                                text.Text
                            );
                    }


                    else if (column == 3)
                    {
                        info.Signer =
                            Append(
                                info.Signer,
                                text.Text
                            );
                    }


                    else if (column == 4)
                    {
                        info.RevisionNumber =
                            Append(
                                info.RevisionNumber,
                                text.Text
                            );
                    }


                }






                // 右侧五列

                else
                {


                    if (column == 5)
                    {
                        info.Mark =
                            Append(
                                info.Mark,
                                text.Text
                            );
                    }


                    else if (column == 6)
                    {
                        info.Description =
                            Append(
                                info.Description,
                                text.Text
                            );
                    }


                    else if (column == 7)
                    {
                        info.Date =
                            Append(
                                info.Date,
                                text.Text
                            );
                    }


                    else if (column == 8)
                    {
                        info.Signer =
                            Append(
                                info.Signer,
                                text.Text
                            );
                    }


                    else if (column == 9)
                    {
                        info.RevisionNumber =
                            Append(
                                info.RevisionNumber,
                                text.Text
                            );
                    }



                }



            }





            return info;


        }








        /// <summary>
        /// 根据X坐标判断所在列
        /// </summary>
        private int GetHorizontalColumn(
            double x)
        {



            for (int i = 0;
                i < HorizontalXLines.Length - 1;
                i++)
            {



                if (
                    x >= HorizontalXLines[i]
                    &&
                    x < HorizontalXLines[i + 1]
                )
                {

                    return i;

                }



            }




            // 最后一列边界

            if (
                x >= HorizontalXLines[
                    HorizontalXLines.Length - 1]
            )
            {
                return 9;
            }




            return -1;


        }







        /// <summary>
        /// 判断横版模板
        /// </summary>
        private bool IsHorizontalTable(
            List<TitleText> texts)
        {
            // 容差
            double eps = 1.0;

            // 横版总区域 X 范围
            double minX = HorizontalXLines.First();
            double maxX = HorizontalXLines.Last();

            // 标题和表头的 Y 区间（参考项目文档）
            double titleTop = 77.145;
            double titleBottom = 73.145;

            double headerTop = 73.145;
            double headerBottom = 67.145;

            // 1) 检查是否存在 "更改记录" 标题，且位于标题区间内并在横版 X 范围内
            bool hasTitle =
                texts.Any(t =>
                    t.Text.Contains("更改记录")
                    && t.X >= minX - eps
                    && t.X <= maxX + eps
                    && t.Y < titleTop + eps
                    && t.Y > titleBottom - eps
                );

            if (!hasTitle)
                return false;

            // 2) 检查五个表头关键字是否都出现在表头区间（位置约束），要求至少五项命中
            string[] headers = new[] { "标记", "更改内容", "更改日期", "签名", "变更号" };

            int headerCount =
                headers.Count(h =>
                    texts.Any(t =>
                        t.Text.Contains(h)
                        && t.X >= minX - eps
                        && t.X <= maxX + eps
                        && t.Y < headerTop + eps
                        && t.Y > headerBottom - eps
                    )
                );

            return headerCount >= 5;
        }






        private bool IsValid(
            RevisionInfo info)
        {



            if (info == null)
                return false;



            //过滤表头

            string all =
    (info.Mark ?? "")
    +
    (info.Description ?? "")
    +
    (info.Date ?? "")
    +
    (info.Signer ?? "")
    +
    (info.RevisionNumber ?? "");


            if (
                all.Contains("标记")
                ||
                all.Contains("更改内容")
                ||
                all.Contains("更改日期")
                ||
                all.Contains("签名")
                ||
                all.Contains("变更号")
            )
            {
                return false;
            }





            return
                !string.IsNullOrWhiteSpace(info.Mark)
                ||
                !string.IsNullOrWhiteSpace(info.Description)
                ||
                !string.IsNullOrWhiteSpace(info.Date)
                ||
                !string.IsNullOrWhiteSpace(info.Signer)
                ||
                !string.IsNullOrWhiteSpace(info.RevisionNumber);


        }






        private string Append(
            string oldText,
            string newText)
        {



            if (string.IsNullOrWhiteSpace(oldText))
            {
                return newText;
            }



            if (oldText == newText)
            {
                return oldText;
            }



            return oldText
                +
                newText;


        }
        //================================================
        // 原竖版：按Y坐标分行
        //================================================


        private List<List<TitleText>> GroupByRow(
            List<TitleText> texts)
        {



            List<List<TitleText>> rows =
                new List<List<TitleText>>();





            List<TitleText> sorted =
                texts
                .OrderByDescending(t => t.Y)
                .ToList();





            foreach (TitleText text in sorted)
            {



                bool added =
                    false;





                foreach (List<TitleText> row in rows)
                {



                    double rowY =
                        row[0].Y;




                    if (Math.Abs(
                        rowY - text.Y)
                        < 1.5)
                    {


                        row.Add(text);

                        added =
                            true;


                        break;


                    }



                }





                if (!added)
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


        //================================================
        // 原竖版：解析一行
        //================================================



        private RevisionInfo ParseRow(
            List<TitleText> row)
        {



            RevisionInfo info =
                new RevisionInfo();


            foreach (TitleText text in row)
            {



                // 标记

                if (
                    text.X >= 82.7599
                    &&
                    text.X < 90.7599
                )
                {

                    info.Mark =
                        text.Text;

                }


                // 更改内容

                else if (
                    text.X >= 90.7599
                    &&
                    text.X < 147.7611
                )
                {

                    info.Description =
                        text.Text;

                }


                // 日期

                else if (
                    text.X >= 147.7611
                    &&
                    text.X < 162.7611
                )
                {

                    info.Date =
                        text.Text;

                }


                // 签名

                else if (
                    text.X >= 162.7611
                    &&
                    text.X < 177.7611
                )
                {

                    info.Signer =
                        text.Text;

                }



                // 变更号

                else if (
                    text.X >= 177.7611
                    &&
                    text.X <= 192.7611
                )
                {

                    info.RevisionNumber =
                        text.Text;

                }



            }


            return info;


        }


        //================================================
        // CAD文字读取
        //================================================



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


                if (btr == null)
                {
                    return result;
                }



                foreach (ObjectId id in btr)
                {



                    Entity ent =
                        tr.GetObject(
                            id,
                            OpenMode.ForRead)
                        as Entity;


                    if (ent == null)
                        continue;


                    // 普通文字

                    if (ent is DBText text)
                    {


                        result.Add(
                            new TitleText()
                            {

                                Text =
                                Clean(
                                    text.TextString),

                                X =
                                text.Position.X,

                                Y =
                                text.Position.Y

                            }
                        );


                    }

                    // 多行文字

                    else if (ent is MText mt)
                    {


                        result.Add(
                            new TitleText()
                            {

                                Text =
                                Clean(
                                    mt.Text),

                                X =
                                mt.Location.X,

                                Y =
                                mt.Location.Y

                            }
                        );


                    }


                    // 属性块文字

                    else if (ent is BlockReference br)
                    {



                        foreach (ObjectId aid
                            in br.AttributeCollection)
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
                                    Clean(
                                        att.TextString),


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

        //================================================
        // 字符清理
        //================================================



        private string Clean(
            string text)
        {


            if (string.IsNullOrEmpty(text))
                return "";


            return text
                .Replace("\\P", "")
                .Trim();


        }

    }



    //================================================
    // 横版一行数据结构
    //================================================



    public class HorizontalRevisionRow
    {


        public int RowNumber
        {
            get;
            set;
        }

        // 左五列

        public RevisionInfo Left
        {
            get;
            set;
        }



        // 右五列

        public RevisionInfo Right
        {
            get;
            set;
        }


        public HorizontalRevisionRow()
        {

            Left =
                new RevisionInfo();

            Right =
                new RevisionInfo();
        }

    }

}