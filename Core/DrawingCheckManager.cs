using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Correct_test1.Models;
using Correct_test1.Checks;
using Correct_test1.Readers;
using Correct_test1.Markers;

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

            //--------------------------------------
            // 1. 项目号检查
            //--------------------------------------
            ProjectReader projectReader = new ProjectReader();
            List<string> projects = projectReader.ReadProjects(db);

            if (projects.Count == 0)
            {
                results.Add(new CheckResult
                {
                    FilePath = filePath,
                    FileName = fileName,
                    LayoutName = "",
                    Mark = "",
                    Type = "项目号检查",
                    ObjectName = "项目号",
                    CurrentValue = "",
                    ExpectedValue = "",
                    Message = "未找到项目号",
                    IsError = true
                });
            }
            else
            {
                FileNameProjectReader fileProjectReader =
                    new FileNameProjectReader();
                FileNameProjectReader.ProjectInfo expectedProject =
                    fileProjectReader.ReadProjectNumber(filePath);

                if (expectedProject != null &&
                    !string.Equals(
                        projects[0],
                        expectedProject.ProjectNumber,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new CheckResult
                    {
                        FilePath = filePath,
                        FileName = fileName,
                        Type = "项目号检查",
                        ObjectName = "项目号",
                        CurrentValue = projects[0],
                        ExpectedValue = expectedProject.ProjectNumber,
                        Message = "当前项目号与要求项目号不一致",
                        IsError = true
                    });

                    if (drawMarker)
                    {
                        new MarkerManager().CreateProjectMarkers(
                            db,
                            projects[0],
                            expectedProject.ProjectNumber);
                    }
                }
            }

            //--------------------------------------
            // 2. 修改记录检查
            //--------------------------------------
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
                //--------------------------------------
                // 标题栏检查
                //--------------------------------------

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
                        // 新增：布局名称
                        LayoutName = issue.LayoutName,
                        // 新增：修改记录标记
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