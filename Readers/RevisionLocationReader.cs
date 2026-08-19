using System;
using System.Collections.Generic;
using System.Linq;

using Correct_test1.Models;


namespace Correct_test1.Readers
{

    public class RevisionLocationReader
    {


        // 横版坐标
        // 左五列 + 右五列

        private readonly double[] HorizontalXLines =
        {
            45.2828,   // 左标记
            55.2828,   // 左内容
            130.2828,  // 左日期
            150.2828,  // 左签名
            170.2828,  // 左变更号

            187.5633,  // 中间

            197.5633,  // 右标记
            272.5633,  // 右内容
            292.5633,  // 右日期
            312.5633,  // 右签名
            329.8438   // 右变更号
        };



        private const double H_MinX = 45.2828;
        private const double H_MaxX = 329.8438;

        private const double H_MinY = 37.145;
        private const double H_MaxY = 67.145;



        // 竖版坐标

        private readonly double[] VerticalXLines =
        {
            82.7599,   // 标记
            90.7599,   // 更改内容
            147.7611,  // 日期
            162.7611,  // 签名
            177.7611,  // 变更号
            192.7611   // 右边界
        };



        private const double V_MinY = 65.4386;
        private const double V_MaxY = 95.4386;





        // 横版入口

        public List<RevisionLocation> ReadHorizontalLocations(
            string layoutName,
            List<TitleText> texts)
        {

            return ReadLocations(
                layoutName,
                texts,
                true
            );

        }





        // 竖版入口

        public List<RevisionLocation> ReadVerticalLocations(
            string layoutName,
            List<TitleText> texts)
        {

            return ReadLocations(
                layoutName,
                texts,
                false
            );

        }





        // 核心读取

        private List<RevisionLocation> ReadLocations(
            string layoutName,
            List<TitleText> texts,
            bool horizontal)
        {


            List<RevisionLocation> result =
                new List<RevisionLocation>();


            if (texts == null)
                return result;




            double minX;
            double maxX;
            double minY;
            double maxY;



            double[] xLines;



            if (horizontal)
            {

                minX = H_MinX;
                maxX = H_MaxX;

                minY = H_MinY;
                maxY = H_MaxY;

                xLines = HorizontalXLines;

            }
            else
            {

                minX = VerticalXLines[0];
                maxX = VerticalXLines[
                    VerticalXLines.Length - 1];

                minY = V_MinY;
                maxY = V_MaxY;

                xLines = VerticalXLines;

            }





            List<TitleText> dataTexts =
                texts
                .Where(t =>
                    t.X >= minX &&
                    t.X <= maxX &&
                    t.Y > minY &&
                    t.Y < maxY
                )
                .ToList();




            double yTol = 1.5;




            foreach (TitleText mark in dataTexts)
            {


                int column =
                    GetColumn(
                        mark.X,
                        xLines
                    );



                // 横版：
                // 左标记列0
                // 右标记列5
                //
                // 竖版：
                // 标记列0


                if (horizontal)
                {

                    if (column != 0 &&
                       column != 5)
                        continue;

                }
                else
                {

                    if (column != 0)
                        continue;

                }



                if (!IsNumber(mark.Text))
                    continue;




                RevisionLocation location =
                    new RevisionLocation();



                location.LayoutName =
                    layoutName;


                location.Mark =
                    mark.Text;


                location.MarkX =
                    mark.X;

                location.MarkY =
                    mark.Y;




                bool rightSide =
                    horizontal &&
                    column == 5;




                int descColumn;
                int dateColumn;
                int signerColumn;



                if (horizontal)
                {

                    descColumn =
                        rightSide ? 6 : 1;


                    dateColumn =
                        rightSide ? 7 : 2;


                    signerColumn =
                        rightSide ? 8 : 3;

                }
                else
                {

                    descColumn = 1;

                    dateColumn = 2;

                    signerColumn = 3;

                }





                // 日期

                TitleText date =
                    FindSameRowText(
                        dataTexts,
                        dateColumn,
                        mark.Y,
                        yTol,
                        xLines
                    );


                if (date != null)
                {

                    location.DateX =
                        date.X;

                    location.DateY =
                        date.Y;

                }
                else
                {

                    int index =
                        dateColumn;


                    location.DateX =
                        (xLines[index]
                        +
                        xLines[index + 1])
                        / 2;


                    location.DateY =
                        mark.Y;

                }






                // 签名

                TitleText signer =
                    FindSameRowText(
                        dataTexts,
                        signerColumn,
                        mark.Y,
                        yTol,
                        xLines
                    );


                if (signer != null)
                {

                    location.SignerX =
                        signer.X;

                    location.SignerY =
                        signer.Y;

                }
                else
                {

                    int index =
                        signerColumn;


                    location.SignerX =
                        (xLines[index]
                        +
                        xLines[index + 1])
                        / 2;


                    location.SignerY =
                        mark.Y;

                }





                // 更改内容


                TitleText desc =
                    FindSameRowText(
                        dataTexts,
                        descColumn,
                        mark.Y,
                        yTol,
                        xLines
                    );



                if (desc != null)
                {

                    location.DescriptionX =
                        desc.X;


                    location.DescriptionY =
                        desc.Y;

                }
                else
                {

                    int index =
                        descColumn;


                    location.DescriptionX =
                        (xLines[index]
                        +
                        xLines[index + 1])
                        / 2;


                    location.DescriptionY =
                        mark.Y;

                }




                result.Add(location);

            }



            return result;

        }





        // 查找同一行指定列文字

        private TitleText FindSameRowText(
            List<TitleText> texts,
            int column,
            double y,
            double tol,
            double[] xLines)
        {


            return texts
                .Where(t =>
                    GetColumn(t.X, xLines) == column
                    &&
                    Math.Abs(t.Y - y) <= tol
                )
                .OrderBy(t => t.X)
                .FirstOrDefault();


        }






        // 根据X判断列

        private int GetColumn(
            double x,
            double[] xLines)
        {


            for (int i = 0; i < xLines.Length - 1; i++)
            {

                if (
                    x >= xLines[i]
                    &&
                    x < xLines[i + 1]
                )
                {
                    return i;
                }

            }


            return -1;

        }






        // 判断是否标记数字

        private bool IsNumber(
            string text)
        {


            if (string.IsNullOrWhiteSpace(text))
                return false;



            foreach (char c in text)
            {

                if (!char.IsDigit(c))
                    return false;

            }


            return true;

        }


    }

}