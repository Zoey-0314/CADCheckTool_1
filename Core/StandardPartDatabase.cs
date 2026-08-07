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
        private static Dictionary<string, List<StandardPart>> partNumberIndex =
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
            partNumberIndex.Clear();

            foreach (StandardPart part in parts)
            {
                string key = PartNumberNormalizer.LooseNormalize(
                    part.ExportPartNumber);

                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                List<StandardPart> matches;
                if (!partNumberIndex.TryGetValue(key, out matches))
                {
                    matches = new List<StandardPart>();
                    partNumberIndex.Add(key, matches);
                }

                matches.Add(part);
            }
        }

        public static List<StandardPart> FindByPartNumber(
            string partNumber)
        {
            EnsureLoaded();

            string key = PartNumberNormalizer.LooseNormalize(partNumber);
            List<StandardPart> matches;

            if (partNumberIndex.TryGetValue(key, out matches))
            {
                return matches;
            }

            return new List<StandardPart>();
        }

    }

}