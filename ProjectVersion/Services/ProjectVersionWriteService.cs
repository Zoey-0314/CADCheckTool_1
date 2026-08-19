using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.Core;
using Correct_test1.Models;
using Correct_test1.ProjectVersion.Models;
using Correct_test1.ProjectVersion.Writers;
using Correct_test1.Readers;

using System.Collections.Generic;

namespace Correct_test1.ProjectVersion.Services
{
    public class ProjectVersionWriteService
    {
        public List<ProjectVersionLayoutResult>
            WriteAllLayouts(
                Database database,
                string value)
        {
            List<ProjectVersionLayoutResult>
                results =
                    new List<ProjectVersionLayoutResult>();

            if (database == null)
                return results;

            if (string.IsNullOrWhiteSpace(value))
                return results;

            LayoutReader layoutReader =
                new LayoutReader();

            TitleBlockReader titleReader =
                new TitleBlockReader();

            ProjectVersionWriter writer =
                new ProjectVersionWriter();

            List<LayoutInfo> layouts =
                layoutReader.ReadLayouts(database);

            layouts.Sort(
                (a, b) =>
                    a.TabOrder.CompareTo(b.TabOrder));

            foreach (LayoutInfo layout in layouts)
            {
                if (layout == null ||
                    layout.IsModelSpace)
                {
                    continue;
                }

                List<TitleText> texts =
                    titleReader.Read(
                        database,
                        new List<LayoutInfo>
                        {
                            layout
                        });

                if (texts == null ||
                    texts.Count == 0)
                {
                    results.Add(
                        new ProjectVersionLayoutResult
                        {
                            LayoutName = layout.LayoutName,
                            Success = false,
                            Skipped = true,
                            Message = "未读取到标题栏文字，已跳过。"
                        });

                    continue;
                }

                TitleBlockAnchorInfo anchorInfo;

                bool hasAnchor =
                    TitleBlockOrientationDetector
                        .TryResolveAnchor(
                            texts,
                            out anchorInfo);

                bool isHorizontal =
                    hasAnchor
                        ? anchorInfo.IsHorizontal
                        : TitleBlockOrientationDetector
                            .IsHorizontal(texts);

                double offsetX =
                    hasAnchor
                        ? anchorInfo.OffsetX
                        : 0.0;

                double offsetY =
                    hasAnchor
                        ? anchorInfo.OffsetY
                        : 0.0;

                ProjectVersionLayoutResult result =
                    writer.Write(
                        database,
                        layout,
                        value,
                        isHorizontal,
                        offsetX,
                        offsetY);

                results.Add(result);
            }

            return results;
        }
    }
}