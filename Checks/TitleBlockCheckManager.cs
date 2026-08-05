using System.Collections.Generic;

using Correct_test1.Models;
using Correct_test1.Readers;
using Correct_test1.Markers;
using System.IO;
namespace Correct_test1.Checks
{


    /// <summary>
    /// 标题栏检查管理器
    ///
    /// 负责:
    /// 1. 读取标题栏文字
    /// 2. 判断横竖版
    /// 3. 解析标题栏
    /// 4. 调用标题栏检查
    ///
    /// 不负责:
    /// 绿色标记
    /// </summary>
    public class TitleBlockCheckManager
    {


        private readonly TitleBlockReader reader;

        private readonly TitleBlockRegionParser parser;

        private readonly TitleBlockChecker checker;

        private readonly TitleBlockDrawingNumberMarker drawingNumberMarker;


        public TitleBlockCheckManager()
        {

            reader =
                new TitleBlockReader();


            parser =
                new TitleBlockRegionParser();


            checker =
                new TitleBlockChecker();

            drawingNumberMarker =
    new TitleBlockDrawingNumberMarker();

        }




        /// <summary>
        /// 检查一个布局标题栏
        /// </summary>
        public List<CheckResult> Check(
            Autodesk.AutoCAD.DatabaseServices.Database db,
            LayoutInfo layout,
            string filePath,
            string fileName,
            bool drawMarker)
        {


            List<CheckResult> results =
                new List<CheckResult>();



            if (layout == null)
                return results;



            //--------------------------------
            // 读取当前布局标题栏文字
            //--------------------------------

            List<TitleText> texts =
                reader.Read(
                    db,
                    new List<LayoutInfo>
                    {
                        layout
                    }
                );



            if (texts.Count == 0)
                return results;




            //--------------------------------
            // 判断横竖版
            // 保持原有逻辑
            //--------------------------------

            int markCount = 0;


            foreach (TitleText t in texts)
            {

                if (t.Text.Contains("标记"))
                {
                    markCount++;
                }

            }



            bool isHorizontal =
                markCount >= 2;



            //--------------------------------
            // 标题栏解析
            //--------------------------------

            DrawingInfo info =
                parser.Parse(
                    texts,
                    isHorizontal
                );



            info.FilePath =
                filePath;


            info.FileName =
                fileName;


            info.LayoutName =
                layout.LayoutName;


            info.IsHorizontal =
                isHorizontal;




            //--------------------------------
            // 标题栏字段检查
            //--------------------------------

            results.AddRange(
                checker.Check(
                    info
                )
            );




            //--------------------------------
            // 标题栏图号一致性检查
            //--------------------------------

            try
            {

                FileNameDrawingNumberReader fileReader =
                    new FileNameDrawingNumberReader();



                string fileDrawingNumber =
                    fileReader.ReadDrawingNumber(
                        filePath
                    )
                    ?? "";



                string titleDrawingNumber =
                    info.DrawingNumber
                    ?? "";



                if (!string.IsNullOrWhiteSpace(fileDrawingNumber)
                    &&
                    !string.IsNullOrWhiteSpace(titleDrawingNumber)
                    &&
                    !fileDrawingNumber.Equals(
                        titleDrawingNumber,
                        System.StringComparison.Ordinal
                    ))
                {

                    results.Add(
     new CheckResult
     {
         FilePath = filePath,

         FileName = fileName,

         LayoutName = info.LayoutName,

         Mark = "",

         Type = "标题栏图号检查",

         ObjectName = "图号",

         CurrentValue = titleDrawingNumber,

         ExpectedValue = fileDrawingNumber,

         Message =
             "标题栏图号与文件名图号不一致",

         IsError = true
     }
 );
                    if (drawMarker)
                    {
                        drawingNumberMarker.DrawMarker(
                            db,
                            layout.LayoutName,
                            info.IsHorizontal,
                            fileDrawingNumber
                        );
                    }

                }

            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "标题栏图号检查异常:"
                    +
                    ex.Message
                );
            }




            return results;

        }


    }

}