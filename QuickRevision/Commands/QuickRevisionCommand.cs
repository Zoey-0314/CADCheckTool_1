using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

using Correct_test1.QuickRevision.Services;

namespace Correct_test1.QuickRevision.Commands
{
    public class QuickRevisionCommand
    {
        /// <summary>
        /// 单次快速划改。
        /// 保留作为测试命令。
        /// </summary>
        [CommandMethod(
            "QREV",
            CommandFlags.Modal)]
        public void StartQuickRevision()
        {
            Document document =
                Application.DocumentManager
                    .MdiActiveDocument;

            if (document == null)
                return;


            Editor editor =
                document.Editor;


            try
            {
                QuickRevisionService service =
                    new QuickRevisionService();


                service.Start();
            }
            catch (System.Exception ex)
            {
                editor.WriteMessage(
                    "\n快速划改执行失败：{0}",
                    ex.Message);
            }
        }


        /// <summary>
        /// 连续快速划改模式。
        ///
        /// 连续修改多个位置，
        /// Esc退出。
        /// </summary>
        [CommandMethod(
            "QREVMODE",
            CommandFlags.Modal)]
        public void StartContinuousQuickRevision()
        {
            Document document =
                Application.DocumentManager
                    .MdiActiveDocument;

            if (document == null)
                return;


            try
            {
                QuickRevisionService service =
                    new QuickRevisionService();


                service.StartContinuous();
            }
            catch (System.Exception ex)
            {
                document.Editor.WriteMessage(
                    "\n连续快速划改启动失败：{0}",
                    ex.Message);
            }
        }


        /// <summary>
        /// 只清除快速划改内容。
        /// </summary>
        [CommandMethod(
            "QREVCLEAR",
            CommandFlags.Modal)]
        public void ClearQuickRevision()
        {
            Document document =
                Application.DocumentManager
                    .MdiActiveDocument;

            if (document == null)
                return;


            try
            {
                QuickRevisionClearService service =
                    new QuickRevisionClearService();


                int count =
                    service.Clear(
                        document);


                document.Editor.WriteMessage(
                    "\n已清除快速划改内容，共 {0} 个对象。",
                    count);
            }
            catch (System.Exception ex)
            {
                document.Editor.WriteMessage(
                    "\n清除快速划改失败：{0}",
                    ex.Message);
            }
        }
    }
}