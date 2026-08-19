using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Correct_test1.Models;
using Correct_test1.Checks;
using Correct_test1.Readers;
using Correct_test1.Markers;
using System;

namespace Correct_test1.Core
{
    public class DrawingCheckManager
    {
        /// <summary>
        /// 检查一张图纸
        /// db: CAD数据库
        /// filePath: 图纸完整路径
        /// drawMarker: 是否在CAD中绘制绿色检查框
        /// 返回： 所有检查结果
        /// </summary>
        public List<CheckResult> CheckDrawing(
            Database db,
            string filePath,
            bool drawMarker,
            List<BomData> boms = null,
            bool allowAutoFix = true)
        {
            List<CheckResult> results = new List<CheckResult>();

            if (db == null)
                return results;

            string fileName = System.IO.Path.GetFileName(filePath);

            // 1. 项目号检查
            //
            // 不再使用 projects[0]。
            //
            // 每一个项目号都绑定它真正所属的Layout。

            ProjectReader projectReader =
                new ProjectReader();


            List<ProjectNumberLocation> projectLocations =
                projectReader.ReadProjectLocations(
                    db);


            if (projectLocations == null ||
                projectLocations.Count == 0)
            {
                results.Add(
                    new CheckResult
                    {
                        FilePath =
                            filePath,

                        FileName =
                            fileName,

                        LayoutName =
                            "",

                        Mark =
                            "",

                        Type =
                            "项目号检查",

                        ObjectName =
                            "项目号",

                        CurrentValue =
                            "",

                        ExpectedValue =
                            "",

                        Message =
                            "未找到项目号",

                        IsError =
                            true
                    });
            }
            else
            {
                FileNameProjectReader fileProjectReader =
                    new FileNameProjectReader();


                FileNameProjectReader.ProjectInfo expectedProject =
                    fileProjectReader.ReadProjectNumber(
                        filePath);


                // 文件名本身没有可识别项目号时，
                // 保持批量检查行为：
                //
                // 不进行项目号一致性比较。

                if (expectedProject != null &&
                    !string.IsNullOrWhiteSpace(
                        expectedProject.ProjectNumber))
                {
                    // 防止：
                    //
                    // 同一个Layout里同一个项目号出现多次，
                    // CSV重复报完全一样的错误。
                    //
                    // Key：
                    //
                    // Layout1|P2026AB001

                    HashSet<string> reportedIssues =
                        new HashSet<string>(
                            StringComparer.OrdinalIgnoreCase);


                    // Marker也不能同一个错误项目号重复创建。
                    //
                    // CreateProjectMarkers本身会重新扫描所有位置，
                    // 所以一个错误项目号调用一次即可。

                    HashSet<string> markedProjects =
                        new HashSet<string>(
                            StringComparer.OrdinalIgnoreCase);


                    foreach (
                        ProjectNumberLocation location
                        in projectLocations)
                    {
                        if (location == null ||
                            string.IsNullOrWhiteSpace(
                                location.ProjectNumber))
                        {
                            continue;
                        }


                        // 当前项目号正确

                        if (string.Equals(
                                location.ProjectNumber,
                                expectedProject.ProjectNumber,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }


                        string layoutName =
                            location.LayoutName
                            ?? "";


                        string issueKey =
                            layoutName
                            + "|"
                            + location.ProjectNumber;


                        // 同Layout同项目号只报告一次

                        if (!reportedIssues.Add(
                                issueKey))
                        {
                            continue;
                        }


                        results.Add(
                            new CheckResult
                            {
                                FilePath =
                                    filePath,

                                FileName =
                                    fileName,

                                LayoutName =
                                    layoutName,

                                Type =
                                    "项目号检查",

                                ObjectName =
                                    "项目号",

                                CurrentValue =
                                    location.ProjectNumber,

                                ExpectedValue =
                                    expectedProject.ProjectNumber,

                                Message =
                                    string.IsNullOrWhiteSpace(
                                        layoutName)

                                        ? "当前项目号与要求项目号不一致"

                                        : "布局 "
                                          + layoutName
                                          + " 的项目号与要求项目号不一致",

                                IsError =
                                    true
                            });


                        // Marker
                        //
                        // 同一个错误项目号只调用一次。
                        //
                        // MarkerManager内部已经会根据
                        // ProjectNumberLocation.LayoutName
                        // 把Marker放到真实Layout。

                        if (drawMarker &&
                            markedProjects.Add(
                                location.ProjectNumber))
                        {
                            new MarkerManager()
                                .CreateProjectMarkers(
                                    db,
                                    location.ProjectNumber,
                                    expectedProject.ProjectNumber);
                        }
                    }
                }
            }
            // 2. 修改记录检查
            LayoutReader layoutReader = new LayoutReader();

            List<LayoutInfo> layouts = layoutReader.ReadLayouts(db);

            RevisionTableReader revisionReader = new RevisionTableReader();
            RevisionChecker revisionChecker = new RevisionChecker();
            RevisionLocationReader locationReader = new RevisionLocationReader();
            RevisionIssueMapper mapper = new RevisionIssueMapper();
            RevisionMarker marker = new RevisionMarker();
            TitleBlockCheckManager titleManager =
                new TitleBlockCheckManager();

            List<LayoutInfo> paperLayouts =
                layouts
                    .FindAll(x => !x.IsModelSpace);

            paperLayouts.Sort(
                (a, b) =>
                    a.TabOrder.CompareTo(b.TabOrder));

            int currentPage = 0;
            int totalPages = paperLayouts.Count;

            foreach (LayoutInfo layout in paperLayouts)
            {
                currentPage++;
                // 标题栏检查

                List<CheckResult> titleResults =
                    titleManager.Check(
                        db,
                        layout,
                        filePath,
                        fileName,
                        drawMarker,
                        currentPage,
                        totalPages,
                        boms,
                        allowAutoFix
                    );


                results.AddRange(
                    titleResults
                );
                // 读取文字
                List<TitleText> texts = revisionReader.ReadAllTexts(db, layout.BlockTableRecordId);
                if (texts.Count == 0)
                    continue;

                // 先尝试横版
                List<RevisionInfo> revisions = revisionReader.ReadHorizontal(texts);
                List<RevisionLocation> locations = locationReader.ReadHorizontalLocations(layout.LayoutName, texts);

                // 横版无结果，尝试竖版
                if (revisions.Count == 0)
                {
                    revisions = revisionReader.ReadVertical(texts);
                    locations = locationReader.ReadVerticalLocations(layout.LayoutName, texts);
                }

                if (revisions.Count == 0)
                    continue;

                // 检查缺项
                List<RevisionCheckIssue> issues = revisionChecker.Check(layout.LayoutName, revisions);
                if (issues.Count == 0)
                    continue;

                // 坐标映射
                List<RevisionMarkPoint> points = mapper.Map(issues, locations);

                // 绘制绿色框
                if (drawMarker)
                {
                    marker.DrawMarkers(db, layout.BlockTableRecordId, points);
                }

                // 转换统一结果
                foreach (RevisionCheckIssue issue in issues)
                {
                    results.Add(new CheckResult
                    {
                        FilePath = filePath,
                        FileName = fileName,
                        // 布局名称
                        LayoutName = issue.LayoutName,
                        // 修改记录标记
                        Mark = issue.Mark,
                        Type = "修改记录检查",
                        ObjectName = "标记" + issue.Mark,
                        CurrentValue = "",
                        ExpectedValue = issue.MissingField,
                        Message = issue.Message,
                        IsError = true
                    });
                }
            }

            return results;
        }
    }
}