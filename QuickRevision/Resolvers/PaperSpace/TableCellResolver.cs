using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Correct_test1.QuickRevision.Models;

namespace Correct_test1.QuickRevision.Resolvers.PaperSpace
{
    /// <summary>
    /// Paper Space中的Table单元格解析器。
    /// 目标：
    /// 1. 根据点击位置找到具体Cell
    /// 2. 读取Cell文字
    /// 3. 优先通过Table.Explode()找到实际显示文字
    /// 4. 获取真实文字位置、宽度、高度、样式
    /// 5. 支持左/中/右对齐
    /// 当前仅处理水平Table。
    /// </summary>
    public class TableCellResolver
    {
        public RevisionTarget Resolve(
            Database database,
            Transaction transaction,
            ObjectId tableId,
            Point3d paperPoint)
        {
            if (database == null ||
                transaction == null ||
                tableId.IsNull ||
                !tableId.IsValid)
            {
                return null;
            }

            Table table =
                transaction.GetObject(
                    tableId,
                    OpenMode.ForRead)
                as Table;

            if (table == null)
                return null;


            // 找到点击的具体Cell

            int row;
            int column;

            double cellLeft;
            double cellRight;
            double cellBottom;
            double cellTop;

            bool foundCell =
                TryFindCell(
                    table,
                    paperPoint,
                    out row,
                    out column,
                    out cellLeft,
                    out cellRight,
                    out cellBottom,
                    out cellTop);

            if (!foundCell)
                return null;


            Cell cell =
                table.Cells[
                    row,
                    column];


            // 读取Cell文字

            string content;

            try
            {
                content =
                    CleanText(
                        cell.TextString);
            }
            catch (System.Exception)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(
                    content))
            {
                return null;
            }


            // 优先：
            //
            // Explode整个Table，
            // 找这个Cell里真正显示出来的
            // MText / DBText。
            //
            // 这样左对齐、居中、右对齐
            // 都不需要我们自己猜。

            Extents3d realTextExtents;

            double realTextHeight;

            ObjectId realTextStyleId;

            bool gotRealText =
                TryGetActualCellTextGeometry(
                    table,
                    paperPoint,
                    content,
                    cellLeft,
                    cellRight,
                    cellBottom,
                    cellTop,
                    out realTextExtents,
                    out realTextHeight,
                    out realTextStyleId);


            if (gotRealText)
            {
                double leftX =
                    realTextExtents.MinPoint.X;

                double rightX =
                    realTextExtents.MaxPoint.X;

                double bottomY =
                    realTextExtents.MinPoint.Y;

                double topY =
                    realTextExtents.MaxPoint.Y;


                if (IsValidRange(
                        leftX,
                        rightX,
                        bottomY,
                        topY))
                {
                    RevisionTarget target =
                        CreateTarget(
                            table,
                            row,
                            column,
                            content,
                            leftX,
                            rightX,
                            bottomY,
                            topY,
                            realTextHeight,
                            realTextStyleId);


                    AttachTableContext(
                        target,
                        table,
                        row,
                        column,
                        cellBottom,
                        cellTop);


                    return target;
                }
            }


            // Explode没有得到实际文字时，
            // 再使用Cell自身属性Fallback。

            RevisionTarget fallbackTarget =
    CreateFallbackTarget(
        database,
        table,
        cell,
        row,
        column,
        content,
        cellLeft,
        cellRight,
        cellBottom,
        cellTop);


            AttachTableContext(
                fallbackTarget,
                table,
                row,
                column,
                cellBottom,
                cellTop);


            return fallbackTarget;
        }


