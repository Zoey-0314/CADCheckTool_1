using Correct_test1.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


namespace Correct_test1.Readers
{

    /// <summary>
    /// 标题栏解析器
    /// 根据文字位置关系解析标题栏信息
    /// </summary>
    public class TitleBlockParser
    {


        public DrawingInfo Parse(
            List<TitleText> texts)
        {


            DrawingInfo info =
                new DrawingInfo();



            if (texts == null || texts.Count == 0)
                return info;



            // 按布局分别处理
            foreach (var layoutGroup in texts.GroupBy(
                x => x.LayoutName))
            {


                List<TitleText> layoutTexts =
                    layoutGroup.ToList();



                ParseLayout(
                    layoutTexts,
                    info
                );


            }



            return info;


        }





        private void ParseLayout(
            List<TitleText> texts,
            DrawingInfo info)
        {



            for (int i = 0;
                i < texts.Count;
                i++)
            {


                TitleText current =
                    texts[i];



                string value =
                    Clean(
                        current.Text);



                if (string.IsNullOrEmpty(value))
                    continue;



                //--------------------------------
                // 图号
                //--------------------------------

                if (value.Contains("图号"))
                {

                    TitleText target =
                        FindRightText(
                            current,
                            texts
                        );


                    if (target != null)
                    {

                        info.DrawingNumber =
                            Clean(target.Text);

                    }

                }




                //--------------------------------
                // 名称
                //--------------------------------

                else if (value.Contains("名称"))
                {

                    TitleText target =
                        FindRightText(
                            current,
                            texts
                        );


                    if (target != null)
                    {

                        info.DrawingName =
                            Clean(target.Text);

                    }

                }





                //--------------------------------
                // 材料
                //--------------------------------

                else if (value.Contains("材料"))
                {

                    TitleText target =
                        FindNearText(
                            current,
                            texts
                        );


                    if (target != null)
                    {

                        info.Material =
                            Clean(target.Text);

                    }


                }




                //--------------------------------
                // 规格
                //--------------------------------

                else if (value.Contains("规格"))
                {

                    TitleText target =
                        FindNearText(
                            current,
                            texts
                        );


                    if (target != null)
                    {

                        info.Specification =
                            Clean(target.Text);

                    }

                }





                //--------------------------------
                // 日期
                //--------------------------------

                else if (value == "日期")
                {


                    TitleText target =
                        FindBelowText(
                            current,
                            texts
                        );



                    if (target != null)
                    {

                        if (IsDate(target.Text))
                        {

                            info.TitleDate =
                                Clean(target.Text);

                        }

                    }


                }




            }


        }








        /// <summary>
        /// 找右侧文字
        /// 例如：
        /// 图号  NS135H
        /// </summary>
        private TitleText FindRightText(
            TitleText source,
            List<TitleText> texts)
        {


            return texts
                .Where(x =>
                    x.X > source.X
                    &&
                    Math.Abs(
                        x.Y - source.Y
                    )
                    < 10
                )
                .OrderBy(
                    x => x.X
                )
                .FirstOrDefault();

        }








        /// <summary>
        /// 找附近文字
        /// </summary>
        private TitleText FindNearText(
            TitleText source,
            List<TitleText> texts)
        {


            return texts
                .Where(x =>
                    x != source
                    &&
                    Math.Abs(
                        x.X - source.X
                    )
                    < 80
                    &&
                    Math.Abs(
                        x.Y - source.Y
                    )
                    < 20
                )
                .OrderBy(
                    x =>
                    Math.Abs(
                        x.Y - source.Y
                    )
                )
                .FirstOrDefault();


        }







        /// <summary>
        /// 找下面文字
        /// 日期：
        /// 2025-05-09
        /// </summary>
        private TitleText FindBelowText(
            TitleText source,
            List<TitleText> texts)
        {


            return texts
                .Where(x =>
                    x.Y < source.Y
                    &&
                    Math.Abs(
                        x.X - source.X
                    )
                    < 30
                )
                .OrderByDescending(
                    x => x.Y
                )
                .FirstOrDefault();


        }







        private bool IsDate(
            string text)
        {

            return Regex.IsMatch(
                text,
                @"20\d{2}-\d{2}-\d{2}"
            );

        }






        private string Clean(
            string text)
        {

            if (string.IsNullOrEmpty(text))
                return "";



            return text
                .Replace("\\P", "")
                .Replace("\n", "")
                .Trim();

        }


    }

}