using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Correct_test1.Models;

namespace Correct_test1.Markers
{
    public class StandardPartMarker : MarkerBase
    {
        public void Create(
            Database database,
            Transaction transaction,
            ObjectId spaceId,
            ObjectId layerId,
            MarkerInfo info)
        {
            BlockTableRecord space =
                transaction.GetObject(
                    spaceId,
                    OpenMode.ForWrite) as BlockTableRecord;

            MText text = new MText();
            text.Location = info.Position + Vector3d.XAxis * 5.0;
            text.TextHeight = 3.0;
            text.Contents = info.Text;
            text.LayerId = layerId;

            space.AppendEntity(text);
            transaction.AddNewlyCreatedDBObject(text, true);

            text.XData = new ResultBuffer(
                new TypedValue(
                    (int)DxfCode.ExtendedDataRegAppName,
                    MarkerManager.XDataAppName),
                new TypedValue(
                    (int)DxfCode.ExtendedDataAsciiString,
                    info.Text));
        }
    }
}
