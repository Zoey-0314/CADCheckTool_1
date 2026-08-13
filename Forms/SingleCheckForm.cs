using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Correct_test1.Checks;
using Correct_test1.Core;
using Correct_test1.Markers;
using Correct_test1.Models;

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
                        report.DrawingNumber,
                        report.DrawingNumberPosition
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
