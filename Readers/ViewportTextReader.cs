using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Correct_test1.Models;

using System;
using System.Collections.Generic;


namespace Correct_test1.Readers
{
    public class ViewportTextReader
    {
        public List<TitleText> Read(
            Database db,
            bool includeNestedBlocks = true,
            bool useViewportFilter = true)
        {
            List<TitleText> result =
                new List<TitleText>();


            if (db == null)
            {
                return result;
            }


            using (
                Transaction tr =
                    db.TransactionManager
                        .StartTransaction())
            {
                DBDictionary layoutDict =
                    tr.GetObject(
                        db.LayoutDictionaryId,
                        OpenMode.ForRead)
                    as DBDictionary;


                if (layoutDict == null)
                {
                    return result;
                }


                BlockTableRecord modelSpace =
                    tr.GetObject(
                        SymbolUtilityServices
                            .GetBlockModelSpaceId(db),
                        OpenMode.ForRead)
                    as BlockTableRecord;


                if (modelSpace == null)
                {
                    return result;
                }


                //==================================================
                // 模型空间文字只扫描一次
                //==================================================

                List<TitleText> modelTexts =
                    new List<TitleText>();


                HashSet<ObjectId> activeBlocks =
                    new HashSet<ObjectId>();


                foreach (
                    ObjectId id
                    in modelSpace)
                {
                    Entity entity;


                    try
                    {
                        entity =
                            tr.GetObject(
                                id,
                                OpenMode.ForRead)
                            as Entity;
                    }
                    catch
                    {
                        continue;
                    }


                    if (entity == null)
                    {
                        continue;
                    }


                    CollectEntityText(
                        tr,
                        entity,
                        Matrix3d.Identity,
                        includeNestedBlocks,
                        activeBlocks,
                        modelTexts);
                }


                //==================================================
                // 遍历所有Layout
                //==================================================

                foreach (
                    DBDictionaryEntry entry
                    in layoutDict)
                {
                    Layout layout =
                        tr.GetObject(
                            entry.Value,
                            OpenMode.ForRead)
                        as Layout;


                    if (layout == null ||
                        layout.ModelType)
                    {
                        continue;
                    }


                    BlockTableRecord paperSpace =
                        tr.GetObject(
                            layout.BlockTableRecordId,
                            OpenMode.ForRead)
                        as BlockTableRecord;


                    if (paperSpace == null)
                    {
                        continue;
                    }


                    //==================================================
                    // 尽量取得图纸空间自己的主视口
                    //==================================================

                    ObjectId paperViewportId =
                        ObjectId.Null;


                    try
                    {
                        ObjectIdCollection knownViewports =
                            layout.GetViewports();


                        if (knownViewports != null &&
                            knownViewports.Count > 0)
                        {
                            paperViewportId =
                                knownViewports[0];
                        }
                    }
                    catch
                    {
                    }


                    bool skippedFallbackPaperViewport =
                        false;


                    //==================================================
                    // 直接遍历Layout对应PaperSpace里的所有Viewport
                    //==================================================

                    foreach (
                        ObjectId entityId
                        in paperSpace)
                    {
                        Viewport viewport;


                        try
                        {
                            viewport =
                                tr.GetObject(
                                    entityId,
                                    OpenMode.ForRead)
                                as Viewport;
                        }
                        catch
                        {
                            continue;
                        }


                        if (viewport == null)
                        {
                            continue;
                        }


                        //==================================================
                        // 跳过PaperSpace自己的主视口
                        //==================================================

                        if (!paperViewportId.IsNull &&
                            entityId == paperViewportId)
                        {
                            continue;
                        }


                        //==================================================
                        // 后台Database中GetViewports可能取不到。
                        //
                        // 此时PaperSpace BTR中的第一个Viewport
                        // 作为主视口跳过。
                        //==================================================

                        if (paperViewportId.IsNull &&
                            !skippedFallbackPaperViewport)
                        {
                            skippedFallbackPaperViewport =
                                true;

                            continue;
                        }


                        //==================================================
                        // 不判断viewport.On。
                        //
                        // 后台批量读取非当前Layout时，
                        // On状态不适合作为是否读取的依据。
                        //==================================================

                        if (viewport.CustomScale <= 0)
                        {
                            continue;
                        }


                        ModelWindow window =
                            CreateWindow(
                                viewport);


                        foreach (
                            TitleText text
                            in modelTexts)
                        {
                            if (text == null)
                            {
                                continue;
                            }


                            if (useViewportFilter &&
                                !window.Contains(
                                    text.X,
                                    text.Y))
                            {
                                continue;
                            }


                            result.Add(
                                new TitleText
                                {
                                    Text =
                                        text.Text,

                                    X =
                                        text.X,

                                    Y =
                                        text.Y,

                                    Height =
                                        text.Height,

                                    LayoutName =
                                        layout.LayoutName,

                                    ViewportId =
                                        viewport.ObjectId
                                });
                        }
                    }
                }


                tr.Commit();
            }


            return result;
        }


