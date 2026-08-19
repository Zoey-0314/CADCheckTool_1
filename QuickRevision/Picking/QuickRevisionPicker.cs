using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using Correct_test1.QuickRevision.Models;
using Correct_test1.QuickRevision.Resolvers;

namespace Correct_test1.QuickRevision.Picking
{
    /// <summary>
    /// QuickRevision用户点击入口。
    ///
    /// 负责：
    ///
    /// 1. 保证处于Paper Space
    /// 2. 获取用户点击位置
    /// 3. 调用RevisionTargetResolver
    /// 4. 返回RevisionTarget
    ///
    /// 不负责：
    /// 绘制
    /// 输入框
    /// Transaction写入
    /// </summary>
    public class QuickRevisionPicker
    {
        private readonly RevisionTargetResolver
            _resolver;


        public QuickRevisionPicker()
        {
            _resolver =
                new RevisionTargetResolver();
        }


        /// <summary>
        /// 兼容原来的单次调用。
        /// </summary>
        public RevisionTarget Pick(
            Document document)
        {
            bool cancelled;

            return Pick(
                document,
                out cancelled);
        }


        /// <summary>
        /// 连续模式使用。
        ///
        /// cancelled = true：
        /// 用户按Esc/取消，应退出连续模式。
        ///
        /// cancelled = false + target=null：
        /// 只是没有识别到对象，连续模式继续。
        /// </summary>
        public RevisionTarget Pick(
            Document document,
            out bool cancelled)
        {
            cancelled =
                false;


            if (document == null)
            {
                cancelled =
                    true;

                return null;
            }


            Database database =
                document.Database;


            Editor editor =
                document.Editor;


            if (database == null ||
                editor == null)
            {
                cancelled =
                    true;

                return null;
            }


            //--------------------------------
            // 只支持Layout
            //--------------------------------

            if (database.TileMode)
            {
                editor.WriteMessage(
                    "\n快速划改目前只支持在布局中使用。");


                cancelled =
                    true;

                return null;
            }


            //--------------------------------
            // 如果用户当前处在Viewport内部，
            // 自动退回Paper Space。
            //--------------------------------

            if (!EnsurePaperSpace(
                    editor))
            {
                editor.WriteMessage(
                    "\n无法切换到布局空间。");


                cancelled =
                    true;

                return null;
            }


            //--------------------------------
            // 获取点击点
            //--------------------------------

            PromptPointOptions options =
                new PromptPointOptions(
                    "\n请选择需要划改的文字、尺寸或BOM内容 <Esc退出>：");


            PromptPointResult result =
                editor.GetPoint(
                    options);


            //--------------------------------
            // Esc / Cancel
            //--------------------------------

            if (result.Status ==
                    PromptStatus.Cancel ||
                result.Status ==
                    PromptStatus.Error ||
                result.Status ==
                    PromptStatus.None)
            {
                cancelled =
                    true;

                return null;
            }


            if (result.Status !=
                PromptStatus.OK)
            {
                cancelled =
                    true;

                return null;
            }


            //--------------------------------
            // 转WCS
            //--------------------------------

            Point3d paperPoint;


            try
            {
                paperPoint =
                    result.Value.TransformBy(
                        editor.CurrentUserCoordinateSystem);
            }
            catch (System.Exception)
            {
                paperPoint =
                    result.Value;
            }


            //--------------------------------
            // Resolver只读
            //--------------------------------

            using (
                Transaction transaction =
                    database
                        .TransactionManager
                        .StartTransaction())
            {
                RevisionTarget target =
                    _resolver.Resolve(
                        database,
                        transaction,
                        paperPoint);


                transaction.Commit();


                //--------------------------------
                // 点到空白/不支持对象
                //
                // 注意：
                // 这里cancelled仍然是false。
                //--------------------------------

                if (target == null)
                {
                    editor.WriteMessage(
                        "\n当前位置没有识别到可划改对象，请继续选择。");

                    return null;
                }


                if (!target.IsValid())
                {
                    editor.WriteMessage(
                        "\n识别到对象，但目标数据无效，请继续选择。");

                    return null;
                }


                return target;
            }
        }


        private static bool EnsurePaperSpace(
            Editor editor)
        {
            if (editor == null)
                return false;


            try
            {
                object value =
                    Autodesk.AutoCAD
                        .ApplicationServices
                        .Application
                        .GetSystemVariable(
                            "CVPORT");


                int cvport =
                    System.Convert.ToInt32(
                        value);


                if (cvport == 1)
                    return true;


                editor.SwitchToPaperSpace();


                value =
                    Autodesk.AutoCAD
                        .ApplicationServices
                        .Application
                        .GetSystemVariable(
                            "CVPORT");


                cvport =
                    System.Convert.ToInt32(
                        value);


                return
                    cvport == 1;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
    }
}