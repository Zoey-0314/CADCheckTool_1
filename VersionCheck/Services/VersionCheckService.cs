using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.Readers;
using Correct_test1.VersionCheck.Core;
using Correct_test1.VersionCheck.Models;
using Correct_test1.VersionCheck.Readers;

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;


namespace Correct_test1.VersionCheck.Services
{
    public class VersionCheckService
    {
        // 从当前DWG文件名读取项目号
        //
        // 例如：
        //
        // AB452J 安防镜 P2026AB003-L0.dwg
        //
        // ↓
        //
        // P2026AB003
        //
        // 版本检查中：
        // 是否存在项目号，是判断标准/非标的唯一依据。

        private static string ReadProjectNumberFromFileName(
            string filePath)
        {
            if (string.IsNullOrWhiteSpace(
                    filePath))
            {
                return "";
            }


            string fileName;


            try
            {
                fileName =
                    Path.GetFileNameWithoutExtension(
                        filePath);
            }
            catch
            {
                return "";
            }


            if (string.IsNullOrWhiteSpace(
                    fileName))
            {
                return "";
            }


            Match match =
                Regex.Match(
                    fileName,
                    @"P\d{4}[A-Z]{2}\d{3}",
                    RegexOptions.IgnoreCase);


            return match.Success
                ? match.Value.ToUpperInvariant()
                : "";
        }


        // 正式版本检查

        public List<VersionCheckResult> Check(
            Database database,
            string filePath,
            VersionArchiveIndex archiveIndex)
        {
            List<VersionCheckResult> results =
                new List<VersionCheckResult>();


            if (database == null)
            {
                return results;
            }


            // 1. 从当前DWG文件名读取图号
            //
            // AB452J 安防镜 ...
            //
            // ↓
            //
            // AB452J

            FileNameDrawingNumberReader
                drawingNumberReader =
                    new FileNameDrawingNumberReader();


            string drawingNumber =
                drawingNumberReader
                    .ReadDrawingNumber(
                        filePath);


            if (string.IsNullOrWhiteSpace(
                    drawingNumber))
            {
                return results;
            }


            // 2. 从当前DWG文件名读取项目号
            //
            // 这是版本检查中判断
            // “标准件 / 非标件”的唯一依据。

            string fileProjectNumber =
                ReadProjectNumberFromFileName(
                    filePath);


            bool fileIsNonStandard =
                !string.IsNullOrWhiteSpace(
                    fileProjectNumber);


            // 3. 读取每个Layout固定版本位置

            DrawingVersionReader versionReader =
                new DrawingVersionReader();


            List<DrawingVersionInfo> versions =
                versionReader.Read(
                    database);


            if (versions == null)
            {
                return results;
            }


            // 4. 每个Layout分别检查

            foreach (
                DrawingVersionInfo version
                in versions)
            {
                if (version == null)
                {
                    continue;
                }


                // 非标图纸
                //
                // 当前DWG文件名存在项目号。
                //
                // 例如：
                //
                // AB452J 安防镜 P2026AB003-L0.dwg
                //
                // 从这里开始：
                //
                // 只允许L版本。
                // V版本全部忽略。

                if (fileIsNonStandard)
                {
                    // 非标图纸必须检测到：
                    //
                    // L0
                    // L1
                    // L2
                    // ...
                    //
                    // 如果：
                    //
                    // 1. 完全没有版本号
                    //
                    // 或
                    //
                    // 2. 固定位置读到了V5这种
                    //    标准件版本
                    //
                    // 都按“缺少L版本号”处理。

                    if (!version.HasVersion ||
                        !version.IsNonStandard)
                    {
                        HandleMissingVersion(
                            results,
                            version,
                            drawingNumber,
                            filePath,
                            true,
                            fileProjectNumber,
                            archiveIndex);


                        continue;
                    }


                    // 版本归档不可用：
                    //
                    // 无法推论最新版本。

                    if (archiveIndex == null ||
                        !archiveIndex.IsAvailable)
                    {
                        continue;
                    }


                    int latestVersion;

                    string latestFilePath;


                    // 非标最新版本查询
                    //
                    // 注意：
                    //
                    // 使用的是：
                    //
                    // 当前DWG文件名项目号
                    //
                    // 而不是图纸里面文字解析出来的项目号。
                    //
                    // 例如：
                    //
                    // 图号：
                    // AB452J
                    //
                    // 当前项目：
                    // P2026AB003
                    //
                    // 只检查：
                    //
                    // AB452J + P2026AB003 + Lx

                    bool found =
                        archiveIndex
                            .TryGetLatestNonStandard(
                                drawingNumber,
                                fileProjectNumber,
                                out latestVersion,
                                out latestFilePath);


                    // 归档中没有同项目记录

                    if (!found)
                    {
                        continue;
                    }


                    // 当前已经是最新版本

                    if (latestVersion <=
                        version.CurrentVersionNumber)
                    {
                        continue;
                    }


                    // 当前版本落后

                    results.Add(
                        new VersionCheckResult
                        {
                            FilePath =
                                filePath,

                            DrawingNumber =
                                drawingNumber,

                            LayoutName =
                                version.LayoutName,

                            IsNonStandard =
                                true,

                            ProjectNumber =
                                fileProjectNumber,

                            CurrentVersion =
                                version.CurrentVersionText,

                            LatestVersion =
                                "L" + latestVersion,

                            LatestFilePath =
                                latestFilePath,

                            Position =
                                version.Position,

                            Message =
                                "当前图号在当前项目中的最新版本号为 L"
                                + latestVersion
                        });


                    continue;
                }


                // 标准件图纸
                //
                // 当前DWG文件名没有项目号。
                //
                // 只允许：
                //
                // V0
                // V1
                // V2
                // ...
                //
                // L版本完全不参与。

                // 标准件必须检测到V版本。
                //
                // 如果：
                //
                // 1. 没有版本
                //
                // 或
                //
                // 2. 固定位置出现了项目号+L版本
                //
                // 都按“缺少V版本号”处理。

                if (!version.HasVersion ||
                    version.IsNonStandard)
                {
                    HandleMissingVersion(
                        results,
                        version,
                        drawingNumber,
                        filePath,
                        false,
                        "",
                        archiveIndex);


                    continue;
                }


                // 归档不可用

                if (archiveIndex == null ||
                    !archiveIndex.IsAvailable)
                {
                    continue;
                }


                int latestStandardVersion;

                string latestStandardFilePath;


                // 标准件只检查：
                //
                // 同图号 + 最大V

                bool standardFound =
                    archiveIndex
                        .TryGetLatestStandard(
                            drawingNumber,
                            out latestStandardVersion,
                            out latestStandardFilePath);


                if (!standardFound)
                {
                    continue;
                }


                // 当前已经最新

                if (latestStandardVersion <=
                    version.CurrentVersionNumber)
                {
                    continue;
                }


                // 当前V版本落后

                results.Add(
                    new VersionCheckResult
                    {
                        FilePath =
                            filePath,

                        DrawingNumber =
                            drawingNumber,

                        LayoutName =
                            version.LayoutName,

                        IsNonStandard =
                            false,

                        ProjectNumber =
                            "",

                        CurrentVersion =
                            version.CurrentVersionText,

                        LatestVersion =
                            "V" + latestStandardVersion,

                        LatestFilePath =
                            latestStandardFilePath,

                        Position =
                            version.Position,

                        Message =
                            "当前图号最新版本号为 V"
                            + latestStandardVersion
                    });
            }


            return results;
        }


