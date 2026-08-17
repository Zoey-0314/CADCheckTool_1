using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.Core;
using Correct_test1.Models;
using Correct_test1.ProjectVersion.Models;
using Correct_test1.ProjectVersion.Writers;
using Correct_test1.Readers;

using System.Collections.Generic;

namespace Correct_test1.ProjectVersion.Services
{
    /// <summary>
    /// 当前DWG全部Layout项目号+版本号写入。
    /// </summary>
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


            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return results;
            }


            LayoutReader layoutReader =
                new LayoutReader();


            TitleBlockReader titleReader =
                new TitleBlockReader();


            ProjectVersionWriter writer =
                new ProjectVersionWriter();


            List<LayoutInfo> layouts =
                layoutReader.ReadLayouts(
                    database);


            //--------------------------------
            // 按Layout标签顺序处理
            //--------------------------------

            layouts.Sort(
                (a, b) =>
                    a.TabOrder
                        .CompareTo(
                            b.TabOrder));


            foreach (
                LayoutInfo layout
                in layouts)
            {
                //--------------------------------
                // Model不处理
                //--------------------------------

                if (layout == null ||
                    layout.IsModelSpace)
                {
                    continue;
                }


                //--------------------------------
                // 读取当前Layout文字。
                //
                // 没有任何标题栏文字：
                // 认为可能是空白Layout，
                // 不乱写。
                //--------------------------------

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
                            LayoutName =
                                layout.LayoutName,

                            Success =
                                false,

                            Skipped =
                                true,

                            Message =
                                "未读取到标题栏文字，已跳过。"
                        });


                    continue;
                }


                //--------------------------------
                // 直接复用现有横竖版规则
                //--------------------------------

                bool isHorizontal =
                    TitleBlockOrientationDetector
                        .IsHorizontal(
                            texts);


                ProjectVersionLayoutResult result =
                    writer.Write(
                        database,
                        layout,
                        value,
                        isHorizontal);


                results.Add(
                    result);
            }


            return results;
        }
    }
}