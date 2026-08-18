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


        //==================================================
        // 快速划改
        //==================================================

        /// <summary>
        /// 进入连续快速划改模式。
        ///
        /// 点击一次按钮后：
        ///
        /// 选择目标
        /// → 输入
        /// → 完成
        /// → 自动继续选择下一处
        ///
        /// Esc退出。
        /// </summary>
        private void btnQuickRevision_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                Document doc =
                    GetActiveDocument();


                if (doc == null)
                {
                    MessageBox.Show(
                        "当前没有打开CAD图纸。",
                        "CAD检查助手");

                    return;
                }


                //--------------------------------
                // 已经处于快速划改模式
                //--------------------------------

                if (IsQuickRevisionRunning())
                {
                    MessageBox.Show(
                        "当前已经处于快速划改模式。\n\n"
                        + "请直接在图纸中继续选择需要划改的位置，"
                        + "按 Esc 退出模式。",
                        "CAD检查助手");

                    return;
                }


                //--------------------------------
                // 不随便取消用户正在执行的其他命令
                //--------------------------------

                if (IsAnyCadCommandRunning())
                {
                    MessageBox.Show(
                        "当前AutoCAD正在执行其他命令。\n\n"
                        + "请先结束当前命令，再进入快速划改模式。",
                        "CAD检查助手");

                    return;
                }


                //--------------------------------
                // 在AutoCAD命令上下文中
                // 正式启动连续划改模式
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
        /// 只清除QuickRevision生成内容。
        ///
        /// 不影响：
        /// CADCHECK_MARKER
        /// 检查标记
        /// 原尺寸
        /// 原BOM
        /// </summary>
        private void btnClearQuickRevision_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                Document doc =
                    GetActiveDocument();


                if (doc == null)
                {
                    MessageBox.Show(
                        "当前没有打开CAD图纸。",
                        "CAD检查助手");

                    return;
                }


                //--------------------------------
                // 如果此时正处于QREVMODE：
                //
                // 先退出连续划改，
                // 然后执行QREVCLEAR。
                //--------------------------------

                if (IsQuickRevisionRunning())
                {
                    doc.SendStringToExecute(
                        "\x03\x03QREVCLEAR ",
                        true,
                        false,
                        false);

                    return;
                }


                //--------------------------------
                // 不取消其他不相关CAD命令
                //--------------------------------

                if (IsAnyCadCommandRunning())
                {
                    MessageBox.Show(
                        "当前AutoCAD正在执行其他命令。\n\n"
                        + "请先结束当前命令，再清除划改。",
                        "CAD检查助手");

                    return;
                }


                //--------------------------------
                // 通过正式AutoCAD命令清除
                //--------------------------------

                doc.SendStringToExecute(
                    "QREVCLEAR ",
                    true,
                    false,
                    false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "清除快速划改失败");
            }
        }


        //==================================================
        // 原有单张检查
        //==================================================

        private void btnCheck_Click(
    object sender,
    EventArgs e)
        {
            try
            {
                Document doc =
                    GetActiveDocument();


                if (doc == null)
                {
                    MessageBox.Show(
                        "当前没有打开CAD图纸");

                    return;
                }


                //--------------------------------
                // report必须声明在using外面，
                // 因为检查完成后的提示也要使用它。
                //--------------------------------

                CheckReport report = null;


                using (
                    DocumentLock lockDoc =
                        doc.LockDocument())
                {
                    new RevisionMarker()
                        .ClearMarkers(
                            doc.Database);

                    new TitleBlockDrawingNumberMarker()
                        .ClearMarkers(
                            doc.Database);

                    new MarkerManager()
                        .ClearMarkers(
                            doc.Database);


                    CheckService checkService =
                        new CheckService();


                    //--------------------------------
                    // 执行所有检查
                    //
                    // 包括：
                    // 标准件
                    // BOM序号
                    // 非标归档
                    //--------------------------------

                    report =
                        checkService.Check(
                            doc.Database);


                    DrawingCheckManager manager =
                        new DrawingCheckManager();


                    manager.CheckDrawing(
                        doc.Database,
                        doc.Name,
                        true,
                        report.Boms);


                    MarkerManager markerManager =
                        new MarkerManager();


                    //--------------------------------
                    // 原有标准件标记
                    //--------------------------------

                    markerManager.CreateMarkers(
                        doc.Database,
                        report.Results);


                    //--------------------------------
                    // 新增：
                    // NS非标件归档缺失标记
                    //--------------------------------

                    markerManager
                        .CreateNonStandardArchiveMarkers(
                            doc.Database,
                            report.NonStandardArchiveResults);

                    //--------------------------------
                    // 新增：非标件号检查标记
                    //--------------------------------

                    markerManager
                        .CreateNonStandardPartNumberMarkers(
                            doc.Database,
                            report.NonStandardPartNumberResults);
                    //--------------------------------
                    // 新增：版本号最新版本提示
                    //--------------------------------

                    markerManager
                        .CreateVersionMarkers(
                            doc.Database,
                            report.VersionCheckResults);


                    //--------------------------------
                    // 原有：
                    // BOM有，但图中没有的序号
                    //--------------------------------

                    markerManager
                        .CreateMissingCalloutMarkers(
                            doc.Database,
                            report
                                .BomCalloutResult
                                .MissingIssues);


                    //--------------------------------
                    // 原有：
                    // 图中有，但BOM没有的序号
                    //--------------------------------

                    markerManager
                        .CreateExtraCalloutMarkers(
                            doc.Database,
                            report
                                .BomCalloutResult
                                .ExtraIssues);
                }


                //--------------------------------
                // 正常完成提示
                //--------------------------------

                string completeMessage =
                    "检查完成，详细问题已标注在图纸中。";


                //--------------------------------
                // Z盘不可用时：
                //
                // 不把NS件误报成“归档不存在”，
                // 但明确告诉用户这一项没有检查。
                //--------------------------------

                if (report != null &&
                    !report.NonStandardArchiveAvailable)
                {
                    completeMessage +=
                        "\n\n注意：非标件归档检查未执行。";


                    if (!string.IsNullOrWhiteSpace(
                            report.NonStandardArchiveError))
                    {
                        completeMessage +=
                            "\n"
                            + report.NonStandardArchiveError;
                    }
                }
                //--------------------------------
                // 版本归档不可用提示
                //--------------------------------

                if (report != null &&
                    !report.VersionArchiveAvailable)
                {
                    completeMessage +=
                        "\n\n注意：版本号最新版本检查未执行。";


                    if (!string.IsNullOrWhiteSpace(
                            report.VersionArchiveError))
                    {
                        completeMessage +=
                            "\n"
                            + report.VersionArchiveError;
                    }
                }


                MessageBox.Show(
                    completeMessage,
                    "CAD检查");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "程序错误");
            }
        }


        //==================================================
        // 原有清除检查标记
        //==================================================

        private void btnClear_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                Document doc =
                    GetActiveDocument();


                if (doc == null)
                {
                    MessageBox.Show(
                        "当前没有打开CAD图纸");

                    return;
                }


                using (
                    DocumentLock lockDoc =
                        doc.LockDocument())
                {
                    RevisionMarker revisionMarker =
                        new RevisionMarker();


                    revisionMarker.ClearMarkers(
                        doc.Database);


                    TitleBlockDrawingNumberMarker
                        titleBlockMarker =
                            new TitleBlockDrawingNumberMarker();


                    titleBlockMarker.ClearMarkers(
                        doc.Database);


                    MarkerManager markerManager =
                        new MarkerManager();


                    markerManager.ClearMarkers(
                        doc.Database);
                }


                MessageBox.Show(
                    "检查标记已清除",
                    "CAD检查助手");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "清除失败");
            }
        }


        //==================================================
        // 关闭
        //==================================================

        private void btnClose_Click(
            object sender,
            EventArgs e)
        {
            this.Close();
        }


        /// <summary>
        /// 无论点击“关闭”按钮还是右上角X，
        /// 如果当前正在QREVMODE，
        /// 都自动退出快速划改模式。
        /// </summary>
        private void SingleCheckForm_FormClosing(
            object sender,
            FormClosingEventArgs e)
        {
            try
            {
                Document doc =
                    GetActiveDocument();


                if (doc == null)
                    return;


                //--------------------------------
                // 只有QREVMODE正在运行时
                // 才发送取消。
                //
                // 不影响其他CAD命令。
                //--------------------------------

                if (IsQuickRevisionRunning())
                {
                    doc.SendStringToExecute(
                        "\x03\x03",
                        true,
                        false,
                        false);
                }
            }
            catch
            {
            }
        }


        //==================================================
        // AutoCAD状态辅助方法
        //==================================================

        private static Document GetActiveDocument()
        {
            try
            {
                return Autodesk.AutoCAD
                    .ApplicationServices
                    .Application
                    .DocumentManager
                    .MdiActiveDocument;
            }
            catch
            {
                return null;
            }
        }


        /// <summary>
        /// 获取当前AutoCAD正在执行的命令名。
        /// </summary>
        private static string GetCurrentCommandNames()
        {
            try
            {
                object value =
                    Autodesk.AutoCAD
                        .ApplicationServices
                        .Application
                        .GetSystemVariable(
                            "CMDNAMES");


                if (value == null)
                    return "";


                return
                    value.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }


        /// <summary>
        /// 当前是否正在运行连续快速划改。
        /// </summary>
        private static bool IsQuickRevisionRunning()
        {
            string commandNames =
                GetCurrentCommandNames();


            return
                commandNames.IndexOf(
                    "QREVMODE",
                    StringComparison.OrdinalIgnoreCase)
                >= 0;
        }


        /// <summary>
        /// 当前是否存在任意CAD命令。
        /// </summary>
        private static bool IsAnyCadCommandRunning()
        {
            return
                !string.IsNullOrWhiteSpace(
                    GetCurrentCommandNames());
        }
    }
}