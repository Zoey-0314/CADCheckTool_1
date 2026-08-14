using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Correct_test1.Core;
using Correct_test1.Models;
using System;

namespace Correct_test1.Markers
{
    public class StandardPartMarker : MarkerBase
    {
        public void Create(
    Database database,
    Transaction transaction,
    ObjectId spaceId,
    ObjectId layerId,
    MarkerInfo info,
    string markerType = "StandardPart")
        {
            if (database == null ||
                transaction == null ||
                info == null ||
                spaceId.IsNull ||
                !spaceId.IsValid ||
                layerId.IsNull ||
                !layerId.IsValid)
            {
                return;
            }

            if (!IsValidPoint(info.Position))
            {
                AppLogger.Info(
                    "跳过StandardPartMarker：坐标无效",
                    "StandardPartMarker");

                return;
            }

            BlockTableRecord space =
                transaction.GetObject(
                    spaceId,
                    OpenMode.ForWrite) as BlockTableRecord;

            if (space == null)
                return;

            Point3d markerPosition =
                info.Position + Vector3d.XAxis * 5.0;

            if (!IsValidPoint(markerPosition))
                return;

            string markerText =
                info.Text ?? "";

            using (MText text = new MText())
            {
                text.SetDatabaseDefaults(database);

                text.Location = markerPosition;
                text.TextHeight = 3.0;
                text.Contents = markerText;
                text.LayerId = layerId;

                space.AppendEntity(text);

                transaction.AddNewlyCreatedDBObject(
                    text,
                    true);

                using (ResultBuffer xdata =
                    new ResultBuffer(
                        new TypedValue(
                            (int)DxfCode.ExtendedDataRegAppName,
                            MarkerManager.XDataAppName),
                        new TypedValue(
    (int)DxfCode.ExtendedDataAsciiString,
    string.IsNullOrWhiteSpace(markerType)
        ? "StandardPart"
        : markerType)))
                {
                    text.XData = xdata;
                }
            }
        }

        private static bool IsValidPoint(Point3d point)
        {
            return IsValidNumber(point.X) &&
                   IsValidNumber(point.Y) &&
                   IsValidNumber(point.Z);
        }

        private static bool IsValidNumber(double value)
        {
            return !double.IsNaN(value) &&
                   !double.IsInfinity(value) &&
                   Math.Abs(value) < 1E12;
        }
    }
}