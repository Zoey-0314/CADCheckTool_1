using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Correct_test1.Checks;
using Correct_test1.Models;
using Correct_test1.Readers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Correcet_test1
{
    public partial class CheckForm : Form
    {
        public CheckForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {

                // 获取当前CAD文档

                Document doc =
                    Autodesk.AutoCAD.ApplicationServices.Application
                    .DocumentManager
                    .MdiActiveDocument;


                if (doc == null)
                {

                    MessageBox.Show(
                        "当前没有打开CAD图纸",
                        "CAD检查助手"
                    );

                    return;

                }



                Database db =
                    doc.Database;


                Editor ed =
                    doc.Editor;



                // 创建项目号读取器

                ProjectReader reader =
                    new ProjectReader();



                // 读取当前图纸项目号

                List<string> projects =
                    reader.ReadProjects(
                        db,
                        ed
                    );



                if (projects.Count == 0)
                {
                    MessageBox.Show(
                        "图纸内部没有找到项目号"
                    );

                    return;
                }


                string drawingProject =
                    projects[0];


                // 创建项目号检查器

                ProjectChecker checker =
                    new ProjectChecker();



                // 检查项目号
                FileNameProjectReader fileReader =
                    new FileNameProjectReader();



                FileNameProjectReader.ProjectInfo projectInfo =
                    fileReader.ReadProjectNumber(
                        doc.Name
                    );


                if (projectInfo == null)
                {

                    MessageBox.Show(
                        "文件名中没有找到项目号"
                    );

                    return;

                }



                CheckResult result =
                    checker.CheckProject(
                        drawingProject,
                        projectInfo.ProjectNumber
                    );



                // 显示结果

                MessageBox.Show(
                    "当前项目号："
                    + result.CurrentValue
                    + "\n\n"
                    + "标准项目号："
                    + result.ExpectedValue
                    + "\n\n"
                    + "检查结果："
                    + result.Message,

                    "CAD检查助手"
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
    }
}
