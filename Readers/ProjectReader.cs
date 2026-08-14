using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Correct_test1.Models;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Correct_test1.Readers
{
    public class ProjectReader
    {
        public List<string> ReadProjects(Database db)
        {
            List<string> projects =
                new List<string>();

            using (Transaction trans =
                db.TransactionManager.StartTransaction())
            {
                BlockTable bt =
                    trans.GetObject(
                        db.BlockTableId,
                        OpenMode.ForRead) as BlockTable;

                ObjectId[] spaces =
                {
                    bt[BlockTableRecord.ModelSpace],
                    bt[BlockTableRecord.PaperSpace]
                };

                foreach (ObjectId spaceId in spaces)
                {
                    BlockTableRecord btr =
                        trans.GetObject(
                            spaceId,
                            OpenMode.ForRead) as BlockTableRecord;

                    foreach (ObjectId id in btr)
                    {
                        Entity ent =
                            trans.GetObject(
                                id,
                                OpenMode.ForRead) as Entity;

                        if (ent == null)
                            continue;

                        if (ent is BlockReference block)
                        {
                            ReadBlock(
                                block,
                                trans,
                                projects);
                        }
                        else if (ent is DBText text)
                        {
                            AddProject(
                                text.TextString,
                                projects);
                        }
                        else if (ent is MText mtext)
                        {
                            AddProject(
                                mtext.Text,
                                projects);
                        }
                    }
                }

                trans.Commit();
            }

            return projects;
        }

        public List<ProjectNumberLocation> ReadProjectLocations(
            Database db)
        {
            List<ProjectNumberLocation> locations =
                new List<ProjectNumberLocation>();

            using (Transaction trans =
                db.TransactionManager.StartTransaction())
            {
                BlockTable bt =
                    trans.GetObject(
                        db.BlockTableId,
                        OpenMode.ForRead) as BlockTable;

                ObjectId[] spaces =
                {
                    bt[BlockTableRecord.ModelSpace],
                    bt[BlockTableRecord.PaperSpace]
                };

                foreach (ObjectId spaceId in spaces)
                {
                    BlockTableRecord btr =
                        trans.GetObject(
                            spaceId,
                            OpenMode.ForRead) as BlockTableRecord;

                    foreach (ObjectId id in btr)
                    {
                        Entity entity =
                            trans.GetObject(
                                id,
                                OpenMode.ForRead) as Entity;

                        AddProjectLocation(
                            entity,
                            trans,
                            locations,
                            Matrix3d.Identity);
                    }
                }

                trans.Commit();
            }

            return locations;
        }

        private void AddProjectLocation(
            Entity entity,
            Transaction trans,
            List<ProjectNumberLocation> locations,
            Matrix3d transform)
        {
            if (entity is DBText text)
            {
                AddProjectLocation(
                    text.TextString,
                    text.Position.TransformBy(transform),
                    locations);
            }
            else if (entity is MText mtext)
            {
                AddProjectLocation(
                    mtext.Text,
                    mtext.Location.TransformBy(transform),
                    locations);
            }
            else if (entity is BlockReference block)
            {
                BlockTableRecord btr =
                    trans.GetObject(
                        block.BlockTableRecord,
                        OpenMode.ForRead) as BlockTableRecord;

                Matrix3d blockTransform =
                    transform * block.BlockTransform;

                foreach (ObjectId id in btr)
                {
                    AddProjectLocation(
                        trans.GetObject(
                            id,
                            OpenMode.ForRead) as Entity,
                        trans,
                        locations,
                        blockTransform);
                }
            }
        }

        private void AddProjectLocation(
            string text,
            Point3d position,
            List<ProjectNumberLocation> locations)
        {
            if (string.IsNullOrEmpty(text))
                return;

            text = text.Replace("\\P", "").Trim();

            if (!IsProjectNumber(text))
                return;

            string projectNumber =
                GetProjectNumber(text);

            if (!string.IsNullOrEmpty(projectNumber))
            {
                locations.Add(new ProjectNumberLocation
                {
                    ProjectNumber = projectNumber,
                    Position = position
                });
            }
        }

        private static bool IsProjectNumber(
            string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            text = text.Trim().ToUpper();

            return Regex.IsMatch(
                text,
                @"N\d{4}[A-Z]{2}\d{3}(-[A-Z0-9]+)?");
        }

        private static string GetProjectNumber(
            string text)
        {
            Match match =
                Regex.Match(
                    text.ToUpper(),
                    @"N\d{4}[A-Z]{2}\d{3}");

            return match.Success
                ? match.Value
                : null;
        }

        private void AddProject(
            string text,
            List<string> projects)
        {
            if (string.IsNullOrEmpty(text))
                return;

            text = text.Replace("\\P", "").Trim();

            if (!IsProjectNumber(text))
                return;

            string projectNumber =
                GetProjectNumber(text);

            if (!string.IsNullOrEmpty(projectNumber))
            {
                projects.Add(projectNumber);
            }
        }

        private void ReadBlock(
            BlockReference block,
            Transaction trans,
            List<string> projects)
        {
            BlockTableRecord btr =
                trans.GetObject(
                    block.BlockTableRecord,
                    OpenMode.ForRead) as BlockTableRecord;

            foreach (ObjectId id in btr)
            {
                Entity ent =
                    trans.GetObject(
                        id,
                        OpenMode.ForRead) as Entity;

                if (ent == null)
                    continue;

                if (ent is DBText text)
                {
                    AddProject(
                        text.TextString,
                        projects);
                }
                else if (ent is MText mtext)
                {
                    AddProject(
                        mtext.Text,
                        projects);
                }
            }
        }
    }
}
