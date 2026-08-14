using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

using Correct_test1.QuickRevision.Forms;
using Correct_test1.QuickRevision.Models;
using Correct_test1.QuickRevision.Picking;
using Correct_test1.QuickRevision.Writers;

using Correct_test1.Readers;

using System.Windows.Forms;

namespace Correct_test1.QuickRevision.Services
{
    /// <summary>
    /// QuickRevision核心业务编排层。
    ///
    /// 普通目标：
    ///
    /// 识别目标
    /// ↓
    /// 输入新内容
    /// ↓
    /// 删除线
    /// ↓
    /// 新文字
    ///
    ///
    /// BOM中原内容NS开头：
    ///
    /// 识别目标
    /// ↓
    /// 从DWG文件名读取项目号
    /// ↓
    /// 输入新内容
    /// ↓
    /// 删除线
    /// ↓
    /// 新文字
    /// ↓
    /// BOM该行右侧生成项目号
    ///
    ///
    /// 所有新增CAD实体使用同一个Transaction。
    /// 任意一步失败均不Commit。
    /// </summary>
    public class QuickRevisionService
    {
        private readonly QuickRevisionPicker
            _picker;

        private readonly StrikeLineWriter
            _strikeLineWriter;

        private readonly ReplacementTextWriter
            _replacementTextWriter;

        private readonly ProjectNumberWriter
            _projectNumberWriter;


        public QuickRevisionService()
        {
            _picker =
                new QuickRevisionPicker();

            _strikeLineWriter =
                new StrikeLineWriter();

            _replacementTextWriter =
                new ReplacementTextWriter();

            _projectNumberWriter =
                new ProjectNumberWriter();
        }


