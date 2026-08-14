using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

using Correct_test1.QuickRevision.Models;
using Correct_test1.QuickRevision.Picking;

namespace Correct_test1.QuickRevision.Commands
{
    /// <summary>
    /// QuickRevision完整识别链测试命令。
    ///
    /// 测试目标：
    ///
    /// 1. 用户始终待在Layout / Paper Space
    /// 2. 不需要双击进入Viewport
    /// 3. 可以识别Paper Space中的BOM / DBText / MText
    /// 4. 可以识别Viewport中的Dimension / DBText / MText
    /// 5. 最终统一得到RevisionTarget
    ///
    /// 本命令只用于开发测试。
    /// 不创建任何CAD实体。
    /// </summary>
    public class QuickRevisionPickTestCommand
    {
        [CommandMethod("QREVPICKTEST")]
        public void TestPick()
        {
            //--------------------------------
            // 获取当前Document
            //--------------------------------

            Document document =
                Application.DocumentManager
                    .MdiActiveDocument;

            if (document == null)
                return;


            Editor editor =
                document.Editor;


            //--------------------------------
            // 创建Picker
            //--------------------------------

            QuickRevisionPicker picker =
                new QuickRevisionPicker();


            //--------------------------------
            // 执行完整识别流程
            //--------------------------------

            RevisionTarget target;

            try
            {
                target =
                    picker.Pick(
                        document);
            }
            catch (System.Exception ex)
            {
                editor.WriteMessage(
                    "\n\nQREVPICKTEST发生错误：");

                editor.WriteMessage(
                    "\n" +
                    ex.Message);

                return;
            }


            //--------------------------------
            // 用户取消或没有识别到
            //--------------------------------

            if (target == null)
            {
                editor.WriteMessage(
                    "\nQREV：没有获得有效目标。");

                return;
            }


            //--------------------------------
            // 输出完整RevisionTarget
            //--------------------------------

            editor.WriteMessage(
                "\n\n================================");

            editor.WriteMessage(
                "\nQuickRevision 识别结果");

            editor.WriteMessage(
                "\n================================");


            //--------------------------------
            // 基本信息
            //--------------------------------

            editor.WriteMessage(
                "\nSourceType：{0}",
                target.SourceType ?? "");


            editor.WriteMessage(
                "\nText：{0}",
                target.Text ?? "");


            editor.WriteMessage(
                "\nSourceId：{0}",
                target.SourceId);


            //--------------------------------
            // 所在空间
            //--------------------------------

            editor.WriteMessage(
                "\nIsInViewport：{0}",
                target.IsInViewport);


            editor.WriteMessage(
                "\nTargetSpaceId：{0}",
                target.TargetSpaceId);


            if (target.IsInViewport)
            {
                editor.WriteMessage(
                    "\nViewportId：{0}",
                    target.ViewportId);
            }


            //--------------------------------
            // 几何范围
            //--------------------------------

            editor.WriteMessage(
                "\n");

            editor.WriteMessage(
                "\nLeftX：{0:0.###}",
                target.LeftX);


            editor.WriteMessage(
                "\nRightX：{0:0.###}",
                target.RightX);


            editor.WriteMessage(
                "\nBottomY：{0:0.###}",
                target.BottomY);


            editor.WriteMessage(
                "\nTopY：{0:0.###}",
                target.TopY);


            editor.WriteMessage(
                "\nCenterY：{0:0.###}",
                target.CenterY);


            //--------------------------------
            // 文字尺寸
            //--------------------------------

            editor.WriteMessage(
                "\n");

            editor.WriteMessage(
                "\nTextWidth：{0:0.###}",
                target.TextWidth);


            editor.WriteMessage(
                "\nTextHeight：{0:0.###}",
                target.TextHeight);


            //--------------------------------
            // 文字样式
            //--------------------------------

            if (!target.TextStyleId.IsNull &&
                target.TextStyleId.IsValid)
            {
                editor.WriteMessage(
                    "\nTextStyleId：{0}",
                    target.TextStyleId);
            }
            else
            {
                editor.WriteMessage(
                    "\nTextStyleId：未指定");
            }


            //--------------------------------
            // 最终有效性
            //--------------------------------

            editor.WriteMessage(
                "\n");

            editor.WriteMessage(
                "\nIsValid：{0}",
                target.IsValid());


            editor.WriteMessage(
                "\n================================\n");
        }
    }
}