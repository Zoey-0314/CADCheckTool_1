using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.Core;
using Correct_test1.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


namespace Correct_test1.Readers
{
    /// <summary>
    /// BOM右侧项目号读取器。
    /// 项目号来源不做限制：
    /// 1. 普通DBText
    /// 2. MText
    /// 3. AttributeReference
    /// 4. BlockReference内部文字
    /// 5. 嵌套Block内部文字
    /// 6. QuickRevision生成的项目号
    /// 只根据：
    /// 文字内容
    /// +
    /// 与当前BOM的空间位置
    /// 判断是否属于当前BOM。
    /// </summary>
    public class BomProjectNumberReader
    {
        // 完整项目号：
        //
        // P2026AB001
        // P2026AB001-L0
        // P2026AB001-PE1

        private static readonly Regex
            ProjectRegex =
                new Regex(
                    @"P\d{4}[A-Z]{2}\d{3}(?:-[A-Z0-9]+)?",
                    RegexOptions.IgnoreCase);


        // 基础项目号：
        //
        // P2026AB001-L0
        // ↓
        // P2026AB001

        private static readonly Regex
            BaseProjectRegex =
                new Regex(
                    @"P\d{4}[A-Z]{2}\d{3}",
                    RegexOptions.IgnoreCase);


        // 搜索范围
        //
        // 项目号必须在BOM右侧附近。

        private const double
            LeftTolerance =
                5.0;


        private const double
            MaxRightDistance =
                150.0;


        private const double
            VerticalTolerance =
                10.0;


        // Layout文字缓存
        //
        // 一次单张检查中：
        //
        // 同一个Layout即使有多个BOM，
        // 也只扫描一次文字。

        private readonly
            Dictionary<string, List<TitleText>>
            layoutTextCache =
                new Dictionary<string, List<TitleText>>(
                    StringComparer.OrdinalIgnoreCase);


        /// <summary>
        /// 读取一个BOM右侧的项目号。
        /// 找到：
        /// 返回 P2026AB001
        /// 没找到：
        /// 返回 ""
        /// </summary>
        public string Read(
            Database database,
            CadTableData table,
            BomData bom)
        {
            if (database == null ||
                table == null ||
                bom == null)
            {
                return "";
            }
            bom.ProjectNumberAmbiguous =
    false;

            if (string.IsNullOrWhiteSpace(
                    bom.SourceLayoutName))
            {
                return "";
            }


            // Table边界无效则不判断

            if (!IsValidNumber(
                    table.TableMinX) ||
                !IsValidNumber(
                    table.TableMaxX) ||
                !IsValidNumber(
                    table.TableMinY) ||
                !IsValidNumber(
                    table.TableMaxY) ||
                table.TableMaxX <=
                    table.TableMinX ||
                table.TableMaxY <=
                    table.TableMinY)
            {
                return "";
            }


            List<TitleText> texts =
                GetLayoutTexts(
                    database,
                    bom.SourceLayoutName);


            if (texts == null ||
                texts.Count == 0)
            {
                return "";
            }


            List<ProjectCandidate> candidates =
                new List<ProjectCandidate>();


            foreach (
                TitleText text
                in texts)
            {
                if (text == null ||
                    string.IsNullOrWhiteSpace(
                        text.Text))
                {
                    continue;
                }


                // 必须在BOM右侧附近

                double rightDistance =
                    text.X -
                    table.TableMaxX;


                // 插入点允许略微落在BOM右边界里面，
                // 用于兼容不同文字对正方式。

                if (rightDistance <
                    -LeftTolerance)
                {
                    continue;
                }


                if (rightDistance >
                    MaxRightDistance)
                {
                    continue;
                }


                // Y必须落在BOM高度附近

                if (text.Y <
                        table.TableMinY -
                        VerticalTolerance ||
                    text.Y >
                        table.TableMaxY +
                        VerticalTolerance)
                {
                    continue;
                }


                MatchCollection matches =
                    ProjectRegex.Matches(
                        text.Text);


                if (matches.Count == 0)
                {
                    continue;
                }


                foreach (
                    Match match
                    in matches)
                {
                    if (match == null ||
                        !match.Success)
                    {
                        continue;
                    }


                    string baseProject =
                        NormalizeProjectNumber(
                            match.Value);


                    if (string.IsNullOrWhiteSpace(
                            baseProject))
                    {
                        continue;
                    }


                    candidates.Add(
                        new ProjectCandidate
                        {
                            ProjectNumber =
                                baseProject,

                            RightDistance =
                                Math.Abs(
                                    rightDistance),

                            X =
                                text.X,

                            Y =
                                text.Y
                        });
                }
            }


            if (candidates.Count == 0)
            {
                return "";
            }


            // 相同项目号可能出现多次。
            //
            // 例如：
            // 同一个BOM划改了多个AB件，
            // 每一行右侧都生成了同一个项目号。
            //
            // 这种情况是正常的。

            List<string> distinctProjects =
                candidates
                    .Select(
                        x =>
                            x.ProjectNumber)
                    .Distinct(
                        StringComparer
                            .OrdinalIgnoreCase)
                    .ToList();


            if (distinctProjects.Count == 1)
            {
                return
                    distinctProjects[0];
            }


            // 如果当前BOM右侧出现多个不同项目号，
            // 不自动猜。
            //
            // 宁可按“无项目号BOM”处理，
            // 也不能错误地进入另一个项目的归档。

            bom.ProjectNumberAmbiguous =
    true;


            AppLogger.Warn(
                "BOM右侧发现多个不同项目号，"
                + "Layout="
                + bom.SourceLayoutName
                + "，图号="
                + (bom.DrawingNumber ?? "")
                + "，项目号="
                + string.Join(
                    ",",
                    distinctProjects),
                "BomProjectNumberReader.Read");


            return "";
        }