        /// <summary>
        /// 启动一次快速划改。
        /// </summary>
        public bool Start()
        {
            //--------------------------------
            // 当前Document
            //--------------------------------

            Document document =
                Autodesk.AutoCAD
                    .ApplicationServices
                    .Application
                    .DocumentManager
                    .MdiActiveDocument;


            if (document == null)
                return false;


            Database database =
                document.Database;


            Editor editor =
                document.Editor;


            if (database == null ||
                editor == null)
            {
                return false;
            }


            //--------------------------------
            // QuickRevision只在Layout中使用
            //--------------------------------

            if (database.TileMode)
            {
                editor.WriteMessage(
                    "\n快速划改目前只支持在布局中使用。");

                return false;
            }


            //--------------------------------
            // 1. 用户选择划改对象
            //--------------------------------

            RevisionTarget target;


            try
            {
                target =
                    _picker.Pick(
                        document);
            }
            catch (System.Exception ex)
            {
                editor.WriteMessage(
                    "\n快速划改目标识别失败：{0}",
                    ex.Message);

                return false;
            }


            if (target == null)
                return false;


            if (!target.IsValid())
            {
                editor.WriteMessage(
                    "\n快速划改目标数据无效。");

                return false;
            }


            //--------------------------------
            // 2. 判断是否需要额外生成项目号
            //
            // 必须满足：
            //
            // TableCell
            // +
            // 原内容NS开头
            //--------------------------------

            bool shouldWriteProjectNumber =
                target.ShouldWriteProjectNumber;


            string projectNumber =
                "";


            //--------------------------------
            // 如果是NS BOM项，
            // 在真正修改图纸之前先读取文件名项目号。
            //--------------------------------

            if (shouldWriteProjectNumber)
            {
                projectNumber =
                    ReadProjectNumberFromDocument(
                        document);


                //--------------------------------
                // NS划改要求必须同时生成项目号。
                //
                // 如果文件名无法获得项目号，
                // 本次操作直接取消，
                // 避免出现划改完成但缺少项目号。
                //--------------------------------

                if (string.IsNullOrWhiteSpace(
                        projectNumber))
                {
                    MessageBox.Show(
                        "当前DWG文件名中没有找到符合规则的项目号。\n\n"
                        + "该BOM内容以NS开头，因此本次快速划改已取消。",
                        "快速划改",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return false;
                }
            }


            //--------------------------------
            // 3. 输入新内容
            //--------------------------------

            string replacementText;


            using (
                RevisionInputForm form =
                    new RevisionInputForm(
                        target.Text))
            {
                DialogResult dialogResult;


                try
                {
                    dialogResult =
                        Autodesk.AutoCAD
                            .ApplicationServices
                            .Application
                            .ShowModalDialog(
                                form);
                }
                catch (System.Exception ex)
                {
                    editor.WriteMessage(
                        "\n无法打开快速划改输入窗口：{0}",
                        ex.Message);

                    return false;
                }


                if (dialogResult !=
                    DialogResult.OK)
                {
                    return false;
                }


                replacementText =
                    form.ReplacementText;
            }


            if (string.IsNullOrWhiteSpace(
                    replacementText))
            {
                return false;
            }


            //--------------------------------
            // 4. 写入CAD
            //--------------------------------

            try
            {
                return WriteRevision(
                    document,
                    database,
                    editor,
                    target,
                    replacementText,
                    shouldWriteProjectNumber,
                    projectNumber);
            }
            catch (System.Exception ex)
            {
                editor.WriteMessage(
                    "\n快速划改写入失败：{0}",
                    ex.Message);

                return false;
            }
        }


        /// <summary>
        /// 从当前DWG文件名读取项目号。
        ///
        /// 直接复用已有FileNameProjectReader。
        ///
        /// 返回例如：
        ///
        /// N2607US004
        ///
        /// 不包含版本号。
        /// </summary>
        private static string ReadProjectNumberFromDocument(
            Document document)
        {
            if (document == null)
                return "";


            try
            {
                //--------------------------------
                // 优先使用Database.Filename。
                //
                // 正常已保存DWG这里是完整路径。
                //--------------------------------

                string filePath =
                    document.Database == null
                        ? ""
                        : document.Database.Filename;


                //--------------------------------
                // 个别情况下Filename为空，
                // 再使用Document.Name。
                //--------------------------------

                if (string.IsNullOrWhiteSpace(
                        filePath))
                {
                    filePath =
                        document.Name;
                }


                if (string.IsNullOrWhiteSpace(
                        filePath))
                {
                    return "";
                }


                FileNameProjectReader reader =
                    new FileNameProjectReader();


                FileNameProjectReader.ProjectInfo info =
                    reader.ReadProjectNumber(
                        filePath);


                if (info == null)
                    return "";


                if (string.IsNullOrWhiteSpace(
                        info.ProjectNumber))
                {
                    return "";
                }


                return
                    info.ProjectNumber.Trim();
            }
            catch (System.Exception)
            {
                return "";
            }
        }


        /// <summary>
        /// 真正执行数据库写入。
        ///
        /// 普通目标：
        ///
        /// 删除线
        /// +
        /// 新文字
        ///
        ///
        /// NS BOM目标：
        ///
        /// 删除线
        /// +
        /// 新文字
        /// +
        /// 行右侧项目号
        ///
        ///
        /// 全部位于同一个Transaction中。
        /// </summary>
        private bool WriteRevision(
            Document document,
            Database database,
            Editor editor,
            RevisionTarget target,
            string replacementText,
            bool shouldWriteProjectNumber,
            string projectNumber)
        {
            if (document == null ||
                database == null ||
                editor == null ||
                target == null)
            {
                return false;
            }


            using (
                DocumentLock documentLock =
                    document.LockDocument())
            {
                using (
                    Transaction transaction =
                        database
                            .TransactionManager
                            .StartTransaction())
                {
                    //--------------------------------
                    // 写入前再次确认原目标仍存在
                    //--------------------------------

                    if (!IsTargetStillValid(
                            database,
                            transaction,
                            target))
                    {
                        editor.WriteMessage(
                            "\n原划改对象已经失效，操作已取消。");

                        return false;
                    }


                    //--------------------------------
                    // NS目标还要再次检查TableContext
                    //--------------------------------

                    if (shouldWriteProjectNumber)
                    {
                        if (!target.IsTableCell ||
                            target.TableContext == null ||
                            !target.TableContext.IsValid())
                        {
                            editor.WriteMessage(
                                "\nBOM表格位置信息无效，操作已取消。");

                            return false;
                        }


                        if (string.IsNullOrWhiteSpace(
                                projectNumber))
                        {
                            editor.WriteMessage(
                                "\n项目号为空，操作已取消。");

                            return false;
                        }
                    }


                    //--------------------------------
                    // 1. 删除线
                    //--------------------------------

                    ObjectId strikeLineId =
                        _strikeLineWriter.Write(
                            database,
                            transaction,
                            target);


                    if (strikeLineId.IsNull ||
                        !strikeLineId.IsValid)
                    {
                        editor.WriteMessage(
                            "\n删除线创建失败，未修改图纸。");

                        return false;
                    }


                    //--------------------------------
                    // 2. 用户输入的新文字
                    //--------------------------------

                    ObjectId replacementTextId =
                        _replacementTextWriter.Write(
                            database,
                            transaction,
                            target,
                            replacementText);


                    if (replacementTextId.IsNull ||
                        !replacementTextId.IsValid)
                    {
                        editor.WriteMessage(
                            "\n新文字创建失败，未修改图纸。");

                        return false;
                    }


                    //--------------------------------
                    // 3. NS开头的BOM内容
                    //
                    // 在当前行BOM右边生成项目号。
                    //--------------------------------

                    if (shouldWriteProjectNumber)
                    {
                        ObjectId projectNumberId =
                            _projectNumberWriter.Write(
                                database,
                                transaction,
                                target,
                                projectNumber);


                        if (projectNumberId.IsNull ||
                            !projectNumberId.IsValid)
                        {
                            editor.WriteMessage(
                                "\n项目号创建失败，未修改图纸。");

                            //--------------------------------
                            // 不Commit。
                            //
                            // 删除线和替换文字也会一起回滚。
                            //--------------------------------

                            return false;
                        }
                    }


                    //--------------------------------
                    // 所有需要的对象全部成功
                    //--------------------------------

                    transaction.Commit();
                }
            }


            //--------------------------------
            // 刷新显示
            //--------------------------------

            try
            {
                editor.Regen();
            }
            catch (System.Exception)
            {
            }


            //--------------------------------
            // 命令行提示
            //--------------------------------

            if (shouldWriteProjectNumber)
            {
                editor.WriteMessage(
                    "\n快速划改完成：{0} → {1}，项目号：{2}",
                    target.Text,
                    replacementText,
                    projectNumber);
            }
            else
            {
                editor.WriteMessage(
                    "\n快速划改完成：{0} → {1}",
                    target.Text,
                    replacementText);
            }


            return true;
        }

        /// <summary>
        /// 连续快速划改模式。
        ///
        /// 进入后：
        ///
        /// 选择目标
        /// ↓
        /// 输入新值
        /// ↓
        /// 完成一次
        /// ↓
        /// 自动继续等待下一次选择
        ///
        /// Esc或关闭输入窗口退出。
        /// </summary>
        public void StartContinuous()
        {
            Document document =
                Autodesk.AutoCAD
                    .ApplicationServices
                    .Application
                    .DocumentManager
                    .MdiActiveDocument;


            if (document == null)
                return;


            Database database =
                document.Database;


            Editor editor =
                document.Editor;


            if (database == null ||
                editor == null)
            {
                return;
            }


            if (database.TileMode)
            {
                editor.WriteMessage(
                    "\n快速划改目前只支持在布局中使用。");

                return;
            }


            editor.WriteMessage(
                "\n================================");


            editor.WriteMessage(
                "\n已进入【连续快速划改模式】");


            editor.WriteMessage(
                "\n连续选择需要修改的位置。");


            editor.WriteMessage(
                "\n按 Esc 退出快速划改模式。");


            editor.WriteMessage(
                "\n================================");


            while (true)
            {
                bool shouldExit;


                try
                {
                    bool completed =
                        RunContinuousIteration(
                            document,
                            out shouldExit);


                    //--------------------------------
                    // 用户Esc / 关闭输入框
                    //--------------------------------

                    if (shouldExit)
                        break;


                    //--------------------------------
                    // completed=false但没有退出：
                    //
                    // 可能点空白
                    // 可能写入失败
                    // 可能NS项目号读取失败
                    //
                    // 继续下一次选择。
                    //--------------------------------
                }
                catch (System.Exception ex)
                {
                    editor.WriteMessage(
                        "\n快速划改发生错误：{0}",
                        ex.Message);


                    //--------------------------------
                    // 单次失败不让整个模式崩掉。
                    //--------------------------------
                }
            }


            editor.WriteMessage(
                "\n已退出连续快速划改模式。");
        }
        /// <summary>
        /// 执行连续模式中的一次划改。
        /// </summary>
        private bool RunContinuousIteration(
            Document document,
            out bool shouldExit)
        {
            shouldExit =
                false;


            if (document == null)
            {
                shouldExit =
                    true;

                return false;
            }


            Database database =
                document.Database;


            Editor editor =
                document.Editor;


            //--------------------------------
            // 1. 选择目标
            //--------------------------------

            bool cancelled;


            RevisionTarget target =
                _picker.Pick(
                    document,
                    out cancelled);


            //--------------------------------
            // Esc
            //--------------------------------

            if (cancelled)
            {
                shouldExit =
                    true;

                return false;
            }


            //--------------------------------
            // 点了空白/无法识别
            //
            // 不退出模式。
            //--------------------------------

            if (target == null ||
                !target.IsValid())
            {
                return false;
            }


            //--------------------------------
            // 2. 判断NS BOM项目号
            //--------------------------------

            bool shouldWriteProjectNumber =
                target.ShouldWriteProjectNumber;


            string projectNumber =
                "";


            if (shouldWriteProjectNumber)
            {
                projectNumber =
                    ReadProjectNumberFromDocument(
                        document);


                if (string.IsNullOrWhiteSpace(
                        projectNumber))
                {
                    MessageBox.Show(
                        "当前DWG文件名中没有找到符合规则的项目号。\n\n"
                        + "本次划改未执行，请继续选择其他位置。",
                        "快速划改",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);


                    return false;
                }
            }


            //--------------------------------
            // 3. 输入新内容
            //--------------------------------

            string replacementText;


            using (
                RevisionInputForm form =
                    new RevisionInputForm(
                        target.Text))
            {
                DialogResult result;


                try
                {
                    result =
                        Autodesk.AutoCAD
                            .ApplicationServices
                            .Application
                            .ShowModalDialog(
                                form);
                }
                catch (System.Exception ex)
                {
                    editor.WriteMessage(
                        "\n输入窗口打开失败：{0}",
                        ex.Message);

                    return false;
                }


                //--------------------------------
                // 点击取消、X关闭输入框
                //
                // 视为退出连续模式。
                //--------------------------------

                if (result !=
                    DialogResult.OK)
                {
                    shouldExit =
                        true;

                    return false;
                }


                replacementText =
                    form.ReplacementText;
            }


            if (string.IsNullOrWhiteSpace(
                    replacementText))
            {
                return false;
            }


            //--------------------------------
            // 4. 写入
            //--------------------------------

            bool success =
                WriteRevision(
                    document,
                    database,
                    editor,
                    target,
                    replacementText,
                    shouldWriteProjectNumber,
                    projectNumber);


            return success;
        }
        /// <summary>
        /// 真正写入前确认原对象和目标空间仍然有效。
        /// </summary>
        private static bool IsTargetStillValid(
            Database database,
            Transaction transaction,
            RevisionTarget target)
        {
            if (database == null ||
                transaction == null ||
                target == null)
            {
                return false;
            }


            if (!target.IsValid())
                return false;


            if (target.SourceId.IsNull ||
                !target.SourceId.IsValid)
            {
                return false;
            }


            if (target.TargetSpaceId.IsNull ||
                !target.TargetSpaceId.IsValid)
            {
                return false;
            }


            try
            {
                DBObject sourceObject =
                    transaction.GetObject(
                        target.SourceId,
                        OpenMode.ForRead,
                        false);


                if (sourceObject == null ||
                    sourceObject.IsErased)
                {
                    return false;
                }


                BlockTableRecord targetSpace =
                    transaction.GetObject(
                        target.TargetSpaceId,
                        OpenMode.ForRead,
                        false)
                    as BlockTableRecord;


                if (targetSpace == null)
                    return false;


                //--------------------------------
                // Table目标额外确认Table仍存在
                //--------------------------------

                if (target.IsTableCell &&
                    target.TableContext != null)
                {
                    DBObject tableObject =
                        transaction.GetObject(
                            target.TableContext.TableId,
                            OpenMode.ForRead,
                            false);


                    if (tableObject == null ||
                        tableObject.IsErased ||
                        !(tableObject is Table))
                    {
                        return false;
                    }
                }


                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
    }
}