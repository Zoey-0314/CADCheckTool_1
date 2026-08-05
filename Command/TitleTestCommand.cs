using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Correct_test1.Checks;
using Correct_test1.Markers;
using Correct_test1.Models;
using Correct_test1.Readers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WinForms = System.Windows.Forms;

namespace Correct_test1
{
    public class TitleTestCommand
    {
        [CommandMethod("TESTCHECK")]
        public void TestCheck()
        {

            Document doc =
                Application.DocumentManager
                .MdiActiveDocument;

            if (doc == null)
                return;

            Database db =
                doc.Database;

            Editor ed =
                doc.Editor;

            try
            {

                LayoutReader layoutReader =
                    new LayoutReader();

                List<LayoutInfo> layouts =
                    layoutReader.ReadLayouts(
                        db,
                        ed
                    );

                RevisionTableReader reader =
                    new RevisionTableReader();

                RevisionChecker checker =
                    new RevisionChecker();

                string path =
                    @"D:\Revision_Check_Error.csv";

                using (StreamWriter sw =
                    new StreamWriter(
                        path,
                        false,
                        Encoding.UTF8))
                {

                    sw.WriteLine(
                        "布局,标记,缺少字段,说明"
                    );

                    foreach (LayoutInfo layout in layouts)
                    {

                        if (layout.IsModelSpace)
                            continue;

                        bool isHorizontal;

                        //==========================
                        // 使用已有标记数量判断横竖
                        //==========================

                        List<TitleText> texts =
                            reader.ReadAllTexts(
                                db,
                                layout.BlockTableRecordId
                            );

                        int markCount = 0;

                        foreach (TitleText t in texts)
                        {

                            if (
                                t.Text.Contains("标记"))
                            {
                                markCount++;
                            }

                        }

                        isHorizontal =
                            markCount >= 2;

                        List<RevisionInfo> revisions =
                            new List<RevisionInfo>();

                        if (isHorizontal)
                        {

                            revisions =
                                reader.ReadHorizontal(
                                    db,
                                    layout.BlockTableRecordId
                                );

                        }
                        else
                        {

                            revisions =
                                reader.ReadVertical(
                                    db,
                                    layout.BlockTableRecordId
                                );

                        }

                        List<RevisionCheckIssue> issues =
                            checker.Check(
                                layout.LayoutName,
                                revisions
                            );

                        foreach (
                            RevisionCheckIssue issue
                            in issues)
                        {

                            sw.WriteLine(

                                Escape(issue.LayoutName)
                                +
                                ","
                                +
                                Escape(issue.Mark)
                                +
                                ","
                                +
                                Escape(issue.MissingField)
                                +
                                ","
                                +
                                Escape(issue.Message)

                            );


                            ed.WriteMessage(
                                "\n问题:"
                                +
                                issue.LayoutName
                                +
                                " 标记:"
                                +
                                issue.Mark
                                +
                                " 缺少:"
                                +
                                issue.MissingField
                            );

                        }

                    }

                }

                System.Windows.Forms.MessageBox.Show(
                    "检查完成\n"
                    +
                    path
                );

            }

            catch (Exception ex)
            {

                System.Windows.Forms.MessageBox.Show(
                    ex.Message
                );

            }

        }


