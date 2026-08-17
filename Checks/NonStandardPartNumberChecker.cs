using Correct_test1.Core;
using Correct_test1.Models;
using Correct_test1.Readers;

using Autodesk.AutoCAD.Geometry;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;


namespace Correct_test1.Checks
{
    /// <summary>
    /// BOM非标件号存在性检查。
    ///
    /// 支持两种被检查BOM形式：
    ///
    /// 形式1：
    /// Part No. = NS333H1
    ///
    /// ↓
    ///
    /// 图号 = NS333H
    /// 件号 = 1
    ///
    ///
    /// 形式2：
    /// Part No. = NS347DH_
    /// P/N      = _999
    ///
    /// ↓
    ///
    /// 图号 = NS347DH
    /// 件号 = 999
    ///
    ///
    /// 然后根据当前被检查DWG
    /// 是否存在项目号，
    /// 锁定正确的归档DWG。
    /// </summary>
    public class NonStandardPartNumberChecker
    {
        public List<NonStandardPartNumberCheckResult>
            Check(
                BomData bom,
                string currentFilePath,
                NonStandardArchiveIndex archiveIndex)
        {
            List<NonStandardPartNumberCheckResult>
                results =
                    new List<NonStandardPartNumberCheckResult>();


            if (bom == null ||
                bom.Items == null)
            {
                return results;
            }


            if (archiveIndex == null ||
                !archiveIndex.IsAvailable)
            {
                return results;
            }


            //==================================================
            // 读取当前被检查DWG的项目号
            //
            // 有项目号：
            // 只检查同项目归档DWG。
            //
            // 没项目号：
            // 仍然检查件号，
            // 但只检查同样没有项目号的归档DWG。
            //==================================================

            FileNameProjectReader projectReader =
                new FileNameProjectReader();


            FileNameProjectReader.ProjectInfo
                currentProjectInfo =
                    projectReader
                        .ReadProjectNumber(
                            currentFilePath);


            bool currentHasProject =
                currentProjectInfo != null &&
                !string.IsNullOrWhiteSpace(
                    currentProjectInfo.ProjectNumber);


            string currentProjectNumber =
                currentHasProject
                    ? currentProjectInfo
                        .ProjectNumber
                        .Trim()
                    : "";


            //==================================================
            // 检查BOM中的每一个NS件
            //==================================================

            foreach (
                BomItem item
                in bom.Items)
            {
                if (item == null)
                {
                    continue;
                }


                //==================================================
                // 只检查NS非标件
                //==================================================

                if (PartNumberTypeClassifier
                        .Classify(
                            item.PartNumber)
                    != PartNumberType
                        .NonStandardPart)
                {
                    continue;
                }


                string originalPartNumber =
                    CadTextCleaner.Clean(
                        item.PartNumber);


                if (string.IsNullOrWhiteSpace(
                        originalPartNumber))
                {
                    continue;
                }


                //==================================================
                // 得到基础归档图号
                //
                // NS333H1
                // ↓
                // NS333H
                //
                // NS347DH_
                // ↓
                // NS347DH
                //==================================================

                string archiveDrawingNumber =
                    NonStandardArchiveChecker
                        .BuildSearchKey(
                            originalPartNumber);


                if (string.IsNullOrWhiteSpace(
                        archiveDrawingNumber))
                {
                    continue;
                }


                //==================================================
                // 得到件号
                //==================================================

                string partSuffix;


                //==================================================
                // 新BOM形式：
                //
                // Part No. = NS347DH_
                // P/N      = _999
                //
                // ↓
                //
                // 图号 = NS347DH
                // 件号 = 999
                //==================================================

                if (originalPartNumber
                        .Trim()
                        .EndsWith(
                            "_",
                            StringComparison.Ordinal) &&
                    TryNormalizeBomPartSuffix(
                        item.PartNumberSuffix,
                        out partSuffix))
                {
                    //--------------------------------
                    // 已从P/N列取得件号
                    //--------------------------------
                }
                else
                {
                    //==================================================
                    // 原有形式：
                    //
                    // NS333H1
                    //
                    // ↓
                    //
                    // 图号 = NS333H
                    // 件号 = 1
                    //==================================================

                    partSuffix =
                        BuildPartSuffix(
                            originalPartNumber,
                            archiveDrawingNumber);
                }


                //--------------------------------
                // 没有件号：
                // 本次件号检查不处理。
                //--------------------------------

                if (string.IsNullOrWhiteSpace(
                        partSuffix))
                {
                    continue;
                }


                //==================================================
                // 统一生成真正完整件号
                //
                // 新格式：
                //
                // NS347DH_ + _999
                // ↓
                // NS347DH999
                //
                // 旧格式：
                //
                // NS333H1
                // ↓
                // NS333H1
                //
                // 后续报告、CSV、提示全部统一使用这个值。
                //==================================================

                string effectivePartNumber =
                    archiveDrawingNumber
                    + partSuffix;


                //==================================================
                // 先确认基础归档图号存在
                //
                // 如果整个NS333H都不存在，
                // 原有NonStandardArchiveChecker
                // 已经会报错。
                //
                // 本检查不重复报。
                //==================================================

                string anyArchiveFile;


                bool baseDrawingExists =
                    archiveIndex.Contains(
                        archiveDrawingNumber,
                        out anyArchiveFile);


                if (!baseDrawingExists)
                {
                    continue;
                }


                //==================================================
                // 根据当前文件类型锁定归档DWG
                //
                // 当前有项目号：
                //
                // 图号 + 当前项目号
                //
                // 当前无项目号：
                //
                // 图号 + 候选文件同样无项目号
                //==================================================

                List<string> candidateFiles =
                    FindCandidateDwgs(
                        archiveIndex,
                        archiveDrawingNumber,
                        currentHasProject,
                        currentProjectNumber);


                //==================================================
                // 没有找到符合条件的归档DWG
                //==================================================

                if (candidateFiles.Count == 0)
                {
                    string message;


                    if (currentHasProject)
                    {
                        message =
                            "非标件号检查失败："
                            + "未找到同时包含图号 "
                            + archiveDrawingNumber
                            + " 和当前项目号 "
                            + currentProjectNumber
                            + " 的归档DWG。";
                    }
                    else
                    {
                        message =
                            "非标件号检查失败："
                            + "未找到图号 "
                            + archiveDrawingNumber
                            + " 且文件名中不包含项目号的归档DWG。";
                    }


                    results.Add(
                        CreateResult(
                            bom,
                            item,

                            //==============================
                            // 使用统一完整件号
                            //==============================

                            effectivePartNumber,

                            archiveDrawingNumber,
                            partSuffix,
                            currentProjectNumber,
                            "",
                            true,
                            message));


                    continue;
                }


                //==================================================
                // 多个归档DWG：
                //
                // 有项目号：
                // 取最高L版本。
                //
                // 无项目号：
                // 取最高V版本。
                //==================================================

                string archiveFile =
                    SelectLatestFile(
                        candidateFiles,
                        currentHasProject);


                if (string.IsNullOrWhiteSpace(
                        archiveFile))
                {
                    continue;
                }


                //==================================================
                // 打开归档DWG，
                // 检查所有Layout。
                //==================================================

                bool contains;

                string error;


                bool inspected =
                    NonStandardPartNumberInspectionCache
                        .TryContains(
                            archiveFile,
                            archiveDrawingNumber,
                            partSuffix,
                            out contains,
                            out error);


                //==================================================
                // 归档DWG读取失败
                //==================================================

                if (!inspected)
                {
                    results.Add(
                        CreateResult(
                            bom,
                            item,

                            //==============================
                            // 使用统一完整件号
                            //==============================

                            effectivePartNumber,

                            archiveDrawingNumber,
                            partSuffix,
                            currentProjectNumber,
                            archiveFile,
                            true,
                            "非标件号检查失败："
                            + error));


                    continue;
                }


                //==================================================
                // 任意Layout找到件号：
                //
                // 正确，不产生错误。
                //==================================================

                if (contains)
                {
                    continue;
                }


                //==================================================
                // 所有Layout都没有找到目标件号
                //==================================================

                results.Add(
                    CreateResult(
                        bom,
                        item,

                        //==============================
                        // 使用统一完整件号
                        //==============================

                        effectivePartNumber,

                        archiveDrawingNumber,
                        partSuffix,
                        currentProjectNumber,
                        archiveFile,
                        false,

                        "非标件号不存在："
                        + effectivePartNumber
                        + "；归档图纸 "
                        + Path.GetFileName(
                            archiveFile)
                        + " 的所有布局中均未找到 "
                        + archiveDrawingNumber
                        + " + _"
                        + partSuffix));
            }


            return results;
        }