        // 一个Layout只读取一次文字

        private List<TitleText> GetLayoutTexts(
            Database database,
            string layoutName)
        {
            List<TitleText> cached;


            if (layoutTextCache.TryGetValue(
                    layoutName,
                    out cached))
            {
                return cached;
            }


            List<TitleText> result =
                new List<TitleText>();


            ObjectId spaceId =
                GetLayoutSpaceId(
                    database,
                    layoutName);


            if (spaceId.IsNull ||
                !spaceId.IsValid)
            {
                layoutTextCache[
                    layoutName] =
                        result;


                return result;
            }


            try
            {
                RevisionTableReader reader =
                    new RevisionTableReader();


                result =
                    reader.ReadAllTexts(
                        database,
                        spaceId)
                    ??
                    new List<TitleText>();
            }
            catch (System.Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "BomProjectNumberReader.GetLayoutTexts");


                result =
                    new List<TitleText>();
            }


            layoutTextCache[
                layoutName] =
                    result;


            return result;
        }


        // 根据Layout名称找空间

        private static ObjectId GetLayoutSpaceId(
            Database database,
            string layoutName)
        {
            if (database == null ||
                string.IsNullOrWhiteSpace(
                    layoutName))
            {
                return ObjectId.Null;
            }


            try
            {
                using (
                    Transaction transaction =
                        database
                            .TransactionManager
                            .StartTransaction())
                {
                    DBDictionary layouts =
                        transaction.GetObject(
                            database.LayoutDictionaryId,
                            OpenMode.ForRead)
                        as DBDictionary;


                    if (layouts == null ||
                        !layouts.Contains(
                            layoutName))
                    {
                        return ObjectId.Null;
                    }


                    Layout layout =
                        transaction.GetObject(
                            layouts.GetAt(
                                layoutName),
                            OpenMode.ForRead)
                        as Layout;


                    if (layout == null)
                    {
                        return ObjectId.Null;
                    }


                    ObjectId result =
                        layout.BlockTableRecordId;


                    transaction.Commit();


                    return result;
                }
            }
            catch (System.Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "BomProjectNumberReader.GetLayoutSpaceId");


                return ObjectId.Null;
            }
        }


        // P2026AB001-L0
        // ↓
        // P2026AB001

        private static string NormalizeProjectNumber(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return "";
            }


            Match match =
                BaseProjectRegex.Match(
                    value);


            return match.Success
                ? match.Value
                    .ToUpperInvariant()
                : "";
        }


        private static bool IsValidNumber(
            double value)
        {
            return
                !double.IsNaN(
                    value) &&
                !double.IsInfinity(
                    value) &&
                Math.Abs(
                    value)
                < 1E15;
        }


        private class ProjectCandidate
        {
            public string ProjectNumber
            {
                get;
                set;
            }


            public double RightDistance
            {
                get;
                set;
            }


            public double X
            {
                get;
                set;
            }


            public double Y
            {
                get;
                set;
            }
        }
    }
}