        // 版本号缺失

        private void HandleMissingVersion(
            List<VersionCheckResult> results,
            DrawingVersionInfo version,
            string drawingNumber,
            string filePath,
            bool fileIsNonStandard,
            string fileProjectNumber,
            VersionArchiveIndex archiveIndex)
        {
            if (results == null ||
                version == null)
            {
                return;
            }


            int latestVersion =
                -1;


            string latestFilePath =
                "";


            string latestVersionText =
                "";


            string message;


            // 非标图纸
            //
            // 当前DWG文件名有项目号。
            //
            // 必须有：
            //
            // L0
            // L1
            // L2
            // ...

            if (fileIsNonStandard)
            {
                // 从归档中寻找：
                //
                // 同图号
                // +
                // 同当前文件项目号
                // +
                // 最大L版本

                bool latestFound =
                    archiveIndex != null &&
                    archiveIndex.IsAvailable &&
                    archiveIndex
                        .TryGetLatestNonStandard(
                            drawingNumber,
                            fileProjectNumber,
                            out latestVersion,
                            out latestFilePath);


                // 找到了归档最新版本

                if (latestFound)
                {
                    latestVersionText =
                        "L" + latestVersion;


                    message =
                        "版本号缺失：未检测到 L0、L1……版本号；"
                        + "当前图号在当前项目中的最新版本号为 "
                        + latestVersionText;
                }
                else
                {
                    // 没找到归档版本，
                    // 但当前图纸缺版本本身仍然是错误。

                    message =
                        "版本号缺失：当前项目号后未检测到 "
                        + "L0、L1……版本号";
                }


                results.Add(
                    new VersionCheckResult
                    {
                        FilePath =
                            filePath,

                        DrawingNumber =
                            drawingNumber,

                        LayoutName =
                            version.LayoutName,

                        IsNonStandard =
                            true,

                        ProjectNumber =
                            fileProjectNumber,

                        CurrentVersion =
                            "未检测到版本号",

                        LatestVersion =
                            latestVersionText,

                        LatestFilePath =
                            latestFilePath,

                        Position =
                            version.Position,

                        Message =
                            message
                    });


                return;
            }


            // 标准件图纸
            //
            // 当前文件名没有项目号。
            //
            // 必须有：
            //
            // V0
            // V1
            // V2
            // ...

            bool standardLatestFound =
                archiveIndex != null &&
                archiveIndex.IsAvailable &&
                archiveIndex
                    .TryGetLatestStandard(
                        drawingNumber,
                        out latestVersion,
                        out latestFilePath);


            // 找到了最新V版本

            if (standardLatestFound)
            {
                latestVersionText =
                    "V" + latestVersion;


                message =
                    "版本号缺失：未检测到 V0、V1……版本号；"
                    + "当前图号最新版本号为 "
                    + latestVersionText;
            }
            else
            {
                message =
                    "版本号缺失：未检测到 "
                    + "V0、V1……版本号";
            }


            results.Add(
                new VersionCheckResult
                {
                    FilePath =
                        filePath,

                    DrawingNumber =
                        drawingNumber,

                    LayoutName =
                        version.LayoutName,

                    IsNonStandard =
                        false,

                    ProjectNumber =
                        "",

                    CurrentVersion =
                        "未检测到版本号",

                    LatestVersion =
                        latestVersionText,

                    LatestFilePath =
                        latestFilePath,

                    Position =
                        version.Position,

                    Message =
                        message
                });
        }
    }
}