using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Correct_test1.Core;
using Correct_test1.Models;
using Correct_test1.ProjectVersion.Configs;
using Correct_test1.Readers;
using Correct_test1.VersionCheck.Models;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace Correct_test1.VersionCheck.Readers
{
    public class DrawingVersionReader
    {
        // 标准件：
        // V0
        // V1
        // V10

        private static readonly Regex
            StandardVersionRegex =
                new Regex(
                    @"^V(?<version>\d+)$",
                    RegexOptions.IgnoreCase);


        // 项目号：
        // P2026AB004

        private static readonly Regex
            ProjectRegex =
                new Regex(
                    @"P\d{4}[A-Z]{2}\d{3}",
                    RegexOptions.IgnoreCase);


        // 非标版本：
        //
        // 支持：
        //
        // -L0
        //  L0
        // _L0
        //
        // 因此：
        //
        // P2026AB004-PE1-L0
        // P2026AB004-PE1 L0
        //
        // 都可以识别。

        private static readonly Regex
            LVersionRegex =
                new Regex(
                    @"(?:^|[-_\s])L(?<version>\d+)(?=$|[-_\s])",
                    RegexOptions.IgnoreCase);


        public List<DrawingVersionInfo> Read(
            Database database)
        {
            List<DrawingVersionInfo> result =
                new List<DrawingVersionInfo>();


            if (database == null)
                return result;


            LayoutReader layoutReader =
                new LayoutReader();


            TitleBlockReader titleReader =
                new TitleBlockReader();


            List<LayoutInfo> layouts =
                layoutReader.ReadLayouts(
                    database);


            foreach (
                LayoutInfo layout
                in layouts)
            {
                if (layout == null ||
                    layout.IsModelSpace)
                {
                    continue;
                }


                // 读取标题栏文字

                List<TitleText> titleTexts =
                    titleReader.Read(
                        database,
                        new List<LayoutInfo>
                        {
                            layout
                        });


                if (titleTexts == null ||
                    titleTexts.Count == 0)
                {
                    continue;
                }


                // A3 / A4 直接决定横竖版，
                // 并用同一基准点偏移版本号查找区域。

                TitleBlockAnchorInfo anchorInfo;

                bool hasAnchor =
                    TitleBlockOrientationDetector
                        .TryResolveAnchor(
                            titleTexts,
                            out anchorInfo);

                bool isHorizontal =
                    hasAnchor
                        ? anchorInfo.IsHorizontal
                        : TitleBlockOrientationDetector
                            .IsHorizontal(
                                titleTexts);

                double offsetX =
                    hasAnchor
                        ? anchorInfo.OffsetX
                        : 0.0;

                double offsetY =
                    hasAnchor
                        ? anchorInfo.OffsetY
                        : 0.0;


                ProjectVersionTemplate template =
                    ProjectVersionConfig.Get(
                        isHorizontal,
                        offsetX,
                        offsetY);


                DrawingVersionInfo info =
                    ReadLayout(
                        database,
                        layout,
                        template,
                        isHorizontal);


                if (info != null)
                {
                    result.Add(
                        info);
                }
            }


            return result;
        }


        private DrawingVersionInfo ReadLayout(
            Database database,
            LayoutInfo layout,
            ProjectVersionTemplate template,
            bool isHorizontal)
        {
            using (
                Transaction transaction =
                    database
                        .TransactionManager
                        .StartTransaction())
            {
                BlockTableRecord layoutSpace =
                    transaction.GetObject(
                        layout.BlockTableRecordId,
                        OpenMode.ForRead)
                    as BlockTableRecord;


                if (layoutSpace == null)
                    return null;


                Candidate best =
                    new Candidate
                    {
                        Distance =
                            double.MaxValue,

                        Priority =
                            -1
                    };


                foreach (
                    ObjectId id
                    in layoutSpace)
                {
                    Entity entity =
                        transaction.GetObject(
                            id,
                            OpenMode.ForRead)
                        as Entity;


                    FindCandidate(
                        transaction,
                        entity,
                        Matrix3d.Identity,
                        template,
                        layout.LayoutName,
                        isHorizontal,
                        best,
                        0);
                }


                // 找到了版本位置附近的：
                //
                // 1. 正常版本文字
                // 或
                // 2. 有项目号但没有L版本

                if (best.Info != null)
                {
                    transaction.Commit();

                    return best.Info;
                }


                // 什么都没找到。
                //
                // 按照你的规则：
                //
                // 没有项目号 = 标准件
                //
                // 标准件固定位置又没有V0/V1...
                // 就属于“版本号缺失”。

                DrawingVersionInfo missing =
                    new DrawingVersionInfo
                    {
                        LayoutName =
                            layout.LayoutName,

                        IsHorizontal =
                            isHorizontal,

                        IsNonStandard =
                            false,

                        HasVersion =
                            false,

                        ProjectNumber =
                            "",

                        CurrentVersionNumber =
                            -1,

                        CurrentVersionText =
                            "",

                        RawText =
                            "",

                        Position =
                            new Point3d(
                                template.X,
                                template.Y,
                                0)
                    };


                transaction.Commit();


                return missing;
            }
        }


        private void FindCandidate(
            Transaction transaction,
            Entity entity,
            Matrix3d transform,
            ProjectVersionTemplate template,
            string layoutName,
            bool isHorizontal,
            Candidate best,
            int depth)
        {
            if (entity == null ||
                depth > 8)
            {
                return;
            }


            // MText

            MText mtext =
                entity as MText;


            if (mtext != null)
            {
                TryAddCandidate(
                    mtext.Text,
                    mtext.Location
                        .TransformBy(
                            transform),
                    template,
                    layoutName,
                    isHorizontal,
                    best);

                return;
            }


            // DBText

            DBText dbText =
                entity as DBText;


            if (dbText != null)
            {
                TryAddCandidate(
                    dbText.TextString,
                    dbText.Position
                        .TransformBy(
                            transform),
                    template,
                    layoutName,
                    isHorizontal,
                    best);

                return;
            }


            // BlockReference

            BlockReference block =
                entity as BlockReference;


            if (block == null)
                return;


            BlockTableRecord definition;


            try
            {
                definition =
                    transaction.GetObject(
                        block.BlockTableRecord,
                        OpenMode.ForRead)
                    as BlockTableRecord;
            }
            catch
            {
                return;
            }


            if (definition == null ||
                definition.IsFromExternalReference)
            {
                return;
            }


            Matrix3d blockTransform =
                transform *
                block.BlockTransform;


            foreach (
                ObjectId childId
                in definition)
            {
                Entity child =
                    transaction.GetObject(
                        childId,
                        OpenMode.ForRead)
                    as Entity;


                FindCandidate(
                    transaction,
                    child,
                    blockTransform,
                    template,
                    layoutName,
                    isHorizontal,
                    best,
                    depth + 1);
            }
        }


        private void TryAddCandidate(
            string text,
            Point3d position,
            ProjectVersionTemplate template,
            string layoutName,
            bool isHorizontal,
            Candidate best)
        {
            if (string.IsNullOrWhiteSpace(
                    text))
            {
                return;
            }


            // 必须先在指定版本区域附近

            double dx =
                position.X -
                template.X;


            double dy =
                position.Y -
                template.Y;


            double distance =
                Math.Sqrt(
                    dx * dx +
                    dy * dy);


            if (distance >
                template.SearchTolerance)
            {
                return;
            }


            DrawingVersionInfo info;

            int priority;


            if (!TryParse(
                    text,
                    out info,
                    out priority))
            {
                return;
            }


            // 优先级：
            //
            // 2 = 找到了完整版本号
            // 1 = 找到项目号但L版本缺失
            //
            // 完整版本优先于缺失版本。

            if (priority <
                best.Priority)
            {
                return;
            }


            if (priority ==
                    best.Priority &&
                distance >=
                    best.Distance)
            {
                return;
            }


            info.LayoutName =
                layoutName ?? "";


            info.IsHorizontal =
                isHorizontal;


            info.Position =
                position;


            best.Priority =
                priority;


            best.Distance =
                distance;


            best.Info =
                info;
        }


        private bool TryParse(
            string text,
            out DrawingVersionInfo info,
            out int priority)
        {
            info =
                null;


            priority =
                -1;


            if (string.IsNullOrWhiteSpace(
                    text))
            {
                return false;
            }


            string value =
                CadTextCleaner
                    .Clean(
                        text)
                    .Replace(
                        "\\P",
                        "")
                    .Replace(
                        "\r",
                        "")
                    .Replace(
                        "\n",
                        "")
                    .Trim()
                    .ToUpperInvariant();


            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return false;
            }


            // 第一优先：
            // 标准件完整V版本

            Match standardMatch =
                StandardVersionRegex
                    .Match(
                        value);


            if (standardMatch.Success)
            {
                int version;


                if (!int.TryParse(
                        standardMatch
                            .Groups["version"]
                            .Value,
                        out version))
                {
                    return false;
                }


                info =
                    new DrawingVersionInfo
                    {
                        IsNonStandard =
                            false,

                        HasVersion =
                            true,

                        ProjectNumber =
                            "",

                        CurrentVersionNumber =
                            version,

                        CurrentVersionText =
                            "V" + version,

                        RawText =
                            value
                    };


                priority =
                    2;


                return true;
            }


            // 第二类：
            // 看有没有项目号

            Match projectMatch =
                ProjectRegex.Match(
                    value);


            if (!projectMatch.Success)
            {
                // 普通无关文字。
                //
                // 不把版本位置附近所有文字
                // 都误认为版本。

                return false;
            }


            string projectNumber =
                projectMatch
                    .Value
                    .ToUpperInvariant();


            // 有项目号：
            // 再寻找L版本

            string afterProject =
                value.Substring(
                    projectMatch.Index +
                    projectMatch.Length);


            Match versionMatch =
                LVersionRegex.Match(
                    afterProject);


            // 找到了L0/L1...

            if (versionMatch.Success)
            {
                int version;


                if (!int.TryParse(
                        versionMatch
                            .Groups["version"]
                            .Value,
                        out version))
                {
                    return false;
                }


                info =
                    new DrawingVersionInfo
                    {
                        IsNonStandard =
                            true,

                        HasVersion =
                            true,

                        ProjectNumber =
                            projectNumber,

                        CurrentVersionNumber =
                            version,

                        CurrentVersionText =
                            "L" + version,

                        RawText =
                            value
                    };


                priority =
                    2;


                return true;
            }


            // 有项目号但没有 L0/L1/L2 等版本后缀时，判定为版本号缺失。

            info =
                new DrawingVersionInfo
                {
                    IsNonStandard =
                        true,

                    HasVersion =
                        false,

                    ProjectNumber =
                        projectNumber,

                    CurrentVersionNumber =
                        -1,

                    CurrentVersionText =
                        "",

                    RawText =
                        value
                };


            priority =
                1;


            return true;
        }


        private class Candidate
        {
            public double Distance
            {
                get;
                set;
            }


            public int Priority
            {
                get;
                set;
            }


            public DrawingVersionInfo Info
            {
                get;
                set;
            }
        }
    }
}
