using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Correct_test1.Core;
using Correct_test1.Models;
using Correct_test1.Markers;

namespace Correct_test1
{
    public partial class SingleCheckForm : Form
    {
        public SingleCheckForm()
        {
            InitializeComponent();
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            try
            {
                Document doc = Autodesk.AutoCAD.ApplicationServices.Application
                    .DocumentManager
                    .MdiActiveDocument;

                if (doc == null)
                {
                    MessageBox.Show("当前没有打开CAD图纸");
                    return;
                }

                List<CheckResult> results;

                using (DocumentLock lockDoc = doc.LockDocument())
                {
                    DrawingCheckManager manager = new DrawingCheckManager();
                    results = manager.CheckDrawing(
                        doc.Database,
                        doc.Name,
                        true
                    );
                }

                if (results == null || results.Count == 0)
                {
                    MessageBox.Show("检查完成，没有发现问题", "CAD检查助手");
                    return;
                }

                string message = "";
                foreach (CheckResult result in results)
                {
                    message += "\n类型：" + result.Type + "\n对象：" + result.ObjectName + "\n问题：" + result.Message + "\n";
                }

                MessageBox.Show(message, "CAD检查结果");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "程序错误");
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            try
            {
                Document doc = Autodesk.AutoCAD.ApplicationServices.Application
                    .DocumentManager
                    .MdiActiveDocument;

                if (doc == null)
                {
                    MessageBox.Show("当前没有打开CAD图纸");
                    return;
                }

                using (DocumentLock lockDoc = doc.LockDocument())
                {
                    RevisionMarker marker = new RevisionMarker();
                    marker.ClearMarkers(doc.Database);
                }

                MessageBox.Show("检查标记已清除", "CAD检查助手");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "清除失败");
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
