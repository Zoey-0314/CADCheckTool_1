using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Correct_test1.Core;
using Correct_test1.Models;
using System;
using System.Collections.Generic;

namespace Correct_test1.Readers
{
    /// <summary>
    /// 从图纸中识别零件序号标注（PartCallout）。
    ///
    /// 识别拓扑结构：
    ///   纯整数文字
    ///   → 附近短水平搁架线（Line）
    ///   → 搁架线端点连接/接近较长引出线（Line 或 Polyline 段）
    ///   → 引出线向远离文字方向延伸
    ///
    /// 容差说明：
    ///   MaxTextToShelfDistance = 15.0
    ///     覆盖文字高度(3-5mm) + 文字到搁架间隙(0-3mm) + 上下叠放时的堆叠距离(≤10mm)
    ///   MaxShelfLength = 25.0
    ///     短水平线通常与文字同宽(5-15mm)；超过25mm的线更可能是图框/BOM边界
    ///   MaxShelfAngleSin = 0.26
    ///     允许搁架线最多偏水平15°（sin15°≈0.26）以容纳轻微倾斜
    ///   MaxEndpointTolerance = 2.5
    ///     搁架端点与引出线端点允许的最大距离(mm)，对应约0.5~1mm制图误差
    ///   MinLeaderLength = 8.0
    ///     引出线至少8mm才认为是有效指向，排除短横线自身误匹配
    /// </summary>
    public class PartCalloutReader
    {
        // ── 几何容差常量 ──────────────────────────────────────────────
        private const double MaxTextToShelfDistance = 15.0;
        private const double MaxShelfLength = 25.0;
        private const double MaxShelfAngleSin = 0.26;   // sin(15°)
        private const double MaxEndpointTolerance = 2.5;
        private const double MinLeaderLength = 8.0;
        private const double BomRegionMargin = 5.0;     // BOM边界扩展余量

        // ── 内部辅助结构 ──────────────────────────────────────────────

        private struct TextCandidate
        {
            public ObjectId Id;
            public string LayoutName;
            public Point3d Position;
            public int Number;
        }

        private struct LineSeg
        {
            public ObjectId Id;
            public Point3d Start;
            public Point3d End;
            public double Length;
        }

        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 读取图纸中所有合法零件序号标注。
        /// </summary>
        /// <param name="db">CAD数据库</param>
        /// <param name="boms">已解析的BOM列表，用于排除BOM表内文字</param>
        public List<PartCallout> Read(Database db, List<BomData> boms)
        {
            List<PartCallout> result = new List<PartCallout>();
            if (db == null)
                return result;

            // 1. 计算BOM表排除区域
            List<Extents3d> bomRegions = BuildBomRegions(boms);

            // 2. 遍历所有BlockTableRecord（包含ModelSpace和PaperSpace），一次性收集实体
            //    与CadTableReader保持一致，扫描全部BTR
            List<TextCandidate> textCandidates = new List<TextCandidate>();
            List<LineSeg> allLineSegs = new List<LineSeg>();
            List<Extents3d> dimensionExtents = new List<Extents3d>();
            HashSet<ObjectId> dimensionSubTextIds = new HashSet<ObjectId>();

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = tr.GetObject(
                        db.BlockTableId, OpenMode.ForRead) as BlockTable;

                    if (bt == null)
                    {
                        tr.Commit();
                        return result;
                    }

                    foreach (ObjectId btrId in bt)
                    {
                        BlockTableRecord btr = tr.GetObject(
                            btrId, OpenMode.ForRead) as BlockTableRecord;

                        if (btr == null)
                            continue;

                        string layoutName = btr.Name ?? "";

                        CollectEntities(
                            tr, btr, layoutName,
                            textCandidates, allLineSegs,
                            dimensionExtents, dimensionSubTextIds);
                    }

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "PartCalloutReader.Read");
                return result;
            }

            // 3. 过滤文字候选：排除BOM区域、Dimension实体
            List<TextCandidate> validTexts = new List<TextCandidate>();
            foreach (TextCandidate tc in textCandidates)
            {
                if (IsInBomRegion(tc.Position, bomRegions))
                    continue;
                if (IsNearDimension(tc.Position, tc.Id, dimensionExtents, dimensionSubTextIds))
                    continue;
                validTexts.Add(tc);
            }

            // 4. 提取水平短线（搁架线候选）
            List<LineSeg> shelfCandidates = new List<LineSeg>();
            foreach (LineSeg seg in allLineSegs)
            {
                if (IsHorizontalShelf(seg))
                    shelfCandidates.Add(seg);
            }