        /// <summary>
        /// 通过Table.Explode取得真正显示的文字。
        /// </summary>
        private static bool TryGetActualCellTextGeometry(
            Table table,
            Point3d clickPoint,
            string expectedText,
            double cellLeft,
            double cellRight,
            double cellBottom,
            double cellTop,
            out Extents3d bestExtents,
            out double bestTextHeight,
            out ObjectId bestTextStyleId)
        {
            bestExtents =
                new Extents3d();

            bestTextHeight =
                0;

            bestTextStyleId =
                ObjectId.Null;


            DBObjectCollection exploded =
                new DBObjectCollection();


            try
            {
                table.Explode(
                    exploded);


                if (exploded.Count == 0)
                    return false;


                bool found =
                    false;

                bool foundExactText =
                    false;

                double bestDistance =
                    double.MaxValue;


                foreach (DBObject obj
                    in exploded)
                {
                    string candidateText =
                        "";

                    Extents3d candidateExtents;

                    double candidateHeight =
                        0;

                    ObjectId candidateStyleId =
                        ObjectId.Null;


                    // MText

                    MText mtext =
    obj as MText;

                    if (mtext != null)
                    {
                        try
                        {
                            candidateText =
                                TextGeometryHelper.CleanText(
                                    mtext.Text);


                            candidateHeight =
                                mtext.TextHeight;


                            candidateStyleId =
                                mtext.TextStyleId;


                            // BOM的Table.Explode出来的MText
                            // 可能仍然保留整个Cell的定义宽度。
                            //
                            // 所以：
                            // 不再直接使用该MText自身的宽度。
                            //
                            // 用Cell真正的文字expectedText，
                            // 按实际字高和样式重新测量紧凑宽度。

                            double actualTextWidth;


                            bool measured =
                                TryMeasureTightTextWidth(
                                    table.Database,
                                    expectedText,
                                    candidateHeight,
                                    candidateStyleId,
                                    out actualTextWidth);


                            if (!measured ||
                                actualTextWidth <= 0)
                            {
                                continue;
                            }


                            // 高度仍然取原MText真实显示高度

                            double actualTextHeight =
                                mtext.ActualHeight;


                            if (!IsValidNumber(
                                    actualTextHeight) ||
                                actualTextHeight <= 0)
                            {
                                actualTextHeight =
                                    candidateHeight;
                            }


                            // 不再拿原MText的宽度，
                            // 只利用它的Location和Attachment
                            // 确定文字实际放置位置。

                            if (!TryBuildTightMTextExtents(
                                    mtext,
                                    actualTextWidth,
                                    actualTextHeight,
                                    out candidateExtents))
                            {
                                continue;
                            }
                        }
                        catch (System.Exception)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // DBText

                        DBText dbText =
                            obj as DBText;


                        if (dbText == null)
                            continue;


                        try
                        {
                            candidateText =
                                CleanText(
                                    dbText.TextString);

                            candidateExtents =
                                dbText.GeometricExtents;

                            candidateHeight =
                                dbText.Height;

                            candidateStyleId =
                                dbText.TextStyleId;
                        }
                        catch (System.Exception)
                        {
                            continue;
                        }
                    }


                    if (string.IsNullOrWhiteSpace(
                            candidateText))
                    {
                        continue;
                    }


                    if (!IsValidExtents(
                            candidateExtents))
                    {
                        continue;
                    }


                    // 文字中心必须位于当前Cell中。

                    Point3d center =
                        GetCenter(
                            candidateExtents);


                    if (!IsPointInsideCell(
                            center,
                            cellLeft,
                            cellRight,
                            cellBottom,
                            cellTop))
                    {
                        continue;
                    }


                    // 优先匹配Cell真实内容。

                    bool exactText =
                        string.Equals(
                            candidateText,
                            expectedText,
                            System.StringComparison.Ordinal);


                    // 已经找到完全匹配文字后，
                    // 不再接受不匹配文字。

                    if (foundExactText &&
                        !exactText)
                    {
                        continue;
                    }


                    // 第一次找到完全匹配：
                    // 清掉非完全匹配的距离优势。

                    if (exactText &&
                        !foundExactText)
                    {
                        foundExactText =
                            true;

                        bestDistance =
                            double.MaxValue;

                        found =
                            false;
                    }


                    double distance =
                        DistanceSquared(
                            center,
                            clickPoint);


                    if (distance <
                        bestDistance)
                    {
                        bestDistance =
                            distance;

                        bestExtents =
                            candidateExtents;

                        bestTextHeight =
                            candidateHeight;

                        bestTextStyleId =
                            candidateStyleId;

                        found =
                            true;
                    }
                }


                return found;
            }
            catch (System.Exception)
            {
                return false;
            }
            finally
            {
                // Explode出来的是临时DBObject，
                // 没有加入Database，全部释放。

                foreach (DBObject obj
                    in exploded)
                {
                    try
                    {
                        obj.Dispose();
                    }
                    catch (System.Exception)
                    {
                    }
                }
            }
        }

