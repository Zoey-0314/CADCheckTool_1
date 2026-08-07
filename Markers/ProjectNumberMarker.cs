using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Correct_test1.Models;
using Correct_test1.Readers;

namespace Correct_test1.Markers
{
    public class ProjectNumberMarker : MarkerBase
    {
        public void Create(
            Database database,
            Transaction transaction,
            ObjectId spaceId,
            ObjectId layerId,
            ProjectNumberLocation location,
            string expectedProject)
        {
            BlockTableRecord space = transaction.GetObject(
                spaceId,
                OpenMode.ForWrite) as BlockTableRecord;

            MText text = new MText();
            text.Location = location.Position + Vector3d.YAxis * -5.0;
            text.TextHeight = 3.5;
            text.Contents = "项目号不一致  应该为: " + expectedProject;
            text.LayerId = layerId;
            text.Color = Color.FromRgb(0, 255, 0);

            space.AppendEntity(text);
            transaction.AddNewlyCreatedDBObject(text, true);
            text.XData = new ResultBuffer(
                new TypedValue(
                    (int)DxfCode.ExtendedDataRegAppName,
                    MarkerManager.XDataAppName),
                new TypedValue(
                    (int)DxfCode.ExtendedDataAsciiString,
                    "ProjectNumber"));
        }
    }
}
