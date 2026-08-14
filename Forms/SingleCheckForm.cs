using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Correct_test1.Checks;
using Correct_test1.Core;
using Correct_test1.Markers;
using Correct_test1.Models;
using Correct_test1.QuickRevision.Services;

namespace Correct_test1
{
    public partial class SingleCheckForm : Form
    {
        public SingleCheckForm()
        {
            InitializeComponent();
        }
        /// <summary>
        /// 进入连续快速划改模式。
        /// </summary>
        private void btnQuickRevision_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                Document doc =
                    Autodesk.AutoCAD
                        .ApplicationServices
                        .Application
                        .DocumentManager
                        .MdiActiveDocument;


                if (doc == null)
                {
                    MessageBox.Show(
                        "当前没有打开CAD图纸",
                        "CAD检查助手");

                    return;
                }


                //--------------------------------
                // 不直接从modeless WinForms里
                // 调用Editor.GetPoint。
                //
                // 让AutoCAD正式进入QREVMODE命令，
                // 这样连续选点运行在CAD命令上下文中，
                // 更稳定。
                //--------------------------------

                doc.SendStringToExecute(
                    "QREVMODE ",
                    true,
                    false,
                    false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "快速划改启动失败");
            }
        }
        /// <summary>
        /// 只清除快速划改内容。
        /// </summary>
        private void btnClearQuickRevision_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                Document doc =
                    Autodesk.AutoCAD
                        .ApplicationServices
                        .Application
                        .DocumentManager
                        .MdiActiveDocument;


                if (doc == null)
                {
                    MessageBox.Show(
                        "当前没有打开CAD图纸",
                        "CAD检查助手");

                    return;
                }


                QuickRevisionClearService service =
                    new QuickRevisionClearService();


                int count =
                    service.Clear(
                        doc);


                MessageBox.Show(
                    "快速划改内容已清除。\n\n"
                    + "清除对象数量："
                    + count,
                    "CAD检查助手");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "清除快速划改失败");
            }
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

                using (DocumentLock lockDoc = doc.LockDocument())
                {
                    CheckService checkService =
                        new CheckService();
                    CheckReport report =
                        checkService.Check(doc.Database);

                    DrawingCheckManager manager = new DrawingCheckManager();
                    manager.CheckDrawing(
                        doc.Database,
                        doc.Name,
                        true,
                        report.Boms
                    );

                    MarkerManager markerManager =
                        new MarkerManager();
                    markerManager.CreateMarkers(
                        doc.Database,
                        report.Results);
                    markerManager.CreateMissingCalloutMarkers(
                        doc.Database,
                        report.BomCalloutResult.MissingCallouts,
                        report.Boms);
                    markerManager.CreateExtraCalloutMarkers(
    doc.Database,
    report.BomCalloutResult.ExtraCallouts,
    report.DrawingTexts);
                }

                MessageBox.Show("检查完成，详细问题已标注在图纸中。", "CAD检查");
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

                    RevisionMarker revisionMarker =
                        new RevisionMarker();


                    revisionMarker.ClearMarkers(
                        doc.Database
                    );



                    TitleBlockDrawingNumberMarker titleBlockMarker =
                        new TitleBlockDrawingNumberMarker();


                    titleBlockMarker.ClearMarkers(
                        doc.Database
                    );

                    MarkerManager markerManager =
                        new MarkerManager();
                    markerManager.ClearMarkers(doc.Database);

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
