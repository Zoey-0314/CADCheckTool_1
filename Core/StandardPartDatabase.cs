using Correct_test1.Models;
using Correct_test1.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Diagnostics;


namespace Correct_test1.Core
{

    public static class StandardPartDatabase
    {


        private static List<StandardPart> parts;
        // 严格索引（Trim, case-insensitive）
        private static Dictionary<string, List<StandardPart>> exportStrictIndex =
            new Dictionary<string, List<StandardPart>>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, List<StandardPart>> nationalStrictIndex =
            new Dictionary<string, List<StandardPart>>(StringComparer.OrdinalIgnoreCase);
        // 宽松索引（使用 PartNumberNormalizer.LooseNormalize）
        private static Dictionary<string, List<StandardPart>> exportLooseIndex =
            new Dictionary<string, List<StandardPart>>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, List<StandardPart>> nationalLooseIndex =
            new Dictionary<string, List<StandardPart>>(StringComparer.OrdinalIgnoreCase);
        private static bool loaded;


        public static IReadOnlyList<StandardPart> Parts
        {
            get
            {
                EnsureLoaded();
                return parts;
            }
        }

        public static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            string folder = Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location);
            string path = Path.Combine(
                folder,
                "Resources",
                "StandardParts.xlsx");

            Autodesk.AutoCAD.ApplicationServices.Application
            .DocumentManager
            .MdiActiveDocument
            .Editor
            .WriteMessage(
                "\nStandardPartDatabase Excel path: "
                + path
            );

            Load(path);
        }

        public static void Load(string path)
        {
            StandardPartExcelReader reader =
                new StandardPartExcelReader();

            parts = reader.Read(path);
            BuildIndex();
            loaded = true;

            Autodesk.AutoCAD.ApplicationServices.Application
            .DocumentManager
            .MdiActiveDocument
            .Editor
            .WriteMessage(
                "\nStandardPartDatabase loaded count: "
                + parts.Count
            );
        }

        public static void BuildIndex()
        {
            // 清空所有索引
            exportStrictIndex.Clear();
            nationalStrictIndex.Clear();
            exportLooseIndex.Clear();
            nationalLooseIndex.Clear();

            foreach (StandardPart part in parts)
            {
                // 严格键: Trim 后（不改变大小写比较，通过 Dictionary 的 comparer 忽略大小写）
                string exportStrictKey = part?.ExportPartNumber == null ? "" : part.ExportPartNumber.Trim();
                if (!string.IsNullOrEmpty(exportStrictKey))
                {
                    List<StandardPart> list;
                    if (!exportStrictIndex.TryGetValue(exportStrictKey, out list))
                    {
                        list = new List<StandardPart>();
                        exportStrictIndex.Add(exportStrictKey, list);
                    }
                    list.Add(part);
                }

                string nationalStrictKey = part?.NationalPartNumber == null ? "" : part.NationalPartNumber.Trim();
                if (!string.IsNullOrEmpty(nationalStrictKey))
                {
                    List<StandardPart> listN;
                    if (!nationalStrictIndex.TryGetValue(nationalStrictKey, out listN))
                    {
                        listN = new List<StandardPart>();
                        nationalStrictIndex.Add(nationalStrictKey, listN);
                    }
                    listN.Add(part);
                }

                // 宽松键: 使用 LooseNormalize
                string exportLooseKey = PartNumberNormalizer.LooseNormalize(part?.ExportPartNumber);
                if (!string.IsNullOrEmpty(exportLooseKey))
                {
                    List<StandardPart> listL;
                    if (!exportLooseIndex.TryGetValue(exportLooseKey, out listL))
                    {
                        listL = new List<StandardPart>();
                        exportLooseIndex.Add(exportLooseKey, listL);
                    }
                    listL.Add(part);
                }

                string nationalLooseKey = PartNumberNormalizer.LooseNormalize(part?.NationalPartNumber);
                if (!string.IsNullOrEmpty(nationalLooseKey))
                {
                    List<StandardPart> listNL;
                    if (!nationalLooseIndex.TryGetValue(nationalLooseKey, out listNL))
                    {
                        listNL = new List<StandardPart>();
                        nationalLooseIndex.Add(nationalLooseKey, listNL);
                    }
                    listNL.Add(part);
                }
            }
        }

        public static List<StandardPart> FindByPartNumber(
            string partNumber)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(partNumber))
            {
                return new List<StandardPart>();
            }
            // 准备调试输出数据
            string strictKey = partNumber.Trim();
            string looseKey = PartNumberNormalizer.LooseNormalize(partNumber);

            List<StandardPart> exportStrictMatches = null;
            List<StandardPart> nationalStrictMatches = null;
            List<StandardPart> exportLooseMatches = null;
            List<StandardPart> nationalLooseMatches = null;

            if (!string.IsNullOrEmpty(strictKey))
            {
                exportStrictIndex.TryGetValue(strictKey, out exportStrictMatches);
                nationalStrictIndex.TryGetValue(strictKey, out nationalStrictMatches);
            }

            if (!string.IsNullOrEmpty(looseKey))
            {
                exportLooseIndex.TryGetValue(looseKey, out exportLooseMatches);
                nationalLooseIndex.TryGetValue(looseKey, out nationalLooseMatches);
            }

            if (exportStrictMatches == null) exportStrictMatches = new List<StandardPart>();
            if (nationalStrictMatches == null) nationalStrictMatches = new List<StandardPart>();
            if (exportLooseMatches == null) exportLooseMatches = new List<StandardPart>();
            if (nationalLooseMatches == null) nationalLooseMatches = new List<StandardPart>();

            // 选择最终返回值（按优先级）
            List<StandardPart> finalReturn = null;
            if (exportStrictMatches.Count > 0)
            {
                finalReturn = exportStrictMatches;
            }
            else if (nationalStrictMatches.Count > 0)
            {
                finalReturn = nationalStrictMatches;
            }
            else if (exportLooseMatches.Count > 0)
            {
                finalReturn = exportLooseMatches;
            }
            else if (nationalLooseMatches.Count > 0)
            {
                finalReturn = nationalLooseMatches;
            }
            else
            {
                finalReturn = new List<StandardPart>();
            }

            // 输出临时调试信息
            var ed = Autodesk.AutoCAD.ApplicationServices.Application
                .DocumentManager
                .MdiActiveDocument
                .Editor;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("\n输入图号:");
            sb.AppendLine(partNumber);
            sb.AppendLine();
            sb.AppendLine("LooseKey:");
            sb.AppendLine(looseKey);
            sb.AppendLine();

            sb.AppendLine("Export匹配:");
            foreach (var p in exportLooseMatches)
            {
                sb.AppendLine($"{p.ExportPartNumber} | {p.NationalPartNumber} | {p.Name}");
            }
            if (exportLooseMatches.Count == 0) sb.AppendLine("(none)");
            sb.AppendLine();

            sb.AppendLine("National匹配:");
            foreach (var p in nationalLooseMatches)
            {
                sb.AppendLine($"{p.ExportPartNumber} | {p.NationalPartNumber} | {p.Name}");
            }
            if (nationalLooseMatches.Count == 0) sb.AppendLine("(none)");
            sb.AppendLine();

            sb.AppendLine("最终返回:");
            foreach (var p in finalReturn)
            {
                sb.AppendLine($"{p.ExportPartNumber} | {p.NationalPartNumber} | {p.Name}");
            }
            if (finalReturn.Count == 0) sb.AppendLine("(none)");

            ed.WriteMessage(sb.ToString());

            return finalReturn;
        }

    }

}