        /// <summary>
        /// 给TableCell的RevisionTarget附加表格上下文。
        /// 保存：
        /// TableId
        /// Row
        /// Column
        /// Table最右边X
        /// 当前行上下边界
        /// 当前行中心Y
        /// 后续ProjectNumberWriter直接使用这些信息，
        /// 不再重新分析Table。
        /// </summary>
        private static void AttachTableContext(
            RevisionTarget target,
            Table table,
            int row,
            int column,
            double selectedCellBottom,
            double selectedCellTop)
        {
            if (target == null ||
                table == null)
            {
                return;
            }


            double tableRightX;
            double rowBottomY;
            double rowTopY;


            bool gotGeometry =
                TryGetTableAndRowGeometry(
                    table,
                    row,
                    out tableRightX,
                    out rowBottomY,
                    out rowTopY);


            // 如果整行几何读取失败，
            // 至少使用当前选中Cell的上下范围。

            if (!gotGeometry)
            {
                rowBottomY =
                    selectedCellBottom;

                rowTopY =
                    selectedCellTop;


                // 再尝试只获取Table最右边界

                if (!TryGetTableRightX(
                        table,
                        out tableRightX))
                {
                    return;
                }
            }


            if (!IsValidNumber(tableRightX) ||
                !IsValidNumber(rowBottomY) ||
                !IsValidNumber(rowTopY) ||
                rowTopY < rowBottomY)
            {
                return;
            }


            TableRevisionContext context =
                new TableRevisionContext();


            context.TableId =
                table.ObjectId;


            context.Row =
                row;


            context.Column =
                column;


            context.TableRightX =
                tableRightX;


            context.RowBottomY =
                rowBottomY;


            context.RowTopY =
                rowTopY;


            context.RowCenterY =
                (
                    rowBottomY +
                    rowTopY
                ) / 2.0;


            target.TableContext =
                context;
        }
        /// <summary>
        /// 获取：
        /// 1. 整张Table最右侧X
        /// 2. 指定Row最下侧Y
        /// 3. 指定Row最上侧Y
        /// 直接复用已经验证正常的Cell.GetExtents()逻辑。
        /// </summary>
        private static bool TryGetTableAndRowGeometry(
            Table table,
            int targetRow,
            out double tableRightX,
            out double rowBottomY,
            out double rowTopY)
        {
            tableRightX =
                double.MinValue;

            rowBottomY =
                double.MaxValue;

            rowTopY =
                double.MinValue;


            if (table == null ||
                targetRow < 0 ||
                targetRow >= table.Rows.Count)
            {
                return false;
            }


            bool foundTableGeometry =
                false;

            bool foundRowGeometry =
                false;


            // 遍历所有Cell：
            //
            // 所有行负责找到Table最右边
            // 目标行负责找到Row上下边界

            for (int r = 0;
                r < table.Rows.Count;
                r++)
            {
                for (int c = 0;
                    c < table.Columns.Count;
                    c++)
                {
                    double left;
                    double right;
                    double bottom;
                    double top;


                    bool got =
                        TryGetCellExtents(
                            table,
                            r,
                            c,
                            out left,
                            out right,
                            out bottom,
                            out top);


                    if (!got)
                        continue;


                    // 整张表最右侧

                    if (right >
                        tableRightX)
                    {
                        tableRightX =
                            right;
                    }


                    foundTableGeometry =
                        true;


                    // 当前目标行

                    if (r ==
                        targetRow)
                    {
                        if (bottom <
                            rowBottomY)
                        {
                            rowBottomY =
                                bottom;
                        }


                        if (top >
                            rowTopY)
                        {
                            rowTopY =
                                top;
                        }


                        foundRowGeometry =
                            true;
                    }
                }
            }


            return
                foundTableGeometry &&
                foundRowGeometry &&
                IsValidNumber(tableRightX) &&
                IsValidNumber(rowBottomY) &&
                IsValidNumber(rowTopY) &&
                rowTopY >= rowBottomY;
        }
        /// <summary>
        /// 单独获取整张Table最右侧X。
        /// </summary>
        private static bool TryGetTableRightX(
            Table table,
            out double tableRightX)
        {
            tableRightX =
                double.MinValue;


            if (table == null)
                return false;


            bool found =
                false;


            for (int r = 0;
                r < table.Rows.Count;
                r++)
            {
                for (int c = 0;
                    c < table.Columns.Count;
                    c++)
                {
                    double left;
                    double right;
                    double bottom;
                    double top;


                    if (!TryGetCellExtents(
                            table,
                            r,
                            c,
                            out left,
                            out right,
                            out bottom,
                            out top))
                    {
                        continue;
                    }


                    if (right >
                        tableRightX)
                    {
                        tableRightX =
                            right;
                    }


                    found =
                        true;
                }
            }


            return
                found &&
                IsValidNumber(
                    tableRightX);
        }
        /// <summary>
        /// Explode失败后的Fallback。
        /// 此时读取Cell真实：
        /// TextHeight
        /// TextStyleId
        /// Alignment
        /// </summary>
        private static RevisionTarget CreateFallbackTarget(
            Database database,
            Table table,
            Cell cell,
            int row,
            int column,
            string content,
            double cellLeft,
            double cellRight,
            double cellBottom,
            double cellTop)
        {
            double cellWidth =
                cellRight -
                cellLeft;

            double cellHeight =
                cellTop -
                cellBottom;


            if (cellWidth <= 0 ||
                cellHeight <= 0)
            {
                return null;
            }


            // 实际文字高度

            double textHeight =
                GetCellTextHeight(
                    cell,
                    cellHeight);


            // 实际文字样式

            ObjectId textStyleId =
                GetCellTextStyleId(
                    cell);


            // 测量文字宽度

            double textWidth;

            bool measured =
                TryMeasureTextWidth(
                    database,
                    content,
                    textHeight,
                    textStyleId,
                    out textWidth);


            if (!measured)
            {
                textWidth =
                    EstimateTextWidth(
                        content,
                        textHeight);
            }


            if (!IsValidNumber(textWidth) ||
                textWidth <= 0)
            {
                return null;
            }


            // 防止异常超过整个单元格

            double maxWidth =
                cellWidth * 0.98;


            if (textWidth > maxWidth)
            {
                textWidth =
                    maxWidth;
            }


            // 获取Cell对齐方式

            string alignmentName =
                "";

            try
            {
                alignmentName =
                    cell.Alignment.ToString();
            }
            catch (System.Exception)
            {
                alignmentName =
                    "MiddleCenter";
            }


            // 少量边缘留白

            double horizontalPadding =
                textHeight * 0.35;

            double verticalPadding =
                textHeight * 0.20;


            // 水平方向

            double textLeft;
            double textRight;


            if (alignmentName.EndsWith(
                    "Left",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                textLeft =
                    cellLeft +
                    horizontalPadding;

                textRight =
                    textLeft +
                    textWidth;
            }
            else if (alignmentName.EndsWith(
                         "Right",
                         System.StringComparison.OrdinalIgnoreCase))
            {
                textRight =
                    cellRight -
                    horizontalPadding;

                textLeft =
                    textRight -
                    textWidth;
            }
            else
            {
                double centerX =
                    (
                        cellLeft +
                        cellRight
                    ) / 2.0;


                textLeft =
                    centerX -
                    textWidth / 2.0;

                textRight =
                    centerX +
                    textWidth / 2.0;
            }


            // 垂直方向

            double textBottom;
            double textTop;


            if (alignmentName.StartsWith(
                    "Top",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                textTop =
                    cellTop -
                    verticalPadding;

                textBottom =
                    textTop -
                    textHeight;
            }
            else if (alignmentName.StartsWith(
                         "Bottom",
                         System.StringComparison.OrdinalIgnoreCase))
            {
                textBottom =
                    cellBottom +
                    verticalPadding;

                textTop =
                    textBottom +
                    textHeight;
            }
            else
            {
                double centerY =
                    (
                        cellBottom +
                        cellTop
                    ) / 2.0;


                textBottom =
                    centerY -
                    textHeight / 2.0;

                textTop =
                    centerY +
                    textHeight / 2.0;
            }


            // 限制在Cell范围内

            if (textLeft < cellLeft)
                textLeft = cellLeft;

            if (textRight > cellRight)
                textRight = cellRight;

            if (textBottom < cellBottom)
                textBottom = cellBottom;

            if (textTop > cellTop)
                textTop = cellTop;


            return CreateTarget(
                table,
                row,
                column,
                content,
                textLeft,
                textRight,
                textBottom,
                textTop,
                textHeight,
                textStyleId);
        }


        private static RevisionTarget CreateTarget(
            Table table,
            int row,
            int column,
            string content,
            double leftX,
            double rightX,
            double bottomY,
            double topY,
            double textHeight,
            ObjectId textStyleId)
        {
            if (!IsValidRange(
                    leftX,
                    rightX,
                    bottomY,
                    topY))
            {
                return null;
            }


            if (!IsValidNumber(textHeight) ||
                textHeight <= 0)
            {
                textHeight =
                    topY -
                    bottomY;
            }


            RevisionTarget target =
                new RevisionTarget();


            target.SourceId =
                table.ObjectId;


            target.SourceType =
                "TableCell[" +
                row +
                "," +
                column +
                "]";


            target.Text =
                content;


            target.TargetSpaceId =
                table.OwnerId;


            target.LeftX =
                leftX;

            target.RightX =
                rightX;

            target.BottomY =
                bottomY;

            target.TopY =
                topY;


            // 删除线

            target.CenterY =
                (
                    bottomY +
                    topY
                ) / 2.0;


            target.TextHeight =
                textHeight;


            target.IsInViewport =
                false;


            target.ViewportId =
                ObjectId.Null;


            target.TextStyleId =
                textStyleId;


            return target;
        }


        private static double GetCellTextHeight(
    Cell cell,
    double cellHeight)
        {
            try
            {
                double? nullableHeight =
                    cell.TextHeight;

                if (nullableHeight.HasValue)
                {
                    double height =
                        nullableHeight.Value;

                    if (IsValidNumber(height) &&
                        height > 0)
                    {
                        return height;
                    }
                }
            }
            catch (System.Exception)
            {
            }


            // Cell自身没有明确设置字高时，
            // 才使用Fallback。

            double fallback =
                cellHeight * 0.60;


            if (!IsValidNumber(fallback) ||
                fallback <= 0)
            {
                fallback = 2.5;
            }


            return fallback;
        }


        private static ObjectId GetCellTextStyleId(
    Cell cell)
        {
            try
            {
                ObjectId? nullableId =
                    cell.TextStyleId;


                if (nullableId.HasValue)
                {
                    ObjectId id =
                        nullableId.Value;


                    if (!id.IsNull &&
                        id.IsValid)
                    {
                        return id;
                    }
                }
            }
            catch (System.Exception)
            {
            }


            return ObjectId.Null;
        }


        private static bool TryFindCell(
            Table table,
            Point3d point,
            out int row,
            out int column,
            out double left,
            out double right,
            out double bottom,
            out double top)
        {
            row =
                -1;

            column =
                -1;

            left =
                0;

            right =
                0;

            bottom =
                0;

            top =
                0;


            for (int r = 0;
                r < table.Rows.Count;
                r++)
            {
                for (int c = 0;
                    c < table.Columns.Count;
                    c++)
                {
                    double currentLeft;
                    double currentRight;
                    double currentBottom;
                    double currentTop;


                    bool got =
                        TryGetCellExtents(
                            table,
                            r,
                            c,
                            out currentLeft,
                            out currentRight,
                            out currentBottom,
                            out currentTop);


                    if (!got)
                        continue;


                    if (point.X < currentLeft ||
                        point.X > currentRight ||
                        point.Y < currentBottom ||
                        point.Y > currentTop)
                    {
                        continue;
                    }


                    row =
                        r;

                    column =
                        c;

                    left =
                        currentLeft;

                    right =
                        currentRight;

                    bottom =
                        currentBottom;

                    top =
                        currentTop;


                    return true;
                }
            }


            return false;
        }


        private static bool TryGetCellExtents(
            Table table,
            int row,
            int column,
            out double left,
            out double right,
            out double bottom,
            out double top)
        {
            left =
                double.MaxValue;

            right =
                double.MinValue;

            bottom =
                double.MaxValue;

            top =
                double.MinValue;


            try
            {
                Point3dCollection points =
                    table.Cells[
                        row,
                        column]
                    .GetExtents();


                if (points == null ||
                    points.Count == 0)
                {
                    return false;
                }


                foreach (Point3d point
                    in points)
                {
                    if (point.X < left)
                        left = point.X;

                    if (point.X > right)
                        right = point.X;

                    if (point.Y < bottom)
                        bottom = point.Y;

                    if (point.Y > top)
                        top = point.Y;
                }


                return IsValidRange(
                    left,
                    right,
                    bottom,
                    top);
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 专门给BOM测量“真实字符串宽度”。
        /// 关键：
        /// MText.Width = 0
        /// Width为0以后不使用Table 的
        /// 整个Cell文本框宽度，而是让文字按内容展开。
        /// </summary>
        private static bool TryMeasureTightTextWidth(
    Database database,
    string text,
    double textHeight,
    ObjectId textStyleId,
    out double width)
        {
            width =
                0;


            if (database == null ||
                string.IsNullOrWhiteSpace(text) ||
                !IsValidNumber(textHeight) ||
                textHeight <= 0)
            {
                return false;
            }


            try
            {
                using (MText temp =
                    new MText())
                {
                    temp.SetDatabaseDefaults(
                        database);


                    temp.Contents =
                        text;


                    temp.TextHeight =
                        textHeight;


                    if (!textStyleId.IsNull &&
                        textStyleId.IsValid)
                    {
                        try
                        {
                            temp.TextStyleId =
                                textStyleId;
                        }
                        catch (System.Exception)
                        {
                        }
                    }


                    // 关闭固定宽度，
                    // 让MText按照实际内容计算宽度。

                    temp.Width =
                        0.0;


                    temp.Attachment =
                        AttachmentPoint.MiddleCenter;


                    temp.Location =
                        Point3d.Origin;


                    // 优先实际文字宽度

                    width =
                        temp.ActualWidth;


                    // ActualWidth无效时Fallback

                    if (!IsValidNumber(width) ||
                        width <= 0)
                    {
                        Extents3d extents =
                            temp.GeometricExtents;


                        width =
                            extents.MaxPoint.X -
                            extents.MinPoint.X;
                    }


                    return
                        IsValidNumber(width) &&
                        width > 0;
                }
            }
            catch (System.Exception)
            {
                width =
                    0;

                return false;
            }
        }

        /// <summary>
        /// 根据原始MText的位置、Attachment，
        /// 使用重新测量出来的紧凑宽高，
        /// 构造真正用于QuickRevision的文字范围。
        /// </summary>
        private static bool TryBuildTightMTextExtents(
            MText source,
            double width,
            double height,
            out Extents3d extents)
        {
            extents =
                new Extents3d();


            if (source == null ||
                !IsValidNumber(width) ||
                !IsValidNumber(height) ||
                width <= 0 ||
                height <= 0)
            {
                return false;
            }


            try
            {
                Point3d location =
                    source.Location;


                double left;
                double right;
                double bottom;
                double top;


                switch (source.Attachment)
                {
                    // Top

                    case AttachmentPoint.TopLeft:

                        left =
                            location.X;

                        right =
                            location.X +
                            width;

                        top =
                            location.Y;

                        bottom =
                            location.Y -
                            height;

                        break;


                    case AttachmentPoint.TopCenter:

                        left =
                            location.X -
                            width / 2.0;

                        right =
                            location.X +
                            width / 2.0;

                        top =
                            location.Y;

                        bottom =
                            location.Y -
                            height;

                        break;


                    case AttachmentPoint.TopRight:

                        left =
                            location.X -
                            width;

                        right =
                            location.X;

                        top =
                            location.Y;

                        bottom =
                            location.Y -
                            height;

                        break;


                    // Middle

                    case AttachmentPoint.MiddleLeft:

                        left =
                            location.X;

                        right =
                            location.X +
                            width;

                        bottom =
                            location.Y -
                            height / 2.0;

                        top =
                            location.Y +
                            height / 2.0;

                        break;


                    case AttachmentPoint.MiddleCenter:

                        left =
                            location.X -
                            width / 2.0;

                        right =
                            location.X +
                            width / 2.0;

                        bottom =
                            location.Y -
                            height / 2.0;

                        top =
                            location.Y +
                            height / 2.0;

                        break;


                    case AttachmentPoint.MiddleRight:

                        left =
                            location.X -
                            width;

                        right =
                            location.X;

                        bottom =
                            location.Y -
                            height / 2.0;

                        top =
                            location.Y +
                            height / 2.0;

                        break;


                    // Bottom

                    case AttachmentPoint.BottomLeft:

                        left =
                            location.X;

                        right =
                            location.X +
                            width;

                        bottom =
                            location.Y;

                        top =
                            location.Y +
                            height;

                        break;


                    case AttachmentPoint.BottomCenter:

                        left =
                            location.X -
                            width / 2.0;

                        right =
                            location.X +
                            width / 2.0;

                        bottom =
                            location.Y;

                        top =
                            location.Y +
                            height;

                        break;


                    case AttachmentPoint.BottomRight:

                        left =
                            location.X -
                            width;

                        right =
                            location.X;

                        bottom =
                            location.Y;

                        top =
                            location.Y +
                            height;

                        break;


                    default:

                        return false;
                }


                if (!IsValidRange(
                        left,
                        right,
                        bottom,
                        top))
                {
                    return false;
                }


                extents =
                    new Extents3d(
                        new Point3d(
                            left,
                            bottom,
                            location.Z),

                        new Point3d(
                            right,
                            top,
                            location.Z));


                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
        private static bool TryMeasureTextWidth(
            Database database,
            string content,
            double textHeight,
            ObjectId textStyleId,
            out double width)
        {
            width =
                0;


            if (database == null ||
                string.IsNullOrWhiteSpace(
                    content) ||
                textHeight <= 0)
            {
                return false;
            }


            try
            {
                using (MText temp =
                    new MText())
                {
                    temp.SetDatabaseDefaults(
                        database);


                    temp.Contents =
                        content;


                    temp.TextHeight =
                        textHeight;


                    temp.Location =
                        Point3d.Origin;


                    temp.Attachment =
                        AttachmentPoint.MiddleCenter;


                    if (!textStyleId.IsNull &&
                        textStyleId.IsValid)
                    {
                        try
                        {
                            temp.TextStyleId =
                                textStyleId;
                        }
                        catch (System.Exception)
                        {
                        }
                    }


                    Extents3d extents =
                        temp.GeometricExtents;


                    width =
                        extents.MaxPoint.X -
                        extents.MinPoint.X;


                    return
                        IsValidNumber(width) &&
                        width > 0;
                }
            }
            catch (System.Exception)
            {
                return false;
            }
        }


        private static double EstimateTextWidth(
            string text,
            double textHeight)
        {
            if (string.IsNullOrEmpty(text))
                return textHeight;


            double width =
                text.Length *
                textHeight *
                0.60;


            if (width < textHeight)
                width = textHeight;


            return width;
        }


        private static string CleanText(
            string text)
        {
            if (text == null)
                return "";


            return text
                .Replace("\\P", " ")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
        }


        private static Point3d GetCenter(
            Extents3d extents)
        {
            return new Point3d(
                (
                    extents.MinPoint.X +
                    extents.MaxPoint.X
                ) / 2.0,

                (
                    extents.MinPoint.Y +
                    extents.MaxPoint.Y
                ) / 2.0,

                (
                    extents.MinPoint.Z +
                    extents.MaxPoint.Z
                ) / 2.0);
        }


        private static bool IsPointInsideCell(
            Point3d point,
            double left,
            double right,
            double bottom,
            double top)
        {
            double tolerance =
                1E-6;


            return
                point.X >= left - tolerance &&
                point.X <= right + tolerance &&
                point.Y >= bottom - tolerance &&
                point.Y <= top + tolerance;
        }


        private static double DistanceSquared(
            Point3d p1,
            Point3d p2)
        {
            double dx =
                p1.X -
                p2.X;

            double dy =
                p1.Y -
                p2.Y;


            return
                dx * dx +
                dy * dy;
        }


        private static bool IsValidExtents(
            Extents3d extents)
        {
            return IsValidRange(
                extents.MinPoint.X,
                extents.MaxPoint.X,
                extents.MinPoint.Y,
                extents.MaxPoint.Y);
        }


        private static bool IsValidRange(
            double left,
            double right,
            double bottom,
            double top)
        {
            return
                IsValidNumber(left) &&
                IsValidNumber(right) &&
                IsValidNumber(bottom) &&
                IsValidNumber(top) &&
                right > left &&
                top >= bottom;
        }


        private static bool IsValidNumber(
            double value)
        {
            return
                !double.IsNaN(value) &&
                !double.IsInfinity(value) &&
                System.Math.Abs(value) < 1E15;
        }
    }
}