            // 5. 对每个有效文字候选，寻找拓扑结构
            foreach (TextCandidate tc in validTexts)
            {
                PartCallout callout = TryBuildCallout(tc, shelfCandidates, allLineSegs);
                if (callout != null)
                    result.Add(callout);
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────
        // 实体收集
        // ─────────────────────────────────────────────────────────────

        private void CollectEntities(
            Transaction tr,
            BlockTableRecord btr,
            string layoutName,
            List<TextCandidate> textCandidates,
            List<LineSeg> lineSegs,
            List<Extents3d> dimensionExtents,
            HashSet<ObjectId> dimensionSubTextIds)
        {
            foreach (ObjectId id in btr)
            {
                Entity ent = null;
                try
                {
                    ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                }
                catch
                {
                    continue;
                }

                if (ent == null)
                    continue;

                // DBText
                DBText dbText = ent as DBText;
                if (dbText != null)
                {
                    int num;
                    string cleaned = CadTextCleaner.Clean(dbText.TextString);
                    if (IsPurePositiveInteger(cleaned, out num))
                    {
                        textCandidates.Add(new TextCandidate
                        {
                            Id = id,
                            LayoutName = layoutName,
                            Position = dbText.Position,
                            Number = num
                        });
                    }
                    continue;
                }

                // MText
                MText mtext = ent as MText;
                if (mtext != null)
                {
                    int num;
                    string cleaned = CadTextCleaner.Clean(mtext.Contents);
                    if (IsPurePositiveInteger(cleaned, out num))
                    {
                        textCandidates.Add(new TextCandidate
                        {
                            Id = id,
                            LayoutName = layoutName,
                            Position = mtext.Location,
                            Number = num
                        });
                    }
                    continue;
                }

                // Line → 收集线段
                Line line = ent as Line;
                if (line != null)
                {
                    lineSegs.Add(new LineSeg
                    {
                        Id = id,
                        Start = line.StartPoint,
                        End = line.EndPoint,
                        Length = line.Length
                    });
                    continue;
                }

                // Polyline → 拆分为段
                Polyline pline = ent as Polyline;
                if (pline != null)
                {
                    for (int i = 0; i < pline.NumberOfVertices - 1; i++)
                    {
                        Point3d p1 = pline.GetPoint3dAt(i);
                        Point3d p2 = pline.GetPoint3dAt(i + 1);
                        double len = p1.DistanceTo(p2);
                        if (len > 0.001)
                        {
                            lineSegs.Add(new LineSeg
                            {
                                Id = id,
                                Start = p1,
                                End = p2,
                                Length = len
                            });
                        }
                    }
                    continue;
                }

                // Dimension → 收集几何范围，用于排除其文字
                Dimension dim = ent as Dimension;
                if (dim != null)
                {
                    try
                    {
                        Extents3d ext = dim.GeometricExtents;
                        // 略微扩展边界以覆盖测量文字
                        Extents3d expanded = new Extents3d(
                            new Point3d(ext.MinPoint.X - 5, ext.MinPoint.Y - 5, ext.MinPoint.Z),
                            new Point3d(ext.MaxPoint.X + 5, ext.MaxPoint.Y + 5, ext.MaxPoint.Z));
                        dimensionExtents.Add(expanded);
                    }
                    catch { }
                    continue;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // BOM 区域计算
        // ─────────────────────────────────────────────────────────────

        private List<Extents3d> BuildBomRegions(List<BomData> boms)
        {
            List<Extents3d> regions = new List<Extents3d>();
            if (boms == null)
                return regions;

            foreach (BomData bom in boms)
            {
                if (bom == null || bom.Items == null || bom.Items.Count == 0)
                    continue;

                double minX = double.MaxValue, minY = double.MaxValue;
                double maxX = double.MinValue, maxY = double.MinValue;
                bool hasPoints = false;

                foreach (BomItem item in bom.Items)
                {
                    ExpandBounds(item.NoCellPosition,          ref minX, ref minY, ref maxX, ref maxY, ref hasPoints);
                    ExpandBounds(item.PartNumberCellPosition,  ref minX, ref minY, ref maxX, ref maxY, ref hasPoints);
                    ExpandBounds(item.NameCellPosition,        ref minX, ref minY, ref maxX, ref maxY, ref hasPoints);
                }

                if (hasPoints)
                {
                    regions.Add(new Extents3d(
                        new Point3d(minX - BomRegionMargin, minY - BomRegionMargin, 0),
                        new Point3d(maxX + BomRegionMargin, maxY + BomRegionMargin, 0)));
                }
            }

            return regions;
        }

        private static void ExpandBounds(
            Point3d pt,
            ref double minX, ref double minY,
            ref double maxX, ref double maxY,
            ref bool hasPoints)
        {
            if (pt == Point3d.Origin && !hasPoints)
                return;
            hasPoints = true;
            if (pt.X < minX) minX = pt.X;
            if (pt.Y < minY) minY = pt.Y;
            if (pt.X > maxX) maxX = pt.X;
            if (pt.Y > maxY) maxY = pt.Y;
        }

        private static bool IsInBomRegion(Point3d pos, List<Extents3d> regions)
        {
            foreach (Extents3d ext in regions)
            {
                if (pos.X >= ext.MinPoint.X && pos.X <= ext.MaxPoint.X &&
                    pos.Y >= ext.MinPoint.Y && pos.Y <= ext.MaxPoint.Y)
                    return true;
            }
            return false;
        }

        // ─────────────────────────────────────────────────────────────
        // Dimension 排除
        // ─────────────────────────────────────────────────────────────

        private static bool IsNearDimension(
            Point3d pos,
            ObjectId id,
            List<Extents3d> dimensionExtents,
            HashSet<ObjectId> dimensionSubTextIds)
        {
            if (dimensionSubTextIds.Contains(id))
                return true;

            foreach (Extents3d ext in dimensionExtents)
            {
                if (pos.X >= ext.MinPoint.X && pos.X <= ext.MaxPoint.X &&
                    pos.Y >= ext.MinPoint.Y && pos.Y <= ext.MaxPoint.Y)
                    return true;
            }
            return false;
        }

        // ─────────────────────────────────────────────────────────────
        // 拓扑识别
        // ─────────────────────────────────────────────────────────────

        private static bool IsHorizontalShelf(LineSeg seg)
        {
            if (seg.Length < 0.5 || seg.Length > MaxShelfLength)
                return false;

            double dy = Math.Abs(seg.End.Y - seg.Start.Y);
            // sin(angle from horizontal) = dy / length < MaxShelfAngleSin
            return (dy / seg.Length) < MaxShelfAngleSin;
        }

        private PartCallout TryBuildCallout(
            TextCandidate tc,
            List<LineSeg> shelfCandidates,
            List<LineSeg> allLineSegs)
        {
            foreach (LineSeg shelf in shelfCandidates)
            {
                // 文字必须在搁架线附近（上方或正上方）
                double distToShelf = DistancePointToSegment(tc.Position, shelf.Start, shelf.End);
                if (distToShelf > MaxTextToShelfDistance)
                    continue;

                // 文字应在搁架线上方（Y坐标大于搁架中心Y），允许小误差
                double shelfCenterY = (shelf.Start.Y + shelf.End.Y) / 2.0;
                if (tc.Position.Y < shelfCenterY - MaxTextToShelfDistance)
                    continue;

                // 寻找与搁架端点连接的引出线
                ObjectId leaderId = FindLeaderFromShelf(shelf, allLineSegs, tc.Position);
                if (leaderId.IsNull)
                    continue;

                // 找到合法拓扑 → 构建 PartCallout
                return new PartCallout
                {
                    Number = tc.Number,
                    LayoutName = tc.LayoutName,
                    TextPosition = tc.Position,
                    TextObjectId = tc.Id,
                    HorizontalLineObjectId = shelf.Id,
                    LeaderObjectId = leaderId
                };
            }

            return null;
        }

        /// <summary>
        /// 寻找与搁架线端点相连的引出线（比搁架线更长，且向远离文字方向延伸）。
        /// </summary>
        private static ObjectId FindLeaderFromShelf(
            LineSeg shelf,
            List<LineSeg> allLineSegs,
            Point3d textPos)
        {
            Point3d[] shelfEndpoints = { shelf.Start, shelf.End };

            foreach (LineSeg seg in allLineSegs)
            {
                if (seg.Id == shelf.Id)
                    continue;
                if (seg.Length < MinLeaderLength)
                    continue;

                // 检查引出线的某端点是否接近搁架线端点
                foreach (Point3d shelfPt in shelfEndpoints)
                {
                    bool startClose = shelfPt.DistanceTo(seg.Start) <= MaxEndpointTolerance;
                    bool endClose   = shelfPt.DistanceTo(seg.End)   <= MaxEndpointTolerance;

                    if (!startClose && !endClose)
                        continue;

                    // 引出线的另一端应比文字更远（引出线向远处延伸）
                    Point3d nearEnd = startClose ? seg.Start : seg.End;
                    Point3d farEnd  = startClose ? seg.End   : seg.Start;

                    double distNear = textPos.DistanceTo(nearEnd);
                    double distFar  = textPos.DistanceTo(farEnd);

                    if (distFar > distNear)
                        return seg.Id;
                }
            }

            return ObjectId.Null;
        }

        // ─────────────────────────────────────────────────────────────
        // 工具方法
        // ─────────────────────────────────────────────────────────────

        private static bool IsPurePositiveInteger(string text, out int number)
        {
            number = 0;
            if (string.IsNullOrEmpty(text))
                return false;
            if (!int.TryParse(text, out number))
                return false;
            return number > 0;
        }

        /// <summary>点到线段的最短距离（2D，忽略Z）</summary>
        private static double DistancePointToSegment(Point3d pt, Point3d segA, Point3d segB)
        {
            double dx = segB.X - segA.X;
            double dy = segB.Y - segA.Y;
            double lenSq = dx * dx + dy * dy;

            if (lenSq < 1e-10)
                return pt.DistanceTo(segA);

            double t = ((pt.X - segA.X) * dx + (pt.Y - segA.Y) * dy) / lenSq;
            t = Math.Max(0, Math.Min(1, t));

            double projX = segA.X + t * dx;
            double projY = segA.Y + t * dy;

            double ddx = pt.X - projX;
            double ddy = pt.Y - projY;
            return Math.Sqrt(ddx * ddx + ddy * ddy);
        }
    }
}