        [CommandMethod("TESTLOCATIONCHECK")]
        public void TestLocationCheck()
        {

            Document doc =
                Application.DocumentManager
                .MdiActiveDocument;


            if (doc == null)
                return;


            Database db =
                doc.Database;


            Editor ed =
                doc.Editor;



            try
            {


                LayoutReader layoutReader =
                    new LayoutReader();



                List<LayoutInfo> layouts =
                    layoutReader.ReadLayouts(
                        db,
                        ed
                    );



                RevisionTableReader reader =
                    new RevisionTableReader();



                RevisionChecker checker =
                    new RevisionChecker();



                RevisionLocationReader locationReader =
                    new RevisionLocationReader();



                RevisionIssueMapper mapper =
                    new RevisionIssueMapper();




                string path =
                    @"D:\Revision_Location_Check.csv";




                using (StreamWriter sw =
                    new StreamWriter(
                        path,
                        false,
                        Encoding.UTF8))
                {


                    sw.WriteLine(
                        "布局,标记,缺失字段,X,Y,说明"
                    );




                    foreach (LayoutInfo layout in layouts)
                    {


                        if (layout.IsModelSpace)
                            continue;




                        //=========================
                        // 读取所有文字
                        //=========================

                        List<TitleText> texts =
                            reader.ReadAllTexts(
                                db,
                                layout.BlockTableRecordId
                            );




                        //=========================
                        // 判断横竖版
                        //=========================

                        int markCount = 0;


                        foreach (TitleText t in texts)
                        {

                            if (
                                t.Text.Contains("标记"))
                            {
                                markCount++;
                            }

                        }



                        bool isHorizontal =
                            markCount >= 2;




                        //=========================
                        // 读取修改记录
                        //=========================

                        List<RevisionInfo> revisions =
                            new List<RevisionInfo>();



                        if (isHorizontal)
                        {

                            revisions =
                                reader.ReadHorizontal(
                                    db,
                                    layout.BlockTableRecordId
                                );

                        }
                        else
                        {

                            revisions =
                                reader.ReadVertical(
                                    db,
                                    layout.BlockTableRecordId
                                );

                        }





                        //=========================
                        // 缺项检查
                        //=========================

                        List<RevisionCheckIssue> issues =
                            checker.Check(
                                layout.LayoutName,
                                revisions
                            );





                        //=========================
                        // 坐标读取
                        //=========================

                        List<RevisionLocation> locations =

                            new List<RevisionLocation>();



                        if (isHorizontal)
                        {

                            locations =
                                locationReader.ReadHorizontalLocations(
                                    layout.LayoutName,
                                    texts
                                );

                        }
                        else
                        {

                            locations =
                                locationReader.ReadVerticalLocations(
                                    layout.LayoutName,
                                    texts
                                );

                        }

                        //=========================
                        // 调试坐标读取结果
                        //=========================

                        foreach (RevisionLocation loc in locations)
                        {
                            ed.WriteMessage(
                                "\nLOCATION:"
                                +
                                "布局="
                                +
                                loc.LayoutName
                                +
                                " 标记="
                                +
                                loc.Mark
                                +
                                " Mark坐标="
                                +
                                loc.MarkX
                                +
                                ","
                                +
                                loc.MarkY
                                +
                                " Date="
                                +
                                loc.DateX
                                +
                                ","
                                +
                                loc.DateY
                                +
                                " Signer="
                                +
                                loc.SignerX
                                +
                                ","
                                +
                                loc.SignerY
                            );
                        }



                        //=========================
                        // 合并错误+坐标
                        //=========================

                        List<RevisionMarkPoint> points =
                            mapper.Map(
                                issues,
                                locations
                            );





                        foreach (
                            RevisionMarkPoint point
                            in points)
                        {


                            sw.WriteLine(

                                Escape(point.LayoutName)
                                +
                                ","
                                +
                                Escape(point.Mark)
                                +
                                ","
                                +
                                Escape(point.MissingField)
                                +
                                ","
                                +
                                point.X
                                +
                                ","
                                +
                                point.Y
                                +
                                ","
                                +
                                Escape(point.Message)

                            );



                            ed.WriteMessage(
                                "\n错误:"
                                +
                                point.LayoutName
                                +
                                " 标记:"
                                +
                                point.Mark
                                +
                                " 缺:"
                                +
                                point.MissingField
                                +
                                " 坐标:"
                                +
                                point.X
                                +
                                ","
                                +
                                point.Y
                            );


                        }



                    }



                }





                System.Windows.Forms.MessageBox.Show(
                    "定位检查完成\n"
                    +
                    path
                );



            }
            catch (Exception ex)
            {

                System.Windows.Forms.MessageBox.Show(
                    ex.Message,
                    "错误"
                );

            }


        }
        [CommandMethod("TESTMARKER")]
        public void TestMarker()
        {

            Document doc =
                Application.DocumentManager
                .MdiActiveDocument;


            if (doc == null)
                return;


            Database db =
                doc.Database;


            Editor ed =
                doc.Editor;



            try
            {

                LayoutReader layoutReader =
                    new LayoutReader();



                List<LayoutInfo> layouts =
                    layoutReader.ReadLayouts(
                        db,
                        ed
                    );



                RevisionTableReader reader =
                    new RevisionTableReader();



                RevisionChecker checker =
                    new RevisionChecker();



                RevisionLocationReader locationReader =
                    new RevisionLocationReader();



                RevisionIssueMapper mapper =
                    new RevisionIssueMapper();



                RevisionMarker marker =
                    new RevisionMarker();




                foreach (LayoutInfo layout in layouts)
                {


                    if (layout.IsModelSpace)
                        continue;



                    List<TitleText> texts =
                        reader.ReadAllTexts(
                            db,
                            layout.BlockTableRecordId
                        );



                    int markCount = 0;


                    foreach (TitleText t in texts)
                    {

                        if (t.Text.Contains("标记"))
                            markCount++;

                    }



                    bool isHorizontal =
                        markCount >= 2;



                    List<RevisionInfo> revisions;


                    if (isHorizontal)
                    {

                        revisions =
                            reader.ReadHorizontal(
                                db,
                                layout.BlockTableRecordId
                            );

                    }
                    else
                    {

                        revisions =
                            reader.ReadVertical(
                                db,
                                layout.BlockTableRecordId
                            );

                    }




                    List<RevisionCheckIssue> issues =
                        checker.Check(
                            layout.LayoutName,
                            revisions
                        );



                    List<RevisionLocation> locations;



                    if (isHorizontal)
                    {

                        locations =
                            locationReader.ReadHorizontalLocations(
                                layout.LayoutName,
                                texts
                            );

                    }
                    else
                    {

                        locations =
                            locationReader.ReadVerticalLocations(
                                layout.LayoutName,
                                texts
                            );

                    }



                    List<RevisionMarkPoint> points =
                        mapper.Map(
                            issues,
                            locations
                        );



                    // 在当前布局绘制

                    marker.DrawMarkers(
                        db,
                        layout.BlockTableRecordId,
                        points
                    );



                    ed.WriteMessage(
                        "\n布局:"
                        +
                        layout.LayoutName
                        +
                        " 标记数量:"
                        +
                        points.Count
                    );


                }




                System.Windows.Forms.MessageBox.Show(
                    "绿色检查框绘制完成"
                );


            }
            catch (System.Exception ex)
            {

                System.Windows.Forms.MessageBox.Show(
                    ex.Message,
                    "错误"
                );

            }


        }

