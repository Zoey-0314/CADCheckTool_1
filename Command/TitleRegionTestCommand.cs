using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

using Correct_test1.Models;
using Correct_test1.Readers;

using System;
using System.Collections.Generic;
using System.Linq;


namespace Correct_test1.Command
{

    /// <summary>
    /// 标题栏区域解析测试
    ///
    /// 命令:
    /// TESTTITLE2
    ///
    /// 测试:
    /// 1. 横竖版判断
    /// 2. 标题栏区域解析
    /// </summary>
    public class TitleRegionTestCommand
    {


        [Autodesk.AutoCAD.Runtime.CommandMethod("TESTTITLE2")]
        public void TestTitleRegion()
        {

            Document doc =
                Application.DocumentManager
                .MdiActiveDocument;


            if (doc == null)
                return;



            Editor ed =
                doc.Editor;


            Database db =
                doc.Database;



            try
            {

                ed.WriteMessage(
                    "\n======开始标题栏测试======"
                );



                //==========================
                // 读取布局
                //==========================

                LayoutReader layoutReader =
                    new LayoutReader();



                List<LayoutInfo> layouts =
                    layoutReader.ReadLayouts(
                        db,
                        ed
                    );



                //==========================
                // 读取标题栏文字
                //==========================

                TitleBlockReader reader =
                    new TitleBlockReader();



                List<TitleText> allTexts =
                    reader.Read(
                        db,
                        layouts
                    );



                ed.WriteMessage(
                    "\n总文字数量:"
                    +
                    allTexts.Count
                );



                //==========================
                // 按布局测试
                //==========================

                foreach (LayoutInfo layout in layouts)
                {


                    if (layout.IsModelSpace)
                        continue;



                    ed.WriteMessage(
                        "\n\n--------------------"
                    );


                    ed.WriteMessage(
                        "\n当前布局:"
                        +
                        layout.LayoutName
                    );



                    List<TitleText> texts =
                        allTexts
                        .Where(x =>
                            x.LayoutName ==
                            layout.LayoutName)
                        .ToList();



                    ed.WriteMessage(
                        "\n当前布局文字数量:"
                        +
                        texts.Count
                    );



                    //==========================
                    // 原有横竖版判断
                    //==========================

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



                    ed.WriteMessage(
                        "\n标记数量:"
                        +
                        markCount
                    );



                    ed.WriteMessage(
                        "\n方向:"
                        +
                        (
                            isHorizontal
                            ?
                            "Horizontal 横版"
                            :
                            "Vertical 竖版"
                        )
                    );



                    //==========================
                    // 标题栏区域解析
                    //==========================

                    TitleBlockRegionParser parser =
                        new TitleBlockRegionParser();



                    DrawingInfo info =
                        parser.Parse(
                            texts,
                            isHorizontal
                        );



                    info.IsHorizontal =
                        isHorizontal;



                    //==========================
                    // 输出结果
                    //==========================


                    ed.WriteMessage(
                        "\n======标题栏结果======"
                    );


                    ed.WriteMessage(
                        "\n图纸名称:"
                        +
                        info.DrawingName
                    );


                    ed.WriteMessage(
                        "\n图号:"
                        +
                        info.DrawingNumber
                    );


                    ed.WriteMessage(
                        "\n材料:"
                        +
                        info.Material
                    );


                    ed.WriteMessage(
                        "\n规格:"
                        +
                        info.Specification
                    );


                    ed.WriteMessage(
                        "\n表面处理:"
                        +
                        info.SurfaceTreatment
                    );


                    ed.WriteMessage(
                        "\n制图:"
                        +
                        info.Designer
                    );


                    ed.WriteMessage(
                        "\n校对:"
                        +
                        info.Checker
                    );


                    ed.WriteMessage(
                        "\n标审:"
                        +
                        info.Reviewer
                    );


                    ed.WriteMessage(
                        "\n批准:"
                        +
                        info.Approver
                    );


                    ed.WriteMessage(
                        "\n日期:"
                        +
                        info.TitleDate
                    );


                    ed.WriteMessage(
                        "\n页码:"
                        +
                        info.PageNumber
                    );


                    ed.WriteMessage(
                        "\n===================="
                    );

                }



                ed.WriteMessage(
                    "\n======测试结束======"
                );


            }
            catch (Exception ex)
            {

                ed.WriteMessage(
                    "\n测试失败:"
                    +
                    ex.Message
                );

            }


        }


    }

}