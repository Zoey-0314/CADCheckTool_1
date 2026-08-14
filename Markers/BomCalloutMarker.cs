using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Correct_test1.Configs;
using Correct_test1.Core;
using Correct_test1.Models;
using System;

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
            CreateInternal(
                database,
                transaction,
                spaceId,
                layerId,
                issue,
                MarkerConfig.BomMarkerTextHeight);
        }

        public void CreateExtraMarker(
            Database database,
            Transaction transaction,
            ObjectId spaceId,
            ObjectId layerId,
            BomCalloutIssue issue)
        {
            CreateInternal(
                database,
                transaction,
                spaceId,
                layerId,
                issue,
                20.0);
        }

        private void CreateInternal(
            Database database,
            Transaction transaction,
            ObjectId spaceId,
            ObjectId layerId,
            BomCalloutIssue issue,
            double textHeight)
        {
            if (database == null ||
                transaction == null ||
                issue == null ||
                spaceId.IsNull ||
                !spaceId.IsValid ||
                layerId.IsNull ||
                !layerId.IsValid)
            {
                return;
            }

            //--------------------------------
            // 检查坐标
            //--------------------------------

            if (!IsValidPoint(issue.Position))
            {
                AppLogger.Info(
                    "跳过BomCalloutMarker：坐标无效，序号=" +
                    issue.Number,
                    "BomCalloutMarker");

                return;
            }

            //--------------------------------
            // 获取目标空间
            //--------------------------------

            BlockTableRecord space =
                transaction.GetObject(
                    spaceId,
                    OpenMode.ForWrite)
                as BlockTableRecord;

            if (space == null)
                return;

            //--------------------------------
            // Marker位置
            //--------------------------------

            Point3d markerPosition =
                issue.Position +
                Vector3d.XAxis * 5.0;

            if (!IsValidPoint(markerPosition))
                return;

            //--------------------------------
            // 检查文字高度
            //--------------------------------

            if (double.IsNaN(textHeight) ||
                double.IsInfinity(textHeight) ||
                textHeight <= 0)
            {
                return;
            }

            //--------------------------------
            // 创建MText
            //--------------------------------

            using (MText text = new MText())
            {
                text.SetDatabaseDefaults(database);

                text.Location =
                    markerPosition;

                text.TextHeight =
                    textHeight;

                text.Contents =
                    issue.Message ?? "";

                text.LayerId =
                    layerId;

                space.AppendEntity(
                    text);

                transaction.AddNewlyCreatedDBObject(
                    text,
                    true);

                //--------------------------------
                // 添加XData
                //--------------------------------

                using (ResultBuffer xdata =
                    new ResultBuffer(
                        new TypedValue(
                            (int)DxfCode.ExtendedDataRegAppName,
                            MarkerManager.XDataAppName),

                        new TypedValue(
                            (int)DxfCode.ExtendedDataAsciiString,
                            "BomCallout")))
                {
                    text.XData =
                        xdata;
                }
            }
        }

        //--------------------------------
        // 坐标检查
        //--------------------------------

        private static bool IsValidPoint(
            Point3d point)
        {
            return
                IsValidNumber(point.X) &&
                IsValidNumber(point.Y) &&
                IsValidNumber(point.Z);
        }

        private static bool IsValidNumber(
            double value)
        {
            return
                !double.IsNaN(value) &&
                !double.IsInfinity(value) &&
                Math.Abs(value) < 1E12;
        }
    }
}