        //==================================================
        // 读取BOM中的P/N列
        //
        // _999
        // ↓
        // 999
        //==================================================

        private static bool TryNormalizeBomPartSuffix(
            string value,
            out string suffix)
        {
            suffix =
                "";


            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return false;
            }


            string cleaned =
                CadTextCleaner.Clean(
                    value)
                    .Trim()
                    .Replace(
                        " ",
                        "");


            //--------------------------------
            // 当前P/N形式必须是：
            //
            // _999
            // _998
            // _1
            //--------------------------------

            if (!cleaned.StartsWith(
                    "_",
                    StringComparison.Ordinal))
            {
                return false;
            }


            string number =
                cleaned.TrimStart(
                    '_');


            if (string.IsNullOrWhiteSpace(
                    number))
            {
                return false;
            }


            foreach (
                char character
                in number)
            {
                if (!char.IsDigit(
                        character))
                {
                    return false;
                }
            }


            suffix =
                number;


            return true;
        }


        //==================================================
        // 从完整BOM件号拆件号
        //
        // NS333H1
        // ↓
        // 1
        //==================================================

        private static string BuildPartSuffix(
            string originalPartNumber,
            string archiveDrawingNumber)
        {
            if (string.IsNullOrWhiteSpace(
                    originalPartNumber) ||
                string.IsNullOrWhiteSpace(
                    archiveDrawingNumber))
            {
                return "";
            }


            string original =
                CadTextCleaner.Clean(
                    originalPartNumber)
                    .Trim();


            string drawing =
                archiveDrawingNumber
                    .Trim();


            if (!original.StartsWith(
                    drawing,
                    StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }


            string suffix =
                original.Substring(
                    drawing.Length)
                    .Trim()
                    .TrimStart(
                        '_',
                        '-');


            if (string.IsNullOrWhiteSpace(
                    suffix))
            {
                return "";
            }


            //--------------------------------
            // 当前件号只接受纯数字
            //--------------------------------

            foreach (
                char character
                in suffix)
            {
                if (!char.IsDigit(
                        character))
                {
                    return "";
                }
            }


            return suffix;
        }


        //==================================================
        // 根据当前图纸类型锁定正确归档DWG
        //==================================================

        private static List<string> FindCandidateDwgs(
            NonStandardArchiveIndex archiveIndex,
            string drawingNumber,
            bool currentHasProject,
            string currentProjectNumber)
        {
            List<string> result =
                new List<string>();


            if (archiveIndex == null)
            {
                return result;
            }


            List<string> files =
                archiveIndex
                    .GetFilePathsSnapshot();


            if (files == null)
            {
                return result;
            }


            FileNameProjectReader projectReader =
                new FileNameProjectReader();


            foreach (
                string file
                in files)
            {
                if (string.IsNullOrWhiteSpace(
                        file))
                {
                    continue;
                }


                //==================================================
                // 件号存在性必须读取DWG内部Layout。
                //
                // PDF不能用于这一步。
                //==================================================

                if (!string.Equals(
                        Path.GetExtension(
                            file),
                        ".dwg",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }


                string fileName;


                try
                {
                    fileName =
                        Path.GetFileNameWithoutExtension(
                            file);
                }
                catch
                {
                    continue;
                }


                if (string.IsNullOrWhiteSpace(
                        fileName))
                {
                    continue;
                }


                //==================================================
                // 必须属于目标图号
                //
                // 防止：
                //
                // NS333T
                //
                // 错误匹配：
                //
                // NS333TA
                // NS333TABC
                //==================================================

                if (!MatchesDrawingNumber(
                        fileName,
                        drawingNumber))
                {
                    continue;
                }


                //==================================================
                // 判断候选归档DWG自己的项目号
                //==================================================

                FileNameProjectReader.ProjectInfo
                    candidateProject =
                        projectReader
                            .ReadProjectNumber(
                                file);


                bool candidateHasProject =
                    candidateProject != null &&
                    !string.IsNullOrWhiteSpace(
                        candidateProject.ProjectNumber);


                //==================================================
                // 当前图纸有项目号
                //==================================================

                if (currentHasProject)
                {
                    //--------------------------------
                    // 候选图纸也必须有项目号
                    //--------------------------------

                    if (!candidateHasProject)
                    {
                        continue;
                    }


                    //--------------------------------
                    // 必须属于相同项目
                    //--------------------------------

                    if (!string.Equals(
                            candidateProject.ProjectNumber,
                            currentProjectNumber,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }


                    result.Add(
                        file);


                    continue;
                }


                //==================================================
                // 当前图纸没有项目号
                //
                // 候选归档DWG也必须没有项目号。
                //==================================================

                if (candidateHasProject)
                {
                    continue;
                }


                result.Add(
                    file);
            }


            return result;
        }


        //==================================================
        // 精确判断归档文件是否属于目标图号
        //==================================================

        private static bool MatchesDrawingNumber(
            string fileName,
            string drawingNumber)
        {
            if (string.IsNullOrWhiteSpace(
                    fileName) ||
                string.IsNullOrWhiteSpace(
                    drawingNumber))
            {
                return false;
            }


            string name =
                fileName.Trim();


            string drawing =
                drawingNumber.Trim();


            if (!name.StartsWith(
                    drawing,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }


            //--------------------------------
            // 文件名刚好等于图号
            //--------------------------------

            if (name.Length ==
                drawing.Length)
            {
                return true;
            }


            //--------------------------------
            // 图号后面允许：
            //
            // 空格
            // -
            // _
            //--------------------------------

            char next =
                name[drawing.Length];


            return
                char.IsWhiteSpace(
                    next)
                ||
                next == '-'
                ||
                next == '_';
        }


        //==================================================
        // 多个候选归档DWG：
        //
        // 有项目：
        // 取最高L。
        //
        // 无项目：
        // 取最高V。
        //==================================================

        private static string SelectLatestFile(
            List<string> files,
            bool currentHasProject)
        {
            if (files == null ||
                files.Count == 0)
            {
                return "";
            }


            string bestFile =
                files[0];


            int bestVersion =
                currentHasProject
                    ? ReadLVersion(
                        bestFile)
                    : ReadVVersion(
                        bestFile);


            foreach (
                string file
                in files)
            {
                int version =
                    currentHasProject
                        ? ReadLVersion(
                            file)
                        : ReadVVersion(
                            file);


                if (version >
                    bestVersion)
                {
                    bestVersion =
                        version;


                    bestFile =
                        file;
                }
            }


            return bestFile;
        }


        //==================================================
        // L版本
        //==================================================

        private static int ReadLVersion(
            string filePath)
        {
            return ReadVersion(
                filePath,
                "L");
        }


        //==================================================
        // V版本
        //==================================================

        private static int ReadVVersion(
            string filePath)
        {
            return ReadVersion(
                filePath,
                "V");
        }


        private static int ReadVersion(
            string filePath,
            string prefix)
        {
            string fileName;


            try
            {
                fileName =
                    Path.GetFileNameWithoutExtension(
                        filePath);
            }
            catch
            {
                return -1;
            }


            if (string.IsNullOrWhiteSpace(
                    fileName))
            {
                return -1;
            }


            MatchCollection matches =
                Regex.Matches(
                    fileName,
                    @"(?:^|[-_\s])"
                    + Regex.Escape(
                        prefix)
                    + @"(?<version>\d+)(?=$|[-_\s])",
                    RegexOptions.IgnoreCase);


            int best =
                -1;


            foreach (
                Match match
                in matches)
            {
                int value;


                if (!int.TryParse(
                        match
                            .Groups["version"]
                            .Value,
                        out value))
                {
                    continue;
                }


                if (value >
                    best)
                {
                    best =
                        value;
                }
            }


            return best;
        }


        //==================================================
        // 决定错误标记放在哪个单元格
        //==================================================

        private static Point3d GetMarkerPosition(
            BomItem item)
        {
            if (item == null)
            {
                return Point3d.Origin;
            }


            //==================================================
            // 新BOM形式：
            //
            // Part No.       P/N
            //
            // NS347DH_       _999
            //
            // 如果件号不存在，
            // 红字应该标在 _999 旁边。
            //==================================================

            if (!string.IsNullOrWhiteSpace(
                    item.PartNumber) &&
                item.PartNumber
                    .Trim()
                    .EndsWith(
                        "_",
                        StringComparison.Ordinal) &&
                item.PartNumberSuffixCellPosition
                    != Point3d.Origin)
            {
                return
                    item.PartNumberSuffixCellPosition;
            }


            //==================================================
            // 旧形式：
            //
            // Part No.
            //
            // NS333H1
            //
            // 标在NS333H1旁边。
            //==================================================

            return
                item.PartNumberCellPosition;
        }


        //==================================================
        // 创建错误结果
        //==================================================

        private static
            NonStandardPartNumberCheckResult
            CreateResult(
                BomData bom,
                BomItem item,
                string originalPartNumber,
                string archiveDrawingNumber,
                string partSuffix,
                string projectNumber,
                string archiveFilePath,
                bool inspectionFailed,
                string message)
        {
            return
                new NonStandardPartNumberCheckResult
                {
                    BomItem =
                        item,

                    DrawingNumber =
                        bom == null
                            ? ""
                            : bom.DrawingNumber,

                    SourceLayoutName =
                        bom == null
                            ? ""
                            : bom.SourceLayoutName,

                    //--------------------------------
                    // 这里现在保存的已经是
                    // 统一后的完整件号：
                    //
                    // NS347DH999
                    // NS333H1
                    //--------------------------------

                    OriginalPartNumber =
                        originalPartNumber,

                    ArchiveDrawingNumber =
                        archiveDrawingNumber,

                    PartSuffix =
                        partSuffix,

                    ProjectNumber =
                        projectNumber,

                    ArchiveFilePath =
                        archiveFilePath,

                    InspectionFailed =
                        inspectionFailed,

                    //--------------------------------
                    // 根据BOM格式决定标记位置
                    //--------------------------------

                    MarkerPosition =
                        GetMarkerPosition(
                            item),

                    Message =
                        message
                };
        }
    }
}