using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Correct_test1.Models;

using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace Correct_test1.Readers
{
    public class ProjectReader
    {
        //==================================================
        // 原有：读取项目号
        //==================================================

        public List<string> ReadProjects(
            Database db)
        {
            List<string> projects =
                new List<string>();


            if (db == null)
            {
                return projects;
            }


            using (
                Transaction trans =
                    db.TransactionManager
                        .StartTransaction())
            {
                BlockTable bt =
                    trans.GetObject(
                        db.BlockTableId,
                        OpenMode.ForRead)
                    as BlockTable;


                if (bt == null)
                {
                    return projects;
                }


                ObjectId[] spaces =
                {
                    bt[BlockTableRecord.ModelSpace],
                    bt[BlockTableRecord.PaperSpace]
                };


                foreach (
                    ObjectId spaceId
                    in spaces)
                {
                    BlockTableRecord btr =
                        trans.GetObject(
                            spaceId,
                            OpenMode.ForRead)
                        as BlockTableRecord;


                    if (btr == null)
                    {
                        continue;
                    }


                    foreach (
                        ObjectId id
                        in btr)
                    {
                        Entity ent =
                            trans.GetObject(
                                id,
                                OpenMode.ForRead)
                            as Entity;


                        if (ent == null)
                        {
                            continue;
                        }


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


        //==================================================
        // 新版：
        // 读取项目号 + 所属Layout + 坐标
        //
        // 解决：
        // Layout2的修正写到当前Layout的问题
        //==================================================

        public List<ProjectNumberLocation>
            ReadProjectLocations(
                Database db)
        {
            List<ProjectNumberLocation> locations =
                new List<ProjectNumberLocation>();


            if (db == null)
            {
                return locations;
            }


            using (
                Transaction trans =
                    db.TransactionManager
                        .StartTransaction())
            {
                DBDictionary layoutDictionary =
                    trans.GetObject(
                        db.LayoutDictionaryId,
                        OpenMode.ForRead)
                    as DBDictionary;


                if (layoutDictionary == null)
                {
                    return locations;
                }


                //==================================================
                // 真正逐个遍历Layout
                //==================================================

                foreach (
                    DBDictionaryEntry entry
                    in layoutDictionary)
                {
                    Layout layout =
                        trans.GetObject(
                            entry.Value,
                            OpenMode.ForRead)
                        as Layout;


                    if (layout == null)
                    {
                        continue;
                    }


                    BlockTableRecord space =
                        trans.GetObject(
                            layout.BlockTableRecordId,
                            OpenMode.ForRead)
                        as BlockTableRecord;


                    if (space == null)
                    {
                        continue;
                    }


                    HashSet<ObjectId> activeBlockDefinitions =
                        new HashSet<ObjectId>();


                    foreach (
                        ObjectId entityId
                        in space)
                    {
                        Entity entity;


                        try
                        {
                            entity =
                                trans.GetObject(
                                    entityId,
                                    OpenMode.ForRead)
                                as Entity;
                        }
                        catch
                        {
                            continue;
                        }


                        AddProjectLocation(
                            entity,
                            trans,
                            locations,
                            Matrix3d.Identity,
                            layout.LayoutName,
                            activeBlockDefinitions,
                            0);
                    }
                }


                trans.Commit();
            }


            return locations;
        }


        //==================================================
        // AddProjectLocation 重载1
        //
        // 负责：
        // Entity / Block递归
        //
        // 注意：
        // 最后一个参数layoutName必须一直传下去
        //==================================================

        private void AddProjectLocation(
    Entity entity,
    Transaction trans,
    List<ProjectNumberLocation> locations,
    Matrix3d transform,
    string layoutName,
    HashSet<ObjectId> activeBlockDefinitions,
    int depth)
        {
            if (entity == null ||
                trans == null ||
                locations == null)
            {
                return;
            }


            //==================================================
            // 防止极端异常图纸无限递归
            //==================================================

            if (depth > 20)
            {
                return;
            }


            //==================================================
            // AttributeDefinition只是块定义中的属性模板。
            //
            // 例如默认值可能写着：
            //
            // N2607US004
            //
            // 但实际块实例AttributeReference可能已经改成：
            //
            // N2608US001
            //
            // 所以不能把AttributeDefinition当成真实项目号。
            //==================================================

            if (entity is AttributeDefinition)
            {
                return;
            }


            //==================================================
            // 1. DBText
            //==================================================

            DBText text =
                entity as DBText;


            if (text != null)
            {
                AddProjectLocation(
                    text.TextString,
                    text.Position
                        .TransformBy(
                            transform),
                    locations,
                    layoutName);


                return;
            }


            //==================================================
            // 2. MText
            //==================================================

            MText mtext =
                entity as MText;


            if (mtext != null)
            {
                AddProjectLocation(
                    mtext.Text,
                    mtext.Location
                        .TransformBy(
                            transform),
                    locations,
                    layoutName);


                return;
            }


            //==================================================
            // 3. BlockReference
            //==================================================

            BlockReference block =
                entity as BlockReference;


            if (block == null)
            {
                return;
            }


            //==================================================
            // 先读取这个Block实例真正的AttributeReference。
            //
            // 这里读取的是实例值，不是AttributeDefinition默认值。
            //==================================================

            ReadBlockAttributeLocations(
                block,
                trans,
                locations,
                transform,
                layoutName);


            //==================================================
            // 再读取块定义中的固定DBText/MText和嵌套块。
            //==================================================

            BlockTableRecord btr;


            try
            {
                btr =
                    trans.GetObject(
                        block.BlockTableRecord,
                        OpenMode.ForRead)
                    as BlockTableRecord;
            }
            catch
            {
                return;
            }


            if (btr == null ||
                btr.IsFromExternalReference)
            {
                return;
            }


            ObjectId definitionId =
                block.BlockTableRecord;


            //==================================================
            // 防止循环块定义
            //==================================================

            if (activeBlockDefinitions != null &&
                activeBlockDefinitions.Contains(
                    definitionId))
            {
                return;
            }


            //==================================================
            // 当前块内部实体转换到外层Layout坐标。
            //
            // 与CadTableReader保持相同变换顺序。
            //==================================================

            Matrix3d blockTransform =
                transform *
                block.BlockTransform;


            if (activeBlockDefinitions != null)
            {
                activeBlockDefinitions.Add(
                    definitionId);
            }


            try
            {
                foreach (
                    ObjectId id
                    in btr)
                {
                    Entity child;


                    try
                    {
                        child =
                            trans.GetObject(
                                id,
                                OpenMode.ForRead)
                            as Entity;
                    }
                    catch
                    {
                        continue;
                    }


                    AddProjectLocation(
                        child,
                        trans,
                        locations,
                        blockTransform,
                        layoutName,
                        activeBlockDefinitions,
                        depth + 1);
                }
            }
            finally
            {
                if (activeBlockDefinitions != null)
                {
                    activeBlockDefinitions.Remove(
                        definitionId);
                }
            }
        }

        private void ReadBlockAttributeLocations(
    BlockReference block,
    Transaction trans,
    List<ProjectNumberLocation> locations,
    Matrix3d parentTransform,
    string layoutName)
        {
            if (block == null ||
                trans == null ||
                locations == null)
            {
                return;
            }


            try
            {
                foreach (
                    ObjectId attributeId
                    in block.AttributeCollection)
                {
                    AttributeReference attribute =
                        trans.GetObject(
                            attributeId,
                            OpenMode.ForRead)
                        as AttributeReference;


                    if (attribute == null ||
                        string.IsNullOrWhiteSpace(
                            attribute.TextString))
                    {
                        continue;
                    }


                    //==================================================
                    // AttributeReference的位置已经包含
                    // 当前BlockReference本身的实例变换。
                    //
                    // 如果当前BlockReference又位于外层块中，
                    // 这里只应用外层parentTransform。
                    //==================================================

                    Point3d position =
                        attribute.Position
                            .TransformBy(
                                parentTransform);


                    AddProjectLocation(
                        attribute.TextString,
                        position,
                        locations,
                        layoutName);
                }
            }
            catch
            {
                //==================================================
                // 某一个异常属性不能导致整张图项目号读取失败。
                //==================================================
            }
        }


        //==================================================
        // AddProjectLocation 重载2
        //
        // 负责：
        // 已经得到文字后，
        // 判断是不是项目号并记录。
        //==================================================

        private void AddProjectLocation(
            string text,
            Point3d position,
            List<ProjectNumberLocation> locations,
            string layoutName)
        {
            if (string.IsNullOrEmpty(
                    text))
            {
                return;
            }


            text =
                text
                    .Replace(
                        "\\P",
                        "")
                    .Trim();


            if (!IsProjectNumber(
                    text))
            {
                return;
            }


            string projectNumber =
                GetProjectNumber(
                    text);


            if (string.IsNullOrEmpty(
                    projectNumber))
            {
                return;
            }


            locations.Add(
                new ProjectNumberLocation
                {
                    ProjectNumber =
                        projectNumber,

                    Position =
                        position,

                    LayoutName =
                        layoutName ?? ""
                });
        }


        //==================================================
        // 项目号判断
        //==================================================

        private static bool IsProjectNumber(
            string text)
        {
            if (string.IsNullOrEmpty(
                    text))
            {
                return false;
            }


            text =
                text
                    .Trim()
                    .ToUpper();


            return Regex.IsMatch(
                text,
                @"N\d{4}[A-Z]{2}\d{3}(-[A-Z0-9]+)?");
        }


        private static string GetProjectNumber(
            string text)
        {
            if (string.IsNullOrWhiteSpace(
                    text))
            {
                return null;
            }


            Match match =
                Regex.Match(
                    text.ToUpper(),
                    @"N\d{4}[A-Z]{2}\d{3}");


            return match.Success
                ? match.Value
                : null;
        }


        //==================================================
        // 原有ReadProjects辅助方法
        //==================================================

        private void AddProject(
            string text,
            List<string> projects)
        {
            if (string.IsNullOrEmpty(
                    text))
            {
                return;
            }


            text =
                text
                    .Replace(
                        "\\P",
                        "")
                    .Trim();


            if (!IsProjectNumber(
                    text))
            {
                return;
            }


            string projectNumber =
                GetProjectNumber(
                    text);


            if (!string.IsNullOrEmpty(
                    projectNumber))
            {
                projects.Add(
                    projectNumber);
            }
        }


        private void ReadBlock(
            BlockReference block,
            Transaction trans,
            List<string> projects)
        {
            if (block == null)
            {
                return;
            }


            BlockTableRecord btr;


            try
            {
                btr =
                    trans.GetObject(
                        block.BlockTableRecord,
                        OpenMode.ForRead)
                    as BlockTableRecord;
            }
            catch
            {
                return;
            }


            if (btr == null ||
                btr.IsFromExternalReference)
            {
                return;
            }


            foreach (
                ObjectId id
                in btr)
            {
                Entity ent =
                    trans.GetObject(
                        id,
                        OpenMode.ForRead)
                    as Entity;


                if (ent == null)
                {
                    continue;
                }


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
                else if (ent is BlockReference childBlock)
                {
                    //--------------------------------
                    // 顺便补上嵌套块递归
                    //--------------------------------

                    ReadBlock(
                        childBlock,
                        trans,
                        projects);
                }
            }
        }
    }
}