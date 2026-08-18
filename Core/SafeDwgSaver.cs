using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.IO;


namespace Correct_test1.Core
{

    /// <summary>
    /// 安全DWG保存器
    ///
    /// 功能:
    /// 1. 保存到临时文件
    /// 2. 验证文件有效性
    /// 3. 替换原DWG
    ///
    /// 避免:
    /// SaveAs异常导致原DWG变0KB
    ///
    /// </summary>
    public static class SafeDwgSaver
    {


        /// <summary>
        /// 安全保存DWG
        /// </summary>
        /// <param name="db">当前Database</param>
        /// <param name="originalFile">原文件路径</param>
        public static bool Save(
            Database db,
            string originalFile
        )
        {

            string tempFile =
                originalFile + ".tmp";


            string backupFile =
                originalFile + ".bak";


            try
            {

                AppLogger.Info(
                    $"开始安全保存:{originalFile}",
                    "SafeDwgSaver"
                );



                //--------------------------------
                // 删除旧临时文件
                //--------------------------------

                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }



                //--------------------------------
                // 保存临时DWG
                //--------------------------------

                AppLogger.Info(
                    $"保存临时文件:{tempFile}",
                    "SafeDwgSaver"
                );

                AppLogger.Info(
                    "SaveAs前: DatabaseDisposed=" + db.IsDisposed +
                    ", ActiveTransactions=" +
                    db.TransactionManager.NumberOfActiveTransactions +
                    ", OriginalExists=" + File.Exists(originalFile),
                    "SafeDwgSaver"
                );


                db.SaveAs(
                    tempFile,
                    true,
                    DwgVersion.Current,
                    db.SecurityParameters
                );

                FileInfo savedInfo = new FileInfo(tempFile);
                AppLogger.Info(
                    "SaveAs后: TempExists=" + File.Exists(tempFile) +
                    ", TempLength=" + savedInfo.Length,
                    "SafeDwgSaver"
                );



                //--------------------------------
                // 检查临时文件
                //--------------------------------

                ValidateFile(
                    tempFile
                );



                AppLogger.Info(
                    $"临时文件验证成功:{tempFile}",
                    "SafeDwgSaver"
                );



                //--------------------------------
                // 创建备份
                //--------------------------------

                if (File.Exists(originalFile))
                {

                    File.Copy(
                        originalFile,
                        backupFile,
                        true
                    );


                    AppLogger.Info(
                        $"创建备份:{backupFile}",
                        "SafeDwgSaver"
                    );

                }



                //--------------------------------
                // 替换原文件
                //--------------------------------

                File.Replace(
                    tempFile,
                    originalFile,
                    backupFile,
                    true
                );



                AppLogger.Info(
                    $"替换原文件成功:{originalFile}",
                    "SafeDwgSaver"
                );



                //--------------------------------
                // 删除临时文件
                //--------------------------------


                if (File.Exists(tempFile))
                {

                    File.Delete(
                        tempFile
                    );

                }



                AppLogger.Info(
                    "安全保存完成",
                    "SafeDwgSaver"
                );

                return true;


            }
            catch (Exception ex)
            {


                AppLogger.Error(
                    new Exception(
                        "SafeDwgSaver保存失败: " + ex.Message +
                        Environment.NewLine + ex.StackTrace,
                        ex),
                    "SafeDwgSaver"
                );



                // 删除失败临时文件

                try
                {

                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }

                }
                catch
                {

                }



                return false;

            }


        }




        /// <summary>
        /// 验证DWG文件
        /// </summary>
        private static void ValidateFile(
    string file)
        {
            if (!File.Exists(file))
            {
                throw new Exception(
                    "临时DWG不存在:" + file);
            }

            FileInfo info =
                new FileInfo(file);

            if (info.Length < 1024)
            {
                throw new Exception(
                    "DWG文件异常，大小过小:"
                    + info.Length
                    + " bytes");
            }

            Database verifyDatabase = null;

            try
            {
                verifyDatabase =
                    new Database(
                        false,
                        true);

                verifyDatabase.ReadDwgFile(
                    file,
                    FileOpenMode.OpenForReadAndAllShare,
                    false,
                    "");

                verifyDatabase.CloseInput(true);

                using (
                    Transaction tr =
                        verifyDatabase
                            .TransactionManager
                            .StartTransaction())
                {
                    BlockTable blockTable =
                        tr.GetObject(
                            verifyDatabase.BlockTableId,
                            OpenMode.ForRead)
                        as BlockTable;

                    if (blockTable == null)
                    {
                        throw new Exception(
                            "无法读取DWG块表");
                    }

                    DBDictionary layouts =
                        tr.GetObject(
                            verifyDatabase.LayoutDictionaryId,
                            OpenMode.ForRead)
                        as DBDictionary;

                    if (layouts == null)
                    {
                        throw new Exception(
                            "无法读取DWG布局信息");
                    }

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "临时DWG重新打开验证失败: "
                    + ex.Message,
                    ex);
            }
            finally
            {
                if (verifyDatabase != null)
                {
                    try
                    {
                        verifyDatabase.Dispose();
                    }
                    catch
                    {
                    }
                }
            }
        }


    }

}