        //==================================================
        // 建立视口对应的模型空间范围
        //==================================================

        private static ModelWindow CreateWindow(
            Viewport viewport)
        {
            double modelHeight =
                viewport.ViewHeight;


            double modelWidth =
                viewport.Width /
                viewport.CustomScale;


            double minX =
                viewport.ViewCenter.X -
                modelWidth / 2.0;


            double maxX =
                viewport.ViewCenter.X +
                modelWidth / 2.0;


            double minY =
                viewport.ViewCenter.Y -
                modelHeight / 2.0;


            double maxY =
                viewport.ViewCenter.Y +
                modelHeight / 2.0;


            return
                new ModelWindow(
                    minX,
                    minY,
                    maxX,
                    maxY);
        }


        //==================================================
        // 读取实体文字
        //==================================================

        private static void CollectEntityText(
            Transaction tr,
            Entity entity,
            Matrix3d transform,
            bool includeNestedBlocks,
            HashSet<ObjectId> activeBlocks,
            List<TitleText> output)
        {
            if (entity == null ||
                output == null)
            {
                return;
            }


            //==================================================
            // 1. DBText
            //==================================================

            DBText dbText =
                entity as DBText;


            if (dbText != null)
            {
                Point3d position =
                    dbText.Position
                        .TransformBy(
                            transform);


                output.Add(
                    new TitleText
                    {
                        Text =
                            Clean(
                                dbText.TextString),

                        X =
                            position.X,

                        Y =
                            position.Y,

                        Height =
                            dbText.Height
                    });


                return;
            }


            //==================================================
            // 2. MText
            //==================================================

            MText mText =
                entity as MText;


            if (mText != null)
            {
                Point3d position =
                    mText.Location
                        .TransformBy(
                            transform);


                output.Add(
                    new TitleText
                    {
                        Text =
                            Clean(
                                mText.Text),

                        X =
                            position.X,

                        Y =
                            position.Y,

                        Height =
                            mText.TextHeight
                    });


                return;
            }


            //==================================================
            // 3. AutoCAD原生MLeader
            //==================================================

            MLeader mLeader =
                entity as MLeader;


            if (mLeader != null)
            {
                if (mLeader.ContentType ==
                    ContentType.MTextContent)
                {
                    MText leaderText =
                        null;


                    try
                    {
                        leaderText =
                            mLeader.MText;


                        if (leaderText != null)
                        {
                            Point3d position;

                            try
                            {
                                Extents3d extents =
                                    leaderText.GeometricExtents;

                                Point3d minPoint =
                                    extents.MinPoint
                                        .TransformBy(
                                            transform);

                                Point3d maxPoint =
                                    extents.MaxPoint
                                        .TransformBy(
                                            transform);

                                position =
                                    new Point3d(
                                        (minPoint.X + maxPoint.X) / 2.0,
                                        (minPoint.Y + maxPoint.Y) / 2.0,
                                        0);
                            }
                            catch
                            {
                                position =
                                    mLeader.TextLocation
                                        .TransformBy(
                                            transform);
                            }


                            output.Add(
                                new TitleText
                                {
                                    Text =
                                        Clean(
                                            leaderText.Text),

                                    X =
                                        position.X,

                                    Y =
                                        position.Y,

                                    Height =
                                        leaderText.TextHeight
                                });
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        if (leaderText != null)
                        {
                            leaderText.Dispose();
                        }
                    }
                }


                return;
            }


            //==================================================
            // 4. AutoCAD Mechanical AMDTNOTE
            //
            // 单张打开时可能是真实AMDTNOTE，
            // 批量后台Database中可能变成ProxyEntity。
            //==================================================

            if (IsAmdtNote(
                    entity))
            {
                CollectAmdtNoteText(
                    tr,
                    entity,
                    transform,
                    includeNestedBlocks,
                    activeBlocks,
                    output);


                return;
            }


            //==================================================
            // 5. 嵌套BlockReference
            //==================================================

            if (!includeNestedBlocks)
            {
                return;
            }


            BlockReference blockRef =
                entity as BlockReference;


            if (blockRef == null)
            {
                return;
            }


            ObjectId blockId =
                blockRef.BlockTableRecord;


            if (blockId.IsNull)
            {
                return;
            }


            if (activeBlocks != null &&
                activeBlocks.Contains(
                    blockId))
            {
                return;
            }


            BlockTableRecord blockDef;


            try
            {
                blockDef =
                    tr.GetObject(
                        blockId,
                        OpenMode.ForRead)
                    as BlockTableRecord;
            }
            catch
            {
                return;
            }


            if (blockDef == null ||
                blockDef.IsFromExternalReference)
            {
                return;
            }


            if (activeBlocks != null)
            {
                activeBlocks.Add(
                    blockId);
            }


            try
            {
                Matrix3d nestedTransform =
                    transform *
                    blockRef.BlockTransform;


                foreach (
                    ObjectId childId
                    in blockDef)
                {
                    Entity child;


                    try
                    {
                        child =
                            tr.GetObject(
                                childId,
                                OpenMode.ForRead)
                            as Entity;
                    }
                    catch
                    {
                        continue;
                    }


                    if (child == null)
                    {
                        continue;
                    }


                    CollectEntityText(
                        tr,
                        child,
                        nestedTransform,
                        includeNestedBlocks,
                        activeBlocks,
                        output);
                }
            }
            finally
            {
                if (activeBlocks != null)
                {
                    activeBlocks.Remove(
                        blockId);
                }
            }
        }


        //==================================================
        // 判断实体是不是Mechanical AMDTNOTE
        //==================================================

        private static bool IsAmdtNote(
            Entity entity)
        {
            if (entity == null)
            {
                return false;
            }


            //==================================================
            // 后台批量Database：
            // Mechanical对象可能成为ProxyEntity
            //==================================================

            ProxyEntity proxy =
                entity as ProxyEntity;


            if (proxy != null)
            {
                try
                {
                    return
                        string.Equals(
                            proxy.OriginalDxfName,
                            "AMDTNOTE",
                            StringComparison
                                .OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            }


            //==================================================
            // 正常Document中：
            // 直接读取真实DXF类型
            //==================================================

            try
            {
                Autodesk.AutoCAD.Runtime.RXClass rxClass =
                    entity.GetRXClass();


                if (rxClass == null)
                {
                    return false;
                }


                return
                    string.Equals(
                        rxClass.DxfName,
                        "AMDTNOTE",
                        StringComparison
                            .OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }


        //==================================================
        // 分解AMDTNOTE并读取其中的文字
        //==================================================

        private static void CollectAmdtNoteText(
            Transaction tr,
            Entity entity,
            Matrix3d transform,
            bool includeNestedBlocks,
            HashSet<ObjectId> activeBlocks,
            List<TitleText> output)
        {
            DBObjectCollection exploded =
                new DBObjectCollection();


            try
            {
                entity.Explode(
                    exploded);


                foreach (
                    DBObject obj
                    in exploded)
                {
                    Entity explodedEntity =
                        obj as Entity;


                    if (explodedEntity == null)
                    {
                        continue;
                    }


                    //==================================================
                    // 不自己重复判断TEXT/MTEXT。
                    //
                    // 直接重新走统一文字读取流程，
                    // 这样以后即使Explode结果是MLeader、
                    // BlockReference等也可以继续处理。
                    //==================================================

                    CollectEntityText(
                        tr,
                        explodedEntity,
                        transform,
                        includeNestedBlocks,
                        activeBlocks,
                        output);
                }
            }
            catch
            {
            }
            finally
            {
                foreach (
                    DBObject obj
                    in exploded)
                {
                    if (obj != null)
                    {
                        try
                        {
                            obj.Dispose();
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }


        //==================================================
        // 清理文字
        //==================================================

        private static string Clean(
            string text)
        {
            if (string.IsNullOrWhiteSpace(
                    text))
            {
                return string.Empty;
            }


            return
                text
                    .Replace(
                        "\\P",
                        "\n")
                    .Replace(
                        "\r\n",
                        "\n")
                    .Replace(
                        "\r",
                        "\n")
                    .Trim();
        }


        //==================================================
        // 视口模型范围
        //==================================================

        private struct ModelWindow
        {
            public ModelWindow(
                double minX,
                double minY,
                double maxX,
                double maxY)
            {
                MinX =
                    minX;

                MinY =
                    minY;

                MaxX =
                    maxX;

                MaxY =
                    maxY;
            }


            public double MinX
            {
                get;
            }


            public double MinY
            {
                get;
            }


            public double MaxX
            {
                get;
            }


            public double MaxY
            {
                get;
            }


            public bool Contains(
                double x,
                double y)
            {
                return
                    x >= MinX &&
                    x <= MaxX &&
                    y >= MinY &&
                    y <= MaxY;
            }
        }
    }
}