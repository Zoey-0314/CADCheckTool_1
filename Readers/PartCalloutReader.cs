using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Correct_test1.Core;
using Correct_test1.Models;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Correct_test1.Readers
{
    public class PartCalloutReader
    {
        private const double HorizontalTolerance = 0.01;

        public List<PartCallout> Read(Database database, ISet<int> bomNumbers)
        {
            List<PartCallout> result = new List<PartCallout>();

            if (database == null || bomNumbers == null || bomNumbers.Count == 0)
                return result;

            Extents3d? frame = new DrawingFrameReader().Read(database);

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                BlockTableRecord space = transaction.GetObject(
                    database.CurrentSpaceId,
                    OpenMode.ForRead) as BlockTableRecord;

                if (space == null)
                    return result;

                List<TextCandidate> texts = new List<TextCandidate>();
                List<HorizontalSegment> segments = new List<HorizontalSegment>();

                foreach (ObjectId entityId in space)
                {
                    Entity entity = transaction.GetObject(entityId, OpenMode.ForRead) as Entity;
                    if (entity == null)
                        continue;

                    DBText dbText = entity as DBText;
                    if (dbText != null)
                    {
                        AddTextCandidate(dbText.TextString, dbText.Position, dbText, bomNumbers, frame, texts);
                        continue;
                    }

                    MText mText = entity as MText;
                    if (mText != null)
                    {
                        AddTextCandidate(mText.Contents, mText.Location, mText, bomNumbers, frame, texts);
                        continue;
                    }

                    Line line = entity as Line;
                    if (line != null)
                    {
                        AddSegment(line.StartPoint, line.EndPoint, segments);
                        continue;
                    }

                    Polyline polyline = entity as Polyline;
                    if (polyline != null)
                        AddPolylineSegments(polyline, segments);
                }

                string layoutName = GetLayoutName(transaction, space);
                foreach (TextCandidate text in texts)
                {
                    if (!HasLineBelow(text, segments))
                        continue;

                    if (HasLineToLeft(text, segments) && !HasLineAbove(text, segments))
                        continue;

                    result.Add(new PartCallout
                    {
                        Number = text.Number,
                        TextPosition = text.Position,
                        LayoutName = layoutName,
                        SpaceId = database.CurrentSpaceId
                    });
                }

                transaction.Commit();
            }

            return result;
        }

        private static void AddTextCandidate(
            string rawText,
            Point3d position,
            Entity entity,
            ISet<int> bomNumbers,
            Extents3d? frame,
            List<TextCandidate> texts)
        {
            int number;
            string text = CadTextCleaner.Clean(rawText);
            if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out number) ||
                !bomNumbers.Contains(number) ||
                (frame != null && !IsInside(position, frame.Value)))
            {
                return;
            }

            double minX = position.X;
            double maxX = position.X;
            double minY = position.Y;
            double maxY = position.Y;
            try
            {
                Extents3d extents = entity.GeometricExtents;
                minX = extents.MinPoint.X;
                maxX = extents.MaxPoint.X;
                minY = extents.MinPoint.Y;
                maxY = extents.MaxPoint.Y;
            }
            catch
            {
            }

            double height = Math.Max(maxY - minY, 1.0);
            texts.Add(new TextCandidate
            {
                Number = number,
                Position = position,
                MinX = minX,
                MaxX = maxX,
                MinY = minY,
                MaxY = maxY,
                Height = height
            });
        }

        private static void AddPolylineSegments(Polyline polyline, List<HorizontalSegment> segments)
        {
            for (int index = 0; index < polyline.NumberOfVertices - 1; index++)
                AddSegment(polyline.GetPoint3dAt(index), polyline.GetPoint3dAt(index + 1), segments);

            if (polyline.Closed && polyline.NumberOfVertices > 1)
                AddSegment(polyline.GetPoint3dAt(polyline.NumberOfVertices - 1), polyline.GetPoint3dAt(0), segments);
        }

        private static void AddSegment(Point3d start, Point3d end, List<HorizontalSegment> segments)
        {
            if (Math.Abs(start.Y - end.Y) > HorizontalTolerance)
                return;

            segments.Add(new HorizontalSegment
            {
                MinX = Math.Min(start.X, end.X),
                MaxX = Math.Max(start.X, end.X),
                Y = (start.Y + end.Y) / 2.0
            });
        }

        private static bool HasLineBelow(TextCandidate text, List<HorizontalSegment> segments)
        {
            double maximumDistance = Math.Max(text.Height * 4.0, 5.0);
            foreach (HorizontalSegment segment in segments)
            {
                if (segment.Y < text.MinY && text.MinY - segment.Y <= maximumDistance &&
                    Overlaps(segment.MinX, segment.MaxX, text.MinX - text.Height, text.MaxX + text.Height))
                    return true;
            }

            return false;
        }

        private static bool HasLineAbove(TextCandidate text, List<HorizontalSegment> segments)
        {
            double maximumDistance = Math.Max(text.Height * 4.0, 5.0);
            foreach (HorizontalSegment segment in segments)
            {
                if (segment.Y > text.MaxY && segment.Y - text.MaxY <= maximumDistance &&
                    Overlaps(segment.MinX, segment.MaxX, text.MinX - text.Height, text.MaxX + text.Height))
                    return true;
            }

            return false;
        }

        private static bool HasLineToLeft(TextCandidate text, List<HorizontalSegment> segments)
        {
            double maximumDistance = Math.Max(text.Height * 2.0, 3.0);
            double centerY = (text.MinY + text.MaxY) / 2.0;
            foreach (HorizontalSegment segment in segments)
            {
                if (segment.MaxX <= text.MinX && text.MinX - segment.MaxX <= maximumDistance &&
                    Math.Abs(segment.Y - centerY) <= text.Height)
                    return true;
            }

            return false;
        }

        private static bool Overlaps(double firstMin, double firstMax, double secondMin, double secondMax)
        {
            return firstMax >= secondMin && secondMax >= firstMin;
        }

        private static bool IsInside(Point3d position, Extents3d frame)
        {
            return position.X >= frame.MinPoint.X && position.X <= frame.MaxPoint.X &&
                   position.Y >= frame.MinPoint.Y && position.Y <= frame.MaxPoint.Y;
        }

        private static string GetLayoutName(Transaction transaction, BlockTableRecord space)
        {
            if (space.LayoutId.IsNull)
                return "";

            Layout layout = transaction.GetObject(space.LayoutId, OpenMode.ForRead) as Layout;
            return layout == null ? "" : layout.LayoutName;
        }

        private class TextCandidate
        {
            public int Number { get; set; }
            public Point3d Position { get; set; }
            public double MinX { get; set; }
            public double MaxX { get; set; }
            public double MinY { get; set; }
            public double MaxY { get; set; }
            public double Height { get; set; }
        }

        private class HorizontalSegment
        {
            public double MinX { get; set; }
            public double MaxX { get; set; }
            public double Y { get; set; }
        }
    }
}