        [CommandMethod("TESTREVISION")]
        public void TestRevision()
        {
            Document doc =
                Application.DocumentManager
                .MdiActiveDocument;

            if (doc == null)
                return;

            Database db =
                doc.Database;

            Editor ed =
                doc.Editor;

            try
            {
                LayoutReader layoutReader =
                    new LayoutReader();

                List<LayoutInfo> layouts =
                    layoutReader.ReadLayouts(
                        db,
                        ed
                    );

                RevisionTableReader reader =
                    new RevisionTableReader();

                string path =
                    @"D:\Revision_Test.csv";

                using (StreamWriter sw =
                    new StreamWriter(
                        path,
                        false,
                        Encoding.UTF8))
                {
                    // 恢复为统一表头：布局,类型,标记,更改内容,更改日期,签名,变更号
                    sw.WriteLine(
                        "布局,类型,标记,更改内容,更改日期,签名,变更号"
                    );

                    foreach (LayoutInfo layout in layouts)
                    {
                        if (layout.IsModelSpace)
                            continue;

                        ed.WriteMessage("\n====================");
                        ed.WriteMessage("\n读取布局:" + layout.LayoutName);

                        // 保留已有的“标记计数”判定横竖逻辑（不可修改）
                        bool isHorizontal = false;

                        List<TitleText> titleTexts = layout.TitleTexts;
                        if (titleTexts == null || titleTexts.Count == 0)
                        {
                            // 兼容：若 LayoutInfo.TitleTexts 未填，读取图块内所有文字
                            titleTexts = reader.ReadAllTexts(db, layout.BlockTableRecordId);
                        }

                        int markCount = 0;
                        foreach (var t in titleTexts)
                        {
                            if (
                                !string.IsNullOrEmpty(t.Text)
                                &&
                                t.Text.Trim() == "标记"
                            )
                            {
                                markCount++;
                            }
                        }

                        // 保持规则：标记出现1次 -> 竖版，出现2次 -> 横版
                        if (markCount >= 2)
                            isHorizontal = true;
                        else
                            isHorizontal = false;

                        // 统一收集为 List<RevisionInfo>
                        List<RevisionInfo> allRevisions = new List<RevisionInfo>();

                        if (isHorizontal)
                        {
                            // 使用 ReadHorizontal()（已存在，返回 List<RevisionInfo>）
                            List<RevisionInfo> horizontalRevs =
                                reader.ReadHorizontal(
                                    db,
                                    layout.BlockTableRecordId
                                );

                            ed.WriteMessage("\n横版读取数量:" + horizontalRevs.Count);

                            allRevisions.AddRange(horizontalRevs);
                        }
                        else
                        {
                            // 使用原有 ReadVertical()
                            List<RevisionInfo> verticalRevs =
                                reader.ReadVertical(
                                    db,
                                    layout.BlockTableRecordId
                                );

                            ed.WriteMessage("\n竖版读取数量:" + verticalRevs.Count);

                            allRevisions.AddRange(verticalRevs);
                        }

                        // 统一输出 allRevisions
                        foreach (RevisionInfo rev in allRevisions)
                        {
                            sw.WriteLine(
                                layout.LayoutName
                                + (isHorizontal ? ",横版," : ",竖版,")
                                + Escape(rev.Mark) + ","
                                + Escape(rev.Description) + ","
                                + Escape(rev.Date) + ","
                                + Escape(rev.Signer) + ","
                                + Escape(rev.RevisionNumber)
                            );
                        }
                    }
                }

                WinForms.MessageBox.Show(
                    "完成\nCSV文件:\n" + path,
                    "修改记录测试"
                );
            }
            catch (System.Exception ex)
            {
                WinForms.MessageBox.Show(
                    ex.Message,
                    "错误"
                );
            }
        }

        /// <summary>
        /// CSV转义
        /// </summary>
        private string Escape(
            string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(","))
            {
                return "\"" + value + "\"";
            }

            return value;
        }
    }
}