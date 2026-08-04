using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.Core;
using Correct_test1.Models;
using Correct_test1.Markers;

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Correct_test1.Batch;
using System.IO;
using Correct_test1.Export;


namespace Correct_test1
{

    public partial class CheckForm : Form
    {
        private void btnBatchCheck_Click(
     object sender,
     EventArgs e)
        {

            try
            {

                //选择文件夹

                FolderBrowserDialog dialog =
                    new FolderBrowserDialog();


                dialog.Description =
                    "请选择需要批量检查的DWG文件夹";



                if (dialog.ShowDialog()
                    != DialogResult.OK)
                {
                    return;
                }



                string folder =
                    dialog.SelectedPath;



                //执行批量检查

                BatchCheckerManager manager =
                    new BatchCheckerManager();



                List<CheckResult> results =
                    manager.CheckFolder(
                        folder
                    );



                //生成CSV

                BatchCsvExporter exporter =
                    new BatchCsvExporter();



                string csvPath =
                    exporter.Export(
                        results,
                        folder
                    );



                //显示结果

                DialogResult result =
     MessageBox.Show(
         "批量检查完成\n\n"
         +
         "发现问题数量："
         +
         results.Count
         +
         "\n\n"
         +
         "是否打开检查报告？",

         "CAD检查助手",

         MessageBoxButtons.YesNo,

         MessageBoxIcon.Information
     );



                if (result == DialogResult.Yes)
                {

                    try
                    {

                        System.Diagnostics.Process.Start(
                            csvPath
                        );

                    }
                    catch (Exception ex)
                    {

                        MessageBox.Show(
                            "打开报告失败：\n"
                            +
                            ex.Message,
                            "CAD检查助手"
                        );

                    }

                }


            }
            catch (Exception ex)
            {

                MessageBox.Show(
                    ex.Message,
                    "批量检查失败"
                );

            }

        }

        public CheckForm()
        {
            InitializeComponent();
        }





        /// <summary>
        /// 检查当前图纸
        /// </summary>
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





                List<CheckResult> results;



                using (DocumentLock lockDoc =
                    doc.LockDocument())
                {


                    DrawingCheckManager manager =
                        new DrawingCheckManager();



                    results =
                        manager.CheckDrawing(
                            doc.Database,
                            doc.Name,
                            true
                        );


                }





                if (results.Count == 0)
                {

                    MessageBox.Show(
                        "检查完成，没有发现问题",
                        "CAD检查助手"
                    );

                    return;

                }




                string message =
                    "";



                foreach (CheckResult result in results)
                {

                    message +=
                        "\n类型："
                        +
                        result.Type
                        +
                        "\n对象："
                        +
                        result.ObjectName
                        +
                        "\n问题："
                        +
                        result.Message
                        +
                        "\n";


                }




                MessageBox.Show(
                    message,
                    "CAD检查结果"
                );



            }
            catch (Exception ex)
            {

                MessageBox.Show(
                    ex.Message,
                    "程序错误"
                );

            }


        }








        /// <summary>
        /// 清除绿色检查框
        /// </summary>
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