using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

using Correct_test1.Checks;
using Correct_test1.Models;
using Correct_test1.Readers;
using Correct_test1.Markers;

using System;
using System.Collections.Generic;
using System.Windows.Forms;


namespace Correcet_test1
{

    public partial class CheckForm : Form
    {


        public CheckForm()
        {
            InitializeComponent();
        }





        private void button1_Click(
            object sender,
            EventArgs e)
        {

            try
            {

                Document doc =
                    Autodesk.AutoCAD.ApplicationServices.Application
                    .DocumentManager
                    .MdiActiveDocument;



                if (doc == null)
                {
                    MessageBox.Show(
                        "当前没有打开CAD图纸"
                    );

                    return;
                }



                using (DocumentLock lockDoc =
                    doc.LockDocument())
                {


                    Database db =
                        doc.Database;


                    Editor ed =
                        doc.Editor;



                    string projectMessage =
                        "";



                    //--------------------------------------
                    // 1 项目号检查
                    //--------------------------------------


                    ProjectReader projectReader =
                        new ProjectReader();



                    List<string> projects =
                        projectReader.ReadProjects(
                            db,
                            ed
                        );



                    if (projects.Count == 0)
                    {

                        projectMessage =
                            "未找到项目号";

                    }
                    else
                    {


                        FileNameProjectReader fileReader =
                            new FileNameProjectReader();



                        FileNameProjectReader.ProjectInfo info =
                            fileReader.ReadProjectNumber(
                                doc.Name
                            );



                        if (info == null)
                        {

                            projectMessage =
                                "文件名无项目号";

                        }
                        else
                        {

                            ProjectChecker checker =
                                new ProjectChecker();



                            CheckResult check =
                                checker.CheckProject(
                                    projects[0],
                                    info.ProjectNumber
                                );


                            projectMessage =
                                check.Message;

                        }

                    }





                    //--------------------------------------
                    // 2 修改记录检查
                    //--------------------------------------


                    LayoutReader layoutReader =
                        new LayoutReader();



                    List<LayoutInfo> layouts =
                        layoutReader.ReadLayouts(
                            db,
                            ed
                        );



                    RevisionTableReader revisionReader =
                        new RevisionTableReader();



                    RevisionChecker revisionChecker =
                        new RevisionChecker();



                    RevisionLocationReader locationReader =
                        new RevisionLocationReader();



                    RevisionIssueMapper mapper =
                        new RevisionIssueMapper();



                    RevisionMarker marker =
                        new RevisionMarker();



                    int markerCount = 0;




                    foreach (LayoutInfo layout in layouts)
                    {


                        //跳过模型空间

                        if (layout.IsModelSpace)
                            continue;



                        List<TitleText> texts =
                            revisionReader.ReadAllTexts(
                                db,
                                layout.BlockTableRecordId
                            );



                        if (texts.Count == 0)
                            continue;




                        //----------------------------------
                        // 横版
                        //----------------------------------

                        List<RevisionInfo> revisions =
                            revisionReader.ReadHorizontal(
                                db,
                                layout.BlockTableRecordId
                            );



                        List<RevisionLocation> locations =
                            locationReader
                            .ReadHorizontalLocations(
                                layout.LayoutName,
                                texts
                            );





                        //----------------------------------
                        // 如果横版没有结果，尝试竖版
                        //----------------------------------

                        if (revisions.Count == 0)
                        {

                            revisions =
                                revisionReader.ReadVertical(
                                    db,
                                    layout.BlockTableRecordId
                                );



                            locations =
                                locationReader
                                .ReadVerticalLocations(
                                    layout.LayoutName,
                                    texts
                                );

                        }




                        if (revisions.Count == 0)
                            continue;




                        List<RevisionCheckIssue> issues =
                            revisionChecker.Check(
                                layout.LayoutName,
                                revisions
                            );



                        if (issues.Count == 0)
                            continue;




                        List<RevisionMarkPoint> points =
                            mapper.Map(
                                issues,
                                locations
                            );



                        marker.DrawMarkers(
                            db,
                            layout.BlockTableRecordId,
                            points
                        );



                        markerCount +=
                            points.Count;


                    }





                    MessageBox.Show(
                        "项目号检查："
                        +
                        projectMessage
                        +
                        "\n\n"
                        +
                        "绿色检查框数量："
                        +
                        markerCount,

                        "CAD检查助手"
                    );



                }

            }
            catch (Exception ex)
            {

                MessageBox.Show(
                    ex.Message,
                    "程序错误"
                );

            }


        }





        private void btnClearMarker_Click(
    object sender,
    EventArgs e)
        {

            try
            {

                Document doc =
                    Autodesk.AutoCAD.ApplicationServices.Application
                    .DocumentManager
                    .MdiActiveDocument;



                if (doc == null)
                {
                    MessageBox.Show(
                        "当前没有打开CAD图纸"
                    );

                    return;
                }



                using (DocumentLock lockDoc =
                    doc.LockDocument())
                {


                    RevisionMarker marker =
                        new RevisionMarker();



                    marker.ClearMarkers(
                        doc.Database
                    );


                }



                MessageBox.Show(
                    "检查标记已清除",
                    "CAD检查助手"
                );


            }
            catch (Exception ex)
            {

                MessageBox.Show(
                    ex.Message,
                    "清除失败"
                );

            }

        }


    }

}