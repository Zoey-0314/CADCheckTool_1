using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Correct_test1.Configs;
using Correct_test1.Models;

namespace Correct_test1.Markers
{
    public class BomCalloutMarker : MarkerBase
    {
        public void Create(
            Database database,
            Transaction transaction,
            ObjectId spaceId,
            ObjectId layerId,
            BomCalloutIssue issue)
        {

            BlockTableRecord space = transaction.GetObject(
                spaceId,
                OpenMode.ForWrite) as BlockTableRecord;

            if (space == null)
                return;

            MText text = new MText();
            text.Location = issue.Position + Vector3d.XAxis * 5.0;
            text.TextHeight = MarkerConfig.BomMarkerTextHeight;
            text.Contents = issue.Message;
            text.LayerId = layerId;

            space.AppendEntity(text);
            transaction.AddNewlyCreatedDBObject(text, true);
            text.XData = new ResultBuffer(
                new TypedValue(
                    (int)DxfCode.ExtendedDataRegAppName,
                    MarkerManager.XDataAppName),
                new TypedValue(
                    (int)DxfCode.ExtendedDataAsciiString,
                    "BomCallout"));
        }
        public void CreateExtraMarker(
    Database database,
    Transaction transaction,
    ObjectId spaceId,
    ObjectId layerId,
    BomCalloutIssue issue)
        {
            BlockTableRecord space = transaction.GetObject(
                spaceId,
                OpenMode.ForWrite) as BlockTableRecord;

            if (space == null)
                return;

            MText text = new MText();

            text.Location =
                issue.Position + Vector3d.XAxis * 5.0;

            text.TextHeight = 20;

            text.Contents = issue.Message;

            text.LayerId = layerId;

            space.AppendEntity(text);

            transaction.AddNewlyCreatedDBObject(
                text,
                true);

            text.XData = new ResultBuffer(
                new TypedValue(
                    (int)DxfCode.ExtendedDataRegAppName,
                    MarkerManager.XDataAppName),
                new TypedValue(
                    (int)DxfCode.ExtendedDataAsciiString,
                    "BomCallout"));
        }
    }
}
