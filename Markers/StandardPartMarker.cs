using Autodesk.AutoCAD.DatabaseServices;
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

            DBText text = new DBText();
            text.Position = info.Position;
            text.Height = 2.5;
            text.TextString = info.Text;
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
