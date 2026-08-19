using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;
using Correct_test1.Core;

namespace Correct_test1.Markers
{
    /// <summary>
    /// Base class for markers to provide common functionality such as ensuring layers.
    /// </summary>
    public abstract class MarkerBase
    {
        protected ObjectId EnsureLayer(
            Database db,
            Transaction tr,
            string layerName,
            Color color)
        {
            LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

            if (lt.Has(layerName))
            {

                Correct_test1.Core.AppLogger.Info(
                    $"Layer已存在:{layerName}",
                    "MarkerBase"
                );


                return lt[layerName];

            }

            lt.UpgradeOpen();

            LayerTableRecord layer = new LayerTableRecord();
            layer.Name = layerName;
            layer.Color = color;

            ObjectId layerId = lt.Add(layer);
            Correct_test1.Core.AppLogger.Info(
    $"创建Layer:{layerName} Id:{layerId}",
    "MarkerBase"
);
            tr.AddNewlyCreatedDBObject(layer, true);

            AppLogger.Info($"EnsureLayer: created layer '{layerName}' ObjectId: {layerId}", "MarkerBase");

            return layerId;
        }
    }
}
