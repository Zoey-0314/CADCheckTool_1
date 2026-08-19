using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.Batch;
using Correct_test1.Core;
using Correct_test1.ProjectVersion.Models;

using System;
using System.Collections.Generic;
using System.IO;

namespace Correct_test1.ProjectVersion.Services
{
    /// <summary>
    /// 文件夹批量写入项目号+版本号。
    /// 递归处理所有DWG。
    /// 底层写入继续复用：
    /// ProjectVersionWriteService
    /// 保存继续复用：
    /// SafeDwgSaver
    /// </summary>
    public class BatchProjectVersionService
    {
        public List<BatchProjectVersionResult>
            WriteFolder(
                string folderPath,
                string value,
                Action<int, int, string> progress)
        {
            List<BatchProjectVersionResult>
                results =
                    new List<BatchProjectVersionResult>();


            if (string.IsNullOrWhiteSpace(
                    folderPath) ||
                !Directory.Exists(
                    folderPath))
            {
                return results;
            }


            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return results;
            }


            // 与现有批量检查一样：
            // 递归所有子文件夹。

            string[] files =
                Directory.GetFiles(
                    folderPath,
                    "*.dwg",
                    SearchOption.AllDirectories);


            if (files.Length == 0)
                return results;


            // 直接复用现有批量检查的
            // 宿主Document逻辑。

            Document hostDocument =
                BatchCheckerManager
                    .EnsureHostDocument();


            if (hostDocument == null ||
                hostDocument.Database == null ||
                hostDocument.Database.IsDisposed)
            {
                throw new InvalidOperationException(
                    "无法获得有效的AutoCAD宿主文档。");
            }


            Database hostDatabase =
                hostDocument.Database;


            // 稳定WorkingDatabase

            HostApplicationServices
                .WorkingDatabase =
                    hostDatabase;


            ProjectVersionWriteService
                writeService =
                    new ProjectVersionWriteService();


            for (
                int fileIndex = 0;
                fileIndex < files.Length;
                fileIndex++)
            {
                string file =
                    files[fileIndex];


                Database db =
                    null;


                BatchProjectVersionResult
                    fileResult =
                        new BatchProjectVersionResult
                        {
                            FilePath =
                                file,

                            FileName =
                                Path.GetFileName(
                                    file)
                        };


                try
                {
                    // 宿主Database必须一直有效

                    if (hostDatabase.IsDisposed)
                    {
                        throw new InvalidOperationException(
                            "AutoCAD宿主Database已经失效。");
                    }


                    HostApplicationServices
                        .WorkingDatabase =
                            hostDatabase;


                    // 建立后台Database

                    db =
                        new Database(
                            false,
                            true);


                    // 读取DWG

                    db.ReadDwgFile(
                        file,
                        FileOpenMode
                            .OpenForReadAndAllShare,
                        false,
                        "");


                    // 切换WorkingDatabase

                    HostApplicationServices
                        .WorkingDatabase =
                            db;


                    // 关闭输入流

                    db.CloseInput(
                        true);


                    // 关键：
                    //
                    // 直接调用已经通过单张测试的
                    // WriteAllLayouts。
                    //
                    // 不重新写横竖判断、
                    // 不重新写MText逻辑。

                    List<ProjectVersionLayoutResult>
                        layoutResults =
                            writeService
                                .WriteAllLayouts(
                                    db,
                                    value);


                    // 汇总当前DWG

                    if (layoutResults != null)
                    {
                        foreach (
                            ProjectVersionLayoutResult
                            layoutResult
                            in layoutResults)
                        {
                            if (layoutResult == null)
                                continue;


                            if (layoutResult.Skipped)
                            {
                                fileResult
                                    .SkippedCount++;

                                continue;
                            }


                            if (!layoutResult.Success)
                            {
                                fileResult
                                    .FailedLayoutCount++;

                                continue;
                            }


                            if (layoutResult.Created)
                            {
                                fileResult
                                    .CreatedCount++;
                            }
                            else
                            {
                                fileResult
                                    .ModifiedCount++;
                            }
                        }
                    }


                    int successLayoutCount =
                        fileResult.ModifiedCount +
                        fileResult.CreatedCount;


                    // 有实际修改才保存。
                    //
                    // 如果整张DWG没有任何符合布局，
                    // 不重新写文件。

                    if (successLayoutCount > 0)
                    {
                        AppLogger.Info(
                            "准备保存批量版本号文件："
                            + fileResult.FileName,
                            "BatchProjectVersionService");


                        bool saved =
                            SafeDwgSaver.Save(
                                db,
                                file);


                        fileResult.Saved =
                            saved;


                        fileResult.Success =
                            saved;


                        if (saved)
                        {
                            fileResult.Message =
                                "版本号写入并保存成功。";
                        }
                        else
                        {
                            fileResult.Message =
                                "版本号已写入内存，但DWG安全保存失败。";
                        }
                    }
                    else
                    {
                        // 没有任何布局被修改。

                        fileResult.Success =
                            fileResult
                                .FailedLayoutCount == 0;


                        fileResult.Saved =
                            false;


                        if (fileResult
                            .FailedLayoutCount > 0)
                        {
                            fileResult.Message =
                                "没有成功写入的布局。";
                        }
                        else
                        {
                            fileResult.Message =
                                "没有需要处理的纸空间布局，未修改文件。";
                        }
                    }
                }
                catch (Exception ex)
                {
                    fileResult.Success =
                        false;


                    fileResult.Saved =
                        false;


                    fileResult.Message =
                        ex.Message;


                    AppLogger.Error(
                        ex,
                        "BatchProjectVersionService",
                        file);
                }
                finally
                {
                    // 和现有BatchCheckerManager一样：
                    //
                    // 一定先恢复宿主WorkingDatabase，
                    // 再Dispose后台Database。

                    try
                    {
                        if (hostDatabase != null &&
                            !hostDatabase.IsDisposed)
                        {
                            HostApplicationServices
                                .WorkingDatabase =
                                    hostDatabase;
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error(
                            ex,
                            "BatchProjectVersionService.Restore",
                            file);
                    }


                    if (db != null)
                    {
                        try
                        {
                            db.Dispose();
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Error(
                                ex,
                                "BatchProjectVersionService.Dispose",
                                file);
                        }


                        db =
                            null;
                    }
                }


                results.Add(
                    fileResult);


                // 进度

                int percent =
                    (int)(
                        (
                            fileIndex + 1
                        )
                        * 100.0
                        /
                        files.Length);


                if (percent < 0)
                    percent = 0;


                if (percent > 100)
                    percent = 100;


                if (progress != null)
                {
                    progress(
                        percent,
                        files.Length,
                        Path.GetFileName(
                            file));
                }
            }


            // 最后再恢复一次宿主Database

            try
            {
                if (hostDatabase != null &&
                    !hostDatabase.IsDisposed)
                {
                    HostApplicationServices
                        .WorkingDatabase =
                            hostDatabase;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "BatchProjectVersionService.FinalRestore");
            }


            return results;
        }
    }
}